﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SpecMonitor = Microsoft.Coyote.Specifications.Monitor;

namespace Microsoft.Coyote.Coverage
{
    /// <summary>
    /// Reports activity coverage.
    /// </summary>
    internal class ActivityCoverageReporter
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
        /// Initializes a new instance of the <see cref="ActivityCoverageReporter"/> class.
        /// </summary>
        public ActivityCoverageReporter(CoverageInfo coverageInfo)
        {
            this.CoverageInfo = coverageInfo;
            this.BuiltInEvents.Add(typeof(SpecMonitor.GotoStateEvent).FullName);
            this.BuiltInEvents.Add(typeof(SpecMonitor.DefaultEvent).FullName);
        }

        /// <summary>
        /// Emits the visualization graph.
        /// </summary>
        public void EmitVisualizationGraph(string graphFile) =>
            this.CoverageInfo.CoverageGraph?.SaveDgml(graphFile, true);

        /// <summary>
        /// Emits the code coverage report.
        /// </summary>
        public void EmitCoverageReport(string coverageFile)
        {
            using var writer = new StreamWriter(coverageFile);
            this.WriteCoverageText(writer);
        }

        /// <summary>
        /// Writes the visualization text.
        /// </summary>
        internal virtual void WriteCoverageText(TextWriter writer)
        {
            var monitors = new List<string>(this.CoverageInfo.Monitors);
            monitors.Sort(StringComparer.Ordinal);

            var monitorTypes = new Dictionary<string, string>();

            bool hasExternalSource = false;
            string externalSrcId = "ExternalCode";

            // look for any external source links.
            foreach (var link in this.CoverageInfo.CoverageGraph.Links)
            {
                string srcId = link.Source.Id;
                if (srcId == externalSrcId && !hasExternalSource)
                {
                    monitors.Add(srcId);
                    hasExternalSource = true;
                }
            }

            foreach (var node in this.CoverageInfo.CoverageGraph.Nodes)
            {
                string id = node.Id;
                if (monitors.Contains(id))
                {
                    monitorTypes[id] = node.Category ?? "SpecificationMonitor";
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

            WriteHeader(writer, string.Format("Total monitor event coverage: {0}%", eventCoverage));

            // Per-monitor data.
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
                writer.WriteLine("Monitor event coverage: {0}%", eventCoverage);

                if (!this.CoverageInfo.MonitorsToStates.ContainsKey(monitor))
                {
                    this.CoverageInfo.MonitorsToStates[monitor] = new HashSet<string>(new string[] { "ExternalMonitorState" });
                }

                // Per-state data.
                foreach (var state in this.CoverageInfo.MonitorsToStates[monitor])
                {
                    var key = monitor + "." + state;
                    int totalStateEvents = (from h in allMonitorEvents where h.Key == key select h.Value.Count).Sum();
                    int uncoveredStateEvents = (from h in uncoveredMonitorEvents where h.Key == key select h.Value.Count).Sum();

                    writer.WriteLine();
                    writer.WriteLine("\tMonitor state: {0}{1}", state, totalStateEvents > 0 && totalStateEvents == uncoveredStateEvents ? " is uncovered" : string.Empty);
                    if (totalStateEvents is 0)
                    {
                        writer.WriteLine("\t\tMonitor state has no expected events, so coverage is 100%");
                    }
                    else if (totalStateEvents != uncoveredStateEvents)
                    {
                        eventCoverage = totalStateEvents is 0 ? "100.0" : ((totalStateEvents - uncoveredStateEvents) * 100.0 / totalStateEvents).ToString("F1");
                        writer.WriteLine("\t\tMonitor state event coverage: {0}%", eventCoverage);
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
                        writer.WriteLine("\t\tMonitor events processed: {0}", string.Join(", ", SortHashSet(processed)));
                    }

                    HashSet<string> raised = new HashSet<string>(this.CoverageInfo.MonitorEventInfo.GetEventsRaised(key));
                    this.RemoveBuiltInEvents(raised);

                    if (raised.Count > 0)
                    {
                        writer.WriteLine("\t\tMonitor events raised: {0}", string.Join(", ", SortHashSet(raised)));
                    }

                    var stateUncoveredEvents = (from h in uncoveredMonitorEvents where h.Key == key select h.Value).FirstOrDefault();
                    if (stateUncoveredEvents != null && stateUncoveredEvents.Count > 0)
                    {
                        writer.WriteLine("\t\tMonitor events not covered: {0}", string.Join(", ", SortHashSet(stateUncoveredEvents)));
                    }

                    if (stateIncomingStates.Count > 0)
                    {
                        writer.WriteLine("\t\tPrevious monitor states: {0}", string.Join(", ", SortHashSet(stateIncomingStates)));
                    }

                    if (stateOutgoingStates.Count > 0)
                    {
                        writer.WriteLine("\t\tNext monitor states: {0}", string.Join(", ", SortHashSet(stateOutgoingStates)));
                    }
                }

                writer.WriteLine();
            }
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
