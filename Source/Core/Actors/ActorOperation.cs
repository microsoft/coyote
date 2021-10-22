// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Runtime;

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
        internal ActorOperation(ulong operationId, string name, Actor actor)
            : base(operationId, name, 0)
        {
            this.Actor = actor;
        }
    }
}
