// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Coyote.Specifications;

namespace Microsoft.Coyote.Samples.CloudMessaging
{
    /// <summary>
    /// Monitor that checks the safety specification that
    /// only one leader can be elected at any given term.
    /// </summary>
    internal class SafetyMonitor : Monitor
    {
        internal class NotifyLeaderElected : Event
        {
            internal int Term;

            internal NotifyLeaderElected(int term)
                : base()
            {
                this.Term = term;
            }
        }

        private HashSet<int> TermsWithLeader;

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        [OnEventDoAction(typeof(NotifyLeaderElected), nameof(ProcessLeaderElected))]
        private class Monitoring : State { }

        private void InitOnEntry()
        {
            this.TermsWithLeader = new HashSet<int>();
        }

        private void ProcessLeaderElected(Event e)
        {
            var term = (e as NotifyLeaderElected).Term;
            this.Assert(!this.TermsWithLeader.Contains(term), $"Detected more than one leader in term {term}.");
            this.TermsWithLeader.Add(term);
        }
    }
}
