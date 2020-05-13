// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.SystematicTesting;
using SystemMonitor = System.Threading.Monitor;

namespace Microsoft.Coyote.Tasks
{
    /// <summary>
    /// Provides a mechanism that synchronizes access to objects. It is implemented as a thin wrapper
    /// on <see cref="SystemMonitor"/>. During testing, the implementation is automatically replaced
    /// with a controlled mocked version. It can be used as a replacement of the lock keyword to allow
    /// systematic testing.
    /// </summary>
    public class SynchronizedBlock : IDisposable
    {
        /// <summary>
        /// The object used for synchronization.
        /// </summary>
        protected readonly object SyncObject;

        /// <summary>
        /// A boolean flag that can turn on verbose debugging output that can be handy
        /// in tracking down deadlocks.
        /// </summary>
        public static bool Verbose;

        /// <summary>
        /// Initializes a new instance of the <see cref="SynchronizedBlock"/> class.
        /// </summary>
        /// <param name="syncObject">The sync object to serialize access to</param>
        protected SynchronizedBlock(object syncObject)
        {
            this.SyncObject = syncObject;
        }

        /// <summary>
        /// Creates a new <see cref="SynchronizedBlock"/> for synchronizing access
        /// to the specified object and enters the lock.
        /// </summary>
        /// <returns>The synchronized block.</returns>
        public static SynchronizedBlock Lock(object syncObject) => CoyoteRuntime.IsExecutionControlled ?
            new Mock(syncObject).EnterLock() : new SynchronizedBlock(syncObject).EnterLock();

        /// <summary>
        /// Enters the lock.
        /// </summary>
        /// <returns>The synchronized block.</returns>
        protected virtual SynchronizedBlock EnterLock()
        {
            SystemMonitor.Enter(this.SyncObject);
            return this;
        }

        /// <summary>
        /// Notifies a thread in the waiting queue of a change in the locked object's state.
        /// </summary>
        public virtual void Pulse() => SystemMonitor.Pulse(this.SyncObject);

        /// <summary>
        /// Notifies all waiting threads of a change in the object's state.
        /// </summary>
        public virtual void PulseAll() => SystemMonitor.PulseAll(this.SyncObject);

        /// <summary>
        /// Releases the lock on an object and blocks the current thread until it reacquires
        /// the lock.
        /// </summary>
        /// <returns>True if the call returned because the caller reacquired the lock for the specified
        /// object. This method does not return if the lock is not reacquired.</returns>
        public virtual bool Wait() => SystemMonitor.Wait(this.SyncObject);

        /// <summary>
        /// Releases the lock on an object and blocks the current thread until it reacquires
        /// the lock. If the specified time-out interval elapses, the thread enters the ready
        /// queue.
        /// </summary>
        /// <param name="millisecondsTimeout">The number of milliseconds to wait before the thread enters the ready queue.</param>
        /// <returns>True if the lock was reacquired before the specified time elapsed; false if the
        /// lock was reacquired after the specified time elapsed. The method does not return
        /// until the lock is reacquired.</returns>
        public virtual bool Wait(int millisecondsTimeout) => SystemMonitor.Wait(this.SyncObject, millisecondsTimeout);

        /// <summary>
        /// Releases the lock on an object and blocks the current thread until it reacquires
        /// the lock. If the specified time-out interval elapses, the thread enters the ready
        /// queue.
        /// </summary>
        /// <param name="timeout">A System.TimeSpan representing the amount of time to wait before the thread enters
        /// the ready queue.</param>
        /// <returns>True if the lock was reacquired before the specified time elapsed; false if the
        /// lock was reacquired after the specified time elapsed. The method does not return
        /// until the lock is reacquired.</returns>
        public virtual bool Wait(TimeSpan timeout) => SystemMonitor.Wait(this.SyncObject, timeout);

        /// <summary>
        /// Releases resources used by the synchronized block.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            SystemMonitor.Exit(this.SyncObject);
        }

