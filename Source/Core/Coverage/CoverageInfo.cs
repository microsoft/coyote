// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;

namespace Microsoft.Coyote.Coverage
{
    /// <summary>
    /// Class for storing coverage-specific data across multiple testing iterations.
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
        /// Initializes a new instance of the <see cref="CoverageInfo"/> class.
        /// </summary>
        public CoverageInfo()
        {
            this.Monitors = new HashSet<string>();
            this.MonitorsToStates = new Dictionary<string, HashSet<string>>();
            this.RegisteredMonitorEvents = new Dictionary<string, HashSet<string>>();
        }

        /// <summary>
        /// Checks if the specification monitor type has already been registered for coverage.
        /// </summary>
        public bool IsMonitorDeclared(string monitorName) => this.MonitorsToStates.ContainsKey(monitorName);

        /// <summary>
        /// Declares a specification monitor state.
        /// </summary>
        public void DeclareMonitorState(string monitor, string state) => this.AddMonitorState(monitor, state);

        /// <summary>
        /// Declares a registered specification monitor state-event pair.
        /// </summary>
        public void DeclareMonitorStateEventPair(string monitor, string state, string eventName)
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
        }
    }
}
