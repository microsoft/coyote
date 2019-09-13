// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.Coyote.Machines;

namespace Microsoft.Coyote.SharedObjects
{
    /// <summary>
    /// Event containing the value of a shared register.
    /// </summary>
    internal class SharedRegisterResponseEvent<T> : Event
    {
        /// <summary>
        /// Value.
        /// </summary>
        internal T Value;

        /// <summary>
        /// Initializes a new instance of the <see cref="SharedRegisterResponseEvent{T}"/> class.
        /// </summary>
        internal SharedRegisterResponseEvent(T value)
        {
            this.Value = value;
        }
    }
}
