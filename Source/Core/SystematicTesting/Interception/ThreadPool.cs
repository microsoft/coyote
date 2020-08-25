// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Coyote.Runtime;
using SystemThreading = System.Threading;

namespace Microsoft.Coyote.SystematicTesting.Interception
{
    /// <summary>
    /// Provides a controlled pool of threads that can be used to execute tasks, post work items,
    /// process asynchronous I/O, wait on behalf of other threads, and process timers.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static class ThreadPool
    {
        // Note: we are only intercepting and modeling a very limited set of APIs to enable specific scenarios such
        // as ASP.NET rewriting. Most `ThreadPool` APIs are not supported by our modeling, and we do not currently
        // aim to support user applications with code that explicitly uses the `ThreadPool`.

        /// <summary>
        /// Retrieves the difference between the maximum number of thread pool threads returned by the
        /// <see cref="ThreadPool.GetMaxThreads"/> method, and the number currently active.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GetAvailableThreads(out int workerThreads, out int completionPortThreads) =>
            SystemThreading.ThreadPool.GetAvailableThreads(out workerThreads, out completionPortThreads);

        /// <summary>
        /// Retrieves the number of requests to the thread pool that can be active concurrently. All requests above
        /// that number remain queued until thread pool threads become available.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GetMaxThreads(out int workerThreads, out int completionPortThreads) =>
            SystemThreading.ThreadPool.GetMaxThreads(out workerThreads, out completionPortThreads);

        /// <summary>
        /// Retrieves the minimum number of threads the thread pool creates on demand, as new requests are made,
        /// before switching to an algorithm for managing thread creation and destruction.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GetMinThreads(out int workerThreads, out int completionPortThreads) =>
            SystemThreading.ThreadPool.GetMinThreads(out workerThreads, out completionPortThreads);

        /// <summary>
        /// Sets the number of requests to the thread pool that can be active concurrently. All requests
        /// above that number remain queued until thread pool threads become available.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool SetMaxThreads(int workerThreads, int completionPortThreads) =>
            SystemThreading.ThreadPool.SetMaxThreads(workerThreads, completionPortThreads);

        /// <summary>
        /// Sets the minimum number of threads the thread pool creates on demand, as new requests are made,
        /// before switching to an algorithm for managing thread creation and destruction.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool SetMinThreads(int workerThreads, int completionPortThreads) =>
            SystemThreading.ThreadPool.SetMinThreads(workerThreads, completionPortThreads);

        /// <summary>
        /// Queues a method for execution. The method executes when a thread pool thread becomes available.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool QueueUserWorkItem(WaitCallback callBack) => QueueUserWorkItem(callBack, null);

        /// <summary>
        /// Queues a method for execution. The method executes when a thread pool thread becomes available.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool QueueUserWorkItem(WaitCallback callBack, object state)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                // TODO: check if we need to capture the execution context.

                // We set `failException` to true to mimic the production behavior of treating an unhandled
                // exception as an error that crashes the application.
                ControlledRuntime.Current.TaskController.ScheduleAction(() => callBack(state), null, false, true, default);
                return true;
            }

            return SystemThreading.ThreadPool.QueueUserWorkItem(callBack, state);
        }

#if NET5_0 || NETCOREAPP3_1 || NETSTANDARD2_1
        /// <summary>
        /// Queues a method specified by an <see cref="Action{T}"/> delegate for execution, and provides data
        /// to be used by the method. The method executes when a thread pool thread becomes available.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool QueueUserWorkItem<TState>(Action<TState> callBack, TState state, bool preferLocal)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                // TODO: check if we need to capture the execution context.

                // We set `failException` to true to mimic the production behavior of treating an unhandled
                // exception as an error that crashes the application.
                ControlledRuntime.Current.TaskController.ScheduleAction(() => callBack(state), null, false, true, default);
                return true;
            }

            return SystemThreading.ThreadPool.QueueUserWorkItem(callBack, state, preferLocal);
        }
