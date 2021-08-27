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
    //public class OperationTaskScheduler : TaskScheduler
    //{
    //    private readonly SynchronizationContext Context;
    //    private readonly TaskScheduler OriginalScheduler;

    //    // private int Counter;

    //    public OperationTaskScheduler(SynchronizationContext context, TaskScheduler current)
    //    {
    //        Console.WriteLine($"      TS: New CoyoteTaskScheduler: thread-id: {Thread.CurrentThread.ManagedThreadId}; task-id: {Task.CurrentId}");
    //        this.Context = context;
    //        this.OriginalScheduler = current;
    //        // this.Counter = 0;
    //    }

    //    protected override IEnumerable<Task> GetScheduledTasks()
    //    {
    //        Console.WriteLine($"      TS: GetScheduledTasks: thread-id: {Thread.CurrentThread.ManagedThreadId}; task-id: {Task.CurrentId}");
    //        MethodInfo method = typeof(TaskScheduler).GetMethod("GetScheduledTasks", BindingFlags.NonPublic | BindingFlags.Instance);
    //        return (IEnumerable<Task>)method.Invoke(this.OriginalScheduler, Array.Empty<object>());
    //    }

    //    protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
    //    {
    //        // return false;
    //        // if (this.Counter > 0)
    //        // {
    //        //    return true;
    //        // }

    //        // this.Counter++;
    //        Console.WriteLine($"      TS: TryExecuteTaskInline: thread-id: {Thread.CurrentThread.ManagedThreadId}; task-id: {Task.CurrentId}; task: {task.Id}");
    //        MethodInfo method = typeof(TaskScheduler).GetMethod("TryExecuteTaskInline", BindingFlags.NonPublic | BindingFlags.Instance);
    //        var result = (bool)method.Invoke(this.OriginalScheduler, new object[] { task, taskWasPreviouslyQueued });
    //        Console.WriteLine($"      TS: TryExecuteTaskInline: thread-id: {Thread.CurrentThread.ManagedThreadId}; task-id: {Task.CurrentId}; task: {task.Id}; res: {result}");
    //        return result;
    //    }

    //    protected override void QueueTask(Task task)
    //    {
    //        Console.WriteLine($"      TS: QueueTask: thread-id: {Thread.CurrentThread.ManagedThreadId}; task-id: {Task.CurrentId}; task: {task.Id}");
    //        // MethodInfo method = typeof(TaskScheduler).GetMethod("QueueTask", BindingFlags.NonPublic | BindingFlags.Instance);
    //        // method.Invoke(this.OriginalScheduler, new object[] { task });
    //    }

    //    protected override bool TryDequeue(Task task)
    //    {
    //        Console.WriteLine($"      TS: TryDequeue: thread-id: {Thread.CurrentThread.ManagedThreadId}; task-id: {Task.CurrentId}; task: {task.Id}");
    //        MethodInfo method = typeof(TaskScheduler).GetMethod("TryDequeue", BindingFlags.NonPublic | BindingFlags.Instance);
    //        return (bool)method.Invoke(this.OriginalScheduler, new object[] { task });
    //    }
    //}

    /// <summary>Provides a task scheduler that targets a specific SynchronizationContext.</summary>
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
