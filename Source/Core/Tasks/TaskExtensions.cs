// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;

namespace Microsoft.Coyote.Tasks
{
    /// <summary>
    /// Extension methods for <see cref="Task"/> and <see cref="Task{TResult}"/> objects.
    /// </summary>
    public static class TaskExtensions
    {
        /// <summary>
        /// Converts the specified <see cref="Task"/> into a <see cref="ControlledTask"/>.
        /// </summary>
        public static ControlledTask ToControlledTask(this Task @this) => new ControlledTask(null, @this);

        /// <summary>
        /// Converts the specified <see cref="Task{TResult}"/> into a <see cref="ControlledTask{TResult}"/>.
        /// </summary>
        public static ControlledTask<TResult> ToControlledTask<TResult>(this Task<TResult> @this) =>
            new ControlledTask<TResult>(null, @this);
    }
}
