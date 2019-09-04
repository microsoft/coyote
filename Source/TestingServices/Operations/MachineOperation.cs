// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.Coyote.Runtime;

namespace Microsoft.Coyote.TestingServices.Scheduling
{
    /// <summary>
    /// Contains information about a machine operation that can be scheduled.
    /// </summary>
    internal sealed class MachineOperation : IAsyncOperation
    {
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
        /// The type of the operation.
        /// </summary>
        public AsyncOperationType Type { get; private set; }

        /// <summary>
        /// The status of the operation. An operation can be scheduled only
        /// if it is <see cref="AsyncOperationStatus.Enabled"/>.
        /// </summary>
        public AsyncOperationStatus Status { get; set; }

        /// <summary>
        /// The target of the operation.
        /// </summary>
        public AsyncOperationTarget Target { get; private set; }

        /// <summary>
        /// Unique id of the target of the operation.
        /// </summary>
        public ulong TargetId { get; private set; }

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
        /// True if it should skip the next receive scheduling point,
        /// because it was already called in the end of the previous
        /// event handler.
        /// </summary>
        internal bool SkipNextReceiveSchedulingPoint;

        /// <summary>
        /// If the next operation is <see cref="AsyncOperationType.Receive"/>, then this value
        /// gives the step index of the corresponding <see cref="AsyncOperationType.Send"/>.
        /// </summary>
        public ulong MatchingSendIndex { get; internal set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MachineOperation"/> class.
        /// </summary>
        internal MachineOperation(AsyncMachine machine)
        {
            this.Machine = machine;
            this.Type = AsyncOperationType.Start;
            this.Status = AsyncOperationStatus.None;
            this.Target = AsyncOperationTarget.Task;
            this.TargetId = machine.Id.Value;
            this.EventDependencies = new HashSet<Type>();
            this.IsActive = false;
            this.IsHandlerRunning = false;
            this.SkipNextReceiveSchedulingPoint = false;
        }

        /// <summary>
        /// Invoked when the operation has been created.
        /// </summary>
        /// <param name="sendIndex">The index of the send that caused the handler to be restarted, or 0 if this does not apply.</param>
        internal void OnCreated(int sendIndex)
        {
            this.Status = AsyncOperationStatus.Enabled;
            this.IsActive = false;
            this.IsHandlerRunning = false;
            this.MatchingSendIndex = (ulong)sendIndex;
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
        internal void OnReceivedEvent(ulong sendStep)
        {
            this.EventDependencies.Clear();
            this.Status = AsyncOperationStatus.Enabled;
            this.MatchingSendIndex = sendStep;
        }

        /// <summary>
        /// Invoked when the operation completes.
        /// </summary>
        internal void OnCompleted()
        {
            this.Status = AsyncOperationStatus.Completed;
            this.IsHandlerRunning = false;
            this.SkipNextReceiveSchedulingPoint = true;
            this.MatchingSendIndex = 0;
        }

        /// <summary>
        /// Sets the next operation to schedule.
        /// </summary>
        internal void SetNextOperation(AsyncOperationType operationType, AsyncOperationTarget target, ulong targetId)
        {
            this.Type = operationType;
            this.Target = target;
            this.TargetId = targetId;
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
