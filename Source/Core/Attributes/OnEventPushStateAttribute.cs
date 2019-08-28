// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Coyote
{
    /// <summary>
    /// Attribute for declaring which state a machine should push transition to
    /// when it receives an event in a given state.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class OnEventPushStateAttribute : Attribute
    {
        /// <summary>
        /// Event type.
        /// </summary>
        internal Type Event;

        /// <summary>
        /// State type.
        /// </summary>
        internal Type State;

        /// <summary>
        /// Initializes a new instance of the <see cref="OnEventPushStateAttribute"/> class.
        /// </summary>
        /// <param name="eventType">Event type</param>
        /// <param name="stateType">State type</param>
        public OnEventPushStateAttribute(Type eventType, Type stateType)
        {
            this.Event = eventType;
            this.State = stateType;
        }
    }
}
