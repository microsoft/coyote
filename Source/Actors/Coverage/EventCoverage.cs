// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Microsoft.Coyote.Actors.Coverage
{
    /// <summary>
    /// This class maintains information about events received and sent from each state of each actor.
    /// </summary>
    [DataContract]
    public class EventCoverage
    {
        /// <summary>
        /// Map from states to the list of events received by that state.  The state id is fully qualified by
        /// the actor id it belongs to.
        /// </summary>
        [DataMember]
        private readonly Dictionary<string, HashSet<string>> EventsReceived = new Dictionary<string, HashSet<string>>();

        /// <summary>
        /// Map from states to the list of events sent by that state.  The state id is fully qualified by
        /// the actor id it belongs to.
        /// </summary>
        [DataMember]
        private readonly Dictionary<string, HashSet<string>> EventsSent = new Dictionary<string, HashSet<string>>();

        internal void AddEventReceived(string stateId, string eventId)
        {
            if (!this.EventsReceived.TryGetValue(stateId, out HashSet<string> set))
            {
                set = new HashSet<string>();
                this.EventsReceived[stateId] = set;
            }

            set.Add(eventId);
        }

        /// <summary>
        /// Get list of events received by the given fully qualified state.
        /// </summary>
        /// <param name="stateId">The actor qualified state name.</param>
        public IEnumerable<string> GetEventsReceived(string stateId)
        {
            if (this.EventsReceived.TryGetValue(stateId, out HashSet<string> set))
            {
                return set;
            }

            return Array.Empty<string>();
        }

        internal void AddEventSent(string stateId, string eventId)
        {
            if (!this.EventsSent.TryGetValue(stateId, out HashSet<string> set))
            {
                set = new HashSet<string>();
                this.EventsSent[stateId] = set;
            }

            set.Add(eventId);
        }

        /// <summary>
        /// Get list of events sent by the given state.
        /// </summary>
        /// <param name="stateId">The actor qualified state name.</param>
        public IEnumerable<string> GetEventsSent(string stateId)
        {
            if (this.EventsSent.TryGetValue(stateId, out HashSet<string> set))
            {
                return set;
            }

            return Array.Empty<string>();
        }

        internal void Merge(EventCoverage other)
        {
            MergeHashSets(this.EventsReceived, other.EventsReceived);
            MergeHashSets(this.EventsSent, other.EventsSent);
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
