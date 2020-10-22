// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CoyoteTasks = Microsoft.Coyote.Tasks;

namespace Microsoft.Coyote.SystematicTesting
{
    /// <summary>
    /// Represents an asynchronous task operation that can be controlled during systematic testing.
    /// </summary>
    internal class TaskOperation : AsyncOperation
    {
        /// <summary>
        /// The scheduler executing this operation.
        /// </summary>
        private readonly OperationScheduler Scheduler;

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
        internal TaskOperation(ulong operationId, string name, OperationScheduler scheduler)
            : base(operationId, name)
        {
            this.Scheduler = scheduler;
            this.JoinDependencies = new HashSet<Task>();
        }

        /// <summary>
        /// Blocks the operation until the specified tasks completes.
        /// </summary>
        internal void BlockUntilTaskCompletes(Task task)
        {
            IO.Debug.WriteLine("<ScheduleDebug> Operation '{0}' is waiting for task '{1}'.", this.Id, task.Id);
            this.JoinDependencies.Add(task);
            this.Status = AsyncOperationStatus.BlockedOnWaitAll;
            this.Scheduler.ScheduleNextOperation();
        }

        /// <summary>
        /// Tries to blocks the operation until the specified tasks completes,
        /// if the task has not already completed.
        /// </summary>
        internal bool TryBlockUntilTaskCompletes(Task task)
        {
            if (!task.IsCompleted)
            {
                this.BlockUntilTaskCompletes(task);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Blocks the operation until all or any of the specified tasks complete.
        /// </summary>
        internal void BlockUntilTasksComplete(Task[] tasks, bool waitAll)
        {
            // In the case where `waitAll` is false (e.g. for `Task.WhenAny` or `Task.WaitAny`), we check if all
            // tasks are not completed. If that is the case, then we add all tasks to `JoinDependencies` and wait
            // at least one to complete. If, however, even one task is completed, then we should not wait, as it
            // can cause potential deadlocks.
            if (waitAll || tasks.All(task => !task.IsCompleted))
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
        }

        /// <summary>
        /// Blocks the operation until all or any of the specified tasks complete.
        /// </summary>
        internal void BlockUntilTasksComplete(CoyoteTasks.Task[] tasks, bool waitAll)
        {
            if (waitAll || tasks.All(task => !task.IsCompleted))
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
        }

        /// <inheritdoc/>
        internal override bool TryEnable()
        {
            if ((this.Status == AsyncOperationStatus.BlockedOnWaitAll && this.JoinDependencies.All(task => task.IsCompleted)) ||
                (this.Status == AsyncOperationStatus.BlockedOnWaitAny && this.JoinDependencies.Any(task => task.IsCompleted)))
            {
                this.JoinDependencies.Clear();
                this.Status = AsyncOperationStatus.Enabled;
                return true;
            }

            return false;
        }

        /// <inheritdoc/>
        internal override bool IsBlockedOnUncontrolledDependency()
        {
            if (this.JoinDependencies.Count > 0)
            {
                foreach (var task in this.JoinDependencies)
                {
                    if (!(task.AsyncState is OperationContext))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Sets the <see cref="Exception"/> that caused this operation to end prematurely.
        /// </summary>
        internal void SetException(Exception exception) => this.Exception = exception;
    }
}
