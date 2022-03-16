// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Coyote;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Specifications;

namespace Microsoft.Coyote.Samples.Monitors
{
    /// <summary>
    /// Monitors allow the Coyote testing engine to detect global safety property
    /// violations. This monitor gathers 'Ping' and 'Pong' events and manages
    /// the per-client history.
    ///
    /// 'Ping' increments the client ping count and 'Pong' decrements it.
    ///
    /// A safety violation is reported if the ping count is less than 0 or
    /// greater than 3 (these indicate unmatched updates).
    /// </summary>
    internal class Safety : Monitor
    {
        internal class Ping : Event
        {
            public ActorId Client;

            public Ping(ActorId client)
            {
                this.Client = client;
            }
        }

        internal class Pong : Event
        {
            public ActorId Node;

            public Pong(ActorId node)
            {
                this.Node = node;
            }
        }

        private Dictionary<ActorId, int> Pending;

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        [OnEventDoAction(typeof(Ping), nameof(PingAction))]
        [OnEventDoAction(typeof(Pong), nameof(PongAction))]
        private class Init : State { }

        private void InitOnEntry()
        {
            this.Pending = new Dictionary<ActorId, int>();
        }

        private void PingAction(Event e)
        {
            var client = (e as Ping).Client;
            if (!this.Pending.ContainsKey(client))
            {
                this.Pending[client] = 0;
            }

            this.Pending[client] = this.Pending[client] + 1;
            this.Assert(this.Pending[client] <= 3, $"'{client}' ping count must be <= 3.");
        }

        private void PongAction(Event e)
        {
            var node = (e as Pong).Node;
            this.Assert(this.Pending.ContainsKey(node), $"'{node}' is not in pending set.");
            this.Assert(this.Pending[node] > 0, $"'{node}' ping count must be > 0.");
            this.Pending[node] = this.Pending[node] - 1;
        }
    }
}
