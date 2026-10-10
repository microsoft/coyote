// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Linq;
using System.Threading;
using SystemTask = System.Threading.Tasks.Task;

namespace Microsoft.Coyote.Runtime
{
    /// <summary>
    /// Provides methods for interacting with tasks using the runtime.
    /// </summary>
    public static class TaskServices
    {
        /// <summary>
        /// Pauses the current operation until the specified task completes.
        /// </summary>
        internal static void WaitUntilTaskCompletes(CoyoteRuntime runtime, SystemTask task) =>
            WaitUntilTaskCompletes(runtime, default, task);

        /// <summary>
        /// Pauses the current operation until the specified task completes.
        /// </summary>
        internal static void WaitUntilTaskCompletes(CoyoteRuntime runtime, ControlledOperation current, SystemTask task)
        {
            if (task != null && !task.IsCompleted && runtime != null)
            {
                bool isTaskUncontrolled = runtime.CheckIfAwaitedTaskIsUncontrolled(task);
                if (current != null || runtime.TryGetExecutingOperation(out current))
                {
                    if (runtime.SchedulingPolicy is SchedulingPolicy.Interleaving)
                    {
                        runtime.PauseOperationUntil(current, () => task.IsCompleted, !isTaskUncontrolled, $"task '{task.Id}' to complete");
                    }
                    else if (runtime.SchedulingPolicy is SchedulingPolicy.Fuzzing)
                    {
                        runtime.DelayOperation(current);
                    }
                }
            }
        }

        /// <summary>
        /// Pauses the current operation until all of the specified tasks complete.
        /// </summary>
        internal static void WaitUntilAllTasksComplete(CoyoteRuntime runtime, SystemTask[] tasks) =>
            WaitUntilAllTasksComplete(runtime, tasks, default);

        /// <summary>
        /// Pauses the current operation until all of the specified tasks complete or
        /// the specified cancellation token is canceled.
        /// </summary>
        /// <remarks>
        /// The caller is responsible for observing the cancellation after the operation resumes.
        /// </remarks>
        internal static void WaitUntilAllTasksComplete(CoyoteRuntime runtime, SystemTask[] tasks,
            CancellationToken cancellationToken)
        {
            bool isAnyTaskUncontrolled = IsAnyTaskUncontrolled(runtime, tasks);
            if (runtime.TryGetExecutingOperation(out ControlledOperation current))
            {
                if (runtime.SchedulingPolicy is SchedulingPolicy.Interleaving)
                {
                    string description = string.Format("all tasks ('{0}') to complete",
                        string.Join("', '", tasks.Select(t => t.Id.ToString())));
                    if (cancellationToken.CanBeCanceled)
                    {
                        runtime.PauseOperationUntil(current,
                            () => cancellationToken.IsCancellationRequested || tasks.All(t => t.IsCompleted),
                            !isAnyTaskUncontrolled, description + " or the wait to be canceled");
                    }
                    else
                    {
                        runtime.PauseOperationUntil(current, () => tasks.All(t => t.IsCompleted),
                            !isAnyTaskUncontrolled, description);
                    }
                }
                else if (runtime.SchedulingPolicy is SchedulingPolicy.Fuzzing)
                {
                    runtime.DelayOperation(current);
                }
            }
        }

        /// <summary>
        /// Pauses the current operation until any of the specified tasks completes.
        /// </summary>
        internal static void WaitUntilAnyTaskCompletes(CoyoteRuntime runtime, SystemTask[] tasks)
        {
            bool isAnyTaskUncontrolled = IsAnyTaskUncontrolled(runtime, tasks);
            if (runtime.TryGetExecutingOperation(out ControlledOperation current))
            {
                if (runtime.SchedulingPolicy is SchedulingPolicy.Interleaving)
                {
                    runtime.PauseOperationUntil(current, () => tasks.Any(t => t.IsCompleted), !isAnyTaskUncontrolled,
                        string.Format("any task ('{0}') to complete", string.Join("', '", tasks.Select(t => t.Id.ToString()))));
                }
                else if (runtime.SchedulingPolicy is SchedulingPolicy.Fuzzing)
                {
                    runtime.DelayOperation(current);
                }
            }
        }

        /// <summary>
        /// Checks if any of the specified tasks is uncontrolled.
        /// </summary>
        private static bool IsAnyTaskUncontrolled(CoyoteRuntime runtime, SystemTask[] tasks)
        {
            bool isAnyTaskUncontrolled = false;
            foreach (var task in tasks)
            {
                if (!task.IsCompleted)
                {
                    isAnyTaskUncontrolled |= runtime.CheckIfAwaitedTaskIsUncontrolled(task);
                }
            }

            return isAnyTaskUncontrolled;
        }
    }
}
