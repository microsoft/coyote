// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Coyote.Machines.Timers
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
