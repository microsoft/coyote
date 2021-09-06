// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Coyote.Runtime
{
    /// <summary>
    /// A scheduler that controls the scheduled tasks.
    /// </summary>
    internal sealed class OperationTaskScheduler : TaskScheduler
    {
        /// <summary>
        /// Responsible for controlling the execution of operations during systematic testing.
        /// </summary>
        private CoyoteRuntime Runtime;

        /// <inheritdoc/>
        public override int MaximumConcurrencyLevel => 1;

        /// <summary>
        /// Initializes a new instance of the <see cref="OperationTaskScheduler"/> class.
        /// </summary>
        public OperationTaskScheduler(CoyoteRuntime runtime)
        {
            this.Runtime = runtime;
        }

        /// <inheritdoc/>
        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            Console.WriteLine($"      TS: TryExecuteTaskInline: thread-id: {Thread.CurrentThread.ManagedThreadId}; task-id: {Task.CurrentId}");
            bool result = this.TryExecuteTask(task);
            Console.WriteLine($"      TS: TryExecuteTaskInline: thread-id: {Thread.CurrentThread.ManagedThreadId}; task-id: {Task.CurrentId}; result: {result}");
            return result;
        }

        /// <inheritdoc/>
        protected override void QueueTask(Task task)
        {
            Console.WriteLine($"      TS: QueueTask: thread-id: {Thread.CurrentThread.ManagedThreadId}; task-id: {Task.CurrentId}");
            this.Runtime.ScheduleTask(task);
        }

        /// <inheritdoc/>
        protected override IEnumerable<Task> GetScheduledTasks() => Enumerable.Empty<Task>();

        /// <summary>
        /// Executes the specified task on this scheduler.
        /// </summary>
        internal void ExecuteTask(Task task)
        {
            Console.WriteLine($"      TS: ExecuteTask: thread-id: {Thread.CurrentThread.ManagedThreadId}; task-id: {Task.CurrentId}; task {task.Id}");
            this.TryExecuteTask(task);
        }
    }
}
