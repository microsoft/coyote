// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Actors;

namespace Microsoft.Coyote.SharedObjects
{
    /// <summary>
    /// Event used to communicate with a shared counter machine.
    /// </summary>
    internal class SharedCounterEvent : Event
    {
        /// <summary>
        /// Supported shared counter operations.
        /// </summary>
        internal enum SharedCounterOperation
        {
            GET,
            SET,
            INC,
            DEC,
            ADD,
            CAS
        }

        /// <summary>
        /// The operation stored in this event.
        /// </summary>
        public SharedCounterOperation Operation { get; private set; }

        /// <summary>
        /// The shared counter value stored in this event.
        /// </summary>
        public int Value { get; private set; }

        /// <summary>
        /// Comparand value stored in this event.
        /// </summary>
        public int Comparand { get; private set; }

        /// <summary>
        /// The sender machine stored in this event.
        /// </summary>
        public ActorId Sender { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SharedCounterEvent"/> class.
        /// </summary>
        private SharedCounterEvent(SharedCounterOperation op, int value, int comparand, ActorId sender)
        {
            this.Operation = op;
            this.Value = value;
            this.Comparand = comparand;
            this.Sender = sender;
        }

        /// <summary>
        /// Creates a new event for the 'INC' operation.
        /// </summary>
        public static SharedCounterEvent IncrementEvent()
        {
            return new SharedCounterEvent(SharedCounterOperation.INC, 0, 0, null);
        }

        /// <summary>
        /// Creates a new event for the 'DEC' operation.
        /// </summary>
        public static SharedCounterEvent DecrementEvent()
        {
            return new SharedCounterEvent(SharedCounterOperation.DEC, 0, 0, null);
        }

        /// <summary>
        /// Creates a new event for the 'SET' operation.
        /// </summary>
        public static SharedCounterEvent SetEvent(ActorId sender, int value)
        {
            return new SharedCounterEvent(SharedCounterOperation.SET, value, 0, sender);
        }

        /// <summary>
        /// Creates a new event for the 'GET' operation.
        /// </summary>
        public static SharedCounterEvent GetEvent(ActorId sender)
        {
            return new SharedCounterEvent(SharedCounterOperation.GET, 0, 0, sender);
        }

        /// <summary>
        /// Creates a new event for the 'ADD' operation.
        /// </summary>
        public static SharedCounterEvent AddEvent(ActorId sender, int value)
        {
            return new SharedCounterEvent(SharedCounterOperation.ADD, value, 0, sender);
        }

        /// <summary>
        /// Creates a new event for the 'CAS' operation.
        /// </summary>
        public static SharedCounterEvent CasEvent(ActorId sender, int value, int comparand)
        {
            return new SharedCounterEvent(SharedCounterOperation.CAS, value, comparand, sender);
        }
    }
}
