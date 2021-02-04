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
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static class ControlledSpinLock
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

            public bool ThreadIdTracking;

            public int? ThreadId = 0;

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SpinLock Create()
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                var sLock = default(SpinLock);
                AuxData data = new AuxData();
                data.ThreadIdTracking = true;

                if (!CoyoteRuntime.Current.Cwt.TryGetValue(sLock, out object _))
                {
                    // Associate the AuxData with the sLock object.
                    CoyoteRuntime.Current.Cwt.Add(sLock, data);
                }
                else
                {
                    Debug.Assert(false, "This object is already present in the Cwt. Weird.");
                }

                return sLock;
            }
            else
            {
                return default;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SpinLock Create(bool enableThreadOwnerTracking)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                var sLock = new SpinLock(enableThreadOwnerTracking);
                AuxData data = new AuxData();
                data.ThreadIdTracking = enableThreadOwnerTracking;

                if (!CoyoteRuntime.Current.Cwt.TryGetValue(sLock, out object _))
                {
                    // Associate the AuxData with the sLock object.
                    CoyoteRuntime.Current.Cwt.Add(sLock, data);
                }
                else
                {
                    Debug.Assert(false, "This object is already present in the Cwt. Weird.");
                }

                return sLock;
            }
            else
            {
                return new SpinLock(enableThreadOwnerTracking);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Enter(ref SpinLock obj, ref bool lockTaken)
        {
            _ = obj;
            lockTaken = true;
/*
            if (CoyoteRuntime.IsExecutionControlled)
            {
                CoyoteRuntime.Current.ScheduleNextOperation();

                if (!CoyoteRuntime.Current.Cwt.TryGetValue(obj, out object _))
                {
                    AuxData dat = new AuxData();
                    dat.ThreadIdTracking = true;
                    CoyoteRuntime.Current.Cwt.Add( obj, dat);
                }

                if (CoyoteRuntime.Current.Cwt.TryGetValue( obj, out object data))
                {
                    AuxData auxdata = data as AuxData;
                    // lockTaken must be set to false before calling this function.
                    if (lockTaken)
                    {
                        throw new ArgumentException("lockTaken must be set to false before caling SpinLock.Enter()");
                    }
                    else
                    {
                        if (auxdata.ThreadIdTracking && (auxdata.ThreadId == Task.CurrentId))
                        {
                            throw new LockRecursionException("spinlock recursion detected");
                        }
                        else
                        {
                            auxdata.ThreadId = Task.CurrentId;
                        }

                        lockTaken = auxdata.TakeWriterLock(true);
                    }
                }
                else
                {
                    throw new Exception("Object not found");
                }
            }
            else
            {
                obj.Enter(ref lockTaken);
            }
*/
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Exit(ref SpinLock obj)
        {
            _ = obj;
/*
            if (CoyoteRuntime.IsExecutionControlled)
            {
                CoyoteRuntime.Current.ScheduleNextOperation();

                if (CoyoteRuntime.Current.Cwt.TryGetValue(obj, out object data))
                {
                    AuxData auxdata = data as AuxData;
                    if (auxdata.ThreadIdTracking && (auxdata.ThreadId != Task.CurrentId))
                    {
                        throw new SynchronizationLockException("spinlock: this lock doesn't belong to the current thread");
                    }

                    auxdata.ReleaseWriterLock();
                }
                else
                {
                    throw new Exception("Object not found");
                }
            }
            else
            {
                obj.Exit();
            }
*/
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Exit(ref SpinLock obj, bool useMemoryBarrier)
        {
            // Just ignoring the argument!!
            _ = useMemoryBarrier;
            Exit(ref obj);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void TryEnter(ref SpinLock obj, ref bool lockTaken)
        {
            _ = obj;
            lockTaken = true;
            /*
            if (CoyoteRuntime.IsExecutionControlled)
            {
                CoyoteRuntime.Current.ScheduleNextOperation();

                if (CoyoteRuntime.Current.Cwt.TryGetValue(obj, out object data))
                {
                    AuxData auxdata = data as AuxData;
                    // lockTaken must be set to false before calling this function.
                    if (lockTaken)
                    {
                        throw new ArgumentException("lockTaken must be set to false before caling SpinLock.Enter()");
                    }
                    else
                    {
                        if (auxdata.ThreadIdTracking && (auxdata.ThreadId == Task.CurrentId))
                        {
                            throw new LockRecursionException("spinlock recursion detected");
                        }
                        else
                        {
                            auxdata.ThreadId = Task.CurrentId;
                        }

                        if (!auxdata.IsLockTaken())
                        {
                            lockTaken = auxdata.TakeWriterLock(true);
                        }
                    }
                }
            }
            else
            {
                obj.TryEnter(ref lockTaken);
            }
            */
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void TryEnter(ref SpinLock obj, int millisecondsTimeout, ref bool lockTaken)
        {
            _ = obj;
            _ = millisecondsTimeout;
            lockTaken = true;
            /*
            if (CoyoteRuntime.IsExecutionControlled)
            {
                CoyoteRuntime.Current.ScheduleNextOperation();

                if (CoyoteRuntime.Current.Cwt.TryGetValue(obj, out object data))
                {
                    AuxData auxdata = data as AuxData;
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
                        if (auxdata.ThreadIdTracking && (auxdata.ThreadId == Task.CurrentId))
                        {
                            throw new LockRecursionException("spinlock recursion detected");
                        }
                        else
                        {
                            auxdata.ThreadId = Task.CurrentId;
                        }

                        // If lock is already taken, wait for the specified timeout.
                        if (!auxdata.IsLockTaken() || CoyoteRuntime.Current.GetNondeterministicBooleanChoice(15, null, null))
                        {
                            lockTaken = auxdata.TakeWriterLock(true);
                        }
                    }
                }
            }
            else
            {
                obj.TryEnter(millisecondsTimeout, ref lockTaken);
            }
            */
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void TryEnter(ref SpinLock obj, TimeSpan timeout, ref bool lockTaken)
        {
            _ = obj;
            _ = timeout;
            lockTaken = true;
            /*
            if (CoyoteRuntime.IsExecutionControlled)
            {
                CoyoteRuntime.Current.ScheduleNextOperation();

                if (CoyoteRuntime.Current.Cwt.TryGetValue(obj, out object data))
                {
                    AuxData auxdata = data as AuxData;
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
                        if (auxdata.ThreadIdTracking && (auxdata.ThreadId == Task.CurrentId))
                        {
                            throw new LockRecursionException("spinlock recursion detected");
                        }
                        else
                        {
                            auxdata.ThreadId = Task.CurrentId;
                        }

                        // If lock is already taken, wait for the specified timeout.
                        if (!auxdata.IsLockTaken() || CoyoteRuntime.Current.GetNondeterministicBooleanChoice(15, null, null))
                        {
                            lockTaken = auxdata.TakeWriterLock(true);
                        }
                    }
                }
            }
            else
            {
                obj.TryEnter(timeout, ref lockTaken);
            }
            */
        }
    }
}
