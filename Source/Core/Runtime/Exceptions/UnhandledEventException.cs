// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Coyote.Runtime
{
    /// <summary>
    /// Signals that an actor received an unhandled event.
    /// </summary>
    public sealed class UnhandledEventException : RuntimeException
    {
        /// <summary>
        ///  The unhandled event.
        /// </summary>
        public Event UnhandledEvent;

        /// <summary>
        /// The name of the current state, if the actor that threw the exception
        /// is a state machine and a state exists, else null.
        /// </summary>
        public string CurrentStateName;

        /// <summary>
        /// Initializes a new instance of the <see cref="UnhandledEventException"/> class.
        /// </summary>
        /// <param name="unhandledEvent">The event that was unhandled.</param>
        /// <param name="currentStateName">The name of the current state, if the actor that threw the exception
        /// is a state machine and a state exists, else null.</param>
        /// <param name="message">The exception message.</param>
        internal UnhandledEventException(Event unhandledEvent, string currentStateName, string message)
            : base(message)
        {
            this.CurrentStateName = currentStateName;
            this.UnhandledEvent = unhandledEvent;
        }
    }
}
