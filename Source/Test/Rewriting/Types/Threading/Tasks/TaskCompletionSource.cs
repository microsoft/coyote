// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Coyote.Runtime;

using SystemCancellationToken = System.Threading.CancellationToken;
#if NET
using SystemTask = System.Threading.Tasks.Task;
using SystemTaskCompletionSource = System.Threading.Tasks.TaskCompletionSource;
#endif
using SystemTasks = System.Threading.Tasks;

namespace Microsoft.Coyote.Rewriting.Types.Threading.Tasks
{
#if NET
    /// <summary>
    /// Represents the producer side of a controlled task unbound to a delegate, providing
    /// access to the consumer side through the task property of the task completion source.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static class TaskCompletionSource
    {
#pragma warning disable CA1000 // Do not declare static members on generic types
        /// <summary>
        /// Gets the task created by this task completion source.
        /// </summary>
#pragma warning disable CA1707 // Remove the underscores from member name
#pragma warning disable SA1300 // Element should begin with an uppercase letter
#pragma warning disable IDE1006 // Naming Styles
        public static SystemTask get_Task(SystemTaskCompletionSource tcs)
        {
            var task = tcs.Task;
            CoyoteRuntime.Current?.RegisterKnownControlledTask(task);
            return task;
        }
#pragma warning restore CA1707 // Remove the underscores from member name
#pragma warning restore SA1300 // Element should begin with an uppercase letter
#pragma warning restore IDE1006 // Naming Styles

        /// <summary>
        /// Transitions the underlying task into the <see cref="SystemTasks.TaskStatus.RanToCompletion"/> state.
        /// </summary>
        public static void SetResult(SystemTaskCompletionSource tcs) => tcs.SetResult();

        /// <summary>
        /// Transitions the underlying task into the <see cref="SystemTasks.TaskStatus.Faulted"/> state
        /// and binds a collection of exception objects to it.
        /// </summary>
        public static void SetException(SystemTaskCompletionSource tcs,
            IEnumerable<Exception> exceptions) =>
            tcs.SetException(exceptions);

        /// <summary>
        /// Transitions the underlying task into the <see cref="SystemTasks.TaskStatus.Faulted"/> state
        /// and binds it to a specified exception.
        /// </summary>
        public static void SetException(SystemTaskCompletionSource tcs, Exception exception) =>
            tcs.SetException(exception);

        /// <summary>
        /// Transitions the underlying task into the <see cref="SystemTasks.TaskStatus.Canceled"/> state.
        /// </summary>
        public static void SetCanceled(SystemTaskCompletionSource tcs) => tcs.SetCanceled();

        /// <summary>
        /// Attempts to transition the underlying task into the <see cref="SystemTasks.TaskStatus.RanToCompletion"/> state.
        /// </summary>
        public static bool TrySetResult(SystemTaskCompletionSource tcs) =>
            tcs.TrySetResult();

        /// <summary>
        /// Attempts to transition the underlying task into the <see cref="SystemTasks.TaskStatus.Faulted"/> state
        /// and binds it to a specified exception.
        /// </summary>
        public static bool TrySetException(SystemTaskCompletionSource tcs, Exception exception) =>
            tcs.TrySetException(exception);

        /// <summary>
        /// Attempts to transition the underlying task into the <see cref="SystemTasks.TaskStatus.Faulted"/> state
        /// and binds a collection of exception objects to it.
        /// </summary>
        public static bool TrySetException(SystemTaskCompletionSource tcs, IEnumerable<Exception> exceptions) =>
            tcs.TrySetException(exceptions);

        /// <summary>
        /// Attempts to transition the underlying task into the <see cref="SystemTasks.TaskStatus.Canceled"/> state.
        /// </summary>
        public static bool TrySetCanceled(SystemTaskCompletionSource tcs) => tcs.TrySetCanceled();

        /// <summary>
        /// Attempts to transition the underlying task into the <see cref="SystemTasks.TaskStatus.Canceled"/> state
        /// and enables a cancellation token to be stored in the canceled task.
        /// </summary>
        public static bool TrySetCanceled(SystemTaskCompletionSource tcs, SystemCancellationToken cancellationToken) =>
            tcs.TrySetCanceled(cancellationToken);
#pragma warning restore CA1000 // Do not declare static members on generic types
    }
#endif

