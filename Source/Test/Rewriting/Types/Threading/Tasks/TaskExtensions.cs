// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Runtime;
using MethodImpl = System.Runtime.CompilerServices.MethodImplAttribute;
using MethodImplOptions = System.Runtime.CompilerServices.MethodImplOptions;
using SystemTask = System.Threading.Tasks.Task;
using SystemTasks = System.Threading.Tasks;

namespace Microsoft.Coyote.Rewriting.Types
{
    /// <summary>
    /// Provides a set of static methods for working with specific kinds of task instances.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static class TaskExtensions
    {
        /// <summary>
        /// Creates a proxy task that represents the asynchronous operation of a task.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SystemTask Unwrap(this SystemTasks.Task<SystemTask> task) =>
            CoyoteRuntime.IsExecutionControlled ?
            CoyoteRuntime.Current.UnwrapTask(task) : SystemTasks.TaskExtensions.Unwrap(task);

        /// <summary>
        /// Creates a proxy generic task that represents the asynchronous operation of a task.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SystemTasks.Task<TResult> Unwrap<TResult>(
            this SystemTasks.Task<SystemTasks.Task<TResult>> task) =>
            CoyoteRuntime.IsExecutionControlled ?
            CoyoteRuntime.Current.UnwrapTask(task) : SystemTasks.TaskExtensions.Unwrap(task);
    }
}
