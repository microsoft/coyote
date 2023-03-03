// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.Runtime.CompilerServices;
using SystemCancellationToken = System.Threading.CancellationToken;
using SystemSemaphoreSlim = System.Threading.SemaphoreSlim;
using SystemTask = System.Threading.Tasks.Task;
using SystemTaskCreationOptions = System.Threading.Tasks.TaskCreationOptions;
using SystemTasks = System.Threading.Tasks;
using SystemThread = System.Threading.Thread;
using SystemThreading = System.Threading;
using SystemTimeout = System.Threading.Timeout;
using SystemWaitHandle = System.Threading.WaitHandle;

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
        /// Initializes a new instance of the <see cref="SemaphoreSlim"/> class, specifying
        /// the initial number of requests that can be granted concurrently.
        /// </summary>
        public static SystemSemaphoreSlim Create(int initialCount) => Create(initialCount, int.MaxValue);

        /// <summary>
        /// Initializes a new instance of the <see cref="SemaphoreSlim"/> class, specifying
        /// the initial and maximum number of requests that can be granted concurrently.
        /// </summary>
        public static SystemSemaphoreSlim Create(int initialCount, int maxCount)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.Interleaving)
            {
                return new Wrapper(runtime, initialCount, maxCount);
            }

            return new SystemSemaphoreSlim(initialCount, maxCount);
        }

        /// <summary>
        /// Returns a <see cref="SystemWaitHandle"/> that can be used to wait on the semaphore.
        /// </summary>
#pragma warning disable CA1707 // Identifiers should not contain underscores
#pragma warning disable SA1300 // Element should begin with upper-case letter
#pragma warning disable IDE1006 // Naming Styles
        public static SystemWaitHandle get_AvailableWaitHandle(SystemSemaphoreSlim instance)
#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore SA1300 // Element should begin with upper-case letter
#pragma warning restore CA1707 // Identifiers should not contain underscores
        {
            if (instance is Wrapper wrapper)
            {
                var runtime = CoyoteRuntime.Current;
                if (runtime.SchedulingPolicy is SchedulingPolicy.Interleaving)
                {
                    runtime.NotifyAssertionFailure("Invoking 'SemaphoreSlim.AvailableWaitHandle' is not supported in systematic testing.");
                }
            }

            return instance.AvailableWaitHandle;
        }

        /// <summary>
        /// Gets the number of remaining threads that can enter the <see cref="SystemSemaphoreSlim"/> object.
        /// </summary>
#pragma warning disable CA1707 // Identifiers should not contain underscores
#pragma warning disable SA1300 // Element should begin with upper-case letter
#pragma warning disable IDE1006 // Naming Styles
        public static int get_CurrentCount(SystemSemaphoreSlim instance)
