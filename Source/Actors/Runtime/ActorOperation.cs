// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Runtime;

namespace Microsoft.Coyote.Actors
{
    /// <summary>
    /// Represents an actor operation that can be controlled during testing.
    /// </summary>
    internal sealed class ActorOperation : ControlledOperation
    {
        /// <summary>
        /// The actor that executes this operation.
        /// </summary>
        internal readonly Actor Actor;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActorOperation"/> class.
        /// </summary>
        internal ActorOperation(ulong operationId, string name, Actor actor, CoyoteRuntime runtime)
            : base(operationId, name, runtime)
        {
            this.Actor = actor;
        }

        /// <inheritdoc/>
        protected override ulong GetLatestHashedState(SchedulingPolicy policy)
        {
            unchecked
            {
                var hash = base.GetLatestHashedState(policy);
                return hash ^ this.Actor.GetHashedState(policy);
            }
        }
    }
}
