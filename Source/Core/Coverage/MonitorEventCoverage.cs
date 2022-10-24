// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Microsoft.Coyote.Coverage
{
    /// <summary>
    /// This class maintains information about events received in each state of each specification monitor.
    /// </summary>
    [DataContract]
    public class MonitorEventCoverage
    {
        /// <summary>
        /// Map from states to the list of events processed by that state. The state id is fully qualified by
        /// the specification monitor type it belongs to.
        /// </summary>
        [DataMember]
        private readonly Dictionary<string, HashSet<string>> EventsProcessed = new Dictionary<string, HashSet<string>>();

        /// <summary>
        /// Map from states to the list of events raised by that state. The state id is fully qualified by
        /// the specification monitor type it belongs to.
        /// </summary>
        [DataMember]
        private readonly Dictionary<string, HashSet<string>> EventsRaised = new Dictionary<string, HashSet<string>>();

        /// <summary>
        /// Get list of events processed by the given fully qualified state.
        /// </summary>
        /// <param name="stateId">The specification monitor qualified state name.</param>
        public IEnumerable<string> GetEventsProcessed(string stateId)
        {
            if (this.EventsProcessed.TryGetValue(stateId, out HashSet<string> set))
            {
                return set;
            }

            return Array.Empty<string>();
        }

        /// <summary>
        /// Get list of events raised by the given state.
        /// </summary>
        /// <param name="stateId">The specification monitor qualified state name.</param>
        public IEnumerable<string> GetEventsRaised(string stateId)
        {
            if (this.EventsRaised.TryGetValue(stateId, out HashSet<string> set))
            {
                return set;
            }

            return Array.Empty<string>();
        }

        internal void AddEventProcessed(string stateId, string eventId)
        {
            if (!this.EventsProcessed.TryGetValue(stateId, out HashSet<string> set))
            {
                set = new HashSet<string>();
                this.EventsProcessed[stateId] = set;
            }

            set.Add(eventId);
        }

        internal void AddEventRaised(string stateId, string eventId)
        {
            if (!this.EventsRaised.TryGetValue(stateId, out HashSet<string> set))
            {
                set = new HashSet<string>();
                this.EventsRaised[stateId] = set;
            }

            set.Add(eventId);
        }

        internal void Merge(MonitorEventCoverage other)
        {
            MergeHashSets(this.EventsProcessed, other.EventsProcessed);
            MergeHashSets(this.EventsRaised, other.EventsRaised);
        }

        private static void MergeHashSets(Dictionary<string, HashSet<string>> ours, Dictionary<string, HashSet<string>> theirs)
        {
            foreach (var pair in theirs)
            {
                var stateId = pair.Key;
                if (!ours.TryGetValue(stateId, out HashSet<string> eventSet))
                {
                    eventSet = new HashSet<string>();
                    ours[stateId] = eventSet;
                }

                eventSet.UnionWith(pair.Value);
            }
        }
    }
}
