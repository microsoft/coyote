// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

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
        /// Causes the calling thread to yield execution to another thread that is ready
        /// to run on the current processor.
        /// </summary>
        public static bool Yield()
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.Interleaving &&
                runtime.TryGetExecutingOperation(out ControlledOperation current))
            {
                return runtime.ScheduleNextOperation(current, SchedulingPointType.Yield, true, true);
            }

            return SystemThread.Yield();
        }
    }
}
