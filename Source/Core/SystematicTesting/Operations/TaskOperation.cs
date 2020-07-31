// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CoyoteTasks = Microsoft.Coyote.Tasks;

namespace Microsoft.Coyote.SystematicTesting
{
    /// <summary>
    /// Contains information about an asynchronous task operation
    /// that can be controlled during testing.
    /// </summary>
    [DebuggerStepThrough]
    internal sealed class TaskOperation : AsyncOperation
    {
        /// <summary>
        /// The scheduler executing this operation.
        /// </summary>
        private readonly OperationScheduler Scheduler;

        /// <summary>
        /// The unique id of the operation.
        /// </summary>
        internal override ulong Id { get; }

        /// <summary>
        /// The unique name of the operation.
        /// </summary>
        internal override string Name { get; }

        /// <summary>
        /// Set of tasks that this operation is waiting to join. All tasks
        /// in the set must complete before this operation can resume.
        /// </summary>
        private readonly HashSet<Task> JoinDependencies;

        /// <summary>
        /// The <see cref="Exception"/> that caused this operation to end prematurely.
        /// If the operation completed successfully or has not yet thrown any exceptions,
        /// this will be null.
        /// </summary>
        /// <remarks>
        /// Only an exception thrown during the execution of an asynchronous state machine
        /// is currently being captured by this property.
        /// </remarks>
        internal Exception Exception { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskOperation"/> class.
        /// </summary>
        internal TaskOperation(ulong operationId, OperationScheduler scheduler)
            : base()
        {
            this.Scheduler = scheduler;
            this.Id = operationId;
            this.Name = $"Task({operationId})";
            this.JoinDependencies = new HashSet<Task>();
        }

        /// <summary>
        /// Invoked when the operation is waiting to join the specified task.
        /// </summary>
        internal void OnWaitTask(Task task)
        {
            if (!task.IsCompleted)
            {
                IO.Debug.WriteLine("<ScheduleDebug> Operation '{0}' is waiting for task '{1}'.", this.Id, task.Id);
                this.JoinDependencies.Add(task);
                this.Status = AsyncOperationStatus.BlockedOnWaitAll;
                this.Scheduler.ScheduleNextOperation();
            }
        }

        /// <summary>
        /// Invoked when the operation is waiting to join the specified tasks.
        /// </summary>
        internal void OnWaitTasks(Task[] tasks, bool waitAll)
        {
            foreach (var task in tasks)
            {
                if (!task.IsCompleted)
                {
                    IO.Debug.WriteLine("<ScheduleDebug> Operation '{0}' is waiting for task '{1}'.", this.Id, task.Id);
                    this.JoinDependencies.Add(task);
                }
            }

            if (this.JoinDependencies.Count > 0)
            {
                this.Status = waitAll ? AsyncOperationStatus.BlockedOnWaitAll : AsyncOperationStatus.BlockedOnWaitAny;
                this.Scheduler.ScheduleNextOperation();
            }
        }

        /// <summary>
        /// Invoked when the operation is waiting to join the specified tasks.
        /// </summary>
        internal void OnWaitTasks(CoyoteTasks.Task[] tasks, bool waitAll)
        {
            foreach (var task in tasks)
            {
                if (!task.IsCompleted)
                {
                    IO.Debug.WriteLine("<ScheduleDebug> Operation '{0}' is waiting for task '{1}'.", this.Id, task.Id);
                    this.JoinDependencies.Add(task.UncontrolledTask);
                }
            }

            if (this.JoinDependencies.Count > 0)
            {
                this.Status = waitAll ? AsyncOperationStatus.BlockedOnWaitAll : AsyncOperationStatus.BlockedOnWaitAny;
                this.Scheduler.ScheduleNextOperation();
            }
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

        /// <summary>
        /// Sets the <see cref="Exception"/> that caused this operation to end prematurely.
        /// </summary>
        internal void SetException(Exception exception) => this.Exception = exception;
    }
}
