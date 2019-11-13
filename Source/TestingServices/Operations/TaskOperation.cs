// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Coyote.TestingServices.Threading.Tasks;
using Microsoft.Coyote.Threading.Tasks;

namespace Microsoft.Coyote.TestingServices.Scheduling
{
    /// <summary>
    /// Contains information about an asynchronous task operation
    /// that can be controlled during testing.
    /// </summary>
    [DebuggerStepThrough]
    internal sealed class TaskOperation : AsyncOperation
    {
        /// <summary>
        /// The executor for this operation.
        /// </summary>
        internal readonly ControlledTaskExecutor Executor;

        /// <summary>
        /// The unique id of the operation.
        /// </summary>
        public override ulong Id => this.Executor.Id.Value;

        /// <summary>
        /// The unique name of the operation.
        /// </summary>
        public override string Name => this.Executor.Id.Name;

        /// <summary>
        /// Set of tasks that this operation is waiting to join. All tasks
        /// in the set must complete before this operation can resume.
        /// </summary>
        private readonly HashSet<Task> JoinDependencies;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskOperation"/> class.
        /// </summary>
        internal TaskOperation(ControlledTaskExecutor executor, OperationScheduler scheduler)
            : base(scheduler)
        {
            this.Executor = executor;
            this.JoinDependencies = new HashSet<Task>();
        }

        internal void OnGetControlledAwaiter()
        {
            IO.Debug.WriteLine("<ScheduleDebug> Operation '{0}' received a controlled awaiter.", this.Id);
            this.IsAwaiterControlled = true;
        }

        /// <summary>
        /// Invoked when the operation is waiting to join the specified task.
        /// </summary>
        internal void OnWaitTask(Task task)
        {
            IO.Debug.WriteLine("<ScheduleDebug> Operation '{0}' is waiting for task '{1}'.", this.Id, task.Id);
            this.JoinDependencies.Add(task);
            this.Status = AsyncOperationStatus.BlockedOnWaitAll;
            this.Scheduler.ScheduleNextEnabledOperation();
            this.IsAwaiterControlled = false;
        }

        /// <summary>
        /// Invoked when the operation is waiting to join the specified tasks.
        /// </summary>
        internal void OnWaitTasks(IEnumerable<ControlledTask> tasks, bool waitAll)
        {
            foreach (var task in tasks)
            {
                if (!task.IsCompleted)
                {
                    IO.Debug.WriteLine("<ScheduleDebug> Operation '{0}' is waiting for task '{1}'.", this.Id, task.Id);
                    this.JoinDependencies.Add(task.AwaiterTask);
                }
            }

            if (this.JoinDependencies.Count > 0)
            {
                this.Status = waitAll ? AsyncOperationStatus.BlockedOnWaitAll : AsyncOperationStatus.BlockedOnWaitAny;
                this.Scheduler.ScheduleNextEnabledOperation();
            }

            this.IsAwaiterControlled = false;
        }

        /// <summary>
        /// Tries to enable the operation, if it was not already enabled.
        /// </summary>
        internal override void TryEnable()
        {
            if (this.Status == AsyncOperationStatus.BlockedOnWaitAll)
            {
                IO.Debug.WriteLine("<ScheduleDebug> Try enable operation '{0}'.", this.Id);
                if (!this.JoinDependencies.All(task => task.IsCompleted))
                {
                    IO.Debug.WriteLine("<ScheduleDebug> Operation '{0}' is waiting for all join tasks to complete.", this.Id);
                    return;
                }

                this.JoinDependencies.Clear();
                this.Status = AsyncOperationStatus.Enabled;
            }
            else if (this.Status == AsyncOperationStatus.BlockedOnWaitAny)
            {
                IO.Debug.WriteLine("<ScheduleDebug> Try enable operation '{0}'.", this.Id);
                if (!this.JoinDependencies.Any(task => task.IsCompleted))
                {
                    IO.Debug.WriteLine("<ScheduleDebug> Operation '{0}' is waiting for any join task to complete.", this.Id);
                    return;
                }

                this.JoinDependencies.Clear();
                this.Status = AsyncOperationStatus.Enabled;
            }
        }
    }
}
