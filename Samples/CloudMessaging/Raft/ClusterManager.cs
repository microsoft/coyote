// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Coyote.Actors;

namespace Microsoft.Coyote.Samples.CloudMessaging
{
    /// <summary>
    /// This state machine represents the a pub/sub system like Azure Service Bus that is used
    /// to send events from one node in the cluster to another node in the cluster, or to
    /// broadcast events to all the nodes in the cluster.
    /// </summary>
    public class ClusterManager : StateMachine
    {
        /// <summary>
        /// The id of the Client
        /// </summary>
        protected ActorId ClientId;

        /// <summary>
        /// The mapping of string to ActorId for the Server state machines in the cluster.
        /// </summary>
        protected readonly Dictionary<string, ActorId> Servers = new Dictionary<string, ActorId>();

        [Start]
        [OnEventDoAction(typeof(VoteRequestEvent), nameof(BroadcastVoteRequestAsync))]
        [OnEventDoAction(typeof(VoteResponseEvent), nameof(SendVoteResponseAsync))]
        [OnEventDoAction(typeof(ClientRequestEvent), nameof(BroadcastClientRequestAsync))]
        [OnEventDoAction(typeof(ClientResponseEvent), nameof(SendClientResponseAsync))]
        [OnEventDoAction(typeof(AppendLogEntriesRequestEvent), nameof(SendAppendEntriesRequestAsync))]
        [OnEventDoAction(typeof(AppendLogEntriesResponseEvent), nameof(SendAppendEntriesResponseAsync))]
        [OnEventDoAction(typeof(RegisterClientEvent), nameof(RegisterClient))]
        [OnEventDoAction(typeof(RegisterServerEvent), nameof(RegisterServer))]
        private class Init : State { }

        public virtual async Task BroadcastVoteRequestAsync(Event e)
        {
            await Task.CompletedTask;
        }

        public virtual async Task SendVoteResponseAsync(Event e)
        {
            await Task.CompletedTask;
        }

        public virtual async Task BroadcastClientRequestAsync(Event e)
        {
            await Task.CompletedTask;
        }

        public virtual async Task SendClientResponseAsync(Event e)
        {
            await Task.CompletedTask;
        }

        public virtual async Task SendAppendEntriesRequestAsync(Event e)
        {
            await Task.CompletedTask;
        }

        public virtual async Task SendAppendEntriesResponseAsync(Event e)
        {
            await Task.CompletedTask;
        }

        public virtual async Task RegisterClient(Event e)
        {
            var reg = e as RegisterClientEvent;
            this.ClientId = reg.ClientId;
            await Task.CompletedTask;
        }

        public virtual async Task RegisterServer(Event e)
        {
            var reg = e as RegisterServerEvent;
            this.Servers[reg.ServerId.Name] = reg.ServerId;
            await Task.CompletedTask;
        }
    }
}