#endif

        /// <summary>
        /// Queues the specified delegate to the thread pool, but does not propagate the calling stack to the worker thread.
        /// </summary>
        public static bool UnsafeQueueUserWorkItem(WaitCallback callBack, object state)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                // TODO: check if we need to capture the execution context.

                // We set `failException` to true to mimic the production behavior of treating an unhandled
                // exception as an error that crashes the application.
                ControlledRuntime.Current.TaskController.ScheduleAction(() => callBack(state), null, false, true, default);
                return true;
            }

            return SystemThreading.ThreadPool.UnsafeQueueUserWorkItem(callBack, state);
        }

#if NET5_0 || NETCOREAPP3_1
        /// <summary>
        /// Queues the specified work item object to the thread pool.
        /// </summary>
        public static bool UnsafeQueueUserWorkItem(IThreadPoolWorkItem callBack, bool preferLocal)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                // TODO: check if we need to capture the execution context.

                // We set `failException` to true to mimic the production behavior of treating an unhandled
                // exception as an error that crashes the application.
                ControlledRuntime.Current.TaskController.ScheduleAction(() => callBack.Execute(), null, false, true, default);
                return true;
            }

            return SystemThreading.ThreadPool.UnsafeQueueUserWorkItem(callBack, preferLocal);
        }

        /// <summary>
        /// Queues a method specified by an <see cref="Action{T}"/> delegate for execution, and provides data
        /// to be used by the method. The method executes when a thread pool thread becomes available.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool UnsafeQueueUserWorkItem<TState>(Action<TState> callBack, TState state, bool preferLocal)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                // TODO: check if we need to capture the execution context.

                // We set `failException` to true to mimic the production behavior of treating an unhandled
                // exception as an error that crashes the application.
                ControlledRuntime.Current.TaskController.ScheduleAction(() => callBack(state), null, false, true, default);
                return true;
            }

            return SystemThreading.ThreadPool.UnsafeQueueUserWorkItem(callBack, state, preferLocal);
        }
