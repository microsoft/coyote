// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Runtime.Serialization;

namespace Microsoft.Coyote.TestingServices.Coverage
{
    /// <summary>
    /// Specifies a program transition.
    /// </summary>
    [DataContract]
    public struct Transition
    {
        /// <summary>
        /// The origin machine.
        /// </summary>
        [DataMember]
        public readonly string MachineOrigin;

        /// <summary>
        /// The origin state.
        /// </summary>
        [DataMember]
        public readonly string StateOrigin;

        /// <summary>
        /// The edge label.
        /// </summary>
        [DataMember]
        public readonly string EdgeLabel;

        /// <summary>
        /// The target machine.
        /// </summary>
        [DataMember]
        public readonly string MachineTarget;

        /// <summary>
        /// The target state.
        /// </summary>
        [DataMember]
        public readonly string StateTarget;

        /// <summary>
        /// Initializes a new instance of the <see cref="Transition"/> struct.
        /// </summary>
        public Transition(string machineOrigin, string stateOrigin, string edgeLabel,
            string machineTarget, string stateTarget)
        {
            this.MachineOrigin = machineOrigin;
            this.StateOrigin = stateOrigin;
            this.EdgeLabel = edgeLabel;
            this.MachineTarget = machineTarget;
            this.StateTarget = stateTarget;
        }

        /// <summary>
        /// Pretty print.
        /// </summary>
        public override string ToString()
        {
            if (this.MachineOrigin == this.MachineTarget)
            {
                return string.Format("{0}: {1} --{2}--> {3}", this.MachineOrigin, this.StateOrigin, this.EdgeLabel, this.StateTarget);
            }

            return string.Format("({0}, {1}) --{2}--> ({3}, {4})", this.MachineOrigin, this.StateOrigin, this.EdgeLabel, this.MachineTarget, this.StateTarget);
        }
    }
}
