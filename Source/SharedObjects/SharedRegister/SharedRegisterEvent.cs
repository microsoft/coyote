// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

namespace Microsoft.Coyote.SharedObjects
{
    /// <summary>
    /// Event used to communicate with a shared register machine.
    /// </summary>
    internal class SharedRegisterEvent : Event
    {
        /// <summary>
        /// Supported shared register operations.
        /// </summary>
        internal enum SharedRegisterOperation
        {
            GET,
            SET,
            UPDATE
        }

        /// <summary>
        /// The operation stored in this event.
        /// </summary>
        public SharedRegisterOperation Operation { get; private set; }

        /// <summary>
        /// The shared register value stored in this event.
        /// </summary>
        public object Value { get; private set; }

        /// <summary>
        /// The shared register func stored in this event.
        /// </summary>
        public object Func { get; private set; }

        /// <summary>
        /// The sender machine stored in this event.
        /// </summary>
        public MachineId Sender { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SharedRegisterEvent"/> class.
        /// </summary>
        private SharedRegisterEvent(SharedRegisterOperation op, object value, object func, MachineId sender)
        {
            this.Operation = op;
            this.Value = value;
            this.Func = func;
            this.Sender = sender;
        }

        /// <summary>
        /// Creates a new event for the 'UPDATE' operation.
        /// </summary>
        public static SharedRegisterEvent UpdateEvent(object func, MachineId sender)
        {
            return new SharedRegisterEvent(SharedRegisterOperation.UPDATE, null, func, sender);
        }

        /// <summary>
        /// Creates a new event for the 'SET' operation.
        /// </summary>
        public static SharedRegisterEvent SetEvent(object value)
        {
            return new SharedRegisterEvent(SharedRegisterOperation.SET, value, null, null);
        }

        /// <summary>
        /// Creates a new event for the 'GET' operation.
        /// </summary>
        public static SharedRegisterEvent GetEvent(MachineId sender)
        {
            return new SharedRegisterEvent(SharedRegisterOperation.GET, null, null, sender);
        }
    }
}
