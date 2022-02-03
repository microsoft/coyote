// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.Runtime.CompilerServices;
using SystemCancellationToken = System.Threading.CancellationToken;
using SystemTask = System.Threading.Tasks.Task;
using SystemTaskContinuationOptions = System.Threading.Tasks.TaskContinuationOptions;
using SystemTaskCreationOptions = System.Threading.Tasks.TaskCreationOptions;
using SystemTasks = System.Threading.Tasks;
using SystemValueTask = System.Threading.Tasks.ValueTask;

namespace Microsoft.Coyote.Rewriting.Types.Threading.Tasks
{
    /// <summary>
    /// Provides methods for creating value tasks that can be controlled during testing.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static class ValueTask
    {
#if NET
        /// <summary>
        /// Gets a value task that has already completed successfully.
        /// </summary>
        public static SystemValueTask CompletedTask { get; } = SystemValueTask.CompletedTask;

        /// <summary>
        /// Creates a value task that has completed successfully with the specified result.
        /// </summary>
        public static SystemTasks.ValueTask<TResult> FromResult<TResult>(TResult result) =>
            SystemValueTask.FromResult(result);

        /// <summary>
        /// Creates a value task that has completed due to cancellation with the specified cancellation token.
        /// </summary>
        public static SystemValueTask FromCanceled(SystemCancellationToken cancellationToken) =>
            SystemValueTask.FromCanceled(cancellationToken);

        /// <summary>
        /// Creates a value task that has completed due to cancellation with the specified cancellation token.
        /// </summary>
        public static SystemTasks.ValueTask<TResult> FromCanceled<TResult>(SystemCancellationToken cancellationToken) =>
            SystemValueTask.FromCanceled<TResult>(cancellationToken);

        /// <summary>
        /// Creates a value task that has completed with the specified exception.
        /// </summary>
        public static SystemValueTask FromException(Exception exception) => SystemValueTask.FromException(exception);

        /// <summary>
        /// Creates a value task that has completed with the specified exception.
        /// </summary>
        public static SystemTasks.ValueTask<TResult> FromException<TResult>(Exception exception) =>
            SystemValueTask.FromException<TResult>(exception);
#endif

        /// <summary>
        /// Retrieves a task object that represents this value task.
        /// </summary>
        public static SystemTask AsTask(in SystemValueTask task) => task.AsTask();

        /// <summary>
        /// Returns a value task awaiter for the specified task.
        /// </summary>
        public static ValueTaskAwaiter GetAwaiter(ref SystemValueTask task) => new ValueTaskAwaiter(ref task);

        /// <summary>
        /// Configures an awaiter used to await this task.
        /// </summary>
        public static ConfiguredValueTaskAwaitable ConfigureAwait(
            ref SystemValueTask task, bool continueOnCapturedContext) =>
            new ConfiguredValueTaskAwaitable(ref task, continueOnCapturedContext);
    }

    /// <summary>
    /// Provides methods for creating generic value tasks that can be controlled during testing.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static class ValueTask<TResult>
    {
#pragma warning disable CA1000 // Do not declare static members on generic types
        /// <summary>
        /// The default generic task factory.
        /// </summary>
        private static SystemTasks.TaskFactory<TResult> DefaultFactory = new SystemTasks.TaskFactory<TResult>();

        /// <summary>
        /// Provides access to factory methods for creating controlled generic task instances.
        /// </summary>
        public static SystemTasks.TaskFactory<TResult> Factory
        {
            get
            {
                var runtime = CoyoteRuntime.Current;
                if (runtime.SchedulingPolicy is SchedulingPolicy.None)
                {
                    return DefaultFactory;
                }

                // TODO: cache this per runtime.
                return new SystemTasks.TaskFactory<TResult>(SystemCancellationToken.None,
                    SystemTaskCreationOptions.HideScheduler, SystemTaskContinuationOptions.HideScheduler,
                    runtime.ControlledTaskScheduler);
            }
        }

        /// <summary>
        /// Gets the result value of the specified generic task.
        /// </summary>
#pragma warning disable CA1707 // Remove the underscores from member name
#pragma warning disable SA1300 // Element should begin with an uppercase letter
#pragma warning disable IDE1006 // Naming Styles
        public static TResult get_Result(ref SystemTasks.ValueTask<TResult> task)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy != SchedulingPolicy.None &&
                ValueTaskAwaiter.TryGetTask<TResult>(ref task, out SystemTasks.Task<TResult> innerTask))
            {
                runtime.WaitUntilTaskCompletes(innerTask);
            }

            return task.Result;
        }
#pragma warning restore CA1707 // Remove the underscores from member name
#pragma warning restore SA1300 // Element should begin with an uppercase letter
#pragma warning restore IDE1006 // Naming Styles

        /// <summary>
        /// Retrieves a task object that represents this value task.
        /// </summary>
        public static SystemTasks.Task<TResult> AsTask(in SystemTasks.ValueTask<TResult> task) => task.AsTask();

        /// <summary>
        /// Returns a generic task awaiter for the specified generic task.
        /// </summary>
        public static ValueTaskAwaiter<TResult> GetAwaiter(ref SystemTasks.ValueTask<TResult> task) =>
            new ValueTaskAwaiter<TResult>(ref task);

        /// <summary>
        /// Configures an awaiter used to await this task.
        /// </summary>
        public static ConfiguredValueTaskAwaitable<TResult> ConfigureAwait(
            ref SystemTasks.ValueTask<TResult> task, bool continueOnCapturedContext) =>
            new ConfiguredValueTaskAwaitable<TResult>(ref task, continueOnCapturedContext);
#pragma warning restore CA1000 // Do not declare static members on generic types
    }
}
