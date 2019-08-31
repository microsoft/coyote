using System;
using System.Collections.Generic;
using Microsoft.Coyote;

namespace FailureDetector
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
            public MachineId Client;

            public Ping(MachineId client)
            {
                this.Client = client;
            }
        }

        internal class Pong : Event
        {
            public MachineId Node;

            public Pong(MachineId node)
            {
                this.Node = node;
            }
        }

        Dictionary<MachineId, int> Pending;

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        [OnEventDoAction(typeof(Ping), nameof(PingAction))]
        [OnEventDoAction(typeof(Pong), nameof(PongAction))]
        class Init : MonitorState { }

        void InitOnEntry()
        {
            this.Pending = new Dictionary<MachineId, int>();
        }

        void PingAction()
        {
            var client = (this.ReceivedEvent as Ping).Client;
            if (!this.Pending.ContainsKey(client))
            {
                this.Pending[client] = 0;
            }

            this.Pending[client] = this.Pending[client] + 1;
            this.Assert(this.Pending[client] <= 3, $"'{client}' ping count must be <= 3.");
        }

        void PongAction()
        {
            var node = (this.ReceivedEvent as Pong).Node;
            this.Assert(this.Pending.ContainsKey(node), $"'{node}' is not in pending set.");
            this.Assert(this.Pending[node] > 0, $"'{node}' ping count must be > 0.");
            this.Pending[node] = this.Pending[node] - 1;
        }
    }
}
