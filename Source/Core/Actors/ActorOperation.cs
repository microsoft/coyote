// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.Testing.Systematic;

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
        internal ActorOperation(ulong operationId, string name, Actor actor, TurnBasedScheduler scheduler)
            : base(operationId, name, scheduler)
        {
            this.Actor = actor;
        }

        /// <summary>
        /// Blocks the operation until the event the actor is waiting for has been received.
        /// </summary>
        internal void BlockUntilEventReceived() => this.Status = AsyncOperationStatus.BlockedOnReceive;

        /// <summary>
        /// Enables the operation due to the received event.
        /// </summary>
        internal void EnableDueToReceivedEvent() => this.Status = AsyncOperationStatus.Enabled;
    }
}
