// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Coyote.Runtime;

namespace Microsoft.Coyote.SystematicTesting.Interception
{
    public class ControlledSpinLock
    {
        internal class AuxData : object
        {
            /// <summary>
            /// List of waiting writer operations on this lock.
            /// </summary>
            private readonly List<AsyncOperation> WaitingWriters = null;

            /// <summary>
            /// Number of Writers that have already taken this lock. This should be less than or equal to 1.
            /// </summary>
            private ulong ExecutingWriterCount;

            /// <summary>
            /// Couote resource corresponding to this RWLock.
            /// </summary>
            private readonly Resource Res;

            public AuxData()
            {
                this.WaitingWriters = new List<AsyncOperation>();
                this.ExecutingWriterCount = 0;
                this.Res = new Resource();
            }

            public bool IsLockTaken()
            {
                return this.ExecutingWriterCount > 0;
            }

            public bool TakeWriterLock(bool isTimeoutInf)
            {
                // If no writer or reader has taken this lock.
                if (this.ExecutingWriterCount == 0)
                {
                    this.ExecutingWriterCount++;
                }

                // No writer is waiting but 1 writer hash currently taken this lock.
                else
                {
                    // To simulate timeout.
                    if (!isTimeoutInf && CoyoteRuntime.Current.GetNondeterministicBooleanChoice(15, null, null))
                    {
                        return false;
                    }

                    // Wait for this lock.
                    this.WaitingWriters.Add(this.Res.Runtime.GetExecutingOperation<AsyncOperation>());
                    this.Res.Wait();

                    this.ExecutingWriterCount++;
                }

                return true;
            }

            public void ReleaseWriterLock()
            {
                this.ExecutingWriterCount--;
                this.ScheduleNextWaitingOperation();
            }

            private void ScheduleNextWaitingOperation()
            {
                Debug.Assert(this.ExecutingWriterCount == 0, "Invariant failed in MockedRWLocks");

                // We will first give priority to writers. In this case, there is no reader and there
                // are writers waiting on this lock.
                if (this.WaitingWriters.Count > 0)
                {
                    // Extract the first element and schedule it.
                    var operation = this.WaitingWriters[0];
                    this.WaitingWriters.RemoveAt(0);

                    this.Res.Signal(operation);
                }
            }
        }

        private readonly AuxData auxData;

        private readonly bool threadIdTracking;

        private int? threadId = 0;

        public ControlledSpinLock()
        {
            this.auxData = new AuxData();
            this.threadIdTracking = true;
        }

        public ControlledSpinLock(bool isThreadTrackingEnable)
        {
            this.auxData = new AuxData();
            this.threadIdTracking = isThreadTrackingEnable;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Enter(ref bool lockTaken)
        {
            // lockTaken must be set to false before calling this function.
            if (lockTaken)
            {
                throw new ArgumentException("lockTaken must be set to false before caling SpinLock.Enter()");
            }
            else
            {
                if (this.threadIdTracking && (this.threadId == Task.CurrentId))
                {
                    throw new LockRecursionException("spinlock recursion detected");
                }
                else
                {
                    this.threadId = Task.CurrentId;
                }

                lockTaken = this.auxData.TakeWriterLock(true);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Exit()
        {
            if (this.threadIdTracking && (this.threadId != Task.CurrentId))
            {
                throw new SynchronizationLockException("spinlock: this lock doesn't belong to the current thread");
            }

            this.auxData.ReleaseWriterLock();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Exit(bool memoryFence)
        {
            // Just ignoring the argument!!
            _ = memoryFence;
            this.Exit();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void TryEnter(ref bool lockTaken)
        {
            // lockTaken must be set to false before calling this function.
            if (lockTaken)
            {
                throw new ArgumentException("lockTaken must be set to false before caling SpinLock.Enter()");
            }
            else
            {
                if (this.threadIdTracking && (this.threadId == Task.CurrentId))
                {
                    throw new LockRecursionException("spinlock recursion detected");
                }
                else
                {
                    this.threadId = Task.CurrentId;
                }

                if (!this.auxData.IsLockTaken())
                {
                    lockTaken = this.auxData.TakeWriterLock(true);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void TryEnter(int millisecondsTimeout, ref bool lockTaken)
        {
            // If timeout is a negative value other than -1, throw error.
            if (millisecondsTimeout < -1)
            {
                throw new ArgumentOutOfRangeException($"SpinLock.TryEnter() wrong arguments {millisecondsTimeout}");
            }

            // lockTaken must be set to false before calling this function.
            if (lockTaken)
            {
                throw new ArgumentException("lockTaken must be set to false before caling SpinLock.Enter()");
            }
            else
            {
                if (this.threadIdTracking && (this.threadId == Task.CurrentId))
                {
                    throw new LockRecursionException("spinlock recursion detected");
                }
                else
                {
                    this.threadId = Task.CurrentId;
                }

                // If lock is already taken, wait for the specified timeout.
                if (!this.auxData.IsLockTaken() || CoyoteRuntime.Current.GetNondeterministicBooleanChoice(15, null, null))
                {
                    lockTaken = this.auxData.TakeWriterLock(true);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void TryEnter(TimeSpan timeout, ref bool lockTaken)
        {
            // If timeout is a negative value other than -1, throw error.
            if (timeout.TotalMilliseconds < -1)
            {
                throw new ArgumentOutOfRangeException($"SpinLock.TryEnter() wrong arguments {timeout.TotalMilliseconds}");
            }

            // lockTaken must be set to false before calling this function.
            if (lockTaken)
            {
                throw new ArgumentException("lockTaken must be set to false before caling SpinLock.Enter()");
            }
            else
            {
                if (this.threadIdTracking && (this.threadId == Task.CurrentId))
                {
                    throw new LockRecursionException("spinlock recursion detected");
                }
                else
                {
                    this.threadId = Task.CurrentId;
                }

                // If lock is already taken, wait for the specified timeout.
                if (!this.auxData.IsLockTaken() || CoyoteRuntime.Current.GetNondeterministicBooleanChoice(15, null, null))
                {
                    lockTaken = this.auxData.TakeWriterLock(true);
                }
            }
        }
    }
}
