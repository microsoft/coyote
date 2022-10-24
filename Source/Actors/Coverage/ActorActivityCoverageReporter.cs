// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Coyote.Coverage;

namespace Microsoft.Coyote.Actors.Coverage
{
    /// <summary>
    /// Reports actor activity coverage.
    /// </summary>
    internal class ActorActivityCoverageReporter : ActivityCoverageReporter
    {
        /// <summary>
        /// Data structure containing information regarding testing coverage.
        /// </summary>
        private readonly ActorCoverageInfo CoverageInfo;

        /// <summary>
        /// Set of built in events which we hide in the coverage report.
        /// </summary>
        private readonly HashSet<string> BuiltInEvents = new HashSet<string>();

        /// <summary>
        /// Initializes a new instance of the <see cref="ActorActivityCoverageReporter"/> class.
        /// </summary>
        public ActorActivityCoverageReporter(ActorCoverageInfo coverageInfo)
            : base(coverageInfo)
        {
            this.CoverageInfo = coverageInfo;
            this.BuiltInEvents.Add(typeof(GotoStateEvent).FullName);
            this.BuiltInEvents.Add(typeof(PushStateEvent).FullName);
            this.BuiltInEvents.Add(typeof(DefaultEvent).FullName);
        }

        /// <summary>
        /// Writes the visualization text.
        /// </summary>
        internal override void WriteCoverageText(TextWriter writer)
        {
            var machines = new List<string>(this.CoverageInfo.Machines);
            machines.Sort(StringComparer.Ordinal);

            var machineTypes = new Dictionary<string, string>();

            bool hasExternalSource = false;
            string externalSrcId = "ExternalCode";

            // look for any external source links.
            foreach (var link in this.CoverageInfo.CoverageGraph.Links)
            {
                string srcId = link.Source.Id;
                if (srcId == externalSrcId && !hasExternalSource)
                {
                    machines.Add(srcId);
                    hasExternalSource = true;
                }
            }

            foreach (var node in this.CoverageInfo.CoverageGraph.Nodes)
            {
                string id = node.Id;
                if (machines.Contains(id))
                {
                    machineTypes[id] = node.Category ?? "StateMachine";
                }
            }

            // machines + "." + states => registered events
            var uncoveredEvents = new Dictionary<string, HashSet<string>>();
            foreach (var item in this.CoverageInfo.RegisteredActorEvents)
            {
                uncoveredEvents[item.Key] = new HashSet<string>(item.Value);
            }

            int totalEvents = (from h in uncoveredEvents select h.Value.Count).Sum();

            // Now use the graph to find incoming links to each state and remove those from the list of uncovered events.
            this.RemoveCoveredEvents(uncoveredEvents);

            int totalUncoveredEvents = (from h in uncoveredEvents select h.Value.Count).Sum();

            string eventCoverage = totalEvents is 0 ? "100.0" : ((totalEvents - totalUncoveredEvents) * 100.0 / totalEvents).ToString("F1");

            WriteHeader(writer, string.Format("Total event coverage: {0}%", eventCoverage));

            // Per-machine data.
            foreach (var machine in machines)
            {
                machineTypes.TryGetValue(machine, out string machineType);
                WriteHeader(writer, string.Format("{0}: {1}", machineType, machine));

                // find all possible events for this machine.
                var uncoveredMachineEvents = new Dictionary<string, HashSet<string>>();
                var allMachineEvents = new Dictionary<string, HashSet<string>>();

                foreach (var item in this.CoverageInfo.RegisteredActorEvents)
                {
                    var id = GetMachineId(item.Key);
                    if (id == machine)
                    {
                        uncoveredMachineEvents[item.Key] = new HashSet<string>(item.Value);
                        allMachineEvents[item.Key] = new HashSet<string>(item.Value);
                    }
                }

                // Now use the graph to find incoming links to each state in this machine and remove those from the list of uncovered events.
                this.RemoveCoveredEvents(uncoveredMachineEvents);

                int totalMachineEvents = (from h in allMachineEvents select h.Value.Count).Sum();
                var totalUncoveredMachineEvents = (from h in uncoveredMachineEvents select h.Value.Count).Sum();

                eventCoverage = totalMachineEvents is 0 ? "100.0" : ((totalMachineEvents - totalUncoveredMachineEvents) * 100.0 / totalMachineEvents).ToString("F1");
                writer.WriteLine("Event coverage: {0}%", eventCoverage);

                if (!this.CoverageInfo.MachinesToStates.ContainsKey(machine))
                {
                    this.CoverageInfo.MachinesToStates[machine] = new HashSet<string>(new string[] { "ExternalState" });
                }

                // Per-state data.
                foreach (var state in this.CoverageInfo.MachinesToStates[machine])
                {
                    var key = machine + "." + state;
                    int totalStateEvents = (from h in allMachineEvents where h.Key == key select h.Value.Count).Sum();
                    int uncoveredStateEvents = (from h in uncoveredMachineEvents where h.Key == key select h.Value.Count).Sum();

                    writer.WriteLine();
                    writer.WriteLine("\tState: {0}{1}", state, totalStateEvents > 0 && totalStateEvents == uncoveredStateEvents ? " is uncovered" : string.Empty);
                    if (totalStateEvents is 0)
                    {
                        writer.WriteLine("\t\tState has no expected events, so coverage is 100%");
                    }
                    else if (totalStateEvents != uncoveredStateEvents)
                    {
                        eventCoverage = totalStateEvents is 0 ? "100.0" : ((totalStateEvents - uncoveredStateEvents) * 100.0 / totalStateEvents).ToString("F1");
                        writer.WriteLine("\t\tState event coverage: {0}%", eventCoverage);
                    }

                    // Now use the graph to find incoming links to each state in this machine
                    HashSet<string> stateIncomingStates = new HashSet<string>();
                    HashSet<string> stateOutgoingStates = new HashSet<string>();
                    foreach (var link in this.CoverageInfo.CoverageGraph.Links)
                    {
                        if (link.Category != "Contains")
                        {
                            string srcId = link.Source.Id;
                            string srcMachine = GetMachineId(srcId);
                            string targetId = link.Target.Id;
                            string targetMachine = GetMachineId(targetId);
                            bool intraMachineTransition = targetMachine == machine && srcMachine == machine;
                            if (intraMachineTransition)
                            {
                                foreach (string id in GetEventIds(link))
                                {
                                    if (targetId == key)
                                    {
                                        // we want to show incoming/outgoing states within the current machine only.
                                        stateIncomingStates.Add(GetStateName(srcId));
                                    }

                                    if (srcId == key)
                                    {
                                        // we want to show incoming/outgoing states within the current machine only.
                                        stateOutgoingStates.Add(GetStateName(targetId));
                                    }
                                }
                            }
                        }
                    }

                    HashSet<string> received = new HashSet<string>(this.CoverageInfo.ActorEventInfo.GetEventsReceived(key));
                    this.RemoveBuiltInEvents(received);

                    if (received.Count > 0)
                    {
                        writer.WriteLine("\t\tEvents received: {0}", string.Join(", ", SortHashSet(received)));
                    }

                    HashSet<string> sent = new HashSet<string>(this.CoverageInfo.ActorEventInfo.GetEventsSent(key));
                    this.RemoveBuiltInEvents(sent);

                    if (sent.Count > 0)
                    {
                        writer.WriteLine("\t\tEvents sent: {0}", string.Join(", ", SortHashSet(sent)));
                    }

                    var stateUncoveredEvents = (from h in uncoveredMachineEvents where h.Key == key select h.Value).FirstOrDefault();
                    if (stateUncoveredEvents != null && stateUncoveredEvents.Count > 0)
                    {
                        writer.WriteLine("\t\tEvents not covered: {0}", string.Join(", ", SortHashSet(stateUncoveredEvents)));
                    }

                    if (stateIncomingStates.Count > 0)
                    {
                        writer.WriteLine("\t\tPrevious states: {0}", string.Join(", ", SortHashSet(stateIncomingStates)));
                    }

                    if (stateOutgoingStates.Count > 0)
                    {
                        writer.WriteLine("\t\tNext states: {0}", string.Join(", ", SortHashSet(stateOutgoingStates)));
                    }
                }

                writer.WriteLine();
            }

            base.WriteCoverageText(writer);
        }

        private void RemoveBuiltInEvents(HashSet<string> eventList)
        {
            foreach (var name in eventList.ToArray())
            {
                if (this.BuiltInEvents.Contains(name))
                {
                    eventList.Remove(name);
                }
            }
        }

        /// <summary>
        /// Remove all events from expectedEvent that are found in the graph.
        /// </summary>
        /// <param name="expectedEvents">The list of all expected events organized by unique state Id.</param>
        private void RemoveCoveredEvents(Dictionary<string, HashSet<string>> expectedEvents)
        {
            foreach (var pair in expectedEvents)
            {
                string stateId = pair.Key;
                var eventSet = pair.Value;

                foreach (var e in this.CoverageInfo.ActorEventInfo.GetEventsReceived(stateId))
                {
                    eventSet.Remove(e);
                }
            }
        }

        private static string GetMachineId(string nodeId)
        {
            int i = nodeId.LastIndexOf(".");
            if (i > 0)
            {
                return nodeId.Substring(0, i);
            }

            return nodeId;
        }
    }
}
