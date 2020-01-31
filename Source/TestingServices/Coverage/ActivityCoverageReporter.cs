// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Coyote.Actors;

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
            if (this.CoverageInfo.CoverageGraph != null)
            {
                this.CoverageInfo.CoverageGraph.SaveDgml(graphFile);
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

        private static IEnumerable<string> GetEventIds(GraphLink link)
        {
            if (link.AttributeLists != null)
            {
                link.AttributeLists.TryGetValue("EventIds", out HashSet<string> idList);
                return idList;
            }

            return null;
        }

        /// <summary>
        /// Writes the visualization text.
        /// </summary>
        internal void WriteCoverageText(TextWriter writer)
        {
            var machines = new List<string>(this.CoverageInfo.Machines);
            machines.Sort();

            bool hasExternalSource = false;
            string externalSrcId = "ExternalCode";

            // (machines + "." + states => registered events
            var uncoveredEvents = new Dictionary<string, HashSet<string>>();
            foreach (var item in this.CoverageInfo.RegisteredEvents)
            {
                uncoveredEvents[item.Key] = new HashSet<string>(item.Value);
            }

            int totalEvents = (from h in uncoveredEvents select h.Value.Count).Sum();

            // Now use the graph to find incoming links to each state and remove those from the list of uncovered events.
            foreach (var link in this.CoverageInfo.CoverageGraph.Links)
            {
                string srcId = link.Source.Id;
                if (srcId == externalSrcId && !hasExternalSource)
                {
                    machines.Add(srcId);
                    hasExternalSource = true;
                }

                string targetId = link.Target.Id;
                IEnumerable<string> eventIds = GetEventIds(link);
                if (link.Category != "Contains" && eventIds != null)
                {
                    if (uncoveredEvents.TryGetValue(srcId, out var sourceEvents))
                    {
                        foreach (var eventId in eventIds)
                        {
                            sourceEvents.Remove(eventId);
                        }
                    }

                    if (uncoveredEvents.TryGetValue(targetId, out var targetEvents))
                    {
                        foreach (var eventId in eventIds)
                        {
                            targetEvents.Remove(eventId);
                        }
                    }
                }
            }

            int totalUncoveredEvents = (from h in uncoveredEvents select h.Value.Count).Sum();

            string eventCoverage = totalEvents == 0 ? "100.0" : ((totalEvents - totalUncoveredEvents) * 100.0 / totalEvents).ToString("F1");

            WriteHeader(writer, string.Format("Total event coverage: {0}%", eventCoverage));

            // Per-machine data.
            foreach (var machine in machines)
            {
                WriteHeader(writer, string.Format("StateMachine: {0}", machine));

                // find all possible events for this machine.
                var uncoveredMachineEvents = new Dictionary<string, HashSet<string>>();
                var allMachineEvents = new Dictionary<string, HashSet<string>>();

                foreach (var item in this.CoverageInfo.RegisteredEvents)
                {
                    var id = GetMachineId(item.Key);
                    if (id == machine)
                    {
                        uncoveredMachineEvents[item.Key] = new HashSet<string>(item.Value);
                        allMachineEvents[item.Key] = new HashSet<string>(item.Value);
                    }
                }

                // Now use the graph to find incoming links to each state in this machine and remove those from the list of uncovered events.
                foreach (var link in this.CoverageInfo.CoverageGraph.Links)
                {
                    string srcId = link.Source.Id;
                    string targetId = link.Target.Id;
                    IEnumerable<string> eventIds = GetEventIds(link);
                    if (link.Category != "Contains" && eventIds != null)
                    {
                        var id = GetMachineId(srcId);
                        if (id == machine)
                        {
                            if (uncoveredMachineEvents.TryGetValue(srcId, out var events))
                            {
                                foreach (var eventId in eventIds)
                                {
                                    events.Remove(eventId);
                                }
                            }
                        }

                        id = GetMachineId(targetId);
                        if (id == machine)
                        {
                            if (uncoveredMachineEvents.TryGetValue(targetId, out var events))
                            {
                                foreach (var eventId in eventIds)
                                {
                                    events.Remove(eventId);
                                }
                            }
                        }
                    }
                }

                int totalMachineEvents = (from h in allMachineEvents select h.Value.Count).Sum();
                var totalUncoveredMachineEvents = (from h in uncoveredMachineEvents select h.Value.Count).Sum();

                eventCoverage = totalMachineEvents == 0 ? "100.0" : ((totalMachineEvents - totalUncoveredMachineEvents) * 100.0 / totalMachineEvents).ToString("F1");
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
                    if (totalStateEvents == 0)
                    {
                        writer.WriteLine("\t\tState has no expected events, so coverage is 100%");
                    }
                    else if (totalStateEvents != uncoveredStateEvents)
                    {
                        eventCoverage = totalStateEvents == 0 ? "100.0" : ((totalStateEvents - uncoveredStateEvents) * 100.0 / totalStateEvents).ToString("F1");
                        writer.WriteLine("\t\tState event coverage: {0}%", eventCoverage);
                    }

                    // Now use the graph to find incoming links to each state in this machine
                    HashSet<string> stateIncomingEvents = new HashSet<string>();
                    HashSet<string> stateIncomingStates = new HashSet<string>();
                    HashSet<string> stateOutgoingEvents = new HashSet<string>();
                    HashSet<string> stateOutgoingStates = new HashSet<string>();
                    string gotoEventId = typeof(GotoStateEvent).FullName;
                    string pushEventId = typeof(PushStateEvent).FullName;
                    foreach (var link in this.CoverageInfo.CoverageGraph.Links)
                    {
                        string srcId = link.Source.Id;
                        string targetId = link.Target.Id;
                        if (link.AttributeLists != null && link.AttributeLists.TryGetValue("EventIds", out HashSet<string> eventList))
                        {
                            foreach (string id in eventList)
                            {
                                if (targetId == key)
                                {
                                    // Hide the special internal only "goto" event which corresponds to user
                                    // explicitly calling Goto() from within some action action.
                                    if (id != gotoEventId && id != pushEventId)
                                    {
                                        stateIncomingEvents.Add(id);
                                    }

                                    if (!srcId.StartsWith(externalSrcId))
                                    {
                                        stateIncomingStates.Add(GetStateName(srcId));
                                    }
                                }

                                if (srcId == key)
                                {
                                    if (id != gotoEventId && id != pushEventId)
                                    {
                                        stateOutgoingEvents.Add(id);
                                    }

                                    if (!srcId.StartsWith(externalSrcId))
                                    {
                                        stateOutgoingStates.Add(GetStateName(targetId));
                                    }
                                }
                            }
                        }
                    }

                    if (stateIncomingEvents.Count > 0)
                    {
                        writer.WriteLine("\t\tEvents received: {0}", string.Join(", ", Sort(stateIncomingEvents)));
                    }

                    if (stateOutgoingEvents.Count > 0)
                    {
                        writer.WriteLine("\t\tEvents sent: {0}", string.Join(", ", Sort(stateOutgoingEvents)));
                    }

                    var stateUncoveredEvents = (from h in uncoveredMachineEvents where h.Key == key select h.Value).FirstOrDefault();
                    if (stateUncoveredEvents != null && stateUncoveredEvents.Count > 0)
                    {
                        writer.WriteLine("\t\tEvents not covered: {0}", string.Join(", ", Sort(stateUncoveredEvents)));
                    }

                    if (stateIncomingStates.Count > 0)
                    {
                        writer.WriteLine("\t\tPrevious states: {0}", string.Join(", ", Sort(stateIncomingStates)));
                    }

                    if (stateOutgoingStates.Count > 0)
                    {
                        writer.WriteLine("\t\tNext states: {0}", string.Join(", ", Sort(stateOutgoingStates)));
                    }
                }

                writer.WriteLine();
            }
        }

        private static List<string> Sort(HashSet<string> items)
        {
            List<string> sorted = new List<string>(items);
            sorted.Sort();
            return sorted;
        }

        private static string GetStateName(string nodeId)
        {
            int i = nodeId.LastIndexOf(".");
            if (i > 0)
            {
                return nodeId.Substring(i + 1);
            }

            return nodeId;
        }

        private static void WriteHeader(TextWriter writer, string header)
        {
            writer.WriteLine(header);
            writer.WriteLine(new string('=', header.Length));
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

        private static string GetStateId(string machineName, string stateName) =>
            string.Format("{0}::{1}", stateName, machineName);
    }
}
