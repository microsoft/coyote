// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Runtime.Serialization;

namespace Microsoft.Coyote
{
    /// <summary>
    /// The goto state event.
    /// </summary>
    [DataContract]
    internal sealed class GotoStateEvent : Event
    {
        /// <summary>
        /// Type of the state to transition to.
        /// </summary>
        public readonly Type State;

        /// <summary>
        /// Initializes a new instance of the <see cref="GotoStateEvent"/> class.
        /// </summary>
        /// <param name="s">Type of the state.</param>
        public GotoStateEvent(Type s)
            : base()
        {
            this.State = s;
        }
    }
}
