// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Coyote.Runtime;
using SystemThread = System.Threading.Thread;
using SystemThreading = System.Threading;

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
                Sleep(current, runtime, millisecondsTimeout);
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
                Sleep(current, runtime, (int)timeout.TotalMilliseconds);
            }
            else
            {
                Thread.Sleep(timeout);
            }
        }

        /// <summary>
        /// Sleeps the current controlled operation for the specified amount of time.
        /// </summary>
        private static void Sleep(ControlledOperation op, CoyoteRuntime runtime, int millisecondsTimeout)
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
    }
}
