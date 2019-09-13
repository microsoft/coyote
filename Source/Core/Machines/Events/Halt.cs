// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Runtime.Serialization;

namespace Microsoft.Coyote.Machines
{
    /// <summary>
    /// The halt event.
    /// </summary>
    [DataContract]
    public sealed class Halt : Event
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Halt"/> class.
        /// </summary>
        public Halt()
            : base()
        {
        }
    }
}
