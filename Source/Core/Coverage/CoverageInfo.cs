// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;

namespace Microsoft.Coyote.Coverage
{
    /// <summary>
    /// Class for storing coverage-specific data across multiple test iterations.
    /// </summary>
    [DataContract]
    public class CoverageInfo
    {
        /// <summary>
        /// The coverage graph.
        /// </summary>
        [DataMember]
        public CoverageGraph CoverageGraph { get; set; }

        /// <summary>
        /// Set of known specification monitors.
        /// </summary>
        [DataMember]
        public HashSet<string> Monitors { get; private set; }

        /// <summary>
        /// Map from specification monitors to set of all states defined in that monitor.
        /// </summary>
        [DataMember]
        public Dictionary<string, HashSet<string>> MonitorsToStates { get; private set; }

        /// <summary>
        /// Set of all all monitor events that can be used for transitioning into each specification monitor state.
        /// </summary>
        /// <remarks>
        /// Set of (specification monitor + "." + state => registered monitor events).
        /// </remarks>
        [DataMember]
        public Dictionary<string, HashSet<string>> RegisteredMonitorEvents { get; private set; }

        /// <summary>
        /// Information about events received by each specification monitor.
        /// </summary>
        [DataMember]
        public MonitorEventCoverage MonitorEventInfo { get; set; }

        /// <summary>
        /// Map from scheduling point types to invocation site stack traces and their frequencies.
        /// </summary>
        [DataMember]
        public Dictionary<string, Dictionary<string, long>> SchedulingPointStackTraces { get; private set; }

        /// <summary>
        /// Set of explored paths represented as ordered operations identified by their creation sequence ids.
        /// </summary>
        [DataMember]
        public HashSet<string> ExploredPaths { get; private set; }