#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore SA1300 // Element should begin with upper-case letter
#pragma warning restore CA1707 // Identifiers should not contain underscores
        {
            if (instance is Wrapper wrapper)
            {
                return wrapper.LockCount;
            }

            return instance.CurrentCount;
        }

        /// <summary>
        /// Blocks the current task until it can enter the semaphore.
        /// </summary>
        public static void Wait(SystemSemaphoreSlim instance) =>
            Wait(instance, SystemTimeout.Infinite, SystemCancellationToken.None);

        /// <summary>
        /// Blocks the current task until it can enter the semaphore, using a <see cref="TimeSpan"/>
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
        /// Blocks the current task until it can enter the semaphore, using a <see cref="TimeSpan"/>
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
            if (instance is Wrapper wrapper)
            {
                return wrapper.Enter(millisecondsTimeout);
            }

            var runtime = CoyoteRuntime.Current;
            if (runtime.Configuration.IsLockAccessRaceCheckingEnabled &&
                runtime.SchedulingPolicy is SchedulingPolicy.Fuzzing &&
                runtime.TryGetExecutingOperation(out ControlledOperation current))
            {
                runtime.DelayOperation(current);
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
        /// Asynchronously waits to enter the semaphore, using a <see cref="TimeSpan"/> that specifies the timeout.
        /// </summary>
        public static SystemTasks.Task<bool> WaitAsync(SystemSemaphoreSlim instance, TimeSpan timeout) =>
            WaitAsync(instance, timeout, default);

        /// <summary>
        /// Asynchronously waits to enter the semaphore, using a <see cref="TimeSpan"/>
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
            if (instance is Wrapper wrapper)
            {
                return wrapper.EnterAsync(millisecondsTimeout);
            }

            var runtime = CoyoteRuntime.Current;
            if (runtime.Configuration.IsLockAccessRaceCheckingEnabled &&
                runtime.SchedulingPolicy is SchedulingPolicy.Fuzzing &&
                runtime.TryGetExecutingOperation(out ControlledOperation current))
            {
                runtime.DelayOperation(current);
            }

            return instance.WaitAsync(millisecondsTimeout, cancellationToken);
        }

        /// <summary>
        /// Releases the <see cref="SemaphoreSlim"/> object once.
        /// </summary>
        public static int Release(SystemSemaphoreSlim instance) =>
            instance is Wrapper wrapper ? wrapper.Exit(1) : instance.Release();

        /// <summary>
        /// Releases the <see cref="SemaphoreSlim"/> object a specified number of times.
        /// </summary>
        public static int Release(SystemSemaphoreSlim instance, int releaseCount) =>
            instance is Wrapper wrapper ? wrapper.Exit(releaseCount) : instance.Release(releaseCount);

        /// <summary>
        /// Wraps a <see cref="SystemSemaphoreSlim"/> so that it can be controlled during testing.
        /// </summary>
        private class Wrapper : SystemSemaphoreSlim
        {
            /// <summary>
            /// The id of the <see cref="CoyoteRuntime"/> that created this semaphore.
            /// </summary>
            private readonly Guid RuntimeId;

            /// <summary>
            /// The resource id of this semaphore.
            /// </summary>
            private readonly Guid ResourceId;

            /// <summary>
            /// Queue of operations waiting to be released.
            /// </summary>
            private readonly Queue<ControlledOperation> PausedOperations;

            /// <summary>
            /// Queue of completion sources that operations are asynchronously awaiting to get released.
            /// </summary>
            private readonly Queue<SystemTasks.TaskCompletionSource<bool>> AsyncAwaiters;

            /// <summary>
            /// The maximum semaphore value.
            /// </summary>
            private readonly int MaxCount;

            /// <summary>
            /// The semaphore lock count.
            /// </summary>
            internal int LockCount { get; private set; }

            /// <summary>
            /// The debug name of this semaphore.
            /// </summary>
            private readonly string DebugName;

            /// <summary>
            /// Initializes a new instance of the <see cref="Wrapper"/> class.
            /// </summary>
            internal Wrapper(CoyoteRuntime runtime, int initialCount, int maxCount)
                : base(initialCount, maxCount)
            {
                this.RuntimeId = runtime.Id;
                this.ResourceId = Guid.NewGuid();
                this.PausedOperations = new Queue<ControlledOperation>();
                this.AsyncAwaiters = new Queue<SystemTasks.TaskCompletionSource<bool>>();
                this.LockCount = initialCount;
                this.MaxCount = maxCount;
                this.DebugName = $"SemaphoreSlim({this.ResourceId})";
            }

            /// <summary>
            /// Pauses the current operation until it can enter the semaphore.
            /// </summary>
            internal bool Enter(int millisecondsTimeout)
            {
                CoyoteRuntime runtime = this.GetRuntime();
                using (runtime.EnterSynchronizedSection())
                {
                    if (!runtime.TryGetExecutingOperation(out ControlledOperation current))
                    {
                        runtime.NotifyUncontrolledSynchronizationInvocation("SemaphoreSlim.Wait");
                    }
                    else if (runtime.Configuration.IsLockAccessRaceCheckingEnabled)
                    {
                        runtime.ScheduleNextOperation(current, SchedulingPointType.Acquire);
                    }

                    while (this.LockCount is 0)
                    {
                        if (millisecondsTimeout is 0)
                        {
                            return false;
                        }

                        runtime.LogWriter.LogDebug(
                            "[coyote::debug] Operation {0} is waiting for '{1}' to get released on thread '{2}'.",
                            current.DebugInfo, this.DebugName, SystemThread.CurrentThread.ManagedThreadId);
                        current.PauseWithResource(this.ResourceId);
                        this.PausedOperations.Enqueue(current);
                        runtime.ScheduleNextOperation(current, SchedulingPointType.Pause);
                    }

                    this.LockCount--;
                    return true;
                }
            }

            /// <summary>
            /// Pauses the current operation asynchronously until it can enter the semaphore.
            /// </summary>
            internal SystemTasks.Task<bool> EnterAsync(int millisecondsTimeout)
            {
                CoyoteRuntime runtime = this.GetRuntime();
                using (runtime.EnterSynchronizedSection())
                {
                    if (!runtime.TryGetExecutingOperation(out ControlledOperation current))
                    {
                        runtime.NotifyUncontrolledSynchronizationInvocation("SemaphoreSlim.WaitAsync");
                    }
                    else if (runtime.Configuration.IsLockAccessRaceCheckingEnabled)
                    {
                        runtime.ScheduleNextOperation(current, SchedulingPointType.Acquire);
                    }

                    if (this.LockCount is 0)
                    {
                        if (millisecondsTimeout is 0)
                        {
                            return Tasks.Task.FromResult(false);
                        }

                        var tcs = new SystemTasks.TaskCompletionSource<bool>(SystemTaskCreationOptions.RunContinuationsAsynchronously);
                        this.AsyncAwaiters.Enqueue(tcs);
                        runtime.RegisterKnownControlledTask(tcs.Task);
                        return AsyncConditionAwaiterStateMachine.RunAsync(runtime, () => tcs.Task.IsCompleted,
                            debugMsg: $"'{this.DebugName}' to get released");
                    }

                    this.LockCount--;
                    return Tasks.Task.FromResult(true);
                }
            }

            /// <summary>
            /// Exits the semaphore a specified number of times.
            /// </summary>
            internal int Exit(int releaseCount)
            {
                CoyoteRuntime runtime = this.GetRuntime();
                using (runtime.EnterSynchronizedSection())
                {
                    if (!runtime.TryGetExecutingOperation(out ControlledOperation current))
                    {
                        runtime.NotifyUncontrolledSynchronizationInvocation("SemaphoreSlim.Release");
                    }

                    // If the release count would result exceeding the maximum count, throw an exception.
                    if (releaseCount > this.MaxCount - this.LockCount)
                    {
                        throw new SystemThreading.SemaphoreFullException();
                    }

                    int previousCount = this.LockCount;
                    int lockCount = previousCount + releaseCount;

                    // Release the next synchronous awaiters, if there are any.
                    while (releaseCount > 0 && this.PausedOperations.Count > 0)
                    {
                        // Release the next operation awaiting synchronously, but do not decrement any counts,
                        // as it is not guaranteed that it will be able to acquire the semaphore immediately.
                        ControlledOperation operation = this.PausedOperations.Dequeue();
                        operation.TryEnable(this.ResourceId);
                    }

                    // Release the next asynchronous awaiters, if there are any.
                    while (releaseCount > 0 && this.AsyncAwaiters.Count > 0)
                    {
                        // Release the next operation awaiting asynchronously. It is assumed that the operation
                        // will acquire the semaphore immediately, so we decrement the lock count.
                        lockCount--;
                        releaseCount--;

                        var tcs = this.AsyncAwaiters.Dequeue();
                        tcs.SetResult(true);
                    }

                    this.LockCount = lockCount;
                    return previousCount;
                }
            }

            /// <summary>
            /// Returns the current runtime, asserting that it is the same runtime that created this resource.
            /// </summary>
            private CoyoteRuntime GetRuntime()
            {
                var runtime = CoyoteRuntime.Current;
                if (runtime.Id != this.RuntimeId)
                {
                    var trace = new StackTrace();
                    runtime.NotifyAssertionFailure($"Accessing '{this.DebugName}' that was created in a " +
                        $"previous test iteration with runtime id '{this.RuntimeId}':\n{trace}");
                }

                return runtime;
            }
        }
    }
}
