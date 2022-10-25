// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Runtime.Serialization;
using Microsoft.Coyote.Coverage;

namespace Microsoft.Coyote.Actors.Coverage
{
    /// <summary>
    /// Class for storing actor coverage-specific data across multiple testing iterations.
    /// </summary>
    [DataContract]
    public class ActorCoverageInfo : CoverageInfo
    {
        /// <summary>
        /// Set of known machines.
        /// </summary>
        [DataMember]
        public HashSet<string> Machines { get; private set; }

        /// <summary>
        /// Map from machines to set of all states states defined in that machine.
        /// </summary>
        [DataMember]
        public Dictionary<string, HashSet<string>> MachinesToStates { get; private set; }

        /// <summary>
        /// Set of (machine + "." + state => registered events). So all events that can
        /// get us into each state.
        /// </summary>
        [DataMember]
        public Dictionary<string, HashSet<string>> RegisteredActorEvents { get; private set; }

        /// <summary>
        /// Information about events sent and received.
        /// </summary>
        [DataMember]
        public ActorEventCoverage ActorEventInfo { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ActorCoverageInfo"/> class.
        /// </summary>
        public ActorCoverageInfo()
            : base()
        {
            this.Machines = new HashSet<string>();
            this.MachinesToStates = new Dictionary<string, HashSet<string>>();
            this.RegisteredActorEvents = new Dictionary<string, HashSet<string>>();
        }

        /// <summary>
        /// Checks if the machine type has already been registered for coverage.
        /// </summary>
        public bool IsMachineDeclared(string machineName) => this.MachinesToStates.ContainsKey(machineName);

        /// <summary>
        /// Declares a machine state.
        /// </summary>
        public void DeclareMachineState(string machine, string state) => this.AddMachineState(machine, state);

        /// <summary>
        /// Declares a registered machine state-event pair.
        /// </summary>
        public void DeclareMachineStateEventPair(string machine, string state, string eventName)
        {
            this.AddMachineState(machine, state);

            string key = machine + "." + state;
            this.AddActorEvent(key, eventName);
        }

        /// <summary>
        /// Adds a new machine state.
        /// </summary>
        private void AddMachineState(string machineName, string stateName)
        {
            this.Machines.Add(machineName);
            if (!this.MachinesToStates.ContainsKey(machineName))
            {
                this.MachinesToStates.Add(machineName, new HashSet<string>());
            }

            this.MachinesToStates[machineName].Add(stateName);
        }

        /// <summary>
        /// Adds a new actor state.
        /// </summary>
        private void AddActorEvent(string key, string eventName)
        {
            if (!this.RegisteredActorEvents.ContainsKey(key))
            {
                this.RegisteredActorEvents.Add(key, new HashSet<string>());
            }

            this.RegisteredActorEvents[key].Add(eventName);
        }

        /// <inheritdoc/>
        public override void Merge(CoverageInfo coverageInfo)
        {
            if (coverageInfo is ActorCoverageInfo actorCoverageInfo)
            {
                foreach (var machine in actorCoverageInfo.Machines)
                {
                    this.Machines.Add(machine);
                }

                foreach (var machine in actorCoverageInfo.MachinesToStates)
                {
                    foreach (var state in machine.Value)
                    {
                        this.DeclareMachineState(machine.Key, state);
                    }
                }

                foreach (var tup in actorCoverageInfo.RegisteredActorEvents)
                {
                    foreach (var e in tup.Value)
                    {
                        this.AddActorEvent(tup.Key, e);
                    }
                }

                if (this.ActorEventInfo is null)
                {
                    this.ActorEventInfo = actorCoverageInfo.ActorEventInfo;
                }
                else if (actorCoverageInfo.ActorEventInfo != null && this.ActorEventInfo != actorCoverageInfo.ActorEventInfo)
                {
                    this.ActorEventInfo.Merge(actorCoverageInfo.ActorEventInfo);
                }
            }

            base.Merge(coverageInfo);
        }
    }
}
