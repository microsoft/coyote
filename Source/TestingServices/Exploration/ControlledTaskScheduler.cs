// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Coyote.TestingServices.Scheduling;

namespace Microsoft.Coyote.TestingServices.Runtime
{
    /// <summary>
    /// A task scheduler that can be controlled during testing.
    /// </summary>
    internal sealed class ControlledTaskScheduler : TaskScheduler
    {
        /// <summary>
        /// Map from ids of tasks that are controlled by the runtime to operations.
        /// </summary>
        private readonly ConcurrentDictionary<int, MachineOperation> ControlledTaskMap;

        /// <summary>
        /// Initializes a new instance of the <see cref="ControlledTaskScheduler"/> class.
        /// </summary>
        internal ControlledTaskScheduler(ConcurrentDictionary<int, MachineOperation> controlledTaskMap)
        {
            this.ControlledTaskMap = controlledTaskMap;
        }

        /// <summary>
        /// Enqueues the given task.
        /// </summary>
        protected override void QueueTask(Task task)
        {
            if (Task.CurrentId.HasValue &&
                this.ControlledTaskMap.TryGetValue(Task.CurrentId.Value, out MachineOperation op) &&
                !this.ControlledTaskMap.ContainsKey(task.Id))
            {
                // If the task does not correspond to a machine operation, then associate
                // it with the currently executing machine operation and schedule it.
                this.ControlledTaskMap.TryAdd(task.Id, op);
                IO.Debug.WriteLine($"<ScheduleDebug> Operation '{op.SourceId}' is associated with task '{task.Id}'.");
            }

            this.Execute(task);
        }

        /// <summary>
        /// Tries to execute the task inline.
        /// </summary>
        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            return false;
        }

        /// <summary>
        /// Returns the wrapped in a machine scheduled tasks.
        /// </summary>
        protected override IEnumerable<Task> GetScheduledTasks()
        {
            throw new InvalidOperationException("The controlled task scheduler does not provide access to the scheduled tasks.");
        }

        /// <summary>
        /// Executes the given scheduled task on the thread pool.
        /// </summary>
        private void Execute(Task task)
        {
            ThreadPool.UnsafeQueueUserWorkItem(
                _ =>
                {
                    this.TryExecuteTask(task);
                }, null);
        }
    }
}