        /// <summary>
        /// Releases resources used by the synchronized block.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Mock implementation of <see cref="SynchronizedBlock"/> that can be controlled during systematic testing.
        /// </summary>
        private class Mock : SynchronizedBlock
        {
            /// <summary>
            /// Cache from synchronized objects to synchronized object states.
            /// </summary>
            private static readonly Dictionary<object, SyncObjectState> Cache = new Dictionary<object, SyncObjectState>();

            /// <summary>
            /// The operation that is currently invoking this synchronized block.
            /// </summary>
            private AsyncOperation Caller;

            /// <summary>
            /// The currently invoked synchronized object state.
            /// </summary>
            private SyncObjectState State;

            /// <summary>
            /// True if the current invocation is reentrant, else false.
            /// </summary>
            private bool IsInvocationReentrant;

            /// <summary>
            /// Initializes a new instance of the <see cref="Mock"/> class.
            /// </summary>
            internal Mock(object syncObject)
                : base(syncObject)
            {
                if (syncObject is null)
                {
                    throw new ArgumentNullException(nameof(syncObject));
                }
            }

            protected override SynchronizedBlock EnterLock()
            {
                if (!Cache.ContainsKey(this.SyncObject))
                {
                    Cache[this.SyncObject] = new SyncObjectState(this.SyncObject);
                }

                this.State = Cache[this.SyncObject];
                this.State.UseCount++;
                this.State.Resource.Runtime.ScheduleNextOperation();

                this.Caller = this.State.Resource.Runtime.GetExecutingOperation<AsyncOperation>();
                if (this.State.Owner != null)
                {
                    if (this.State.Owner == this.Caller)
                    {
                        // The owner is re-entering the lock.
                        this.IsInvocationReentrant = true;
                        return this;
                    }
                    else
                    {
                        // Another op has the lock right now, so add the executing op to the ready queue and block it.
                        this.State.AddToReadyQueue(this.Caller);
                        return this;
                    }
                }

                // The executing op acquired the lock and can proceed.
                this.State.Owner = this.Caller;
                return this;
            }

            /// <inheritdoc/>
            public override void Pulse() => this.State.PulseNext(this.Caller);

            /// <inheritdoc/>
            public override void PulseAll() => this.State.PulseAll(this.Caller);

            /// <inheritdoc/>
            public override bool Wait() => this.State.Wait(this.Caller);

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

            /// <inheritdoc/>
            protected override void Dispose(bool disposing)
            {
                if (!this.IsInvocationReentrant)
                {
                    var runtime = this.State.Resource.Runtime;
                    this.State.UnlockNextReady();
                    runtime.ScheduleNextOperation();
                }

                this.State.Release();
            }

            /// <summary>
            /// State associated with a synchronization object.
            /// </summary>
            private class SyncObjectState
            {
                /// <summary>
                /// The resource associated with this synchronization object.
                /// </summary>
                internal readonly Resource Resource;

                /// <summary>
                /// The current owner of this synchronization object.
                /// </summary>
                internal AsyncOperation Owner;

                internal readonly List<AsyncOperation> WaitQueue = new List<AsyncOperation>();
                internal readonly List<AsyncOperation> ReadyQueue = new List<AsyncOperation>();

                // How many SynchronizedBlocks are using this object.
                internal int UseCount;

                private bool Disposed;
                private readonly object SyncObject;

                /// <summary>
                /// Initializes a new instance of the <see cref="SyncObjectState"/> class.
                /// </summary>
                internal SyncObjectState(object syncObject)
                {
                    this.SyncObject = syncObject;
                    this.Resource = new Resource();
                    this.WaitQueue = new List<AsyncOperation>();
                    this.ReadyQueue = new List<AsyncOperation>();
                }

                internal bool Wait(AsyncOperation owner)
                {
                    this.AssertNotDisposed();
                    var op = this.Resource.Runtime.GetExecutingOperation<AsyncOperation>();
                    this.Resource.Runtime.Assert(owner == op, "Object synchronization method was called from a task that did not create this SynchronizedBlock.");
                    this.Resource.Runtime.Assert(this.Owner == owner, "Cannot invoke Wait without first taking the lock.");

                    this.ReadyQueue.Remove(op);
                    if (!this.WaitQueue.Contains(op))
                    {
                        this.WaitQueue.Add(op);
                    }

                    this.UnlockNextReady();
                    if (Verbose)
                    {
                        this.Resource.Runtime.Logger.WriteLine("<SynchronizedBlock> Task {0} is waiting", op.Name);
                    }

                    this.Resource.Wait();
                    return true;
                }