#endif

        /// <summary>
        /// Binds an operating system handle to the <see cref="ThreadPool"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool BindHandle(SafeHandle osHandle) =>
            CoyoteRuntime.IsExecutionControlled ?
            throw new NotSupportedException($"{nameof(SystemThreading.ThreadPool.BindHandle)} is not supported during systematic testing.") :
            SystemThreading.ThreadPool.BindHandle(osHandle);

        /// <summary>
        /// Registers a delegate to wait for a <see cref="WaitHandle"/>, specifying a <see cref="TimeSpan"/> for
        /// the time-out.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RegisteredWaitHandle RegisterWaitForSingleObject(WaitHandle waitObject, WaitOrTimerCallback callBack,
            object state, TimeSpan timeout, bool executeOnlyOnce) =>
            CoyoteRuntime.IsExecutionControlled ?
            throw new NotSupportedException($"{nameof(SystemThreading.ThreadPool.RegisterWaitForSingleObject)} is not supported during systematic testing.") :
            SystemThreading.ThreadPool.RegisterWaitForSingleObject(waitObject, callBack, state, timeout, executeOnlyOnce);

        /// <summary>
        /// Registers a delegate to wait for a <see cref="WaitHandle"/>, specifying a 32-bit signed integer for
        /// the time-out in milliseconds.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RegisteredWaitHandle RegisterWaitForSingleObject(WaitHandle waitObject, WaitOrTimerCallback callBack,
            object state, int millisecondsTimeOutInterval, bool executeOnlyOnce) =>
            CoyoteRuntime.IsExecutionControlled ?
            throw new NotSupportedException($"{nameof(SystemThreading.ThreadPool.RegisterWaitForSingleObject)} is not supported during systematic testing.") :
            SystemThreading.ThreadPool.RegisterWaitForSingleObject(waitObject, callBack, state, millisecondsTimeOutInterval, executeOnlyOnce);

        /// <summary>
        /// Registers a delegate to wait for a <see cref="WaitHandle"/>, specifying a 64-bit signed integer for
        /// the time-out in milliseconds.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RegisteredWaitHandle RegisterWaitForSingleObject(WaitHandle waitObject, WaitOrTimerCallback callBack,
            object state, long millisecondsTimeOutInterval, bool executeOnlyOnce) =>
            CoyoteRuntime.IsExecutionControlled ?
            throw new NotSupportedException($"{nameof(SystemThreading.ThreadPool.RegisterWaitForSingleObject)} is not supported during systematic testing.") :
            SystemThreading.ThreadPool.RegisterWaitForSingleObject(waitObject, callBack, state, millisecondsTimeOutInterval, executeOnlyOnce);

        /// <summary>
        /// Registers a delegate to wait for a <see cref="WaitHandle"/>, specifying a 32-bit unsigned integer for
        /// the time-out in milliseconds.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RegisteredWaitHandle RegisterWaitForSingleObject(WaitHandle waitObject, WaitOrTimerCallback callBack,
            object state, uint millisecondsTimeOutInterval, bool executeOnlyOnce) =>
            CoyoteRuntime.IsExecutionControlled ?
            throw new NotSupportedException($"{nameof(SystemThreading.ThreadPool.RegisterWaitForSingleObject)} is not supported during systematic testing.") :
            SystemThreading.ThreadPool.RegisterWaitForSingleObject(waitObject, callBack, state, millisecondsTimeOutInterval, executeOnlyOnce);

        /// <summary>
        /// Registers a delegate to wait for a <see cref="WaitHandle"/>, specifying a <see cref="TimeSpan"/> for
        /// the time-out. This method does not propagate the calling stack to the worker thread.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RegisteredWaitHandle UnsafeRegisterWaitForSingleObject(WaitHandle waitObject, WaitOrTimerCallback callBack,
            object state, TimeSpan timeout, bool executeOnlyOnce) =>
            CoyoteRuntime.IsExecutionControlled ?
            throw new NotSupportedException($"{nameof(SystemThreading.ThreadPool.UnsafeRegisterWaitForSingleObject)} is not supported during systematic testing.") :
            SystemThreading.ThreadPool.UnsafeRegisterWaitForSingleObject(waitObject, callBack, state, timeout, executeOnlyOnce);

        /// <summary>
        /// Registers a delegate to wait for a <see cref="WaitHandle"/>, specifying a 32-bit signed integer for
        /// the time-out in milliseconds. This method does not propagate the calling stack to the worker thread.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RegisteredWaitHandle UnsafeRegisterWaitForSingleObject(WaitHandle waitObject, WaitOrTimerCallback callBack,
            object state, int millisecondsTimeOutInterval, bool executeOnlyOnce) =>
            CoyoteRuntime.IsExecutionControlled ?
            throw new NotSupportedException($"{nameof(SystemThreading.ThreadPool.UnsafeRegisterWaitForSingleObject)} is not supported during systematic testing.") :
            SystemThreading.ThreadPool.UnsafeRegisterWaitForSingleObject(waitObject, callBack, state, millisecondsTimeOutInterval, executeOnlyOnce);

        /// <summary>
        /// Registers a delegate to wait for a <see cref="WaitHandle"/>, specifying a 64-bit signed integer for
        /// the time-out in milliseconds. This method does not propagate the calling stack to the worker thread.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RegisteredWaitHandle UnsafeRegisterWaitForSingleObject(WaitHandle waitObject, WaitOrTimerCallback callBack,
            object state, long millisecondsTimeOutInterval, bool executeOnlyOnce) =>
            CoyoteRuntime.IsExecutionControlled ?
            throw new NotSupportedException($"{nameof(SystemThreading.ThreadPool.UnsafeRegisterWaitForSingleObject)} is not supported during systematic testing.") :
            SystemThreading.ThreadPool.UnsafeRegisterWaitForSingleObject(waitObject, callBack, state, millisecondsTimeOutInterval, executeOnlyOnce);

        /// <summary>
        /// Registers a delegate to wait for a <see cref="WaitHandle"/>, specifying a 32-bit unsigned integer for
        /// the time-out in milliseconds. This method does not propagate the calling stack to the worker thread.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RegisteredWaitHandle UnsafeRegisterWaitForSingleObject(WaitHandle waitObject, WaitOrTimerCallback callBack,
            object state, uint millisecondsTimeOutInterval, bool executeOnlyOnce) =>
            CoyoteRuntime.IsExecutionControlled ?
            throw new NotSupportedException($"{nameof(SystemThreading.ThreadPool.UnsafeRegisterWaitForSingleObject)} is not supported during systematic testing.") :
            SystemThreading.ThreadPool.UnsafeRegisterWaitForSingleObject(waitObject, callBack, state, millisecondsTimeOutInterval, executeOnlyOnce);
    }
}
