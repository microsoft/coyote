// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Runtime.Serialization;

namespace Microsoft.Coyote.Machines
{
    /// <summary>
    /// The wild card event.
    /// </summary>
    [DataContract]
    public sealed class WildCardEvent : Event
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WildCardEvent"/> class.
        /// </summary>
        public WildCardEvent()
            : base()
        {
        }
    }
}
