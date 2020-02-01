// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Coyote.Actors.Timers
{
    /// <summary>
    /// Defines a timer elapsed event that is sent from a timer to the actor that owns the timer.
    /// </summary>
    /// <remarks>
    /// See <see href="/coyote/learn/programming-models/actors/timers">Using Timers</see> for more information.
    /// </remarks>
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
