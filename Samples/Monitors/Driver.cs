// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Coyote;
using Microsoft.Coyote.Actors;

namespace Microsoft.Coyote.Samples.Monitors
{
    /// <summary>
    /// This is the test harness responsible for creating a
    /// user-defined number of nodes, and registering them
    /// with a failure detector machine.
    ///
    /// The driver is also responsible for injecting failures
    /// to the nodes for testing purposes.
    /// </summary>
    internal class Driver : StateMachine
    {
        internal class Config : Event
        {
            public int NumOfNodes;

            public Config(int numOfNodes)
            {
                this.NumOfNodes = numOfNodes;
            }
        }

        internal class RegisterClient : Event
        {
            public ActorId Client;

            public RegisterClient(ActorId client)
            {
                this.Client = client;
            }
        }

        internal class UnregisterClient : Event
        {
            public ActorId Client;

            public UnregisterClient(ActorId client)
            {
                this.Client = client;
            }
        }

        private ActorId FailureDetector;
        private HashSet<ActorId> Nodes;
        private int NumOfNodes;

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        private class Init : State { }

        private void InitOnEntry(Event e)
        {
            this.NumOfNodes = (e as Config).NumOfNodes;

            // Initializes the nodes.
            this.Nodes = new HashSet<ActorId>();
            for (int i = 0; i < this.NumOfNodes; i++)
            {
                var node = this.CreateActor(typeof(Node));
                this.Nodes.Add(node);
            }

            // Notifies the liveness monitor that the nodes are initialized.
            this.Monitor<Liveness>(new Liveness.RegisterNodes(this.Nodes));

            this.FailureDetector = this.CreateActor(typeof(FailureDetector), new FailureDetector.Config(this.Nodes));
            this.SendEvent(this.FailureDetector, new RegisterClient(this.Id));

            this.RaiseGotoStateEvent<InjectFailures>();
        }

        [OnEntry(nameof(InjectFailuresOnEntry))]
        [OnEventDoAction(typeof(FailureDetector.NodeFailed), nameof(NodeFailedAction))]
        private class InjectFailures : State { }

        /// <summary>
        /// Injects failures (modelled with the special Coyote event 'halt').
        /// </summary>
        private void InjectFailuresOnEntry()
        {
            foreach (var node in this.Nodes)
            {
                this.SendEvent(node, HaltEvent.Instance);
            }
        }

        /// <summary>
        /// Notify liveness monitor of node failure.
        /// </summary>
        private void NodeFailedAction(Event e)
        {
            var node = (e as FailureDetector.NodeFailed).Node;
            this.Monitor<Liveness>(new Liveness.NodeFailed(node));
        }
    }
}
