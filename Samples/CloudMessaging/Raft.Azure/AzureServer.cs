// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.ServiceBus.Management;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Random;
using Microsoft.Coyote.Runtime;

namespace Microsoft.Coyote.Samples.CloudMessaging
{
    /// <summary>
    /// Basic implementation of a host that wraps the <see cref="Server"/> state machine
    /// instance executing in this process. The host uses the Azure Service Bus messaging
    /// framework to allow communication between the hosted server instance and all other
    /// remote servers that are part of the same Raft service, as well as the Raft client.
    /// </summary>
    public class AzureServer : IServerManager
    {
        /// <summary>
        /// The Coyote runtime responsible for executing the hosted state machine.
        /// </summary>
        private readonly IActorRuntime Runtime;

        /// <summary>
        /// Client providing access to the Azure Service Bus account.
        /// </summary>
        private readonly ManagementClient ManagementClient;

        /// <summary>
        /// Connection string to the Azure Service Bus account.
        /// </summary>
        private readonly string ConnectionString;

        /// <summary>
        /// The name of the Azure Service Bus topic.
        /// </summary>
        private readonly string TopicName;

        /// <summary>
        /// Actor id that provides access to the hosted <see cref="Server"/> state machine.
        /// </summary>
        public readonly ActorId HostedServer;

        /// <summary>
        /// Actor id that provides access to the hosted <see cref="ClusterManager"/> state machine.
        /// </summary>
        private readonly ActorId ClusterManager;

        /// <summary>
        /// Set that contains the id of each remote server in the Raft service.
        /// </summary>
        private readonly HashSet<string> RemoteServers;

        /// <summary>
        /// The id of the managed server.
        /// </summary>
        public string ServerId { get; }

        /// <summary>
        /// Collection of all remote server ids.
        /// </summary>
        public IEnumerable<string> RemoteServerIds => this.RemoteServers.ToList();

        /// <summary>
        /// Total number of servers in the service.
        /// </summary>
        public int NumServers { get; }

        /// <summary>
        /// Random generator for timeout values.
        /// </summary>
        private readonly Generator RandomValueGenerator;

        /// <summary>
        /// The leader election due time.
        /// </summary>
        public TimeSpan LeaderElectionDueTime => TimeSpan.FromSeconds(10 + this.RandomValueGenerator.NextInteger(10));

        /// <summary>
        /// The leader election periodic time interval.
        /// </summary>
        public TimeSpan LeaderElectionPeriod => TimeSpan.FromSeconds(30 + this.RandomValueGenerator.NextInteger(30));

        /// <summary>
        /// The number of times to ignore HandleTimeout
        /// </summary>
        public int TimeoutDelay => 0;

        public AzureServer(IActorRuntime runtime, string connectionString, string topicName,
            int serverId, int numServers, ActorId clusterManager)
        {
            this.Runtime = runtime;
            this.ManagementClient = new ManagementClient(connectionString);
            this.ConnectionString = connectionString;
            this.TopicName = topicName;
            this.NumServers = numServers;
            this.ClusterManager = clusterManager;
            this.ServerId = $"Server-{serverId}";
            this.RandomValueGenerator = Generator.Create();

            this.RemoteServers = new HashSet<string>();
            for (int id = 0; id < numServers; id++)
            {
                this.RemoteServers.Add($"Server-{id}");
            }

            // Create an actor id that will uniquely identify the server state machine
            // and act as a proxy for sending it received events by Azure Service Bus.
            this.HostedServer = this.Runtime.CreateActorIdFromName(typeof(Server), this.ServerId);
        }

        public virtual void Initialize()
        {
            // Creates and runs an instance of the Server state machine.
            this.Runtime.CreateActor(this.HostedServer, typeof(Server), new Server.SetupServerEvent(this, this.ClusterManager));
        }

        public virtual void Start()
        {
            // Run an instance of the Server state machine.
            this.Runtime.SendEvent(this.HostedServer, new NotifyJoinedServiceEvent());
        }

        public void NotifyElectedLeader(int term)
        {
        }
    }
}
