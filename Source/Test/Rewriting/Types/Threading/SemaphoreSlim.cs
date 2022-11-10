// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.Runtime.CompilerServices;
using SystemCancellationToken = System.Threading.CancellationToken;
using SystemSemaphoreSlim = System.Threading.SemaphoreSlim;
using SystemTask = System.Threading.Tasks.Task;
using SystemTasks = System.Threading.Tasks;
using SystemTimeout = System.Threading.Timeout;

namespace Microsoft.Coyote.Rewriting.Types.Threading
{
    /// <summary>
    /// Provides methods for creating semaphores that can be controlled during testing.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static class SemaphoreSlim
    {
        /// <summary>
        /// Blocks the current task until it can enter the semaphore.
        /// </summary>
        public static void Wait(SystemSemaphoreSlim instance) =>
            Wait(instance, SystemTimeout.Infinite, SystemCancellationToken.None);

        /// <summary>
        /// Blocks the current task until it can enter the semaphore, using a timespan
        /// that specifies the timeout.
        /// </summary>
        public static bool Wait(SystemSemaphoreSlim instance, TimeSpan timeout) =>
            Wait(instance, timeout, SystemCancellationToken.None);

        /// <summary>
        /// Blocks the current task until it can enter the semaphore, using a 32-bit signed integer
        /// that specifies the timeout.
        /// </summary>
        public static bool Wait(SystemSemaphoreSlim instance, int millisecondsTimeout) =>
            Wait(instance, millisecondsTimeout, SystemCancellationToken.None);

        /// <summary>
        /// Blocks the current task until it can enter the semaphore, while observing a cancellation token.
        /// </summary>
        public static void Wait(SystemSemaphoreSlim instance, SystemCancellationToken cancellationToken) =>
            Wait(instance, SystemTimeout.Infinite, cancellationToken);

        /// <summary>
        /// Blocks the current task until it can enter the semaphore, using a timespan
        /// that specifies the timeout, while observing a cancellation token.
        /// </summary>
        public static bool Wait(SystemSemaphoreSlim instance, TimeSpan timeout, SystemCancellationToken cancellationToken)
        {
            long totalMilliseconds = (long)timeout.TotalMilliseconds;
            if (totalMilliseconds < -1 || totalMilliseconds > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(timeout));
            }

            return Wait(instance, (int)totalMilliseconds, cancellationToken);
        }

        /// <summary>
        /// Blocks the current task until it can enter the semaphore, using a 32-bit signed integer
        /// that specifies the timeout, while observing a cancellation token.
        /// </summary>
        public static bool Wait(SystemSemaphoreSlim instance, int millisecondsTimeout, SystemCancellationToken cancellationToken)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy != SchedulingPolicy.None &&
                runtime.TryGetExecutingOperation(out ControlledOperation current))
            {
                if (runtime.SchedulingPolicy is SchedulingPolicy.Interleaving)
                {
                    if (runtime.Configuration.IsLockAccessRaceCheckingEnabled)
                    {
                        runtime.ScheduleNextOperation(current, SchedulingPointType.Default);
                    }

                    runtime.PauseOperationUntil(current, () => instance.CurrentCount > 0);
                }
                else if (runtime.SchedulingPolicy is SchedulingPolicy.Fuzzing &&
                    runtime.Configuration.IsLockAccessRaceCheckingEnabled)
                {
                    runtime.DelayOperation(current);
                }
            }

            return instance.Wait(millisecondsTimeout, cancellationToken);
        }

        /// <summary>
        /// Asynchronously waits to enter the semaphore.
        /// </summary>
        public static SystemTask WaitAsync(SystemSemaphoreSlim instance) => WaitAsync(instance, SystemTimeout.Infinite, default);

        /// <summary>
        /// Asynchronously waits to enter the semaphore, while observing a cancellation token.
        /// </summary>
        public static SystemTask WaitAsync(SystemSemaphoreSlim instance, SystemCancellationToken cancellationToken) =>
            WaitAsync(instance, SystemTimeout.Infinite, cancellationToken);

        /// <summary>
        /// Asynchronously waits to enter the semaphore, using a 32-bit signed integer
        /// that specifies the timeout.
        /// </summary>
        public static SystemTasks.Task<bool> WaitAsync(SystemSemaphoreSlim instance, int millisecondsTimeout) =>
            WaitAsync(instance, millisecondsTimeout, default);

        /// <summary>
        /// Asynchronously waits to enter the semaphore, using a timespan that specifies the timeout.
        /// </summary>
        public static SystemTasks.Task<bool> WaitAsync(SystemSemaphoreSlim instance, TimeSpan timeout) =>
            WaitAsync(instance, timeout, default);

        /// <summary>
        /// Asynchronously waits to enter the semaphore, using a timespan
        /// that specifies the timeout, while observing a cancellation token.
        /// </summary>
        public static SystemTasks.Task<bool> WaitAsync(SystemSemaphoreSlim instance, TimeSpan timeout, SystemCancellationToken cancellationToken)
        {
            long totalMilliseconds = (long)timeout.TotalMilliseconds;
            if (totalMilliseconds < -1 || totalMilliseconds > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(timeout));
            }

            return WaitAsync(instance, (int)totalMilliseconds, cancellationToken);
        }

        /// <summary>
        /// Asynchronously waits to enter the semaphore, using a 32-bit signed integer
        /// that specifies the timeout, while observing a cancellation token.
        /// </summary>
        public static SystemTasks.Task<bool> WaitAsync(SystemSemaphoreSlim instance, int millisecondsTimeout, SystemCancellationToken cancellationToken)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy != SchedulingPolicy.None &&
                runtime.TryGetExecutingOperation(out ControlledOperation current))
            {
                if (runtime.SchedulingPolicy is SchedulingPolicy.Interleaving)
                {
                    if (runtime.Configuration.IsLockAccessRaceCheckingEnabled)
                    {
                        runtime.ScheduleNextOperation(current, SchedulingPointType.Default);
                    }

                    var task = instance.WaitAsync(millisecondsTimeout, cancellationToken);
                    runtime.RegisterKnownControlledTask(task);
                    return AsyncTaskAwaiterStateMachine<bool>.RunAsync(runtime, task, true);
                }
                else if (runtime.SchedulingPolicy is SchedulingPolicy.Fuzzing &&
                    runtime.Configuration.IsLockAccessRaceCheckingEnabled)
                {
                    runtime.DelayOperation(current);
                }
            }

            return instance.WaitAsync(millisecondsTimeout, cancellationToken);
        }
    }
}
