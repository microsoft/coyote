// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Coyote.Runtime;

namespace Microsoft.Coyote.SystematicTesting.Interception
{
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static class ControlledReaderWriterLock
    {
        internal class AuxData : object
        {
            /// <summary>
            /// List of Waiting Readers operations on this lock.
            /// </summary>
            private readonly List<AsyncOperation> WaitingReaders = null;

            /// <summary>
            /// List of waiting writer operations on this lock.
            /// </summary>
            private readonly List<AsyncOperation> WaitingWriters = null;

            /// <summary>
            /// Number of readers that have already taken this lock.
            /// </summary>
            private ulong ExecutingReaderCount;

            /// <summary>
            /// Number of Writers that have already taken this lock. This should be less than or equal to 1.
            /// </summary>
            private ulong ExecutingWriterCount;

            /// <summary>
            /// Couote resource corresponding to this RWLock.
            /// </summary>
            private readonly Resource Res;

            /// <summary>
            /// Every RWLock object is associated with a LockCookie.
            /// </summary>
            public LockCookie Lc;

            public AuxData()
            {
                this.WaitingReaders = new List<AsyncOperation>();
                this.WaitingWriters = new List<AsyncOperation>();
                this.ExecutingReaderCount = 0;
                this.ExecutingWriterCount = 0;
                this.Res = new Resource();
                this.Lc = default;
            }

            public bool TakeReaderLock(bool isTimeoutInf)
            {
                // No writer has taken or is waiting for this lock.
                if (this.ExecutingWriterCount == 0)
                {
                    this.ExecutingReaderCount++;
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
                    this.WaitingReaders.Add(this.Res.Runtime.GetExecutingOperation<AsyncOperation>());
                    this.Res.Wait();

                    this.ExecutingReaderCount++;
                }

                return true;
            }

            public bool TakeWriterLock(bool isTimeoutInf)
            {
                // If no writer or reader has taken this lock.
                if (this.ExecutingWriterCount == 0 && this.ExecutingReaderCount == 0)
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

            public void ReleaseReaderLock()
            {
                this.ExecutingReaderCount--;
                this.ScheduleNextWaitingOperation();
            }

            private void ScheduleNextWaitingOperation()
            {
                Debug.Assert(this.ExecutingWriterCount == 0, "Invariant failed in MockedRWLocks");

                // We will first give priority to writers. In this case, there is no reader and there
                // are writers waiting on this lock.
                if (this.WaitingWriters.Count > 0 && this.ExecutingReaderCount == 0)
                {
                    // Extract the first element and schedule it.
                    var operation = this.WaitingWriters[0];
                    this.WaitingWriters.RemoveAt(0);

                    this.Res.Signal(operation);
                }

                // In this case, there is a writer waiting on this lock and there are some readers
                // which already taken the lock.
                else if (this.WaitingWriters.Count > 0 && this.ExecutingReaderCount != 0)
                {
                    // In this case, don't schedule!
                    return;
                }

                // In this case, there are no writers and so, we can scedule all readers altogether.
                else
                {
                    // Simultaneously signal all the readers!!
                    this.WaitingReaders.Clear();

                    this.Res.SignalAll();
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReaderWriterLock Create()
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                CoyoteRuntime.Current.ScheduleNextOperation();

                var rwLock = new ReaderWriterLock();
                AuxData data = new AuxData();

                if (!CoyoteRuntime.Current.Cwt.TryGetValue(rwLock, out object _))
                {
                    // Associate the AuxData with the rwLock object.
                    CoyoteRuntime.Current.Cwt.Add(rwLock, data);
                }
                else
                {
                    Debug.Assert(false, "This object is already present in the Cwt. Weird.");
                }

                return rwLock;
            }
            else
            {
                return new ReaderWriterLock();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AcquireReaderLock(ReaderWriterLock rwLock, int millisecondsTimeout)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                CoyoteRuntime.Current.ScheduleNextOperation();

                if (CoyoteRuntime.Current.Cwt.TryGetValue(rwLock, out object data))
                {
                    AuxData auxdata = data as AuxData;
                    Debug.Assert(auxdata != null, "Cwt returned an invalid Aux data");

                    bool isSuccess = auxdata.TakeReaderLock(millisecondsTimeout == -1);
                    if (!isSuccess)
                    {
                        // To simulate behaviour of oriinal AcquireReaderLock method.
                        throw new ApplicationException();
                    }
                }
                else
                {
                    Debug.Assert(false, "AuxData for this rwLock was not present in the Cwt");
                }
            }
            else
            {
                rwLock.AcquireReaderLock(millisecondsTimeout);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AcquireReaderLock(ReaderWriterLock rwLock, TimeSpan timeout)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                CoyoteRuntime.Current.ScheduleNextOperation();
                // TimeSpan.Milliseconds == -1 means that timeout is infinite.
                TimeSpan infinite = TimeSpan.FromMilliseconds(-1);
                int time = (infinite == timeout) ? -1 : 1;

                AcquireReaderLock(rwLock, time);
            }
            else
            {
                rwLock.AcquireReaderLock(timeout);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AcquireWriterLock(ReaderWriterLock rwLock, int millisecondsTimeout)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                CoyoteRuntime.Current.ScheduleNextOperation();
                if (CoyoteRuntime.Current.Cwt.TryGetValue(rwLock, out object data))
                {
                    AuxData auxdata = data as AuxData;
                    Debug.Assert(auxdata != null, "Cwt returned an invalid Aux data");

                    bool isSuccess = auxdata.TakeWriterLock(millisecondsTimeout == -1);
                    if (!isSuccess)
                    {
                        // To simulate behaviour of oriinal AcquireWriterLock method.
                        throw new ApplicationException();
                    }
                }
                else
                {
                    Debug.Assert(false, "AuxData for this rwLock was not present in the Cwt");
                }
            }
            else
            {
                rwLock.AcquireWriterLock(millisecondsTimeout);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AcquireWriterLock(ReaderWriterLock rwLock, TimeSpan timeout)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                CoyoteRuntime.Current.ScheduleNextOperation();
                // TimeSpan.Milliseconds == -1 means that timeout is infinite.
                TimeSpan infinite = TimeSpan.FromMilliseconds(-1);
                int time = (infinite == timeout) ? -1 : 1;

                AcquireWriterLock(rwLock, time);
            }
            else
            {
                rwLock.AcquireWriterLock(timeout);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReleaseReaderLock(ReaderWriterLock rwLock)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                CoyoteRuntime.Current.ScheduleNextOperation();
                if (CoyoteRuntime.Current.Cwt.TryGetValue(rwLock, out object data))
                {
                    AuxData auxdata = data as AuxData;
                    Debug.Assert(auxdata != null, "Cwt returned an invalid Aux data");

                    auxdata.ReleaseReaderLock();
                }
                else
                {
                    Debug.Assert(false, "AuxData for this rwLock was not present in the Cwt");
                }
            }
            else
            {
                rwLock.ReleaseReaderLock();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReleaseWriterLock(ReaderWriterLock rwLock)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                CoyoteRuntime.Current.ScheduleNextOperation();
                if (CoyoteRuntime.Current.Cwt.TryGetValue(rwLock, out object data))
                {
                    AuxData auxdata = data as AuxData;
                    Debug.Assert(auxdata != null, "Cwt returned an invalid Aux data");

                    auxdata.ReleaseWriterLock();
                }
                else
                {
                    Debug.Assert(false, "AuxData for this rwLock was not present in the Cwt");
                }
            }
            else
            {
                rwLock.ReleaseWriterLock();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LockCookie UpgradeToWriterLock(ReaderWriterLock rwLock, int millisecondsTimeout)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                CoyoteRuntime.Current.ScheduleNextOperation();
                if (CoyoteRuntime.Current.Cwt.TryGetValue(rwLock, out object data))
                {
                    AuxData auxdata = data as AuxData;
                    Debug.Assert(auxdata != null, "Cwt returned an invalid Aux data");

                    auxdata.ReleaseReaderLock();
                    bool success = auxdata.TakeWriterLock(millisecondsTimeout == -1);
                    if (!success)
                    {
                        // To simulate behaviour of oriinal UpgradeToWriterLock method.
                        throw new ApplicationException();
                    }

                    return auxdata.Lc;
                }
                else
                {
                    Debug.Assert(false, "AuxData for this rwLock was not present in the Cwt");
                }

                // Control should not reach this point.
                return default;
            }
            else
            {
                return rwLock.UpgradeToWriterLock(millisecondsTimeout);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DowngradeFromWriterLock(ReaderWriterLock rwLock, ref LockCookie lockCookie)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                CoyoteRuntime.Current.ScheduleNextOperation();
                if (CoyoteRuntime.Current.Cwt.TryGetValue(rwLock, out object data))
                {
                    AuxData auxdata = data as AuxData;
                    Debug.Assert(auxdata != null, "Cwt returned an invalid Aux data");
                    Debug.Assert(lockCookie.Equals(auxdata.Lc), "lockCookie value doesn't match");

                    auxdata.ReleaseWriterLock();
                    auxdata.TakeReaderLock(true);
                }
                else
                {
                    Debug.Assert(false, "AuxData for this rwLock was not present in the Cwt");
                }
            }
            else
            {
                rwLock.DowngradeFromWriterLock(ref lockCookie);
            }
        }
    }
}