        /// <summary>
        /// Set of visited program states represented as hashes.
        /// </summary>
        [DataMember]
        public HashSet<int> VisitedStates { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CoverageInfo"/> class.
        /// </summary>
        public CoverageInfo()
        {
            this.Monitors = new HashSet<string>();
            this.MonitorsToStates = new Dictionary<string, HashSet<string>>();
            this.RegisteredMonitorEvents = new Dictionary<string, HashSet<string>>();
            this.SchedulingPointStackTraces = new Dictionary<string, Dictionary<string, long>>();
            this.ExploredPaths = new HashSet<string>();
            this.VisitedStates = new HashSet<int>();
        }

        /// <summary>
        /// Checks if the specification monitor type has already been registered for coverage.
        /// </summary>
        internal bool IsMonitorDeclared(string monitorName) => this.MonitorsToStates.ContainsKey(monitorName);

        /// <summary>
        /// Declares a specification monitor state.
        /// </summary>
        internal void DeclareMonitorState(string monitor, string state) => this.AddMonitorState(monitor, state);

        /// <summary>
        /// Declares a registered specification monitor state-event pair.
        /// </summary>
        internal void DeclareMonitorStateEventPair(string monitor, string state, string eventName)
        {
            this.AddMonitorState(monitor, state);

            string key = monitor + "." + state;
            this.AddMonitorEvent(key, eventName);
        }

        /// <summary>
        /// Adds a new specification monitor state.
        /// </summary>
        private void AddMonitorState(string monitorName, string stateName)
        {
            this.Monitors.Add(monitorName);
            if (!this.MonitorsToStates.ContainsKey(monitorName))
            {
                this.MonitorsToStates.Add(monitorName, new HashSet<string>());
            }

            this.MonitorsToStates[monitorName].Add(stateName);
        }

        /// <summary>
        /// Adds a new specification monitor event.
        /// </summary>
        private void AddMonitorEvent(string key, string eventName)
        {
            if (!this.RegisteredMonitorEvents.ContainsKey(key))
            {
                this.RegisteredMonitorEvents.Add(key, new HashSet<string>());
            }

            this.RegisteredMonitorEvents[key].Add(eventName);
        }

        /// <summary>
        /// Declares a new scheduling point invocation with its stack trace.
        /// </summary>
        internal void DeclareSchedulingPoint(string type, string trace)
        {
            if (this.SchedulingPointStackTraces.TryGetValue(type, out Dictionary<string, long> traces))
            {
                if (traces.TryGetValue(trace, out long count))
                {
                    traces[trace] = count + 1;
                }
                else
                {
                    traces.Add(trace, 1);
                }
            }
            else
            {
                this.SchedulingPointStackTraces.Add(type, new Dictionary<string, long> { { trace, 1 } });
            }
        }

        /// <summary>
        /// Declares a new explored execution path.
        /// </summary>
        internal void DeclareExploredExecutionPath(string path) => this.ExploredPaths.Add(path);

        /// <summary>
        /// Declares a new visited state.
        /// </summary>
        internal void DeclareVisitedState(int state) => this.VisitedStates.Add(state);

        /// <summary>
        /// Loads the coverage info XML file into a <see cref="CoverageInfo"/> object of the specified type.
        /// </summary>
        /// <param name="filename">Path to the file to load.</param>
        /// <returns>The deserialized coverage info.</returns>
        public static T Load<T>(string filename)
            where T : CoverageInfo
        {
            using var fs = new FileStream(filename, FileMode.Open);
            using var reader = XmlDictionaryReader.CreateTextReader(fs, new XmlDictionaryReaderQuotas());
            DataContractSerializerSettings settings = new DataContractSerializerSettings
            {
                PreserveObjectReferences = true
            };

            var ser = new DataContractSerializer(typeof(T), settings);
            return (T)ser.ReadObject(reader, true);
        }

        /// <summary>
        /// Saves the coverage info to the given XML file.
        /// </summary>
        /// <param name="serFilePath">The path to the file to create.</param>
        public void Save(string serFilePath)
        {
            using var fs = new FileStream(serFilePath, FileMode.Create);
            DataContractSerializerSettings settings = new DataContractSerializerSettings
            {
                PreserveObjectReferences = true
            };

            var ser = new DataContractSerializer(this.GetType(), settings);
            ser.WriteObject(fs, this);
        }

        /// <summary>
        /// Merges the information from the specified coverage info. This is not thread-safe.
        /// </summary>
        public virtual void Merge(CoverageInfo coverageInfo)
        {
            foreach (var monitor in coverageInfo.Monitors)
            {
                this.Monitors.Add(monitor);
            }

            foreach (var monitor in coverageInfo.MonitorsToStates)
            {
                foreach (var state in monitor.Value)
                {
                    this.DeclareMonitorState(monitor.Key, state);
                }
            }

            foreach (var tup in coverageInfo.RegisteredMonitorEvents)
            {
                foreach (var e in tup.Value)
                {
                    this.AddMonitorEvent(tup.Key, e);
                }
            }

            if (this.CoverageGraph is null)
            {
                this.CoverageGraph = coverageInfo.CoverageGraph;
            }
            else if (coverageInfo.CoverageGraph != null && this.CoverageGraph != coverageInfo.CoverageGraph)
            {
                this.CoverageGraph.Merge(coverageInfo.CoverageGraph);
            }

            if (this.MonitorEventInfo is null)
            {
                this.MonitorEventInfo = coverageInfo.MonitorEventInfo;
            }
            else if (coverageInfo.MonitorEventInfo != null && this.MonitorEventInfo != coverageInfo.MonitorEventInfo)
            {
                this.MonitorEventInfo.Merge(coverageInfo.MonitorEventInfo);
            }

            foreach (var kvp in coverageInfo.SchedulingPointStackTraces)
            {
                if (!this.SchedulingPointStackTraces.TryGetValue(kvp.Key, out Dictionary<string, long> traces))
                {
                    traces = new Dictionary<string, long>();
                    this.SchedulingPointStackTraces.Add(kvp.Key, traces);
                }

                foreach (var trace in kvp.Value)
                {
                    if (traces.TryGetValue(trace.Key, out long count))
                    {
                        traces[trace.Key] = count + trace.Value;
                    }
                    else
                    {
                        traces.Add(trace.Key, trace.Value);
                    }
                }
            }

            this.ExploredPaths.UnionWith(coverageInfo.ExploredPaths);
            this.VisitedStates.UnionWith(coverageInfo.VisitedStates);
        }
    }
}
