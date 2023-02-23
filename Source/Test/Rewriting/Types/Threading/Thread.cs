// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Coyote.Runtime;
using SystemThread = System.Threading.Thread;
using SystemThreading = System.Threading;
using SystemTimeout = System.Threading.Timeout;

namespace Microsoft.Coyote.Rewriting.Types.Threading
{
    /// <summary>
    /// Provides methods for creating threads that can be controlled during testing.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static class Thread
    {
        /// <summary>
        /// Initializes a new thread instance.
        /// </summary>
        public static SystemThread Create(SystemThreading.ThreadStart start) => Create(start, 0);

        /// <summary>
        /// Initializes a new thread instance, specifying the maximum stack size for the thread.
        /// </summary>
        public static SystemThread Create(SystemThreading.ThreadStart start, int maxStackSize)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.None)
            {
                return new SystemThread(start);
            }

            SystemThread thread = runtime.Schedule(start, maxStackSize);
            return thread ?? new SystemThread(() => { });
        }

        /// <summary>
        /// Initializes a new thread instance, specifying a delegate that allows an object
        /// to be passed to the thread when the thread is started and specifying the maximum
        /// stack size for the thread.
        /// </summary>
        public static SystemThread Create(SystemThreading.ParameterizedThreadStart start) => Create(start, 0);

        /// <summary>
        /// Initializes a new thread instance, specifying a delegate that allows an object
        /// to be passed to the thread when the thread is started.
        /// </summary>
        public static SystemThread Create(SystemThreading.ParameterizedThreadStart start, int maxStackSize)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.None)
            {
                return new SystemThread(start);
            }

            SystemThread thread = runtime.Schedule(start, maxStackSize);
            return thread ?? new SystemThread(() => { });
        }

        /// <summary>
        /// Causes the operating system to change the state of the current instance to <see cref="SystemThreading.ThreadState.Running"/>.
        /// </summary>
        public static void Start(SystemThread instance)
        {
            instance.Start();

            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy != SchedulingPolicy.None)
            {
                runtime.ScheduleNextOperation(default, SchedulingPointType.Create);
            }
        }

        /// <summary>
        /// Causes the operating system to change the state of the current instance to <see cref="SystemThreading.ThreadState.Running"/>
        /// and optionally supplies an object containing data to be used by the method the thread executes.
        /// </summary>
        public static void Start(SystemThread instance, object parameter)
        {
            instance.Start(parameter);

            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy != SchedulingPolicy.None)
            {
                runtime.ScheduleNextOperation(default, SchedulingPointType.Create);
            }
        }

        /// <summary>
        /// Suspends the current thread for the specified number of milliseconds.
        /// </summary>
        public static void Sleep(int millisecondsTimeout)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.Interleaving &&
                runtime.TryGetExecutingOperation(out ControlledOperation current))
            {
                DelayCurrentOperation(current, runtime, millisecondsTimeout);
            }
            else
            {
                Thread.Sleep(millisecondsTimeout);
            }
        }

        /// <summary>
        /// Suspends the current thread for the specified amount of time.
        /// </summary>
        public static void Sleep(TimeSpan timeout)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.Interleaving &&
                runtime.TryGetExecutingOperation(out ControlledOperation current))
            {
                DelayCurrentOperation(current, runtime, (int)timeout.TotalMilliseconds);
            }
            else
            {
                Thread.Sleep(timeout);
            }
        }

        /// <summary>
        /// Causes the current thread to wait the number of times defined by the iterations parameter.
        /// </summary>
        public static void SpinWait(int iterations)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.Interleaving &&
                runtime.TryGetExecutingOperation(out ControlledOperation current))
            {
                // We model 'SpinWait' by delaying the current operation, similar to 'Sleep'.
                DelayCurrentOperation(current, runtime, iterations);
            }
            else
            {
                Thread.SpinWait(iterations);
            }
        }

        /// <summary>
        /// Delays the currently executing controlled operation for the specified amount of time.
        /// </summary>
        private static void DelayCurrentOperation(ControlledOperation op, CoyoteRuntime runtime, int millisecondsTimeout)
        {
            if (millisecondsTimeout is 0)
            {
                return;
            }

            uint timeout = (uint)runtime.GetNextNondeterministicIntegerChoice((int)runtime.Configuration.TimeoutDelay, null, null);
            if (timeout is 0)
            {
                return;
            }

            op.PauseWithDelay(timeout);
            runtime.ScheduleNextOperation(op, SchedulingPointType.Yield);
        }

        /// <summary>
        /// Causes the calling thread to yield execution to another thread that is ready
        /// to run on the current processor.
        /// </summary>
        public static bool Yield()
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.Interleaving &&
                runtime.TryGetExecutingOperation(out ControlledOperation current))
            {
                return runtime.ScheduleNextOperation(current, SchedulingPointType.Yield, isYielding: true);
            }

            return SystemThread.Yield();
        }

        /// <summary>
        /// Blocks the calling thread until the thread represented by this instance terminates.
        /// </summary>
        public static void Join(SystemThread instance) => Join(instance, SystemTimeout.Infinite);

        /// <summary>
        /// Blocks the calling thread until the thread represented by this instance terminates or the specified time elapses.
        /// </summary>
        public static bool Join(SystemThread instance, int millisecondsTimeout)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.Interleaving &&
                runtime.TryGetExecutingOperation(out ControlledOperation current))
            {
                return PauseUntilThreadCompletes(current, runtime, instance, millisecondsTimeout);
            }

            return instance.Join(millisecondsTimeout);
        }

        /// <summary>
        /// Blocks the calling thread until the thread represented by this instance terminates or the specified time elapses.
        /// </summary>
        public static bool Join(SystemThread instance, TimeSpan timeout)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.Interleaving &&
                runtime.TryGetExecutingOperation(out ControlledOperation current))
            {
                return PauseUntilThreadCompletes(current, runtime, instance, (int)timeout.TotalMilliseconds);
            }

            return instance.Join(timeout);
        }

        /// <summary>
        /// Pauses the currently executing controlled operation until the specified thread completes or the specified time elapses.
        /// </summary>
        private static bool PauseUntilThreadCompletes(ControlledOperation current, CoyoteRuntime runtime,
            SystemThread thread, int millisecondsTimeout)
        {
            // TODO: support timeouts during testing.
            millisecondsTimeout = SystemTimeout.Infinite;
            bool isThreadUncontrolled = runtime.CheckIfAwaitedThreadIsUncontrolled(thread);
            ControlledOperation threadOp = isThreadUncontrolled ? null : runtime.GetOperationExecutingOnThread(thread);
            Func<bool> condition = threadOp is null ? () => thread.Join(0) : (Func<bool>)(() => threadOp.Status is OperationStatus.Completed);
            runtime.PauseOperationUntil(current, condition, !isThreadUncontrolled, $"thread '{thread.ManagedThreadId}' to complete");
            return true;
        }
    }
}
