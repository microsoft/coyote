// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Coyote.Tasks;

namespace Microsoft.Coyote.Actors
{
    /// <summary>
    /// An object representing a long running operation involving one or more actors.
    /// An operation can be provided as an optional argument in CreateActor and SendEvent.
    /// If a null operation is passed then the operation is inherited from the sender
    /// or target actors (based on which ever one has a <see cref="Actor.CurrentOperation"/>).
    /// In this way an operation is automatically communicated to all actors involved in
    /// completing some logical operation. Each actor involved can find the operation using
    /// their <see cref="Actor.CurrentOperation"/> property.
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
