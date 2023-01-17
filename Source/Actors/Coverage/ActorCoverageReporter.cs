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
    /// Reports actor coverage statistics and information.
    /// </summary>
    internal class ActorCoverageReporter : CoverageReporter
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
        /// Initializes a new instance of the <see cref="ActorCoverageReporter"/> class.
        /// </summary>
        public ActorCoverageReporter(ActorCoverageInfo coverageInfo)
            : base(coverageInfo)
        {
            this.CoverageInfo = coverageInfo;
            this.BuiltInEvents.Add(typeof(DefaultEvent).FullName);
            this.BuiltInEvents.Add(typeof(GotoStateEvent).FullName);
            this.BuiltInEvents.Add(typeof(PopStateEvent).FullName);
            this.BuiltInEvents.Add(typeof(PushStateEvent).FullName);
        }

        /// <inheritdoc/>
        internal override void WriteActivityCoverageText(TextWriter writer)
        {
            var machines = new List<string>(this.CoverageInfo.Machines);
            machines.Sort(StringComparer.Ordinal);

            var machineTypes = new Dictionary<string, string>();
            foreach (var node in this.CoverageInfo.CoverageGraph.Nodes)
            {
                if (machines.Contains(node.Id))
                {
                    machineTypes[node.Id] = node.Category ?? ActorRuntimeLogGraphBuilder.StateMachineCategory;
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

            if (machines.Count > 0)
            {
                WriteHeader(writer, string.Format("Total actor coverage: {0}%", eventCoverage));
                foreach (var machine in machines)
                {
                    machineTypes.TryGetValue(machine, out string machineType);
                    WriteHeader(writer, string.Format("{0}: {1}", machineType, machine));

                    // Find all possible events for this machine.
                    var uncoveredMachineEvents = new Dictionary<string, HashSet<string>>();
                    var allMachineEvents = new Dictionary<string, HashSet<string>>();
                    foreach (var item in this.CoverageInfo.RegisteredActorEvents)
                    {
                        var id = GetActorId(item.Key);
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
                                string srcMachine = GetActorId(srcId);
                                string targetId = link.Target.Id;
                                string targetMachine = GetActorId(targetId);
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

                // Check if a non-actor event sender exists.
                if (this.CoverageInfo.CoverageGraph.Links.Any(link => link.Source.Id == ActorRuntimeLogGraphBuilder.ExternalCodeName))
                {
                    WriteHeader(writer, ActorRuntimeLogGraphBuilder.ExternalCodeName);

                    HashSet<string> sent = new HashSet<string>(this.CoverageInfo.ActorEventInfo.GetEventsSent(
                        $"{ActorRuntimeLogGraphBuilder.ExternalCodeName}.{ActorRuntimeLogGraphBuilder.ExternalStateName}"));
                    this.RemoveBuiltInEvents(sent);
                    if (sent.Count > 0)
                    {
                        writer.WriteLine("Events sent: {0}", string.Join(", ", SortHashSet(sent)));
                    }
                }
            }
            else
            {
                WriteHeader(writer, "Total actor coverage: N/A");
            }

            base.WriteActivityCoverageText(writer);
        }

        /// <inheritdoc/>
        internal override bool IsActivityCoverageAvailable() => this.CoverageInfo.Machines.Count > 0 || base.IsActivityCoverageAvailable();

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

        private static string GetActorId(string nodeId)
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
