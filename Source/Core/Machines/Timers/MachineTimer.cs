// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Threading;

namespace Microsoft.Coyote.Machines.Timers
{
    /// <summary>
    /// A timer that can send timeout events to its owner machine.
    /// </summary>
    internal sealed class MachineTimer : IMachineTimer
    {
        /// <summary>
        /// Stores information about this timer.
        /// </summary>
        public TimerInfo Info { get; private set; }

        /// <summary>
        /// The machine that owns this timer.
        /// </summary>
        private readonly Machine Owner;

        /// <summary>
        /// The internal timer.
        /// </summary>
        private readonly Timer InternalTimer;

        /// <summary>
        /// The timeout event.
        /// </summary>
        private readonly TimerElapsedEvent TimeoutEvent;

        /// <summary>
        /// Initializes a new instance of the <see cref="MachineTimer"/> class.
        /// </summary>
        /// <param name="info">Stores information about this timer.</param>
        /// <param name="owner">The machine that owns this timer.</param>
        public MachineTimer(TimerInfo info, Machine owner)
        {
            this.Info = info;
            this.Owner = owner;

            this.TimeoutEvent = new TimerElapsedEvent(this.Info);

            // To avoid a race condition between assigning the field of the timer
            // and HandleTimeout accessing the field before the assignment happens,
            // we first create a timer that cannot get triggered, then assign it to
            // the field, and finally we start the timer.
            this.InternalTimer = new Timer(this.HandleTimeout, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            this.InternalTimer.Change(this.Info.DueTime, Timeout.InfiniteTimeSpan);
        }

        /// <summary>
        /// Handles the timeout.
        /// </summary>
        private void HandleTimeout(object state)
        {
            // Send a timeout event.
            this.Owner.Runtime.SendEvent(this.Owner.Id, this.TimeoutEvent);

            if (this.Info.Period.TotalMilliseconds >= 0)
            {
                // The timer is periodic, so schedule the next timeout.
                try
                {
                    // Start the next timeout period.
                    this.InternalTimer.Change(this.Info.Period, Timeout.InfiniteTimeSpan);
                }
                catch (ObjectDisposedException)
                {
                    // Benign race condition while disposing the timer.
                }
            }
        }

        /// <summary>
        /// Determines whether the specified System.Object is equal
        /// to the current System.Object.
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj is MachineTimer timer)
            {
                return this.Info == timer.Info;
            }

            return false;
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        public override int GetHashCode() => this.Info.GetHashCode();

        /// <summary>
        /// Returns a string that represents the current instance.
        /// </summary>
        public override string ToString() => this.Info.ToString();

        /// <summary>
        /// Indicates whether the specified <see cref="MachineId"/> is equal
        /// to the current <see cref="MachineId"/>.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>true if the current object is equal to the other parameter; otherwise, false.</returns>
        public bool Equals(MachineTimer other)
        {
            return this.Equals((object)other);
        }

        /// <summary>
        /// Disposes the resources held by this timer.
        /// </summary>
        public void Dispose()
        {
            this.InternalTimer.Dispose();
        }
    }
}