    /// <summary>
    /// Represents the producer side of a controlled task unbound to a delegate, providing
    /// access to the consumer side through the task property of the task completion source.
    /// </summary>
    /// <typeparam name="TResult">The type of the result value.</typeparam>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static class TaskCompletionSource<TResult>
    {
#pragma warning disable CA1000 // Do not declare static members on generic types
        /// <summary>
        /// Gets the task created by this task completion source.
        /// </summary>
#pragma warning disable CA1707 // Remove the underscores from member name
#pragma warning disable SA1300 // Element should begin with an uppercase letter
#pragma warning disable IDE1006 // Naming Styles
        public static SystemTasks.Task<TResult> get_Task(SystemTasks.TaskCompletionSource<TResult> tcs)
        {
            var task = tcs.Task;
            CoyoteRuntime.Current?.RegisterKnownControlledTask(task);
            return task;
        }
#pragma warning restore CA1707 // Remove the underscores from member name
#pragma warning restore SA1300 // Element should begin with an uppercase letter
#pragma warning restore IDE1006 // Naming Styles

        /// <summary>
        /// Transitions the underlying task into the <see cref="SystemTasks.TaskStatus.RanToCompletion"/> state.
        /// </summary>
        public static void SetResult(SystemTasks.TaskCompletionSource<TResult> tcs, TResult result) => tcs.SetResult(result);

        /// <summary>
        /// Transitions the underlying task into the <see cref="SystemTasks.TaskStatus.Faulted"/> state
        /// and binds a collection of exception objects to it.
        /// </summary>
        public static void SetException(SystemTasks.TaskCompletionSource<TResult> tcs,
            IEnumerable<Exception> exceptions) =>
            tcs.SetException(exceptions);

        /// <summary>
        /// Transitions the underlying task into the <see cref="SystemTasks.TaskStatus.Faulted"/> state
        /// and binds it to a specified exception.
        /// </summary>
        public static void SetException(SystemTasks.TaskCompletionSource<TResult> tcs, Exception exception) =>
            tcs.SetException(exception);

        /// <summary>
        /// Transitions the underlying task into the <see cref="SystemTasks.TaskStatus.Canceled"/> state.
        /// </summary>
        public static void SetCanceled(SystemTasks.TaskCompletionSource<TResult> tcs) => tcs.SetCanceled();

        /// <summary>
        /// Attempts to transition the underlying task into the <see cref="SystemTasks.TaskStatus.RanToCompletion"/> state.
        /// </summary>
        public static bool TrySetResult(SystemTasks.TaskCompletionSource<TResult> tcs, TResult result) =>
            tcs.TrySetResult(result);

        /// <summary>
        /// Attempts to transition the underlying task into the <see cref="SystemTasks.TaskStatus.Faulted"/> state
        /// and binds it to a specified exception.
        /// </summary>
        public static bool TrySetException(SystemTasks.TaskCompletionSource<TResult> tcs, Exception exception) =>
            tcs.TrySetException(exception);

        /// <summary>
        /// Attempts to transition the underlying task into the <see cref="SystemTasks.TaskStatus.Faulted"/> state
        /// and binds a collection of exception objects to it.
        /// </summary>
        public static bool TrySetException(SystemTasks.TaskCompletionSource<TResult> tcs, IEnumerable<Exception> exceptions) =>
            tcs.TrySetException(exceptions);

        /// <summary>
        /// Attempts to transition the underlying task into the <see cref="SystemTasks.TaskStatus.Canceled"/> state.
        /// </summary>
        public static bool TrySetCanceled(SystemTasks.TaskCompletionSource<TResult> tcs) => tcs.TrySetCanceled();

        /// <summary>
        /// Attempts to transition the underlying task into the <see cref="SystemTasks.TaskStatus.Canceled"/> state
        /// and enables a cancellation token to be stored in the canceled task.
        /// </summary>
        public static bool TrySetCanceled(SystemTasks.TaskCompletionSource<TResult> tcs,
            SystemCancellationToken cancellationToken) =>
            tcs.TrySetCanceled(cancellationToken);
#pragma warning restore CA1000 // Do not declare static members on generic types
    }
}
