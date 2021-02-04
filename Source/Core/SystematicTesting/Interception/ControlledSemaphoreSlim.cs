// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.SystematicTesting.Interception;
using Microsoft.Coyote.Tasks;

namespace Microsoft.Coyote.SystematicTesting.Interception
{
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static class ControlledSemaphoreSlimStaticWrapper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SemaphoreSlim Create(int initialCount) => CoyoteRuntime.IsExecutionControlled ?
            new ControlledSemaphoreSlim(initialCount) : new SemaphoreSlim(initialCount);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SemaphoreSlim Create(int initialCount, int maxCount) => CoyoteRuntime.IsExecutionControlled ?
            new ControlledSemaphoreSlim(initialCount, maxCount) : new SemaphoreSlim(initialCount, maxCount);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Dispose(SemaphoreSlim obj)
        {
            if (CoyoteRuntime.IsExecutionControlled && obj is ControlledSemaphoreSlim mockedCSemaphore)
            {
                mockedCSemaphore.ReleaseAllResource();
            }
            else
            {
                obj.Dispose();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Release(SemaphoreSlim obj)
        {
            if (CoyoteRuntime.IsExecutionControlled && obj is ControlledSemaphoreSlim mockedCSemaphore)
            {
                return mockedCSemaphore.ReleaseResource();
            }
            else
            {
                return obj.Release();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Release(SemaphoreSlim obj, int releaseCount)
        {
            if (CoyoteRuntime.IsExecutionControlled && obj is ControlledSemaphoreSlim mockedCSemaphore)
            {
                return mockedCSemaphore.ReleaseResource(releaseCount);
            }
            else
            {
                return obj.Release(releaseCount);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Wait(SemaphoreSlim obj)
        {
            if (CoyoteRuntime.IsExecutionControlled && obj is ControlledSemaphoreSlim mockedCSemaphore)
            {
                mockedCSemaphore.TakeLock();
            }
            else
            {
                obj.Wait();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Wait(SemaphoreSlim obj, int timeout)
        {
            if (CoyoteRuntime.IsExecutionControlled && obj is ControlledSemaphoreSlim mockedCSemaphore)
            {
                return mockedCSemaphore.TakeLock(timeout);
            }
            else
            {
                return obj.Wait(timeout);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Wait(SemaphoreSlim obj, TimeSpan timeout)
        {
            if (CoyoteRuntime.IsExecutionControlled && obj is ControlledSemaphoreSlim mockedCSemaphore)
            {
                return mockedCSemaphore.TakeLock(timeout);
            }
            else
            {
                return obj.Wait(timeout);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Wait(SemaphoreSlim obj, CancellationToken token)
        {
            if (CoyoteRuntime.IsExecutionControlled && obj is ControlledSemaphoreSlim mockedCSemaphore)
            {
                mockedCSemaphore.TakeLock(0, token);
            }
            else
            {
                obj.Wait(token);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Wait(SemaphoreSlim obj, int timeout, CancellationToken token)
        {
            if (CoyoteRuntime.IsExecutionControlled && obj is ControlledSemaphoreSlim mockedCSemaphore)
            {
                return mockedCSemaphore.TakeLock(timeout, token);
            }
            else
            {
                return obj.Wait(timeout, token);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Wait(SemaphoreSlim obj, TimeSpan timeout, CancellationToken token)
        {
            if (CoyoteRuntime.IsExecutionControlled && obj is ControlledSemaphoreSlim mockedCSemaphore)
            {
                return mockedCSemaphore.TakeLock(timeout, token);
            }
            else
            {
                return obj.Wait(timeout, token);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task WaitAsync(SemaphoreSlim obj) => WaitAsync(obj, Timeout.Infinite, default);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<bool> WaitAsync(SemaphoreSlim obj, TimeSpan timeout) => WaitAsync(obj, timeout, default);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<bool> WaitAsync(SemaphoreSlim obj, int millisecondsTimeout) => WaitAsync(obj, millisecondsTimeout, default);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task WaitAsync(SemaphoreSlim obj, CancellationToken cancellationToken) => WaitAsync(obj, Timeout.Infinite, cancellationToken);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<bool> WaitAsync(SemaphoreSlim obj, TimeSpan timeout, CancellationToken cancellationToken) =>
            Task.FromResult(Wait(obj, timeout, cancellationToken));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<bool> WaitAsync(SemaphoreSlim obj, int millisecondsTimeout, CancellationToken cancellationToken) =>
            Task.FromResult(Wait(obj, millisecondsTimeout, cancellationToken));
    }

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public class ControlledSemaphoreSlim : SemaphoreSlim
    {
        internal class AuxData : object
        {
            /// <summary>
            /// List of waiting writer operations on this lock.
            /// </summary>
            private readonly List<AsyncOperation> WaitingThreads = null;

            /// <summary>
            /// Initial count of the semaphore.
            /// </summary>
            private int InitCount;

            /// <summary>
            /// Initial count of the semaphore.
            /// </summary>
            private readonly int MaxCount;

            /// <summary>
            /// Coyote resource corresponding to this RWLock.
            /// </summary>
            private readonly Resource Res;

            /// <summary>
            /// Boolean variable to denote whether this semaphore is disposed or not.
            /// </summary>
            public bool IsDisposed;

            public AuxData(int initCount, int maxCount)
            {
                this.WaitingThreads = new List<AsyncOperation>();
                this.InitCount = initCount;
                this.MaxCount = maxCount;
                this.IsDisposed = false;
                this.Res = new Resource();
            }

            public bool IsLockTaken()
            {
                return this.WaitingThreads.Count > 0;
            }

            public bool TakeLock(bool isTimeoutInf)
            {
                if (this.IsDisposed)
                {
                    throw new ObjectDisposedException($"{this}");
                }

                // If no writer or reader has taken this lock.
                if (this.InitCount > 0)
                {
                    this.InitCount--;
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
                    this.WaitingThreads.Add(this.Res.Runtime.GetExecutingOperation<AsyncOperation>());
                    this.Res.Wait();

                    this.InitCount--;
                }

                return true;
            }

            public int ReleaseWriterLock()
            {
                if (this.IsDisposed)
                {
                    throw new ObjectDisposedException($"{this}");
                }

                this.InitCount++;

                // InitCount can't be bigger than MaxCount => SemaphoreFullException.
                if (this.MaxCount > 0 && this.InitCount > this.MaxCount)
                {
                    return -1;
                }

                this.ScheduleNextWaitingOperation();
                return this.InitCount - 1;
            }

            public int ReleaseWriterLock(int count)
            {
                if (this.IsDisposed)
                {
                    throw new ObjectDisposedException($"{this}");
                }

                this.InitCount += count;

                // InitCount can't be bigger than MaxCount => SemaphoreFullException.
                if (this.MaxCount > 0 && this.InitCount > this.MaxCount)
                {
                    return -1;
                }

                this.ScheduleNextWaitingOperation();
                return this.InitCount - count;
            }

            public void Dispose()
            {
                if (this.IsDisposed)
                {
                    throw new ObjectDisposedException($"{this}");
                }

                this.IsDisposed = true;
            }

            private void ScheduleNextWaitingOperation()
            {
                if (this.WaitingThreads.Count > 0)
                {
                    // Extract the first element and schedule it.
                    var operation = this.WaitingThreads[0];
                    this.WaitingThreads.RemoveAt(0);

                    this.Res.Signal(operation);
                }
            }
        }

        private AuxData Data;

        private void Init(int initialCount, int maxCount)
        {
            this.Data = new AuxData(initialCount, maxCount);
        }

        public ControlledSemaphoreSlim(int initialCount, int maxCount)
            : base(initialCount, maxCount) => this.Init(initialCount, maxCount);

        public ControlledSemaphoreSlim(int initialCount)
            : base(initialCount) => this.Init(initialCount, -1);

        public void ReleaseAllResource()
        {
            this.Data.Dispose();
        }

        public int ReleaseResource()
        {
            int retval = this.Data.ReleaseWriterLock();

            if (retval < 0)
            {
                throw new SemaphoreFullException();
            }

            return retval;
        }

        public int ReleaseResource(int count)
        {
            if (count < 1)
            {
                throw new ArgumentOutOfRangeException();
            }

            int retval = this.Data.ReleaseWriterLock(count);

            if (retval < 0)
            {
                throw new SemaphoreFullException();
            }

            return retval;
        }

        public bool TakeLock(TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            long totalMilliseconds = (long)timeout.TotalMilliseconds;
            if (totalMilliseconds < -1 || totalMilliseconds > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(timeout));
            }

            return this.TakeLock((int)totalMilliseconds, cancellationToken);
        }

#pragma warning disable CA1801 // Review unused parameters
        public bool TakeLock(int millisecondsTimeout = 0, CancellationToken cancellationToken = default)
#pragma warning restore CA1801 // Review unused parameters
        {
            this.Data.TakeLock(true);

            // TODO: support timeouts during testing, this would become false if there is a timeout.
            return true;
        }
    }
}
