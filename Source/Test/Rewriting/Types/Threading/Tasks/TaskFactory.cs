// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Coyote.Runtime;

using SystemCancellationToken = System.Threading.CancellationToken;
using SystemTask = System.Threading.Tasks.Task;
using SystemTaskContinuationOptions = System.Threading.Tasks.TaskContinuationOptions;
using SystemTaskCreationOptions = System.Threading.Tasks.TaskCreationOptions;
using SystemTaskFactory = System.Threading.Tasks.TaskFactory;
using SystemTasks = System.Threading.Tasks;
using SystemTaskScheduler = System.Threading.Tasks.TaskScheduler;

namespace Microsoft.Coyote.Rewriting.Types.Threading.Tasks
{
#pragma warning disable CA1068 // CancellationToken parameters must come last
#pragma warning disable CA2008 // Do not create tasks without passing a TaskScheduler
    /// <summary>
    /// Provides support for creating and scheduling controlled task objects.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static class TaskFactory
    {
#pragma warning disable CA1707 // Remove the underscores from member name
#pragma warning disable SA1300 // Element should begin with an uppercase letter
#pragma warning disable IDE1006 // Naming Styles
        /// <summary>
        /// The default task continuation options for this task factory.
        /// </summary>
        public static SystemTaskContinuationOptions get_ContinuationOptions(SystemTaskFactory factory)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.None)
            {
                return factory.ContinuationOptions;
            }

            return runtime.TaskFactory.ContinuationOptions;
        }

        /// <summary>
        /// The default task cancellation token for this task factory.
        /// </summary>
        public static SystemCancellationToken get_CancellationToken(SystemTaskFactory factory)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.None)
            {
                return factory.CancellationToken;
            }

            return runtime.TaskFactory.CancellationToken;
        }

        /// <summary>
        /// The default task creation options for this task factory.
        /// </summary>
        public static SystemTaskCreationOptions get_CreationOptions(SystemTaskFactory factory)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.None)
            {
                return factory.CreationOptions;
            }

            return runtime.TaskFactory.CreationOptions;
        }

        /// <summary>
        /// The default task scheduler for this task factory.
        /// </summary>
        public static SystemTaskScheduler get_Scheduler(SystemTaskFactory factory)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.None)
            {
                return factory.Scheduler;
            }

            return runtime.TaskFactory.Scheduler;
        }
