// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable SA1005 // Single line comments should begin with single space
namespace Microsoft.Coyote.Runtime
{
    /// <summary>
    /// A scheduler that controls the scheduled tasks.
    /// </summary>
    internal sealed class OperationTaskScheduler : TaskScheduler
    {
        /// <summary>
        /// Responsible for controlling the execution of tasks during systematic testing.
        /// </summary>
        private CoyoteRuntime Runtime;

        private readonly OperationSynchronizationContext Context;
        private readonly ConcurrentQueue<Task> Tasks;

        /// <inheritdoc/>
        public override int MaximumConcurrencyLevel => 1;

        public OperationTaskScheduler(CoyoteRuntime runtime, OperationSynchronizationContext context)
        {
            Console.WriteLine($"      TS: New CoyoteTaskScheduler: thread-id: {Thread.CurrentThread.ManagedThreadId}; task-id: {Task.CurrentId}");
            this.Runtime = runtime;
            this.Context = context;
            this.Tasks = new ConcurrentQueue<Task>();
        }

        /// <inheritdoc/>
        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            Console.WriteLine($"      TS: TryExecuteTaskInline: thread-id: {Thread.CurrentThread.ManagedThreadId}; task-id: {Task.CurrentId}; task: {task.Id}");
            if (SynchronizationContext.Current == this.Context)
            {
                Console.WriteLine($"      TS: TryExecuteTaskInline: true");
                return this.TryExecuteTask(task);
            }

            Console.WriteLine($"      TS: TryExecuteTaskInline: false: {new StackTrace()}");
            return false;
        }

        /// <inheritdoc/>
        protected override void QueueTask(Task task) => this.Runtime.Schedule(task);

        /// <inheritdoc/>
        protected override IEnumerable<Task> GetScheduledTasks() => Enumerable.Empty<Task>();

        internal void ExecuteTask(Task task) => this.TryExecuteTask(task);
    }
}
