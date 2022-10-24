// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Specifications;

namespace Microsoft.Coyote.Tests.Common.Events
{
    /// <summary>
    /// Basic event that contains no payload.
    /// </summary>
    public sealed class MonitorUnitEvent : Monitor.Event
    {
        /// <summary>
        /// Gets an instance of this event.
        /// </summary>
        public static MonitorUnitEvent Instance { get; } = new MonitorUnitEvent();

        /// <summary>
        /// Initializes a new instance of the <see cref="MonitorUnitEvent"/> class.
        /// </summary>
        private MonitorUnitEvent()
            : base()
        {
        }
    }
}