#pragma warning restore CA1707 // Remove the underscores from member name
#pragma warning restore SA1300 // Element should begin with an uppercase letter
#pragma warning restore IDE1006 // Naming Styles

        /// <summary>
        /// Creates and starts a task.
        /// </summary>
        public static SystemTask StartNew(SystemTaskFactory factory, Action action) =>
            StartNew(factory, action, SystemCancellationToken.None);

        /// <summary>
        /// Creates and starts a task.
        /// </summary>
        public static SystemTask StartNew(SystemTaskFactory factory, Action action, SystemCancellationToken cancellationToken)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.None)
            {
                return factory.StartNew(action, cancellationToken);
            }

            return runtime.TaskFactory.StartNew(action, cancellationToken,
                runtime.TaskFactory.CreationOptions, runtime.TaskFactory.Scheduler);
        }

        /// <summary>
        /// Creates and starts a task.
        /// </summary>
        public static SystemTask StartNew(SystemTaskFactory factory, Action<object> action, object state) =>
            StartNew(factory, action, state, SystemCancellationToken.None);

        /// <summary>
        /// Creates and starts a task.
        /// </summary>
        public static SystemTask StartNew(SystemTaskFactory factory, Action<object> action, object state,
            SystemCancellationToken cancellationToken)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.None)
            {
                return factory.StartNew(action, state, cancellationToken);
            }

            return runtime.TaskFactory.StartNew(action, state, cancellationToken,
                runtime.TaskFactory.CreationOptions, runtime.TaskFactory.Scheduler);
        }

        /// <summary>
        /// Creates and starts a generic task.
        /// </summary>
        public static SystemTasks.Task<TResult> StartNew<TResult>(SystemTaskFactory factory, Func<TResult> function) =>
            StartNew(factory, function, SystemCancellationToken.None);

        /// <summary>
        /// Creates and starts a generic task.
        /// </summary>
        public static SystemTasks.Task<TResult> StartNew<TResult>(SystemTaskFactory factory, Func<TResult> function,
            SystemCancellationToken cancellationToken)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.None)
            {
                return factory.StartNew(function, cancellationToken);
            }

            return runtime.TaskFactory.StartNew(function, cancellationToken,
                runtime.TaskFactory.CreationOptions, runtime.TaskFactory.Scheduler);
        }

        /// <summary>
        /// Creates and starts a generic task.
        /// </summary>
        public static SystemTasks.Task<TResult> StartNew<TResult>(SystemTaskFactory factory,
            Func<object, TResult> function, object state) =>
            StartNew(factory, function, state, SystemCancellationToken.None);

        /// <summary>
        /// Creates and starts a generic task.
        /// </summary>
        public static SystemTasks.Task<TResult> StartNew<TResult>(SystemTaskFactory factory,
            Func<object, TResult> function, object state, SystemCancellationToken cancellationToken)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.None)
            {
                return factory.StartNew(function, state, cancellationToken);
            }

            return runtime.TaskFactory.StartNew(function, state, cancellationToken,
                runtime.TaskFactory.CreationOptions, runtime.TaskFactory.Scheduler);
        }
    }

    /// <summary>
    /// Provides support for creating and scheduling controlled generic task objects.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static class TaskFactory<TResult>
    {
#pragma warning disable CA1000 // Do not declare static members on generic types
#pragma warning disable CA1707 // Remove the underscores from member name
#pragma warning disable SA1300 // Element should begin with an uppercase letter
#pragma warning disable IDE1006 // Naming Styles
        /// <summary>
        /// The default task continuation options for this task factory.
        /// </summary>
        public static SystemTaskContinuationOptions get_ContinuationOptions(SystemTasks.TaskFactory<TResult> factory)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.None)
            {
                return factory.ContinuationOptions;
            }

            return runtime.TaskFactory.ContinuationOptions;
        }

        /// <summary>
        /// The default task cancellation token for this task factory.
        /// </summary>
        public static SystemCancellationToken get_CancellationToken(SystemTasks.TaskFactory<TResult> factory)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.None)
            {
                return factory.CancellationToken;
            }

            return runtime.TaskFactory.CancellationToken;
        }

        /// <summary>
        /// The default task creation options for this task factory.
        /// </summary>
        public static SystemTaskCreationOptions get_CreationOptions(SystemTasks.TaskFactory<TResult> factory)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.None)
            {
                return factory.CreationOptions;
            }

            return runtime.TaskFactory.CreationOptions;
        }

        /// <summary>
        /// The default task scheduler for this task factory.
        /// </summary>
        public static SystemTaskScheduler get_Scheduler(SystemTasks.TaskFactory<TResult> factory)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.None)
            {
                return factory.Scheduler;
            }

            return runtime.TaskFactory.Scheduler;
        }
#pragma warning restore CA1707 // Remove the underscores from member name
#pragma warning restore SA1300 // Element should begin with an uppercase letter
#pragma warning restore IDE1006 // Naming Styles

        /// <summary>
        /// Creates and starts a task.
        /// </summary>
        public static SystemTasks.Task<TResult> StartNew(SystemTasks.TaskFactory<TResult> factory, Func<TResult> function) =>
            StartNew(factory, function, SystemCancellationToken.None);

        /// <summary>
        /// Creates and starts a task.
        /// </summary>
        public static SystemTasks.Task<TResult> StartNew(SystemTasks.TaskFactory<TResult> factory,
            Func<TResult> function, SystemCancellationToken cancellationToken)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.None)
            {
                return factory.StartNew(function, cancellationToken);
            }

            return runtime.TaskFactory.StartNew(function, cancellationToken,
                runtime.TaskFactory.CreationOptions, runtime.TaskFactory.Scheduler);
        }

        /// <summary>
        /// Creates and starts a task.
        /// </summary>
        public static SystemTasks.Task<TResult> StartNew(SystemTasks.TaskFactory<TResult> factory,
            Func<object, TResult> function, object state) =>
            StartNew(factory, function, state, SystemCancellationToken.None);

        /// <summary>
        /// Creates and starts a task.
        /// </summary>
        public static SystemTasks.Task<TResult> StartNew(SystemTasks.TaskFactory<TResult> factory,
            Func<object, TResult> function, object state, SystemCancellationToken cancellationToken)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.None)
            {
                return factory.StartNew(function, state, cancellationToken);
            }

            return runtime.TaskFactory.StartNew(function, state, cancellationToken,
                runtime.TaskFactory.CreationOptions, runtime.TaskFactory.Scheduler);
        }
#pragma warning restore CA1000 // Do not declare static members on generic types
    }
#pragma warning restore CA1068 // CancellationToken parameters must come last
#pragma warning restore CA2008 // Do not create tasks without passing a TaskScheduler
}
