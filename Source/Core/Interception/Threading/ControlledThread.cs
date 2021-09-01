// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Coyote.Runtime;

namespace Microsoft.Coyote.Interception
{
    /// <summary>
    /// Provides methods for creating threads that can be controlled during testing.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static class ControlledThread
    {
        private static readonly ConcurrentDictionary<Thread, Task> ThreadTasks = new ConcurrentDictionary<Thread, Task>();

        /// <summary>
        /// This iks called after each test iteration is complete just so this dictionary doesn't get too big with junk threads.
        /// </summary>
        internal static void ClearCache()
        {
            ThreadTasks.Clear();
        }

        /// <summary>
        /// Initializes a new instance of the System.Threading.Thread class.
        /// </summary>
        /// <param name="start">A System.Threading.ThreadStart delegate that represents the methods to be invoked
        /// when this thread begins executing.</param>
        public static Thread Create(ThreadStart start)
        {
            // if (CoyoteRuntime.IsExecutionControlled)
            // {
            //     return new Thread(() =>
            //     {
            //         ScheduleAction(() =>
            //         {
            //             try
            //             {
            //                 start();
            //             }
            //             catch (ExecutionCanceledException)
            //             {
            //                 // this is normal termination of a test iteration, not something to be worried about.
            //             }
            //         });
            //     });
            // }
            // else
            {
                return new Thread(start);
            }
        }

        /// <summary>
        /// Initializes a new instance of the System.Threading.Thread class.
        /// </summary>
        /// <param name="start">A System.Threading.ThreadStart delegate that represents the methods to be invoked
        /// when this thread begins executing.</param>
        /// <param name="maxStackSize">
        /// The maximum stack size, in bytes, to be used by the thread, or 0 to use the default
        /// maximum stack size specified in the header for the executable. Important For
        /// partially trusted code, maxStackSize is ignored if it is greater than the default
        /// stack size. No exception is thrown.</param>
        public static Thread Create(ThreadStart start, int maxStackSize)
        {
            // if (CoyoteRuntime.IsExecutionControlled)
            // {
            //     return new Thread(() =>
            //     {
            //         ScheduleAction(() =>
            //         {
            //             try
            //             {
            //                 start();
            //             }
            //             catch (ExecutionCanceledException)
            //             {
            //                 // this is normal termination of a test iteration, not something to be worried about.
            //             }
            //         });
            //     }, maxStackSize);
            // }
            // else
            {
                return new Thread(start, maxStackSize);
            }
        }

        /// <summary>
        /// Initializes a new instance of the System.Threading.Thread class.
        /// </summary>
        /// <param name="start">A System.Threading.ParameterizedThreadStart delegate that represents the methods
        /// to be invoked when this thread begins executing.</param>
        public static Thread Create(ParameterizedThreadStart start)
        {
            // if (CoyoteRuntime.IsExecutionControlled)
            // {
            //     return new Thread((object parameter) =>
            //     {
            //         ScheduleAction(() =>
            //         {
            //             try
            //             {
            //                 start(parameter);
            //             }
            //             catch (ExecutionCanceledException)
            //             {
            //                 // this is normal termination of a test iteration, not something to be worried about.
            //             }
            //         });
            //     });
            // }
            // else
            {
                return new Thread(start);
            }
        }

        /// <summary>
        /// Initializes a new instance of the System.Threading.Thread class.
        /// </summary>
        /// <param name="start">A System.Threading.ParameterizedThreadStart delegate that represents the methods
        /// to be invoked when this thread begins executing.</param>
        /// <param name="maxStackSize">
        /// The maximum stack size, in bytes, to be used by the thread, or 0 to use the default
        /// maximum stack size specified in the header for the executable. Important For
        /// partially trusted code, maxStackSize is ignored if it is greater than the default
        /// stack size. No exception is thrown.</param>
        public static Thread Create(ParameterizedThreadStart start, int maxStackSize)
        {
            // if (CoyoteRuntime.IsExecutionControlled)
            // {
            //     return new Thread((object parameter) =>
            //     {
            //         ScheduleAction(() =>
            //         {
            //             try
            //             {
            //                 start(parameter);
            //             }
            //             catch (ExecutionCanceledException)
            //             {
            //                 // this is normal termination of a test iteration, not something to be worried about.
            //             }
            //         });
            //     }, maxStackSize);
            // }
            // else
            {
                return new Thread(start, maxStackSize);
            }
        }

        /// <summary>
        /// Schedule a Coyote task to run the action.
        /// </summary>
        /// <param name="action">The action to schedule.</param>
        private static void ScheduleAction(Action action)
        {
            var task = ControlledTask.Run(action);
            ThreadTasks.TryAdd(Thread.CurrentThread, task);
        }

        /// <summary>
        ///  Suspends the current thread for the specified number of milliseconds.
        /// </summary>
        /// <param name="millisecondsTimeout">
        /// The number of milliseconds for which the thread is suspended. If the value of
        /// the millisecondsTimeout argument is zero, the thread relinquishes the remainder
        /// of its time slice to any thread of equal priority that is ready to run. If there
        /// are no other threads of equal priority that are ready to run, execution of the
        /// current thread is not suspended.</param>
        public static void Sleep(int millisecondsTimeout)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                var runtime = CoyoteRuntime.Current;
                runtime.ScheduleDelay(TimeSpan.FromMilliseconds(millisecondsTimeout), CancellationToken.None).Wait();
            }
            else
            {
                Thread.Sleep(millisecondsTimeout);
            }
        }

        /// <summary>
        /// Suspends the current thread for the specified amount of time.
        /// </summary>
        /// <param name="timeout">
        /// The amount of time for which the thread is suspended. If the value of the millisecondsTimeout
        /// argument is System.TimeSpan.Zero, the thread relinquishes the remainder of its
        /// time slice to any thread of equal priority that is ready to run. If there are
        /// no other threads of equal priority that are ready to run, execution of the current
        /// thread is not suspended.</param>
        public static void Sleep(TimeSpan timeout)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                var runtime = CoyoteRuntime.Current;
                runtime.ScheduleDelay(timeout, CancellationToken.None).Wait();
            }
            else
            {
                Thread.Sleep(timeout);
            }
        }

        /// <summary>
        /// Causes a thread to wait the number of times defined by the iterations parameter.
        /// </summary>
        /// <param name="iterations">
        /// A 32-bit signed integer that defines how long a thread is to wait.</param>
        public static void SpinWait(int iterations)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                var runtime = CoyoteRuntime.Current;
                runtime.ScheduleDelay(TimeSpan.FromMilliseconds(1), CancellationToken.None).Wait();
            }
            else
            {
                Thread.SpinWait(iterations);
            }
        }

        /// <summary>
        /// Causes the calling thread to yield execution to another thread that is ready
        /// to run on the current processor. The operating system selects the thread to yield
        /// to.
        /// </summary>
        public static bool Yield()
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                ControlledTask.Yield().GetAwaiter().GetResult();
                return true;
            }
            else
            {
                return Thread.Yield();
            }
        }

        /// <summary>
        /// Blocks the calling thread until the thread represented by this instance terminates
        /// or the specified time elapses, while continuing to perform standard COM and SendMessage
        /// pumping.
        /// </summary>
        /// <param name="thread">The thread to join.</param>
        /// <param name="timeout">
        /// A System.TimeSpan set to the amount of time to wait for the thread to terminate.</param>
        /// <returns>
        /// true if the thread terminated; false if the thread has not terminated after the
        /// amount of time specified by the timeout parameter has elapsed.
        /// </returns>
        public static bool Join(Thread thread, TimeSpan timeout)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                if (ThreadTasks.TryGetValue(thread, out Task task))
                {
                    ControlledTask.Wait(task, timeout);
                    return thread.Join(timeout);
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return thread.Join(timeout);
            }
        }

        /// <summary>
        /// Blocks the calling thread until the thread represented by this instance terminates
        /// or the specified time elapses, while continuing to perform standard COM and SendMessage
        /// pumping.
        /// </summary>
        /// <param name="thread">The thread to join.</param>
        /// <param name="millisecondsTimeout">
        /// The number of milliseconds to wait for the thread to terminate.</param>
        /// <returns>
        /// true if the thread has terminated; false if the thread has not terminated after
        /// the amount of time specified by the millisecondsTimeout parameter has elapsed.
        /// </returns>
        public static bool Join(Thread thread, int millisecondsTimeout)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                if (ThreadTasks.TryGetValue(thread, out Task task))
                {
                    // TODO: support timeouts and cancellation tokens.
                    ControlledTask.Wait(task, millisecondsTimeout);
                    return thread.Join(millisecondsTimeout);
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return thread.Join(millisecondsTimeout);
            }
        }

        /// <summary>
        /// Blocks the calling thread until the thread represented by this instance terminates,
        /// while continuing to perform standard COM and SendMessage pumping.
        /// </summary>
        /// <param name="thread">The thread to join.</param>
        public static void Join(Thread thread)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                if (ThreadTasks.TryGetValue(thread, out Task task))
                {
                    ControlledTask.Wait(task);
                    thread.Join();
                }
            }
            else
            {
                thread.Join();
            }
        }

        /// <summary>
        /// Causes the operating system to change the state of the current instance to System.Threading.ThreadState.Running,
        /// and optionally supplies an object containing data to be used by the method the
        /// thread executes.
        /// </summary>
        /// <param name="thread">The thread to start.</param>
        /// <param name="parameter">An object that contains data to be used by the method the thread executes.</param>
        public static void Start(Thread thread, object parameter)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                thread.Start(parameter);

                // for Join to work we need to wait for thread to really start.
                while (!ThreadTasks.ContainsKey(thread))
                {
                    Thread.Sleep(1);
                }
            }
            else
            {
                thread.Start(parameter);
            }
        }

        /// <summary>
        /// Causes the operating system to change the state of the current instance to System.Threading.ThreadState.Running.
        /// </summary>
        /// <param name="thread">The thread to start.</param>
        public static void Start(Thread thread)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                thread.Start();

                // for Join to work we need to wait for thread to really start.
                while (!ThreadTasks.ContainsKey(thread))
                {
                    Thread.Sleep(1);
                }
            }
            else
            {
                thread.Start();
            }
        }
    }
}
