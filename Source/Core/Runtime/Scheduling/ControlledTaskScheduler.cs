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
    internal sealed class ControlledTaskScheduler : TaskScheduler, IDisposable
    {
        /// <summary>
        /// Responsible for controlling the execution of operations during systematic testing.
        /// </summary>
        private CoyoteRuntime Runtime;

        /// <inheritdoc/>
        public override int MaximumConcurrencyLevel =>
            this.Runtime?.SchedulingPolicy is SchedulingPolicy.Systematic ? 1 :
            base.MaximumConcurrencyLevel;

        /// <summary>
        /// Initializes a new instance of the <see cref="ControlledTaskScheduler"/> class.
        /// </summary>
        public ControlledTaskScheduler(CoyoteRuntime runtime)
        {
            this.Runtime = runtime;
        }

        /// <inheritdoc/>
        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued) =>
            this.TryExecuteTask(task);

        /// <inheritdoc/>
        protected override void QueueTask(Task task)
        {
            IO.Debug.WriteLine("<Coyote> Enqueuing task '{0}' from thread '{1}'.",
                task.Id, Thread.CurrentThread.ManagedThreadId);
            this.Runtime?.Schedule(task);
        }

        /// <inheritdoc/>
        protected override IEnumerable<Task> GetScheduledTasks() => Enumerable.Empty<Task>();

        /// <summary>
        /// Executes the specified task on this scheduler.
        /// </summary>
        internal void ExecuteTask(Task task) => this.TryExecuteTask(task);

        /// <inheritdoc/>
        public void Dispose()
        {
            this.Runtime = null;
        }
    }
}
