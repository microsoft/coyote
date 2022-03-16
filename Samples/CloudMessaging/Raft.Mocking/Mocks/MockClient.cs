// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Actors.Timers;

namespace Microsoft.Coyote.Samples.CloudMessaging
{
    /// <summary>
    /// Mock implementation of a client that sends a specified number of requests to
    /// the Raft cluster.
    /// </summary>
    [OnEventDoAction(typeof(ClientResponseEvent), nameof(HandleResponse))]
    [OnEventDoAction(typeof(TimerElapsedEvent), nameof(HandleTimeout))]
    public class MockClient : Actor
    {
        public class SetupEvent : Event
        {
            internal readonly ActorId Cluster;
            internal readonly int NumRequests;
            internal readonly TimeSpan RetryTimeout;
            public TaskCompletionSource<bool> Finished;

            public SetupEvent(ActorId cluster, int numRequests, TimeSpan retryTimeout)
            {
                this.Cluster = cluster;
                this.NumRequests = numRequests;
                this.RetryTimeout = retryTimeout;
                this.Finished = new TaskCompletionSource<bool>();
            }
        }

        private SetupEvent ClientInfo;
        private int NumResponses;

        private string NextCommand => $"request-{this.NumResponses}";

        protected override Task OnInitializeAsync(Event initialEvent)
        {
            var setup = initialEvent as SetupEvent;
            this.ClientInfo = setup;
            this.NumResponses = 0;

            // Start by sending the first request.
            this.SendNextRequest();

            // Create a periodic timer to retry sending requests, if needed.
            // The chosen time does not matter, as the client will run under
            // test mode, and thus the time is controlled by the runtime.
            this.StartPeriodicTimer(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
            return Task.CompletedTask;
        }

        private void SendNextRequest()
        {
            this.SendEvent(this.ClientInfo.Cluster, new ClientRequestEvent(this.NextCommand));

            this.Logger.WriteLine($"<Client> sent {this.NextCommand}.");
        }

        private void HandleResponse(Event e)
        {
            var response = e as ClientResponseEvent;
            if (response.Command == this.NextCommand)
            {
                this.Logger.WriteLine($"<Client> received response for {response.Command} from  {response.Server}.");
                this.NumResponses++;

                if (this.NumResponses == this.ClientInfo.NumRequests)
                {
                    // Halt the client, as all responses have been received.
                    this.RaiseHaltEvent();
                    this.ClientInfo.Finished.SetResult(true);
                }
                else
                {
                    this.SendNextRequest();
                }
            }
        }

        /// <summary>
        /// Retry to send the request.
        /// </summary>
        private void HandleTimeout() => this.SendNextRequest();
    }
}
