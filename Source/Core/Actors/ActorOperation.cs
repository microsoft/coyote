// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.SystematicTesting;

namespace Microsoft.Coyote.Actors
{
    /// <summary>
    /// Represents an asynchronous actor operation that can be controlled during systematic testing.
    /// </summary>
    internal sealed class ActorOperation : TaskOperation
    {
        /// <summary>
        /// The actor that executes this operation.
        /// </summary>
        internal readonly Actor Actor;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActorOperation"/> class.
        /// </summary>
        internal ActorOperation(ulong operationId, string name, Actor actor, OperationScheduler scheduler)
            : base(operationId, name, scheduler)
        {
            this.Actor = actor;
        }

        /// <summary>
        /// Invoked when the operation is waiting to receive an event.
        /// </summary>
        internal void OnWaitEvent() => this.Status = AsyncOperationStatus.BlockedOnReceive;

        /// <summary>
        /// Invoked when the operation dequeued an event it was waiting to receive.
        /// </summary>
        internal void OnReceivedEvent() => this.Status = AsyncOperationStatus.Enabled;
    }
}
