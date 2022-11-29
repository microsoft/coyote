// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SpecMonitor = Microsoft.Coyote.Specifications.Monitor;

namespace Microsoft.Coyote.Coverage
{
    /// <summary>
    /// Reports coverage statistics and information.
    /// </summary>
    internal class CoverageReporter
    {
        /// <summary>
        /// Data structure containing information regarding testing coverage.
        /// </summary>
        private readonly CoverageInfo CoverageInfo;

        /// <summary>
        /// Set of built in events which we hide in the coverage report.
        /// </summary>
        private readonly HashSet<string> BuiltInEvents = new HashSet<string>();

        /// <summary>
        /// Initializes a new instance of the <see cref="CoverageReporter"/> class.
        /// </summary>
        public CoverageReporter(CoverageInfo coverageInfo)
        {
            this.CoverageInfo = coverageInfo;
            this.BuiltInEvents.Add(typeof(SpecMonitor.GotoStateEvent).FullName);
            this.BuiltInEvents.Add(typeof(SpecMonitor.DefaultEvent).FullName);
        }

        /// <summary>
        /// Emits the visualization graph, if it is available.
        /// </summary>
        public bool TryEmitVisualizationGraph(string graphFile)
        {
            bool isAvailable = this.CoverageInfo.CoverageGraph?.IsAvailable() ?? false;
            if (isAvailable)
            {
                this.CoverageInfo.CoverageGraph.SaveDgml(graphFile, true);
            }

            return isAvailable;
        }

        /// <summary>
        /// Emits the activity coverage report, if it is available.
        /// </summary>
        public bool TryEmitActivityCoverageReport(string coverageFile)
        {
            bool isAvailable = this.IsActivityCoverageAvailable();
            if (isAvailable)
            {
                using var writer = new StreamWriter(coverageFile);
                this.WriteActivityCoverageText(writer);
            }

            return isAvailable;
        }

