// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Coyote
{
    /// <summary>
    /// Attribute for declaring which state a machine should transition to
    /// when it receives an event in a given state.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class OnEventGotoStateAttribute : Attribute
    {
        /// <summary>
        /// Event type.
        /// </summary>
        internal readonly Type Event;

        /// <summary>
        /// State type.
        /// </summary>
        internal readonly Type State;

        /// <summary>
        /// Action name.
        /// </summary>
        internal readonly string Action;

        /// <summary>
        /// Initializes a new instance of the <see cref="OnEventGotoStateAttribute"/> class.
        /// </summary>
        /// <param name="eventType">Event type</param>
        /// <param name="stateType">State type</param>
        public OnEventGotoStateAttribute(Type eventType, Type stateType)
        {
            this.Event = eventType;
            this.State = stateType;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OnEventGotoStateAttribute"/> class.
        /// </summary>
        /// <param name="eventType">Event type</param>
        /// <param name="stateType">State type</param>
        /// <param name="actionName">Name of action to perform on exit</param>
        public OnEventGotoStateAttribute(Type eventType, Type stateType, string actionName)
        {
            this.Event = eventType;
            this.State = stateType;
            this.Action = actionName;
        }
    }
}
