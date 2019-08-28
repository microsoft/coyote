// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Coyote
{
    /// <summary>
    /// Attribute for declaring what action a machine should perform
    /// when it receives an event in a given state.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class OnEventDoActionAttribute : Attribute
    {
        /// <summary>
        /// Event type.
        /// </summary>
        internal Type Event;

        /// <summary>
        /// Action name.
        /// </summary>
        internal string Action;

        /// <summary>
        /// Initializes a new instance of the <see cref="OnEventDoActionAttribute"/> class.
        /// </summary>
        /// <param name="eventType">Event type</param>
        /// <param name="actionName">Action name</param>
        public OnEventDoActionAttribute(Type eventType, string actionName)
        {
            this.Event = eventType;
            this.Action = actionName;
        }
    }
}
