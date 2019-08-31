using System;
using System.Collections.Generic;
using Microsoft.Coyote;

namespace FailureDetector
{
    /// <summary>
    /// This is the test harness responsible for creating a
    /// user-defined number of nodes, and registering them
    /// with a failure detector machine.
    ///
    /// The driver is also responsible for injecting failures
    /// to the nodes for testing purposes.
    /// </summary>
    internal class Driver : Machine
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
            public MachineId Client;

            public RegisterClient(MachineId client)
            {
                this.Client = client;
            }
        }

        internal class UnregisterClient : Event
        {
            public MachineId Client;

            public UnregisterClient(MachineId client)
            {
                this.Client = client;
            }
        }

        MachineId FailureDetector;
        HashSet<MachineId> Nodes;
        int NumOfNodes;

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        class Init : MachineState { }

        void InitOnEntry()
        {
            this.NumOfNodes = (this.ReceivedEvent as Config).NumOfNodes;

            // Initializes the nodes.
            this.Nodes = new HashSet<MachineId>();
            for (int i = 0; i < this.NumOfNodes; i++)
            {
                var node = this.CreateMachine(typeof(Node));
                this.Nodes.Add(node);
            }

            // Notifies the liveness monitor that the nodes are initialized.
            this.Monitor<Liveness>(new Liveness.RegisterNodes(this.Nodes));

            this.FailureDetector = this.CreateMachine(typeof(FailureDetector), new FailureDetector.Config(this.Nodes));
            this.Send(this.FailureDetector, new RegisterClient(this.Id));

            this.Goto<InjectFailures>();
        }

        [OnEntry(nameof(InjectFailuresOnEntry))]
        [OnEventDoAction(typeof(FailureDetector.NodeFailed), nameof(NodeFailedAction))]
        class InjectFailures : MachineState { }

        /// <summary>
        /// Injects failures (modelled with the special Coyote event 'halt').
        /// </summary>
        void InjectFailuresOnEntry()
        {
            foreach (var node in this.Nodes)
            {
                this.Send(node, new Halt());
            }
        }

        /// <summary>
        /// Notify liveness monitor of node failure.
        /// </summary>
        void NodeFailedAction()
        {
            this.Monitor<Liveness>(this.ReceivedEvent);
        }
    }
}

