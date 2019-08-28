// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Runtime.Serialization;

namespace Microsoft.Coyote
{
    /// <summary>
    /// Signals that a machine has reached quiescence.
    /// </summary>
    [DataContract]
    internal sealed class QuiescentEvent : Event
    {
        /// <summary>
        /// The id of the machine that has reached quiescence.
        /// </summary>
        public MachineId MachineId;

        /// <summary>
        /// Initializes a new instance of the <see cref="QuiescentEvent"/> class.
        /// </summary>
        /// <param name="mid">The id of the machine that has reached quiescence.</param>
        public QuiescentEvent(MachineId mid)
        {
            this.MachineId = mid;
        }
    }
}