                internal void PulseNext(AsyncOperation owner)
                {
                    this.AssertNotDisposed();
                    var op = this.Resource.Runtime.GetExecutingOperation<AsyncOperation>();
                    this.Resource.Runtime.Assert(owner == op, "Object synchronization method was called from a task that did not create this SynchronizedBlock.");
                    this.Resource.Runtime.Assert(this.Owner == owner, "Cannot invoke Pulse without first taking the lock.");

                    this.UseCount++;
                    // Pulse has a delay in the Operating System, we can simulate that here with a scheduled action.
                    Task.Run(() =>
                    {
                        if (this.WaitQueue.Count > 0)
                        {
                            // System.Threading.Monitor has FIFO semantics.
                            var waitingOp = this.WaitQueue[0];
                            this.WaitQueue.RemoveAt(0);
                            this.ReadyQueue.Add(waitingOp);
                            if (Verbose)
                            {
                                this.Resource.Runtime.Logger.WriteLine("<SynchronizedBlock> Task {0} is pulsed", op.Name);
                            }
                        }

                        if (this.Owner == null)
                        {
                            this.UnlockNextReady();
                        }

                        this.Release();
                    });
                }

                internal void PulseAll(AsyncOperation owner)
                {
                    this.AssertNotDisposed();
                    var op = this.Resource.Runtime.GetExecutingOperation<AsyncOperation>();
                    this.Resource.Runtime.Assert(owner == op, "Object synchronization method was called from a task that did not create this SynchronizedBlock.");
                    this.Resource.Runtime.Assert(this.Owner == owner, "Cannot invoke PulseAll without first taking the lock.");

                    this.UseCount++;
                    // Pulse has a delay in the Operating System, we can simulate that here with a scheduled action.
                    Task.Run(() =>
                    {
                        foreach (var waitingOp in this.WaitQueue)
                        {
                            this.ReadyQueue.Add(waitingOp);
                            if (Verbose)
                            {
                                this.Resource.Runtime.Logger.WriteLine("<SynchronizedBlock> Task {0} is pulsed", waitingOp.Name);
                            }
                        }

                        this.WaitQueue.Clear();
                        if (this.Owner == null)
                        {
                            this.UnlockNextReady();
                        }

                        this.Release();
                    });
                }

                internal void UnlockNextReady()
                {
                    this.AssertNotDisposed();
                    AsyncOperation op;
                    this.Owner = null;
                    if (this.ReadyQueue.Count > 0)
                    {
                        // System.Threading.Monitor has FIFO semantics.
                        op = this.ReadyQueue[0];
                        this.ReadyQueue.RemoveAt(0);
                        this.Owner = op;
                        this.Resource.Signal(op);
                        if (Verbose)
                        {
                            this.Resource.Runtime.Logger.WriteLine("<SynchronizedBlock> Task {0} is waking up", op.Name);
                        }
                    }
                }

                internal void AddToReadyQueue(AsyncOperation op)
                {
                    this.AssertNotDisposed();
                    this.WaitQueue.Remove(op);
                    if (!this.ReadyQueue.Contains(op))
                    {
                        this.ReadyQueue.Add(op);
                    }

                    this.Resource.Wait();
                }

                internal bool IsEmpty() =>
                    this.UseCount == 0 && this.Owner == null && this.ReadyQueue.Count == 0 && this.WaitQueue.Count == 0;

                internal void Release()
                {
                    this.UseCount--;
                    if (this.IsEmpty() && Cache[this.SyncObject] == this)
                    {
                        Cache.Remove(this.SyncObject);
                        this.Disposed = true;
                    }
                }

                internal void AssertNotDisposed()
                {
                    if (this.Disposed)
                    {
                        this.Resource.Runtime.Assert(false, "Cannot use a disposed SyncObjectState");
                    }
                }
            }
        }
    }
}
