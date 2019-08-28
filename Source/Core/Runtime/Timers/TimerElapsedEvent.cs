// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

namespace Microsoft.Coyote.Timers
{
    /// <summary>
    /// Defines a timer elapsed event that is sent from a timer to the machine that owns the timer.
    /// </summary>
    public class TimerElapsedEvent : Event
    {
        /// <summary>
        /// Stores information about the timer.
        /// </summary>
        public readonly TimerInfo Info;

        /// <summary>
        /// Initializes a new instance of the <see cref="TimerElapsedEvent"/> class.
        /// </summary>
        /// <param name="info">Stores information about the timer.</param>
        internal TimerElapsedEvent(TimerInfo info)
        {
            this.Info = info;
        }
    }
}
