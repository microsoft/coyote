// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Runtime.Serialization;

using Microsoft.Coyote.Runtime;

namespace Microsoft.Coyote
{
    /// <summary>
    /// Contains an <see cref="Event"/>, and its associated metadata.
    /// </summary>
    [DataContract]
    internal class EventInfo
    {
        /// <summary>
        /// Event name.
        /// </summary>
        [DataMember]
        internal string EventName { get; private set; }

        /// <summary>
        /// Information regarding the event origin.
        /// </summary>
        [DataMember]
        internal EventOriginInfo OriginInfo { get; private set; }

        /// <summary>
        /// True if this event must always be handled, else false.
        /// </summary>
        internal bool MustHandle { get; set; }

        /// <summary>
        /// Specifies that there must not be more than N instances of the
        /// event in the inbox queue of the receiver machine.
        /// </summary>
        internal int Assert { get; set; }

        /// <summary>
        /// Speciﬁes that during testing, an execution that increases the cardinality of the
        /// event beyond N in the receiver machine inbox queue must not be generated.
        /// </summary>
        internal int Assume { get; set; }

        /// <summary>
        /// User-defined hash of the event payload. The default value is 0. Set it to a custom value
        /// to improve the accuracy of liveness checking when state-caching is enabled.
        /// </summary>
        internal int HashedState { get; set; }

        /// <summary>
        /// The step from which this event was sent.
        /// </summary>
        internal int SendStep { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventInfo"/> class.
        /// </summary>
        internal EventInfo(Event e)
        {
            this.EventName = e.GetType().FullName;
            this.MustHandle = false;
            this.Assert = -1;
            this.Assume = -1;
            this.HashedState = 0;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventInfo"/> class.
        /// </summary>
        internal EventInfo(Event e, EventOriginInfo originInfo)
            : this(e)
        {
            this.OriginInfo = originInfo;
        }
    }
}
