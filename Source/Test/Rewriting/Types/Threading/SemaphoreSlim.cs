// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.Runtime.CompilerServices;
using SystemCancellationToken = System.Threading.CancellationToken;
using SystemCompiler = System.Runtime.CompilerServices;
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
            if (runtime.SchedulingPolicy is SchedulingPolicy.Interleaving)
            {
                if (runtime.Configuration.IsLockAccessRaceCheckingEnabled)
                {
                    runtime.ScheduleNextOperation(SchedulingPointType.Default);
                }

                runtime.PauseOperationUntil(() => instance.CurrentCount > 0);
            }
            else if (runtime.SchedulingPolicy is SchedulingPolicy.Fuzzing &&
                runtime.Configuration.IsLockAccessRaceCheckingEnabled)
            {
                runtime.DelayOperation();
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
        public static async SystemTasks.Task<bool> WaitAsync2(SystemSemaphoreSlim instance, int millisecondsTimeout, SystemCancellationToken cancellationToken)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.Interleaving)
            {
                if (runtime.Configuration.IsLockAccessRaceCheckingEnabled)
                {
                    runtime.ScheduleNextOperation(SchedulingPointType.Default);
                }

                await runtime.PauseOperationUntilAsync(() =>
                {
                    return instance.CurrentCount > 0;
                }, true);
            }
            else if (runtime.SchedulingPolicy is SchedulingPolicy.Fuzzing &&
                runtime.Configuration.IsLockAccessRaceCheckingEnabled)
            {
                runtime.DelayOperation();
            }

            return await instance.WaitAsync(millisecondsTimeout, cancellationToken);
        }

        /// <summary>
        /// Asynchronously waits to enter the semaphore, using a 32-bit signed integer
        /// that specifies the timeout, while observing a cancellation token.
        /// </summary>
        [SystemCompiler.AsyncStateMachine(typeof(WaitAsyncStateMachine))]
        public static SystemTasks.Task<bool> WaitAsync(SystemSemaphoreSlim instance, int millisecondsTimeout, SystemCancellationToken cancellationToken)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.Interleaving)
            {
                if (runtime.Configuration.IsLockAccessRaceCheckingEnabled)
                {
                    runtime.ScheduleNextOperation(SchedulingPointType.Default);
                }

                var stateMachine = new WaitAsyncStateMachine(runtime, instance, millisecondsTimeout, cancellationToken);
                stateMachine.Builder.Start(ref stateMachine);
                return stateMachine.Builder.Task;
            }
            else if (runtime.SchedulingPolicy is SchedulingPolicy.Fuzzing &&
                runtime.Configuration.IsLockAccessRaceCheckingEnabled)
            {
                runtime.DelayOperation();
            }

            return instance.WaitAsync(millisecondsTimeout, cancellationToken);
        }

        /// <summary>
        /// Implements an asynchronous state machine that is used to drive and take control of the
        /// <see cref="WaitAsync(SystemSemaphoreSlim, int, SystemCancellationToken)"/> method.
        /// </summary>
        [SystemCompiler.CompilerGenerated]
        private class WaitAsyncStateMachine : AsyncStateMachine<bool>
        {
            private SystemSemaphoreSlim Instance;
            private int MillisecondsTimeout;
            private SystemCancellationToken CancellationToken;

            /// <summary>
            /// Initializes a new instance of the <see cref="WaitAsyncStateMachine"/> class.
            /// </summary>
            internal WaitAsyncStateMachine(CoyoteRuntime runtime, SystemSemaphoreSlim instance,
                int millisecondsTimeout, SystemCancellationToken cancellationToken)
                : base(runtime, 2)
            {
                this.Instance = instance;
                this.MillisecondsTimeout = millisecondsTimeout;
                this.CancellationToken = cancellationToken;
            }

            /// <inheritdoc/>
            protected override bool TryExecuteState(uint state)
            {
                IO.Debug.WriteLine($">>> WaitAsyncStateMachine::TryExecuteState state({state}) '{System.Threading.Thread.CurrentThread.ManagedThreadId}'.");
                if (state is 0)
                {
                    this.Awaiter = this.Runtime.PauseOperationUntilAsync(() =>
                    {
                        return this.Instance.CurrentCount > 0;
                    }, true).GetAwaiter();
                    return ((IControllableAwaiter)this.Awaiter).IsDone;
                }

                var task = this.Instance.WaitAsync(this.MillisecondsTimeout, this.CancellationToken);
                this.Awaiter = new TaskAwaiter<bool>(task);
                return ((IControllableAwaiter<bool>)this.Awaiter).IsDone;
            }

            /// <inheritdoc/>
            protected override void CompleteState(uint state)
            {
                if (state is 0)
                {
                    ((IControllableAwaiter)this.Awaiter).WaitCompletion();
                }
                else if (state is 1)
                {
                    this.Result = ((IControllableAwaiter<bool>)this.Awaiter).WaitCompletion();
                }
            }
        }
    }
}
