// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Coyote.Tests.Common.Events
{
    /// <summary>
    /// Basic event that contains no payload.
    /// </summary>
    public sealed class UnitEvent : Event
    {
        /// <summary>
        /// Gets an instance of this event.
        /// </summary>
        public static UnitEvent Instance { get; } = new UnitEvent();

        /// <summary>
        /// Initializes a new instance of the <see cref="UnitEvent"/> class.
        /// </summary>
        private UnitEvent()
            : base()
        {
        }
    }
}