        /// <summary>
        /// Emits the activity coverage report.
        /// </summary>
        internal virtual void WriteActivityCoverageText(TextWriter writer)
        {
            var monitors = new List<string>(this.CoverageInfo.Monitors);
            monitors.Sort(StringComparer.Ordinal);

            var monitorTypes = new Dictionary<string, string>();
            foreach (var node in this.CoverageInfo.CoverageGraph.Nodes)
            {
                string id = node.Id;
                if (monitors.Contains(id))
                {
                    monitorTypes[id] = node.Category ?? "Monitor";
                }
            }

            // monitors + "." + states => registered monitor events
            var uncoveredEvents = new Dictionary<string, HashSet<string>>();
            foreach (var item in this.CoverageInfo.RegisteredMonitorEvents)
            {
                uncoveredEvents[item.Key] = new HashSet<string>(item.Value);
            }

            int totalEvents = (from h in uncoveredEvents select h.Value.Count).Sum();

            // Now use the graph to find incoming links to each state and remove those from the list of uncovered monitor events.
            this.RemoveCoveredEvents(uncoveredEvents);

            int totalUncoveredEvents = (from h in uncoveredEvents select h.Value.Count).Sum();
            string eventCoverage = totalEvents is 0 ? "100.0" : ((totalEvents - totalUncoveredEvents) * 100.0 / totalEvents).ToString("F1");

            if (monitors.Count > 0)
            {
                WriteHeader(writer, string.Format("Total specification monitor coverage: {0}%", eventCoverage));
                foreach (var monitor in monitors)
                {
                    monitorTypes.TryGetValue(monitor, out string monitorType);
                    WriteHeader(writer, string.Format("{0}: {1}", monitorType, monitor));

                    // Find all possible events for this specification monitor.
                    var uncoveredMonitorEvents = new Dictionary<string, HashSet<string>>();
                    var allMonitorEvents = new Dictionary<string, HashSet<string>>();
                    foreach (var item in this.CoverageInfo.RegisteredMonitorEvents)
                    {
                        var id = GetMonitorId(item.Key);
                        if (id == monitor)
                        {
                            uncoveredMonitorEvents[item.Key] = new HashSet<string>(item.Value);
                            allMonitorEvents[item.Key] = new HashSet<string>(item.Value);
                        }
                    }

                    // Now use the graph to find incoming links to each state in this specification monitor
                    // and remove those from the list of uncovered events.
                    this.RemoveCoveredEvents(uncoveredMonitorEvents);

                    int totalMonitorEvents = (from h in allMonitorEvents select h.Value.Count).Sum();
                    var totalUncoveredMonitorEvents = (from h in uncoveredMonitorEvents select h.Value.Count).Sum();

                    eventCoverage = totalMonitorEvents is 0 ? "100.0" : ((totalMonitorEvents - totalUncoveredMonitorEvents) * 100.0 / totalMonitorEvents).ToString("F1");
                    writer.WriteLine("Event coverage: {0}%", eventCoverage);

                    foreach (var state in this.CoverageInfo.MonitorsToStates[monitor])
                    {
                        var key = monitor + "." + state;
                        int totalStateEvents = (from h in allMonitorEvents where h.Key == key select h.Value.Count).Sum();
                        int uncoveredStateEvents = (from h in uncoveredMonitorEvents where h.Key == key select h.Value.Count).Sum();

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

                        // Now use the graph to find incoming links to each state in this monitor.
                        HashSet<string> stateIncomingStates = new HashSet<string>();
                        HashSet<string> stateOutgoingStates = new HashSet<string>();
                        foreach (var link in this.CoverageInfo.CoverageGraph.Links)
                        {
                            if (link.Category != "Contains")
                            {
                                string srcId = link.Source.Id;
                                string srcMonitor = GetMonitorId(srcId);
                                string targetId = link.Target.Id;
                                string targetMonitor = GetMonitorId(targetId);
                                bool intraMonitorTransition = targetMonitor == monitor && srcMonitor == monitor;
                                if (intraMonitorTransition)
                                {
                                    foreach (string id in GetEventIds(link))
                                    {
                                        if (targetId == key)
                                        {
                                            // We want to show incoming/outgoing states within the current monitor only.
                                            stateIncomingStates.Add(GetStateName(srcId));
                                        }

                                        if (srcId == key)
                                        {
                                            // We want to show incoming/outgoing states within the current monitor only.
                                            stateOutgoingStates.Add(GetStateName(targetId));
                                        }
                                    }
                                }
                            }
                        }

                        HashSet<string> processed = new HashSet<string>(this.CoverageInfo.MonitorEventInfo.GetEventsProcessed(key));
                        this.RemoveBuiltInEvents(processed);
                        if (processed.Count > 0)
                        {
                            writer.WriteLine("\t\tEvents processed: {0}", string.Join(", ", SortHashSet(processed)));
                        }

                        HashSet<string> raised = new HashSet<string>(this.CoverageInfo.MonitorEventInfo.GetEventsRaised(key));
                        this.RemoveBuiltInEvents(raised);
                        if (raised.Count > 0)
                        {
                            writer.WriteLine("\t\tEvents raised: {0}", string.Join(", ", SortHashSet(raised)));
                        }

                        var stateUncoveredEvents = (from h in uncoveredMonitorEvents where h.Key == key select h.Value).FirstOrDefault();
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
            }
            else
            {
                WriteHeader(writer, "Total specification monitor coverage: N/A");
            }
        }

        /// <summary>
        /// Checks if the activity coverage report is available.
        /// </summary>
        internal virtual bool IsActivityCoverageAvailable() => this.CoverageInfo.Monitors.Count > 0;

        /// <summary>
        /// Emits the schedule coverage report, if it is available.
        /// </summary>
        public bool TryEmitScheduleCoverageReport(string coverageFile)
        {
            bool isAvailable = this.IsScheduleCoverageAvailable();
            if (isAvailable)
            {
                using var writer = new StreamWriter(coverageFile);
                this.WriteScheduleCoverageText(writer);
            }

            return isAvailable;
        }

        /// <summary>
        /// Emits the schedule coverage report.
        /// </summary>
        internal void WriteScheduleCoverageText(TextWriter writer)
        {
            var schedulingPointTypes = new List<string>(this.CoverageInfo.SchedulingPointStackTraces.Keys);
            schedulingPointTypes.Sort(StringComparer.Ordinal);

            if (schedulingPointTypes.Count > 0)
            {
                long numCallStacks = this.CoverageInfo.SchedulingPointStackTraces.Values.Sum(map => map.Values.Sum());
                WriteHeader(writer, string.Format("Total scheduling decision call stacks: {0}", numCallStacks));
                foreach (var schedulingPointType in schedulingPointTypes)
                {
                    WriteHeader(writer, string.Format("Scheduling decision: {0}", schedulingPointType));
                    this.CoverageInfo.SchedulingPointStackTraces.TryGetValue(schedulingPointType, out Dictionary<string, long> traces);
                    foreach (var trace in traces)
                    {
                        writer.WriteLine("Invocation frequency: {0}", trace.Value);
                        writer.WriteLine(trace.Key);
                    }
                }
            }
            else
            {
                WriteHeader(writer, "Total scheduling decision call stacks: N/A");
            }
        }

        /// <summary>
        /// Checks if the schedule coverage report is available.
        /// </summary>
        internal bool IsScheduleCoverageAvailable() => this.CoverageInfo.SchedulingPointStackTraces.Count > 0;

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
        /// <param name="expectedEvents">The list of all expected events organized by unique state id.</param>
        private void RemoveCoveredEvents(Dictionary<string, HashSet<string>> expectedEvents)
        {
            foreach (var pair in expectedEvents)
            {
                string stateId = pair.Key;
                var eventSet = pair.Value;

                foreach (var e in this.CoverageInfo.MonitorEventInfo.GetEventsProcessed(stateId))
                {
                    eventSet.Remove(e);
                }
            }
        }

        protected static List<string> SortHashSet(HashSet<string> items)
        {
            List<string> sorted = new List<string>(items);
            sorted.Sort(StringComparer.Ordinal);
            return sorted;
        }

        protected static string GetStateName(string nodeId)
        {
            int i = nodeId.LastIndexOf(".");
            if (i > 0)
            {
                return nodeId.Substring(i + 1);
            }

            return nodeId;
        }

        /// <summary>
        /// Return all events represented by this link.
        /// </summary>
        protected static IEnumerable<string> GetEventIds(CoverageGraph.Link link)
        {
            if (link.AttributeLists != null)
            {
                // A collapsed edge graph
                if (link.AttributeLists.TryGetValue("EventIds", out HashSet<string> idList))
                {
                    return idList;
                }
            }

            // A fully expanded edge graph has individual links for each event.
            if (link.Attributes.TryGetValue("EventId", out string eventId))
            {
                return new string[] { eventId };
            }

            return Array.Empty<string>();
        }

        protected static void WriteHeader(TextWriter writer, string header)
        {
            writer.WriteLine(new string('=', header.Length));
            writer.WriteLine(header);
            writer.WriteLine(new string('=', header.Length));
        }

        private static string GetMonitorId(string nodeId)
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
