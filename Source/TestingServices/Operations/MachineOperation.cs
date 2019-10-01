// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Coyote.Machines;

namespace Microsoft.Coyote.TestingServices.Scheduling
{
    /// <summary>
    /// Contains information about a machine operation that can be scheduled.
    /// </summary>
    [DebuggerStepThrough]
    internal sealed class MachineOperation : IAsyncOperation
    {
        /// <summary>
        /// Stores the unique scheduler id that executes this operation.
        /// </summary>
        internal static readonly AsyncLocal<Guid> SchedulerId = new AsyncLocal<Guid>();

        /// <summary>
        /// The scheduler executing this operation.
        /// </summary>
        internal readonly OperationScheduler Scheduler;

        /// <summary>
        /// The machine that owns this operation.
        /// </summary>
        internal readonly AsyncMachine Machine;

        /// <summary>
        /// Unique id of the source of the operation.
        /// </summary>
        public ulong SourceId => this.Machine.Id.Value;

        /// <summary>
        /// Unique name of the source of the operation.
        /// </summary>
        public string SourceName => this.Machine.Id.Name;

        /// <summary>
        /// The status of the operation. An operation can be scheduled only
        /// if it is <see cref="AsyncOperationStatus.Enabled"/>.
        /// </summary>
        public AsyncOperationStatus Status { get; set; }

        /// <summary>
        /// Set of tasks that this operation is waiting to join. All tasks
        /// in the set must complete before this operation can resume.
        /// </summary>
        private readonly HashSet<Task> JoinDependencies;

        /// <summary>
        /// Set of events that this operation is waiting to receive. Receiving any
        /// event in the set allows this operation to resume.
        /// </summary>
        private readonly HashSet<Type> EventDependencies;

        /// <summary>
        /// Is the source of the operation active.
        /// </summary>
        internal bool IsActive;

        /// <summary>
        /// True if the handler of the source of the operation is running, else false.
        /// </summary>
        internal bool IsHandlerRunning;

        /// <summary>
        /// True if the next awaiter is controlled, else false.
        /// </summary>
        internal bool IsAwaiterControlled;

        /// <summary>
        /// True if it should skip the next receive scheduling point,
        /// because it was already called in the end of the previous
        /// event handler.
        /// </summary>
        internal bool SkipNextReceiveSchedulingPoint;

        /// <summary>
        /// Initializes a new instance of the <see cref="MachineOperation"/> class.
        /// </summary>
        internal MachineOperation(AsyncMachine machine, OperationScheduler scheduler)
        {
            this.Scheduler = scheduler;
            this.Machine = machine;
            this.Status = AsyncOperationStatus.None;
            this.JoinDependencies = new HashSet<Task>();
            this.EventDependencies = new HashSet<Type>();
            this.IsActive = false;
            this.IsHandlerRunning = false;
            this.IsAwaiterControlled = false;
            this.SkipNextReceiveSchedulingPoint = false;
        }

        /// <summary>
        /// Invoked when the operation has been created.
        /// </summary>
        internal void OnCreated()
        {
            this.Status = AsyncOperationStatus.Enabled;
            this.IsActive = false;
            this.IsHandlerRunning = false;
        }

        internal void OnGetControlledAwaiter()
        {
            IO.Debug.WriteLine("<ScheduleDebug> Operation '{0}' received a controlled awaiter.", this.SourceId);
            this.IsAwaiterControlled = true;
        }

        /// <summary>
        /// Invoked when the operation is waiting to join the specified task.
        /// </summary>
        internal void OnWaitTask(Task task)
        {
            IO.Debug.WriteLine("<ScheduleDebug> Operation '{0}' is waiting for task '{1}'.", this.SourceId, task.Id);
            this.JoinDependencies.Add(task);
            this.Status = AsyncOperationStatus.BlockedOnWaitAll;
            this.Scheduler.ScheduleNextEnabledOperation();
            this.IsAwaiterControlled = false;
        }

        /// <summary>
        /// Invoked when the operation is waiting to join the specified tasks.
        /// </summary>
        internal void OnWaitTasks(IEnumerable<Task> tasks, bool waitAll)
        {
            foreach (var task in tasks)
            {
                if (!task.IsCompleted)
                {
                    IO.Debug.WriteLine("<ScheduleDebug> Operation '{0}' is waiting for task '{1}'.", this.SourceId, task.Id);
                    this.JoinDependencies.Add(task);
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
        /// Invoked when the operation is waiting to receive an event of the specified type or types.
        /// </summary>
        internal void OnWaitEvent(IEnumerable<Type> eventTypes)
        {
            this.EventDependencies.UnionWith(eventTypes);
            this.Status = AsyncOperationStatus.BlockedOnReceive;
        }

        /// <summary>
        /// Invoked when the operation received an event from the specified operation.
        /// </summary>
        internal void OnReceivedEvent()
        {
            this.EventDependencies.Clear();
            this.Status = AsyncOperationStatus.Enabled;
        }

        /// <summary>
        /// Invoked when the operation completes.
        /// </summary>
        internal void OnCompleted()
        {
            this.Status = AsyncOperationStatus.Completed;
            this.IsHandlerRunning = false;
            this.SkipNextReceiveSchedulingPoint = true;
            this.Scheduler.ScheduleNextEnabledOperation();
        }

        /// <summary>
        /// Tries to enable the operation, if it was not already enabled.
        /// </summary>
        internal void TryEnable()
        {
            if (this.Status == AsyncOperationStatus.BlockedOnWaitAll)
            {
                IO.Debug.WriteLine("<ScheduleDebug> Try enable operation '{0}'.", this.SourceId);
                if (!this.JoinDependencies.All(task => task.IsCompleted))
                {
                    IO.Debug.WriteLine("<ScheduleDebug> Operation '{0}' is waiting for all join tasks to complete.", this.SourceId);
                    return;
                }

                this.JoinDependencies.Clear();
                this.Status = AsyncOperationStatus.Enabled;
            }
            else if (this.Status == AsyncOperationStatus.BlockedOnWaitAny)
            {
                IO.Debug.WriteLine("<ScheduleDebug> Try enable operation '{0}'.", this.SourceId);
                if (!this.JoinDependencies.Any(task => task.IsCompleted))
                {
                    IO.Debug.WriteLine("<ScheduleDebug> Operation '{0}' is waiting for any join task to complete.", this.SourceId);
                    return;
                }

                this.JoinDependencies.Clear();
                this.Status = AsyncOperationStatus.Enabled;
            }
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj is MachineOperation op)
            {
                return this.SourceId == op.SourceId;
            }

            return false;
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        public override int GetHashCode() => (int)this.SourceId;
    }
}
