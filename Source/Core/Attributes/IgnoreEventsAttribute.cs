// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Coyote
{
    /// <summary>
    /// Attribute for declaring what events should be ignored in
    /// a machine state.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class IgnoreEventsAttribute : Attribute
    {
        /// <summary>
        /// Event types.
        /// </summary>
        internal Type[] Events;

        /// <summary>
        /// Initializes a new instance of the <see cref="IgnoreEventsAttribute"/> class.
        /// </summary>
        /// <param name="eventTypes">Event types</param>
        public IgnoreEventsAttribute(params Type[] eventTypes)
        {
            this.Events = eventTypes;
        }
    }
}
