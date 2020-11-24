// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Runtime;
using MethodImpl = System.Runtime.CompilerServices.MethodImplAttribute;
using MethodImplOptions = System.Runtime.CompilerServices.MethodImplOptions;
using SystemTasks = System.Threading.Tasks;

namespace Microsoft.Coyote.SystematicTesting.Interception
{
    /// <summary>
    /// Provides a set of static methods for working with specific kinds of <see cref="Task"/> instances.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static class TaskExtensions
    {
        /// <summary>
        /// Creates a proxy <see cref="Task"/> that represents the asynchronous operation
        /// of a <see cref="TaskScheduler.TryExecuteTaskInline"/>.
        /// </summary>
        /// <param name="task">The task to unwrap.</param>
        /// <returns>A Task that represents the asynchronous operation of the provided task.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task Unwrap(this Task<Task> task) => CoyoteRuntime.IsExecutionControlled ?
            CoyoteRuntime.Current.UnwrapTask(task) : SystemTasks.TaskExtensions.Unwrap(task);

        /// <summary>
        /// Creates a proxy <see cref="Task{TResult}"/> that represents the asynchronous operation of a task.
        /// </summary>
        /// <param name="task">The task to unwrap.</param>
        /// <returns>A Task that represents the asynchronous operation of the provided task.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<TResult> Unwrap<TResult>(this Task<Task<TResult>> task) => CoyoteRuntime.IsExecutionControlled ?
            CoyoteRuntime.Current.UnwrapTask(task) : SystemTasks.TaskExtensions.Unwrap(task);
    }
}
