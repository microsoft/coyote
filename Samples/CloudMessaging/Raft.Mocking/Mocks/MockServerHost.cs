// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Coyote.Actors;

namespace Microsoft.Coyote.Samples.CloudMessaging
{
    /// <summary>
    /// Mock implementation of a host that wraps one of the <see cref="Server"/>
    /// state machine instances executing as part of a Raft service. The host
    /// maintains a set of all <see cref="Server"/> instances and allows them
    /// to communicate in-memory, so that Coyote can systematically test the
    /// Raft service logic.
    /// </summary>
    public class MockServerHost : IServerManager
    {
        /// <summary>
        /// The Coyote runtime responsible for executing the hosted state machine.
        /// </summary>
        protected readonly IActorRuntime Runtime;

        /// <summary>
        /// Actor id that provides access to the hosted <see cref="Server"/> state machine.
        /// </summary>
        protected readonly ActorId ServerProxy;

        /// <summary>
        /// Set that contains the actor id of each remote server in the Raft service.
        /// </summary>
        protected readonly Dictionary<string, ActorId> RemoteServers;

        /// <summary>
        /// The Raft client.
        /// </summary>
        protected readonly ActorId Client;

        /// <summary>
        /// The id of the managed server.
        /// </summary>
        public string ServerId { get; }

        /// <summary>
        /// Collection of all remote server ids.
        /// </summary>
        public IEnumerable<string> RemoteServerIds { get; }

        /// <summary>
        /// Total number of servers in the service.
        /// </summary>
        public int NumServers { get; }

        /// <summary>
        /// The leader election due time.
        /// </summary>
        public TimeSpan LeaderElectionDueTime => TimeSpan.FromSeconds(1);

        /// <summary>
        /// The leader election periodic time interval.
        /// </summary>
        public TimeSpan LeaderElectionPeriod => TimeSpan.FromSeconds(1);

        /// <summary>
        /// The number of times to ignore HandleTimeout
        /// </summary>
        public int TimeoutDelay => 10;

        /// <summary>
        /// Actor id that provides access to the hosted <see cref="ClusterManager"/> state machine.
        /// </summary>
        private readonly ActorId ClusterManager;

        public MockServerHost(IActorRuntime runtime, ActorId serverProxy,
            IEnumerable<ActorId> serverProxies, ActorId client, ActorId cluster)
        {
            this.Runtime = runtime;
            this.ServerProxy = serverProxy;
            this.ServerId = serverProxy.Name;
            this.ClusterManager = cluster;

            this.RemoteServers = new Dictionary<string, ActorId>();
            foreach (var server in serverProxies)
            {
                this.RemoteServers.Add(server.Name, server);
            }

            this.RemoteServerIds = this.RemoteServers.Keys.ToList();
            this.NumServers = this.RemoteServers.Count + 1;
            this.Client = client;
        }

        public virtual void Initialize()
        {
            // Creates an instance of the Server state machine and associates
            // it with the given actor id.
            this.Runtime.CreateActor(this.ServerProxy, typeof(Server), new Server.SetupServerEvent(this, this.ClusterManager));
        }

        public virtual void Start()
        {
            this.Runtime.SendEvent(this.ServerProxy, new NotifyJoinedServiceEvent());
        }

        public void NotifyElectedLeader(int term)
        {
            this.Runtime.Monitor<SafetyMonitor>(new SafetyMonitor.NotifyLeaderElected(term));
        }
    }
}
