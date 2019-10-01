// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Microsoft.Coyote.TestingServices.Coverage
{
    /// <summary>
    /// Class for storing coverage-specific data
    /// across multiple testing iterations.
    /// </summary>
    [DataContract]
    public class CoverageInfo
    {
        /// <summary>
        /// Map from machines to states.
        /// </summary>
        [DataMember]
        public Dictionary<string, HashSet<string>> MachinesToStates { get; private set; }

        /// <summary>
        /// Set of (machines, states, registered events).
        /// </summary>
        [DataMember]
        public HashSet<Tuple<string, string, string>> RegisteredEvents { get; private set; }

        /// <summary>
        /// Set of machine transitions.
        /// </summary>
        [DataMember]
        public HashSet<Transition> Transitions { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CoverageInfo"/> class.
        /// </summary>
        public CoverageInfo()
        {
            this.MachinesToStates = new Dictionary<string, HashSet<string>>();
            this.RegisteredEvents = new HashSet<Tuple<string, string, string>>();
            this.Transitions = new HashSet<Transition>();
        }

        /// <summary>
        /// Checks if the machine type has already been registered for coverage.
        /// </summary>
        public bool IsMachineDeclared(string machineName) => this.MachinesToStates.ContainsKey(machineName);

        /// <summary>
        /// Adds a new transition.
        /// </summary>
        public void AddTransition(string machineOrigin, string stateOrigin, string edgeLabel,
            string machineTarget, string stateTarget)
        {
            this.AddState(machineOrigin, stateOrigin);
            this.AddState(machineTarget, stateTarget);
            this.Transitions.Add(new Transition(machineOrigin, stateOrigin,
                edgeLabel, machineTarget, stateTarget));
        }

        /// <summary>
        /// Declares a state.
        /// </summary>
        public void DeclareMachineState(string machine, string state) => this.AddState(machine, state);

        /// <summary>
        /// Declares a registered state, event pair.
        /// </summary>
        public void DeclareStateEvent(string machine, string state, string eventName)
        {
            this.AddState(machine, state);
            this.RegisteredEvents.Add(Tuple.Create(machine, state, eventName));
        }

        /// <summary>
        /// Merges the information from the specified
        /// coverage info. This is not thread-safe.
        /// </summary>
        public void Merge(CoverageInfo coverageInfo)
        {
            foreach (var machine in coverageInfo.MachinesToStates)
            {
                foreach (var state in machine.Value)
                {
                    this.DeclareMachineState(machine.Key, state);
                }
            }

            foreach (var tup in coverageInfo.RegisteredEvents)
            {
                this.DeclareStateEvent(tup.Item1, tup.Item2, tup.Item3);
            }

            foreach (var transition in coverageInfo.Transitions)
            {
                this.AddTransition(transition.MachineOrigin, transition.StateOrigin,
                    transition.EdgeLabel, transition.MachineTarget, transition.StateTarget);
            }
        }

        /// <summary>
        /// Adds a new state.
        /// </summary>
        private void AddState(string machineName, string stateName)
        {
            if (!this.MachinesToStates.ContainsKey(machineName))
            {
                this.MachinesToStates.Add(machineName, new HashSet<string>());
            }

            this.MachinesToStates[machineName].Add(stateName);
        }
    }
}
