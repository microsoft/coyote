// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Machines;
using Microsoft.Coyote.Machines.Timers;

namespace Microsoft.Coyote.TestingServices.Timers
{
    /// <summary>
    /// Defines a timer elapsed event that is sent from a timer to the machine that owns the timer.
    /// </summary>
    internal class TimerSetupEvent : Event
    {
        /// <summary>
        /// Stores information about the timer.
        /// </summary>
        internal readonly TimerInfo Info;

        /// <summary>
        /// The machine that owns the timer.
        /// </summary>
        internal readonly Machine Owner;

        /// <summary>
        /// Adjusts the probability of firing a timeout event.
        /// </summary>
        internal readonly uint Delay;

        /// <summary>
        /// Initializes a new instance of the <see cref="TimerSetupEvent"/> class.
        /// </summary>
        /// <param name="info">Stores information about the timer.</param>
        /// <param name="owner">The machine that owns the timer.</param>
        /// <param name="delay">Adjusts the probability of firing a timeout event.</param>
        internal TimerSetupEvent(TimerInfo info, Machine owner, uint delay)
        {
            this.Info = info;
            this.Owner = owner;
            this.Delay = delay;
        }
    }
}
