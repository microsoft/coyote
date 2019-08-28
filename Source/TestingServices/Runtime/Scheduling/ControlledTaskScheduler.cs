// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.TestingServices.Runtime;

namespace Microsoft.Coyote.TestingServices.Scheduling
{
    /// <summary>
    /// A task scheduler that can be controlled during testing.
    /// </summary>
    internal sealed class ControlledTaskScheduler : TaskScheduler
    {
        /// <summary>
        /// The Coyote testing runtime.
        /// </summary>
        private readonly SystematicTestingRuntime Runtime;

        /// <summary>
        /// Map from task that are controlled by the runtime to machines.
        /// </summary>
        private readonly ConcurrentDictionary<int, AsyncMachine> ControlledTaskMap;

        /// <summary>
        /// Initializes a new instance of the <see cref="ControlledTaskScheduler"/> class.
        /// </summary>
        internal ControlledTaskScheduler(SystematicTestingRuntime runtime, ConcurrentDictionary<int, AsyncMachine> controlledTaskMap)
        {
            this.Runtime = runtime;
            this.ControlledTaskMap = controlledTaskMap;
        }

        /// <summary>
        /// Enqueues the given task. If the task does not correspond to a Coyote machine,
        /// then it wraps it in a task machine and schedules it.
        /// </summary>
        protected override void QueueTask(Task task)
        {
            if (Task.CurrentId.HasValue &&
                this.ControlledTaskMap.TryGetValue(Task.CurrentId.Value, out AsyncMachine machine))
            {
                // The task was spawned by user-code (e.g. due to async/await).
                this.ControlledTaskMap.TryAdd(task.Id, machine);
                IO.Debug.WriteLine($"<ScheduleDebug> '{machine.Id}' changed task '{Task.CurrentId.Value}' to '{task.Id}'.");
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
            throw new InvalidOperationException("The BugFindingTaskScheduler does not provide access to the scheduled tasks.");
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
