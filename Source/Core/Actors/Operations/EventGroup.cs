// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Coyote.Tasks;

namespace Microsoft.Coyote.Actors
{
    /// <summary>
    /// An object representing a long running context involving one or more actors.
    /// An `EventGroup` can be provided as an optional argument in CreateActor and SendEvent.
    /// If a null `EventGroup` is passed then the `EventGroup` is inherited from the sender
    /// or target actors (based on which ever one has a <see cref="Actor.CurrentEventGroup"/>).
    /// In this way an `EventGroup` is automatically communicated to all actors involved in
    /// completing some larger operation. Each actor involved can find the `EventGroup` using
    /// their <see cref="Actor.CurrentEventGroup"/> property.
    /// </summary>
    public class EventGroup
    {
        /// <summary>
        /// The unique id of this `EventGroup`, initialized with Guid.Empty.
        /// </summary>
        public Guid Id { get; internal set; }

        /// <summary>
        /// An optional friendly name for this `EventGroup`.
        /// </summary>
        public string Name { get; internal set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventGroup"/> class.
        /// </summary>
        /// <param name="id">The id for this `EventGroup` (defaults to Guid.Empty).</param>
        public EventGroup(Guid id = default)
        {
            this.Id = id;
        }

        /// <summary>
        /// A special null event group that can be used to stop the <see cref="Actor.CurrentEventGroup"/> from
        /// being passed along in a CreateActor or SendEvent call.
        /// </summary>
        public static EventGroup NullEventGroup = new EventGroup();
    }
}
