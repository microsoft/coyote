// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Coyote.Tasks;

namespace Microsoft.Coyote.Actors
{
    /// <summary>
    /// An object representing a long running operation involving one or more actors.
    /// This operation is passed along automatically during any subsequent CreateActor
    /// or SendEvent calls so that any actor in a network of actors can complete this
    /// operation.  An actor can find the operation using the <see cref="Actor.CurrentOperation"/>
    /// property.
    /// </summary>
    public class Operation
    {
        /// <summary>
        /// The unique id of this operation, initialized with Guid.Empty.
        /// </summary>
        public Guid Id { get; internal set; }

        /// <summary>
        /// An optional friendly name for this operation.
        /// </summary>
        public string Name { get; internal set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Operation"/> class.
        /// </summary>
        /// <param name="id">The id for this operation (defaults to Guid.Empty).</param>
        public Operation(Guid id = default)
        {
            this.Id = id;
        }

        /// <summary>
        /// A special null operation that can be used to stop the <see cref="Actor.CurrentOperation"/> from
        /// being passed along in a CreateActor or SendEvent call.
        /// </summary>
        public static Operation NullOperation = new Operation();
    }
}
