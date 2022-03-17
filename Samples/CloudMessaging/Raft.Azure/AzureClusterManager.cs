// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Newtonsoft.Json;

namespace Microsoft.Coyote.Samples.CloudMessaging
{
    internal class AzureClusterManager : ClusterManager
    {
        [DataContract]
        public class RegisterMessageBusEvent : Event
        {
            public ITopicClient TopicClient;
        }

        public ITopicClient TopicClient;

        protected override Task OnInitializeAsync(Event initialEvent)
        {
            var reg = initialEvent as RegisterMessageBusEvent;
            this.TopicClient = reg.TopicClient;
            return base.OnInitializeAsync(initialEvent);
        }

        public override async Task BroadcastVoteRequestAsync(Event e)
        {
            var request = e as VoteRequestEvent;
            Message message = new Message(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(request)))
            {
                Label = "VoteRequest",
                ReplyTo = request.CandidateId
            };

            await this.TopicClient.SendAsync(message);
        }

        public override async Task SendVoteResponseAsync(Event e)
        {
            var response = e as VoteResponseEvent;

            Message message = new Message(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(response)))
            {
                Label = "VoteResponse",
                To = response.TargetId
            };

            await this.TopicClient.SendAsync(message);
        }

        public override async Task BroadcastClientRequestAsync(Event e)
        {
            var req = e as ClientRequestEvent;
            Message message = new Message(Encoding.UTF8.GetBytes(
                JsonConvert.SerializeObject(req)))
            {
                Label = "ClientRequest"
            };

            await this.TopicClient.SendAsync(message);
        }

        public override async Task SendClientResponseAsync(Event e)
        {
            var response = e as ClientResponseEvent;
            Message message = new Message(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(response)))
            {
                Label = "ClientResponse"
            };

            await this.TopicClient.SendAsync(message);
        }

        public override async Task SendAppendEntriesRequestAsync(Event e)
        {
            var request = e as AppendLogEntriesRequestEvent;
            Message message = new Message(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(request)))
            {
                Label = "AppendEntriesRequest",
                To = request.To,
                ReplyTo = request.LeaderId
            };

            await this.TopicClient.SendAsync(message);
        }

        public override async Task SendAppendEntriesResponseAsync(Event e)
        {
            var response = e as AppendLogEntriesResponseEvent;
            Message message = new Message(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(response)))
            {
                Label = "AppendEntriesResponse",
                To = response.To,
                ReplyTo = response.SenderId
            };

            await this.TopicClient.SendAsync(message);
        }
    }
}
