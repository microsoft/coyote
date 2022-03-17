// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;

namespace Microsoft.Coyote.Samples.CloudMessaging
{
    public class MockClusterManager : ClusterManager
    {
        public override async Task BroadcastVoteRequestAsync(Event e)
        {
            var request = e as VoteRequestEvent;

            foreach (var pair in this.Servers)
            {
                if (request.CandidateId != pair.Key)
                {
                    this.SendEvent(pair.Value, request);
                }
            }

            await Task.CompletedTask;
        }

        public override async Task SendVoteResponseAsync(Event e)
        {
            var resp = e as VoteResponseEvent;
            this.SendEvent(this.Servers[resp.TargetId], resp);
            await Task.CompletedTask;
        }

        public override async Task BroadcastClientRequestAsync(Event e)
        {
            foreach (var server in this.Servers)
            {
                // We naively sent the request to all servers, but this could be optimized
                // by providing an intermediate "service" mock actor that redirects events.
                this.SendEvent(server.Value, e);
            }

            await Task.CompletedTask;
        }

        public override async Task SendClientResponseAsync(Event e)
        {
            var resp = e as ClientResponseEvent;
            this.SendEvent(this.ClientId, resp);
            await Task.CompletedTask;
        }

        public override async Task SendAppendEntriesRequestAsync(Event e)
        {
            var req = e as AppendLogEntriesRequestEvent;
            this.SendEvent(this.Servers[req.To], req);
            await Task.CompletedTask;
        }

        public override async Task SendAppendEntriesResponseAsync(Event e)
        {
            var req = e as AppendLogEntriesResponseEvent;
            this.SendEvent(this.Servers[req.To], req);
            await Task.CompletedTask;
        }
    }
}
