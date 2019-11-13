// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Microsoft.Coyote.Actors.Timers
{
    /// <summary>
    /// Stores information about a timer that can send timeout events to its owner actor.
    /// </summary>
    public class TimerInfo : IEquatable<TimerInfo>
    {
        /// <summary>
        /// The unique id of the timer.
        /// </summary>
        private readonly Guid Id;

        /// <summary>
        /// The id of the actor that owns the timer.
        /// </summary>
        public readonly ActorId OwnerId;

        /// <summary>
        /// The amount of time to wait before sending the first timeout event.
        /// </summary>
        public readonly TimeSpan DueTime;

        /// <summary>
        /// The time interval between timeout events.
        /// </summary>
        public readonly TimeSpan Period;

        /// <summary>
        /// The optional payload of the timer. This is null if there is no payload.
        /// </summary>
        public readonly object Payload;

        /// <summary>
        /// Initializes a new instance of the <see cref="TimerInfo"/> class.
        /// </summary>
        /// <param name="ownerId">The id of the actor that owns this timer.</param>
        /// <param name="dueTime">The amount of time to wait before sending the first timeout event.</param>
        /// <param name="period">The time interval between timeout events.</param>
        /// <param name="payload">Optional payload of the timeout event.</param>
        internal TimerInfo(ActorId ownerId, TimeSpan dueTime, TimeSpan period, object payload)
        {
            this.Id = Guid.NewGuid();
            this.OwnerId = ownerId;
            this.DueTime = dueTime;
            this.Period = period;
            this.Payload = payload;
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj is TimerInfo timerInfo)
            {
                return this.Id == timerInfo.Id;
            }

            return false;
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        public override int GetHashCode() => this.Id.GetHashCode();

        /// <summary>
        /// Returns a string that represents the current instance.
        /// </summary>
        public override string ToString() => this.Id.ToString();

        /// <summary>
        /// Indicates whether the specified <see cref="TimerInfo"/> is equal
        /// to the current <see cref="TimerInfo"/>.
        /// </summary>
        public bool Equals(TimerInfo other)
        {
            return this.Equals((object)other);
        }

        bool IEquatable<TimerInfo>.Equals(TimerInfo other)
        {
            return this.Equals(other);
        }
    }
}
