∩╗┐// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
/*
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.SystematicTesting;
using SystemMonitor = System.Threading.Monitor;

namespace Microsoft.Coyote.Tasks
{
    public class ReaderWriterLock
    {
        /// <summary>
        /// The object used for synchronization.
        /// </summary>
        protected readonly object RWLObject;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReaderWriterLock"/> class.
        /// </summary>
        protected ReaderWriterLock(object rWLObject)
        {
            this.RWLObject = rWLObject;
        }

        public static void AcquireReaderLock(object syncObject, int timeout)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                Mock.Create(syncObject).AcquireReaderLock(timeout);
            }
            else
            {
                syncObject.AcquireReaderLock(timeout);
            }
        }

        public static void AcquireWriterLock(object syncObject, int timeout)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                Mock.Create(syncObject).AcquireWriterLock(timeout);
            }
            else
            {
                syncObject.AcquireWriterLock(timeout);
            }
        }

        /// <summary>
        /// Mock implementation of <see cref="SynchronizedBlock"/> that can be controlled during systematic testing.
        /// </summary>
        internal class Mock : ReaderWriterLock
        {
            /// <summary>
            /// Cache from synchronized objects to mock instances.
            /// </summary>
            private static readonly ConcurrentDictionary<object, Lazy<Mock>> Cache =
                new ConcurrentDictionary<object, Lazy<Mock>>();

            /// <summary>
            /// The resource associated with this synchronization object.
            /// </summary>
            private readonly Resource Resource;

            /// <summary>
            /// The current owner of this synchronization object.
            /// </summary>
            private AsyncOperation Owner;

            /// <summary>
            /// Wait queue of asynchronous operations.
            /// </summary>
            private readonly List<AsyncOperation> WaitQueue;

            /// <summary>
            /// Ready queue of asynchronous operations.
            /// </summary>
            private readonly List<AsyncOperation> ReadyQueue;

            /// <summary>
            /// Queue of nondeterministically buffered pulse operations to be performed after releasing
            /// the lock. This allows modeling delayed pulse operations by the operation system.
            /// </summary>
            private readonly Queue<PulseOperation> PulseQueue;

            /// <summary>
            /// The number of times that the lock has been acquired per owner. The lock can only
            /// be acquired more than one times by the same owner. A count > 1 indicates that the
            /// invocation by the current owner is reentrant.
            /// </summary>
            private readonly Dictionary<AsyncOperation, int> LockCountMap;

            /// <summary>
            /// Used to reference count accesses to this synchronized block
            /// so that it can be removed from the cache.
            /// </summary>
            private int UseCount;

            /// <summary>
            /// Initializes a new instance of the <see cref="Mock"/> class.
            /// </summary>
            private Mock(object syncObject)
                : base(syncObject)
            {
                if (syncObject is null)
                {
                    throw new ArgumentNullException(nameof(syncObject));
                }

                this.Resource = new Resource();
                this.WaitQueue = new List<AsyncOperation>();
                this.ReadyQueue = new List<AsyncOperation>();
                this.PulseQueue = new Queue<PulseOperation>();
                this.LockCountMap = new Dictionary<AsyncOperation, int>();
                this.UseCount = 0;
            }

            /// <summary>
            /// Creates a new mock for the specified synchronization object.
            /// </summary>
            internal static Mock Create(object syncObject) =>
                Cache.GetOrAdd(syncObject, key => new Lazy<Mock>(() => new Mock(key))).Value;

            /// <summary>
            /// Finds the mock associated with the specified synchronization object.
            /// </summary>
            internal static Mock Find(object syncObject) =>
                Cache.TryGetValue(syncObject, out Lazy<Mock> lazyMock) ? lazyMock.Value : null;

            /// <summary>
            /// Determines whether the current thread holds the lock on the sync object.
            /// </summary>
            internal bool IsEntered()
            {
                if (this.Owner != null)
                {
                    var op = this.Resource.Runtime.GetExecutingOperation<AsyncOperation>();
                    return this.Owner == op;
                }

                return false;
            }

            /// <summary>
            /// For use by ControlledMonitor only.
            /// </summary>
            internal void Lock() => this.EnterLock();

            protected override SynchronizedBlock EnterLock()
            {
                this.IsLockTaken = true;
                Interlocked.Increment(ref this.UseCount);

                if (this.Owner is null)
                {
                    // If this operation is trying to acquire this lock while it is free, then inject a scheduling
                    // point to give another enabled operation the chance to race and acquire this lock.
                    this.Resource.Runtime.ScheduleNextOperation();
                }

                if (this.Owner != null)
                {
                    var op = this.Resource.Runtime.GetExecutingOperation<AsyncOperation>();
                    if (this.Owner == op)
                    {
                        // The owner is re-entering the lock.
                        this.LockCountMap[op]++;
                        return this;
                    }
                    else
                    {
                        // Another op has the lock right now, so add the executing op
                        // to the ready queue and block it.
                        this.WaitQueue.Remove(op);
                        if (!this.ReadyQueue.Contains(op))
                        {
                            this.ReadyQueue.Add(op);
                        }

                        this.Resource.Wait();
                        this.LockCountMap.Add(op, 1);
                        return this;
                    }
                }

                // The executing op acquired the lock and can proceed.
                this.Owner = this.Resource.Runtime.GetExecutingOperation<AsyncOperation>();
                this.LockCountMap.Add(this.Owner, 1);
                return this;
            }

            /// <inheritdoc/>
            public override void Pulse() => this.SchedulePulse(PulseOperation.Next);

            /// <inheritdoc/>
            public override void PulseAll() => this.SchedulePulse(PulseOperation.All);

            /// <summary>
            /// Schedules a pulse operation that will either execute immediately or be scheduled
            /// to execute after the current owner releases the lock. This nondeterministic action
            /// is controlled by the runtime to simulate scenarios where the pulse is delayed by
            /// the operation system.
            /// </summary>
            private void SchedulePulse(PulseOperation pulseOperation)
            {
                var op = this.Resource.Runtime.GetExecutingOperation<AsyncOperation>();
                if (this.Owner != op)
                {
                    throw new SynchronizationLockException();
                }

                // Pulse has a delay in the operating system, we can simulate that here
                // by scheduling the pulse operation to be executed nondeterministically.
                this.PulseQueue.Enqueue(pulseOperation);
                if (this.PulseQueue.Count is 1)
                {
                    // Create a task for draining the queue. To optimize the testing performance,
                    // we create and maintain a single task to perform this role.
                    Task.Run(this.DrainPulseQueue);
                }
            }

            /// <summary>
            /// Drains the pulse queue, if it contains one or more buffered pulse operations.
            /// </summary>
            private void DrainPulseQueue()
            {
                while (this.PulseQueue.Count > 0)
                {
                    // Pulses can happen nondeterministically while other operations execute,
                    // which models delays by the OS.
                    this.Resource.Runtime.ScheduleNextOperation();

                    var pulseOperation = this.PulseQueue.Dequeue();
                    this.Pulse(pulseOperation);

                    if (this.Owner is null)
                    {
                        this.UnlockNextReady();
                    }
                }
            }

            /// <summary>
            /// Invokes the pulse operation.
            /// </summary>
            private void Pulse(PulseOperation pulseOperation)
            {
                if (pulseOperation is PulseOperation.Next)
                {
                    if (this.WaitQueue.Count > 0)
                    {
                        // System.Threading.Monitor has FIFO semantics.
                        var waitingOp = this.WaitQueue[0];
                        this.WaitQueue.RemoveAt(0);
                        this.ReadyQueue.Add(waitingOp);
                        IO.Debug.WriteLine("<SynchronizedBlockDebug> Operation '{0}' is pulsed by task '{1}'.", waitingOp.Id, Task.CurrentId);
                    }
                }
                else
                {
                    foreach (var waitingOp in this.WaitQueue)
                    {
                        this.ReadyQueue.Add(waitingOp);
                        IO.Debug.WriteLine("<SynchronizedBlockDebug> Operation '{0}' is pulsed by task '{1}'.", waitingOp.Id, Task.CurrentId);
                    }

                    this.WaitQueue.Clear();
                }
            }

            /// <inheritdoc/>
            public override bool Wait()
            {
                var op = this.Resource.Runtime.GetExecutingOperation<AsyncOperation>();
                if (this.Owner != op)
                {
                    throw new SynchronizationLockException();
                }

                this.ReadyQueue.Remove(op);
                if (!this.WaitQueue.Contains(op))
                {
                    this.WaitQueue.Add(op);
                }

                this.UnlockNextReady();
                IO.Debug.WriteLine("<SynchronizedBlockDebug> Operation '{0}' with task id '{1}' is waiting.", op.Id, Task.CurrentId);

                // Block this operation and schedule the next enabled operation.
                this.Resource.Wait();
                return true;
            }

            /// <inheritdoc/>
            public override bool Wait(int millisecondsTimeout)
            {
                // TODO: how to implement mock timeout?
                // This is a bit more tricky to model, one way is to have a loop that checks
                // for controlled random boolean choice, and if it becomes true then it fails
                // the wait. This would be similar to timers in actors, so we want to use a
                // lower probability to not fail very frequently during systematic testing.
                // In the future we might want to introduce a RandomTimeout choice (similar to
                // RandomBoolean and RandomInteger), with the benefit being that the underlying
                // testing strategy will know that this is a timeout and perhaps treat it in a
                // more intelligent manner, but for now piggybacking on the other randoms should
                // work (as long as its not with a high probability).
                return this.Wait();
            }

            /// <inheritdoc/>
            public override bool Wait(TimeSpan timeout)
            {
                // TODO: how to implement mock timeout?
                return this.Wait();
            }

            /// <summary>
            /// Assigns the lock to the next operation waiting in the ready queue, if there is one,
            /// following the FIFO semantics of <see cref="SystemMonitor"/>.
            /// </summary>
            private void UnlockNextReady()
            {
                // Preparing to unlock so give up ownership.
                this.Owner = null;
                if (this.ReadyQueue.Count > 0)
                {
                    // If there is a operation waiting in the ready queue, then signal it.
                    AsyncOperation op = this.ReadyQueue[0];
                    this.ReadyQueue.RemoveAt(0);
                    this.Owner = op;
                    this.Resource.Signal(op);
                }
            }

            internal void Exit()
            {
                var op = this.Resource.Runtime.GetExecutingOperation<AsyncOperation>();
                this.Resource.Runtime.Assert(this.LockCountMap.ContainsKey(op), "Cannot invoke Dispose without acquiring the lock.");

                this.LockCountMap[op]--;
                if (this.LockCountMap[op] is 0)
                {
                    // Only release the lock if the invocation is not reentrant.
                    this.LockCountMap.Remove(op);
                    this.UnlockNextReady();
                    this.Resource.Runtime.ScheduleNextOperation();
                }

                int useCount = Interlocked.Decrement(ref this.UseCount);
                if (useCount is 0 && Cache[this.SyncObject].Value == this)
                {
                    // It is safe to remove this instance from the cache.
                    Cache.TryRemove(this.SyncObject, out _);
                }
            }

            /// <inheritdoc/>
            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    this.Exit();
                }
            }

            private void AcquireReaderLock(int timeout)
            {
            }

            /// <summary>
            /// The type of a pulse operation.
            /// </summary>
            private enum PulseOperation
            {
                /// <summary>
                /// Pulses the next waiting operation.
                /// </summary>
                Next,

                /// <summary>
                /// Pulses all waiting operations.
                /// </summary>
                All
            }
        }
    }
}
*/
