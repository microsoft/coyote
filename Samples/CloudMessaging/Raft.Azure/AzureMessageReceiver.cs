// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Runtime;
using Newtonsoft.Json;

namespace Microsoft.Coyote.Samples.CloudMessaging
{
    /// <summary>
    /// This class receives messages from Azure Service Bus and pumps them into the Coyote runtime.
    /// </summary>
    internal class AzureMessageReceiver
    {
        /// <summary>
        /// The receiver for receiving messages from the topic.
        /// </summary>
        public IMessageReceiver SubscriptionReceiver;

        /// <summary>
        /// Id of the local actor that owns this cluster manager.
        /// </summary>
        private readonly ActorId LocalActorId;

        /// <summary>
        /// The name of the local actor.
        /// </summary>
        private readonly string LocalActorName;

        /// <summary>
        /// The Coyote runtime.
        /// </summary>
        private readonly IActorRuntime ActorRuntime;

        /// <summary>
        /// This event is raised when the cluster receives a ClientResponseEvent
        /// </summary>
        public event EventHandler<ClientResponseEvent> ResponseReceived;

        public AzureMessageReceiver(IActorRuntime runtime, string connectionString, string topicName, ActorId actorId, string subscriptionName)
        {
            this.ActorRuntime = runtime;
            this.LocalActorId = actorId;
            this.LocalActorName = (actorId == null) ? "Client" : actorId.Name;
            this.SubscriptionReceiver = new MessageReceiver(connectionString,
                EntityNameHelper.FormatSubscriptionPath(topicName, subscriptionName),
                ReceiveMode.ReceiveAndDelete);
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            await this.ReceiveMessagesAsync(cancellationToken);
        }

        /// <summary>
        /// Handle the receiving of messages from the Azure Message Bus
        /// </summary>
        /// <param name="cancellationToken">A way to cancel the process</param>
        /// <returns>An async task</returns>
        internal async Task ReceiveMessagesAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                // Receive the next message through Azure Service Bus.
                Message message = await this.SubscriptionReceiver.ReceiveAsync(TimeSpan.FromMilliseconds(50));

                // Now if the To field is empty then it is a broadcast (ClientRequest and VoteRequest)
                // otherwise ignore the message if it was meant for someone else.
                if (message != null && (string.IsNullOrEmpty(message.To) || message.To == this.LocalActorName))
                {
                    Event e = default;
                    string messageBody = Encoding.UTF8.GetString(message.Body);
                    if (message.Label == "ClientRequest")
                    {
                        e = JsonConvert.DeserializeObject<ClientRequestEvent>(messageBody);
                    }
                    else if (message.Label == "ClientResponse")
                    {
                        e = JsonConvert.DeserializeObject<ClientResponseEvent>(messageBody);
                    }
                    else if (message.Label == "VoteRequest")
                    {
                        var request = JsonConvert.DeserializeObject<VoteRequestEvent>(messageBody);
                        // do not broadcast back to ourselves!
                        if (request.CandidateId != this.LocalActorName)
                        {
                            e = request;
                        }
                    }
                    else if (message.Label == "VoteResponse")
                    {
                        e = JsonConvert.DeserializeObject<VoteResponseEvent>(messageBody);
                    }
                    else if (message.Label == "AppendEntriesRequest")
                    {
                        e = JsonConvert.DeserializeObject<AppendLogEntriesRequestEvent>(messageBody);
                    }
                    else if (message.Label == "AppendEntriesResponse")
                    {
                        e = JsonConvert.DeserializeObject<AppendLogEntriesResponseEvent>(messageBody);
                    }

                    if (e != default)
                    {
                        if (e is ClientResponseEvent clientResponse && this.ResponseReceived != null)
                        {
                            this.ResponseReceived(this, clientResponse);
                        }

                        // Special hack for the Client state machine, it is only expecting one event type, namely ClientResponseEvent
                        if (this.LocalActorId != null && (this.LocalActorName.Contains("Server") || e is ClientResponseEvent))
                        {
                            // Now bring this Service Bus message back into the Coyote framework by passing
                            // it along using the Coyote Runtime SendEvent.
                            this.ActorRuntime.SendEvent(this.LocalActorId, e);
                        }
                    }
                }
            }

            await this.SubscriptionReceiver.CloseAsync();
        }
    }
}
