// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Runtime;

namespace Microsoft.Coyote.Samples.CloudMessaging
{
    /// <summary>
    /// Tests the Raft service implementation by creating, hosting and executing
    /// in-memory the <see cref="Server"/> Coyote state machine instances, as well
    /// as a mock in-memory client.
    /// </summary>
    public class RaftTestScenario
    {
        /// <summary>
        /// During testing, Coyote injects a special version of the <see cref="IActorRuntime"/>
        /// that takes control of the test execution and systematically exercises interleavings
        /// and other sources of nondeterminism to find bugs in the specified scenario.
        /// </summary>
        public void RunTest(IActorRuntime runtime, int numServers, int numRequests)
        {
            // Register a safety monitor for checking the specification that
            // only one leader can be elected at any given term.
            runtime.RegisterMonitor<SafetyMonitor>();

            // Create the actor id for a client that will be sending requests to the Raft service.
            var client = runtime.CreateActorIdFromName(typeof(MockClient), "Client");

            // Create the actor for a cluster manager.
            var cluster = this.CreateClusterManager(runtime);

            var serverProxies = new List<ActorId>();
            for (int serverId = 0; serverId < numServers; serverId++)
            {
                // Create an actor id that will uniquely identify the server state machine
                // and act as a proxy for communicating with that state machine.
                serverProxies.Add(runtime.CreateActorIdFromName(typeof(Server), $"Server-{serverId}"));
            }

            runtime.SendEvent(cluster, new RegisterClientEvent() { ClientId = client });

            // Create the mock server hosts for wrapping and handling communication between
            // all server state machines that execute in-memory during this test.
            var serverHosts = new List<IServerManager>();
            foreach (var serverProxy in serverProxies)
            {
                // pass the remote server id's to the ClusterManager.
                runtime.SendEvent(cluster, new RegisterServerEvent() { ServerId = serverProxy });

                // Pass the actor id of each remote server to the host.
                serverHosts.Add(this.CreateServerHost(runtime, serverProxy, serverProxies.Where(
                    id => id != serverProxy), client, cluster));
            }

            // Create the server actors
            foreach (var serverHost in serverHosts)
            {
                serverHost.Initialize();
            }

            // Start executing each server. It is important to do this only after all state machines
            // have been initialized, since each one will try to asynchronously communicate with the
            // others, and thus they have to be already bound to their corresponding actor ids (else
            // the events cannot be delivered, and the runtime will catch it as an error).
            foreach (var serverHost in serverHosts)
            {
                serverHost.Start();
            }

            // Create the client actor instance, so the runtime starts executing it.
            runtime.CreateActor(client, typeof(MockClient), new MockClient.SetupEvent(cluster, numRequests, TimeSpan.FromSeconds(1)));
        }

        /// <summary>
        /// Creates a new cluster manager.
        /// </summary>
        protected virtual ActorId CreateClusterManager(IActorRuntime runtime) =>
            runtime.CreateActor(typeof(MockClusterManager));

        /// <summary>
        /// Creates a new server host.
        /// </summary>
        protected virtual IServerManager CreateServerHost(IActorRuntime runtime, ActorId serverProxy,
            IEnumerable<ActorId> serverProxies, ActorId client, ActorId cluster) =>
            new MockServerHost(runtime, serverProxy, serverProxies, client, cluster);
    }
}
