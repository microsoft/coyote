// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Coyote.TestingServices.Scheduling
{
    /// <summary>
    /// Contains information about an asynchronous operation that can be scheduled.
    /// </summary>
    internal sealed class AsyncOperation : IAsyncOperation
    {
        /// <summary>
        /// Unique id of the source of this operation.
        /// </summary>
        public ulong SourceId { get; private set; }

        /// <summary>
        /// Unique name of the source of this operation.
        /// </summary>
        public string SourceName { get; private set; }

        /// <summary>
        /// The task that performs this operation.
        /// </summary>
        internal Task Task;

        /// <summary>
        /// The type of the operation.
        /// </summary>
        public AsyncOperationType Type { get; private set; }

        /// <summary>
        /// The target of the operation.
        /// </summary>
        public AsyncOperationTarget Target { get; private set; }

        /// <summary>
        /// Unique id of the target of the operation.
        /// </summary>
        public ulong TargetId { get; private set; }

        /// <summary>
        /// Set of operations that must complete before this operation can resume.
        /// </summary>
        private readonly HashSet<IAsyncOperation> Dependencies;

        /// <summary>
        /// True if the task that performs this operation is enabled, else false.
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// Is the source of the operation active.
        /// </summary>
        internal bool IsActive;

        /// <summary>
        /// Is the source of the operation waiting to receive an event.
        /// </summary>
        internal bool IsWaitingToReceive;

        /// <summary>
        /// Is the inbox handler of the source of the operation running.
        /// </summary>
        internal bool IsInboxHandlerRunning;

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
        /// Initializes a new instance of the <see cref="AsyncOperation"/> class.
        /// </summary>
        internal AsyncOperation(MachineId id)
        {
            this.SourceId = id.Value;
            this.SourceName = id.Name;
            this.Type = AsyncOperationType.Start;
            this.Target = AsyncOperationTarget.Task;
            this.TargetId = id.Value;
            this.Dependencies = new HashSet<IAsyncOperation>();
            this.IsEnabled = false;
            this.IsActive = false;
            this.IsWaitingToReceive = false;
            this.IsInboxHandlerRunning = false;
            this.SkipNextReceiveSchedulingPoint = false;
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
        /// Notify that the operation has been created and will run on the specified task.
        /// </summary>
        /// <param name="task">The task that performs this operation.</param>
        /// <param name="sendIndex">The index of the send that caused the event handler to be restarted, or 0 if this does not apply.</param>
        internal void NotifyCreated(Task task, int sendIndex)
        {
            this.Task = task;
            this.IsEnabled = true;
            this.IsActive = false;
            this.IsWaitingToReceive = false;
            this.IsInboxHandlerRunning = false;
            this.MatchingSendIndex = (ulong)sendIndex;
        }

        /// <summary>
        /// Notify that the operation has completed.
        /// </summary>
        internal void NotifyCompleted()
        {
            this.IsEnabled = false;
            this.IsInboxHandlerRunning = false;
            this.SkipNextReceiveSchedulingPoint = true;
            this.MatchingSendIndex = 0;
        }
    }
}
