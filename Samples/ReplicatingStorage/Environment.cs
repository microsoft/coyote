using System;
using System.Collections.Generic;
using Microsoft.Coyote;

namespace ReplicatingStorage
{
    internal class Environment : Machine
    {
        #region events

        public class NotifyNode : Event
        {
            public MachineId Node;

            public NotifyNode(MachineId node)
                : base()
            {
                this.Node = node;
            }
        }

        public class FaultInject : Event { }

        private class CreateFailure : Event { }
        private class LocalEvent : Event { }

        #endregion

        #region fields

        private MachineId NodeManager;
        private int NumberOfReplicas;

        private List<MachineId> AliveNodes;
        private int NumberOfFaults;

        private MachineId Client;

        /// <summary>
        /// The failure timer.
        /// </summary>
        private MachineId FailureTimer;

        #endregion

        #region states

        [Start]
        [OnEntry(nameof(EntryOnInit))]
        [OnEventGotoState(typeof(LocalEvent), typeof(Configuring))]
        class Init : MachineState { }

        void EntryOnInit()
        {
            this.NumberOfReplicas = 3;
            this.NumberOfFaults = 1;
            this.AliveNodes = new List<MachineId>();

            this.Monitor<LivenessMonitor>(new LivenessMonitor.ConfigureEvent(this.NumberOfReplicas));

            this.NodeManager = this.CreateMachine(typeof(NodeManager));
            this.Client = this.CreateMachine(typeof(Client));

            this.Raise(new LocalEvent());
        }

        [OnEntry(nameof(ConfiguringOnInit))]
        [OnEventGotoState(typeof(LocalEvent), typeof(Active))]
        [DeferEvents(typeof(FailureTimer.TimeoutEvent))]
        class Configuring : MachineState { }

        void ConfiguringOnInit()
        {
            this.Send(this.NodeManager, new NodeManager.ConfigureEvent(this.Id, this.NumberOfReplicas));
            this.Send(this.Client, new Client.ConfigureEvent(this.NodeManager));
            this.Raise(new LocalEvent());
        }

        [OnEventDoAction(typeof(NotifyNode), nameof(UpdateAliveNodes))]
        [OnEventDoAction(typeof(FailureTimer.TimeoutEvent), nameof(InjectFault))]
        class Active : MachineState { }

        void UpdateAliveNodes()
        {
            var node = (this.ReceivedEvent as NotifyNode).Node;
            this.AliveNodes.Add(node);

            if (this.AliveNodes.Count == this.NumberOfReplicas &&
                this.FailureTimer == null)
            {
                this.FailureTimer = this.CreateMachine(typeof(FailureTimer));
                this.Send(this.FailureTimer, new FailureTimer.ConfigureEvent(this.Id));
            }
        }

        void InjectFault()
        {
            if (this.NumberOfFaults == 0 ||
                this.AliveNodes.Count == 0)
            {
                return;
            }

            int nodeId = this.RandomInteger(this.AliveNodes.Count);
            var node = this.AliveNodes[nodeId];

            this.Logger.WriteLine("\n [Environment] injecting fault.\n");

            this.Send(node, new FaultInject());
            this.Send(this.NodeManager, new NodeManager.NotifyFailure(node));
            this.AliveNodes.Remove(node);

            this.NumberOfFaults--;
            if (this.NumberOfFaults == 0)
            {
                this.Send(this.FailureTimer, new Halt());
            }
        }

        #endregion
    }
}
