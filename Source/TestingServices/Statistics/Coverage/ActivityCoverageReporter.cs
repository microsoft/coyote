// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace Microsoft.Coyote.TestingServices.Coverage
{
    /// <summary>
    /// The Coyote code coverage reporter.
    /// </summary>
    public class ActivityCoverageReporter
    {
        /// <summary>
        /// Data structure containing information
        /// regarding testing coverage.
        /// </summary>
        private readonly CoverageInfo CoverageInfo;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActivityCoverageReporter"/> class.
        /// </summary>
        public ActivityCoverageReporter(CoverageInfo coverageInfo)
        {
            this.CoverageInfo = coverageInfo;
        }

        /// <summary>
        /// Emits the visualization graph.
        /// </summary>
        public void EmitVisualizationGraph(string graphFile)
        {
            using (var writer = new XmlTextWriter(graphFile, Encoding.UTF8))
            {
                this.WriteVisualizationGraph(writer);
            }
        }

        /// <summary>
        /// Emits the code coverage report.
        /// </summary>
        public void EmitCoverageReport(string coverageFile)
        {
            using (var writer = new StreamWriter(coverageFile))
            {
                this.WriteCoverageText(writer);
            }
        }

        /// <summary>
        /// Writes the visualization graph.
        /// </summary>
        private void WriteVisualizationGraph(XmlTextWriter writer)
        {
            // Starts document.
            writer.WriteStartDocument(true);
            writer.Formatting = Formatting.Indented;
            writer.Indentation = 2;

            // Starts DirectedGraph element.
            writer.WriteStartElement("DirectedGraph", @"http://schemas.microsoft.com/vs/2009/dgml");

            // Starts Nodes element.
            writer.WriteStartElement("Nodes");

            // Iterates machines.
            foreach (var machine in this.CoverageInfo.MachinesToStates.Keys)
            {
                writer.WriteStartElement("Node");
                writer.WriteAttributeString("Id", machine);
                writer.WriteAttributeString("Group", "Expanded");
                writer.WriteEndElement();
            }

            // Iterates states.
            foreach (var tup in this.CoverageInfo.MachinesToStates)
            {
                var machine = tup.Key;
                foreach (var state in tup.Value)
                {
                    writer.WriteStartElement("Node");
                    writer.WriteAttributeString("Id", GetStateId(machine, state));
                    writer.WriteAttributeString("Label", state);
                    writer.WriteEndElement();
                }
            }

            // Ends Nodes element.
            writer.WriteEndElement();

            // Starts Links element.
            writer.WriteStartElement("Links");

            // Iterates states.
            foreach (var tup in this.CoverageInfo.MachinesToStates)
            {
                var machine = tup.Key;
                foreach (var state in tup.Value)
                {
                    writer.WriteStartElement("Link");
                    writer.WriteAttributeString("Source", machine);
                    writer.WriteAttributeString("Target", GetStateId(machine, state));
                    writer.WriteAttributeString("Category", "Contains");
                    writer.WriteEndElement();
                }
            }

            var parallelEdgeCounter = new Dictionary<Tuple<string, string>, int>();

            // Iterates transitions.
            foreach (var transition in this.CoverageInfo.Transitions)
            {
                var source = GetStateId(transition.MachineOrigin, transition.StateOrigin);
                var target = GetStateId(transition.MachineTarget, transition.StateTarget);
                var counter = 0;
                if (parallelEdgeCounter.ContainsKey(Tuple.Create(source, target)))
                {
                    counter = parallelEdgeCounter[Tuple.Create(source, target)];
                    parallelEdgeCounter[Tuple.Create(source, target)] = counter + 1;
                }
                else
                {
                    parallelEdgeCounter[Tuple.Create(source, target)] = 1;
                }

                writer.WriteStartElement("Link");
                writer.WriteAttributeString("Source", source);
                writer.WriteAttributeString("Target", target);
                writer.WriteAttributeString("Label", transition.EdgeLabel);
                if (counter != 0)
                {
                    writer.WriteAttributeString("Index", counter.ToString());
                }

                writer.WriteEndElement();
            }

            // Ends Links element.
            writer.WriteEndElement();

            // Ends DirectedGraph element.
            writer.WriteEndElement();

            // Ends document.
            writer.WriteEndDocument();
        }

        /// <summary>
        /// Writes the visualization text.
        /// </summary>
        internal void WriteCoverageText(TextWriter writer)
        {
            var machines = new List<string>(this.CoverageInfo.MachinesToStates.Keys);

            var uncoveredEvents = new HashSet<Tuple<string, string, string>>(this.CoverageInfo.RegisteredEvents);
            foreach (var transition in this.CoverageInfo.Transitions)
            {
                if (transition.MachineOrigin == transition.MachineTarget)
                {
                    uncoveredEvents.Remove(Tuple.Create(transition.MachineOrigin, transition.StateOrigin, transition.EdgeLabel));
                }
                else
                {
                    uncoveredEvents.Remove(Tuple.Create(transition.MachineTarget, transition.StateTarget, transition.EdgeLabel));
                }
            }

            string eventCoverage = this.CoverageInfo.RegisteredEvents.Count == 0 ? "100.0" :
                ((this.CoverageInfo.RegisteredEvents.Count - uncoveredEvents.Count) * 100.0 / this.CoverageInfo.RegisteredEvents.Count).ToString("F1");
            writer.WriteLine("Total event coverage: {0}%", eventCoverage);

            // Map from machines to states to registered events.
            var machineToStatesToEvents = new Dictionary<string, Dictionary<string, HashSet<string>>>();
            machines.ForEach(m => machineToStatesToEvents.Add(m, new Dictionary<string, HashSet<string>>()));
            machines.ForEach(m =>
            {
                foreach (var state in this.CoverageInfo.MachinesToStates[m])
                {
                    machineToStatesToEvents[m].Add(state, new HashSet<string>());
                }
            });

            foreach (var ev in this.CoverageInfo.RegisteredEvents)
            {
                machineToStatesToEvents[ev.Item1][ev.Item2].Add(ev.Item3);
            }

            // Maps from machines to transitions.
            var machineToOutgoingTransitions = new Dictionary<string, List<Transition>>();
            var machineToIncomingTransitions = new Dictionary<string, List<Transition>>();
            var machineToIntraTransitions = new Dictionary<string, List<Transition>>();

            machines.ForEach(m => machineToIncomingTransitions.Add(m, new List<Transition>()));
            machines.ForEach(m => machineToOutgoingTransitions.Add(m, new List<Transition>()));
            machines.ForEach(m => machineToIntraTransitions.Add(m, new List<Transition>()));

            foreach (var tr in this.CoverageInfo.Transitions)
            {
                if (tr.MachineOrigin == tr.MachineTarget)
                {
                    machineToIntraTransitions[tr.MachineOrigin].Add(tr);
                }
                else
                {
                    machineToIncomingTransitions[tr.MachineTarget].Add(tr);
                    machineToOutgoingTransitions[tr.MachineOrigin].Add(tr);
                }
            }

            // Per-machine data.
            foreach (var machine in machines)
            {
                writer.WriteLine("Machine: {0}", machine);
                writer.WriteLine("***************");

                var machineUncoveredEvents = new Dictionary<string, HashSet<string>>();
                foreach (var state in this.CoverageInfo.MachinesToStates[machine])
                {
                    machineUncoveredEvents.Add(state, new HashSet<string>(machineToStatesToEvents[machine][state]));
                }

                foreach (var tr in machineToIncomingTransitions[machine])
                {
                    machineUncoveredEvents[tr.StateTarget].Remove(tr.EdgeLabel);
                }

                foreach (var tr in machineToIntraTransitions[machine])
                {
                    machineUncoveredEvents[tr.StateOrigin].Remove(tr.EdgeLabel);
                }

                var numTotalEvents = 0;
                foreach (var tup in machineToStatesToEvents[machine])
                {
                    numTotalEvents += tup.Value.Count;
                }

                var numUncoveredEvents = 0;
                foreach (var tup in machineUncoveredEvents)
                {
                    numUncoveredEvents += tup.Value.Count;
                }

                eventCoverage = numTotalEvents == 0 ? "100.0" : ((numTotalEvents - numUncoveredEvents) * 100.0 / numTotalEvents).ToString("F1");
                writer.WriteLine("Machine event coverage: {0}%", eventCoverage);

                // Find uncovered states.
                var uncoveredStates = new HashSet<string>(this.CoverageInfo.MachinesToStates[machine]);
                foreach (var tr in machineToIntraTransitions[machine])
                {
                    uncoveredStates.Remove(tr.StateOrigin);
                    uncoveredStates.Remove(tr.StateTarget);
                }

                foreach (var tr in machineToIncomingTransitions[machine])
                {
                    uncoveredStates.Remove(tr.StateTarget);
                }

                foreach (var tr in machineToOutgoingTransitions[machine])
                {
                    uncoveredStates.Remove(tr.StateOrigin);
                }

                // State maps.
                var stateToIncomingEvents = new Dictionary<string, HashSet<string>>();
                foreach (var tr in machineToIncomingTransitions[machine])
                {
                    if (!stateToIncomingEvents.ContainsKey(tr.StateTarget))
                    {
                        stateToIncomingEvents.Add(tr.StateTarget, new HashSet<string>());
                    }

                    stateToIncomingEvents[tr.StateTarget].Add(tr.EdgeLabel);
                }

                var stateToOutgoingEvents = new Dictionary<string, HashSet<string>>();
                foreach (var tr in machineToOutgoingTransitions[machine])
                {
                    if (!stateToOutgoingEvents.ContainsKey(tr.StateOrigin))
                    {
                        stateToOutgoingEvents.Add(tr.StateOrigin, new HashSet<string>());
                    }

                    stateToOutgoingEvents[tr.StateOrigin].Add(tr.EdgeLabel);
                }

                var stateToOutgoingStates = new Dictionary<string, HashSet<string>>();
                var stateToIncomingStates = new Dictionary<string, HashSet<string>>();
                foreach (var tr in machineToIntraTransitions[machine])
                {
                    if (!stateToOutgoingStates.ContainsKey(tr.StateOrigin))
                    {
                        stateToOutgoingStates.Add(tr.StateOrigin, new HashSet<string>());
                    }

                    stateToOutgoingStates[tr.StateOrigin].Add(tr.StateTarget);

                    if (!stateToIncomingStates.ContainsKey(tr.StateTarget))
                    {
                        stateToIncomingStates.Add(tr.StateTarget, new HashSet<string>());
                    }

                    stateToIncomingStates[tr.StateTarget].Add(tr.StateOrigin);
                }

                // Per-state data.
                foreach (var state in this.CoverageInfo.MachinesToStates[machine])
                {
                    writer.WriteLine();
                    writer.WriteLine("\tState: {0}{1}", state, uncoveredStates.Contains(state) ? " is uncovered" : string.Empty);
                    if (!uncoveredStates.Contains(state))
                    {
                        eventCoverage = machineToStatesToEvents[machine][state].Count == 0 ? "100.0" :
                            ((machineToStatesToEvents[machine][state].Count - machineUncoveredEvents[state].Count) * 100.0 /
                              machineToStatesToEvents[machine][state].Count).ToString("F1");
                        writer.WriteLine("\t\tState event coverage: {0}%", eventCoverage);
                    }

                    if (stateToIncomingEvents.ContainsKey(state) && stateToIncomingEvents[state].Count > 0)
                    {
                        writer.Write("\t\tEvents received: ");
                        foreach (var e in stateToIncomingEvents[state])
                        {
                            writer.Write("{0} ", e);
                        }

                        writer.WriteLine();
                    }

                    if (stateToOutgoingEvents.ContainsKey(state) && stateToOutgoingEvents[state].Count > 0)
                    {
                        writer.Write("\t\tEvents sent: ");
                        foreach (var e in stateToOutgoingEvents[state])
                        {
                            writer.Write("{0} ", e);
                        }

                        writer.WriteLine();
                    }

                    if (machineUncoveredEvents.ContainsKey(state) && machineUncoveredEvents[state].Count > 0)
                    {
                        writer.Write("\t\tEvents not covered: ");
                        foreach (var e in machineUncoveredEvents[state])
                        {
                            writer.Write("{0} ", e);
                        }

                        writer.WriteLine();
                    }

                    if (stateToIncomingStates.ContainsKey(state) && stateToIncomingStates[state].Count > 0)
                    {
                        writer.Write("\t\tPrevious states: ");
                        foreach (var s in stateToIncomingStates[state])
                        {
                            writer.Write("{0} ", s);
                        }

                        writer.WriteLine();
                    }

                    if (stateToOutgoingStates.ContainsKey(state) && stateToOutgoingStates[state].Count > 0)
                    {
                        writer.Write("\t\tNext states: ");
                        foreach (var s in stateToOutgoingStates[state])
                        {
                            writer.Write("{0} ", s);
                        }

                        writer.WriteLine();
                    }
                }

                writer.WriteLine();
            }
        }

        private static string GetStateId(string machineName, string stateName) =>
            string.Format("{0}::{1}", stateName, machineName);
    }
}
