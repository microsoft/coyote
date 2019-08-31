using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Coyote;

namespace ReplicatingStorage
{
    internal class NodeManager : Machine
    {
        #region events

        /// <summary>
        /// Used to configure the node manager.
        /// </summary>
        public class ConfigureEvent : Event
        {
            public MachineId Environment;
            public int NumberOfReplicas;

            public ConfigureEvent(MachineId env, int numOfReplicas)
                : base()
            {
                this.Environment = env;
                this.NumberOfReplicas = numOfReplicas;
            }
        }

        public class NotifyFailure : Event
        {
            public MachineId Node;

            public NotifyFailure(MachineId node)
                : base()
            {
                this.Node = node;
            }
        }

        internal class ShutDown : Event { }
        private class LocalEvent : Event { }

        #endregion

        #region fields

        /// <summary>
        /// The environment.
        /// </summary>
        private MachineId Environment;

        /// <summary>
        /// The storage nodes.
        /// </summary>
        private List<MachineId> StorageNodes;

        /// <summary>
        /// The number of storage replicas that must
        /// be sustained.
        /// </summary>
        private int NumberOfReplicas;

        /// <summary>
        /// Map from storage node ids to a boolean value that
        /// denotes if the node is alive or not.
        /// </summary>
        private Dictionary<int, bool> StorageNodeMap;

        /// <summary>
        /// Map from storage node ids to data they contain.
        /// </summary>
        private Dictionary<int, int> DataMap;

        /// <summary>
        /// The repair timer.
        /// </summary>
        private MachineId RepairTimer;

        /// <summary>
        /// The client who sent the latest request.
        /// </summary>
        private MachineId Client;

        #endregion

        #region states

        [Start]
        [OnEntry(nameof(EntryOnInit))]
        [OnEventDoAction(typeof(ConfigureEvent), nameof(Configure))]
        [OnEventGotoState(typeof(LocalEvent), typeof(Active))]
        [DeferEvents(typeof(Client.Request), typeof(RepairTimer.TimeoutEvent))]
        class Init : MachineState { }

        void EntryOnInit()
        {
            this.StorageNodes = new List<MachineId>();
            this.StorageNodeMap = new Dictionary<int, bool>();
            this.DataMap = new Dictionary<int, int>();

            this.RepairTimer = this.CreateMachine(typeof(RepairTimer));
            this.Send(this.RepairTimer, new RepairTimer.ConfigureEvent(this.Id));
        }

        void Configure()
        {
            this.Environment = (this.ReceivedEvent as ConfigureEvent).Environment;
            this.NumberOfReplicas = (this.ReceivedEvent as ConfigureEvent).NumberOfReplicas;

            for (int idx = 0; idx < this.NumberOfReplicas; idx++)
            {
                this.CreateNewNode();
            }

            this.Raise(new LocalEvent());
        }

        void CreateNewNode()
        {
            var idx = this.StorageNodes.Count;
            var node = this.CreateMachine(typeof(StorageNode));
            this.StorageNodes.Add(node);
            this.StorageNodeMap.Add(idx, true);
            this.Send(node, new StorageNode.ConfigureEvent(this.Environment, this.Id, idx));
        }

        [OnEventDoAction(typeof(Client.Request), nameof(ProcessClientRequest))]
        [OnEventDoAction(typeof(RepairTimer.TimeoutEvent), nameof(RepairNodes))]
        [OnEventDoAction(typeof(StorageNode.SyncReport), nameof(ProcessSyncReport))]
        [OnEventDoAction(typeof(NotifyFailure), nameof(ProcessFailure))]
        class Active : MachineState { }

        void ProcessClientRequest()
        {
            this.Client = (this.ReceivedEvent as Client.Request).Client;
            var command = (this.ReceivedEvent as Client.Request).Command;

            var aliveNodeIds = this.StorageNodeMap.Where(n => n.Value).Select(n => n.Key);
            foreach (var nodeId in aliveNodeIds)
            {
                this.Send(this.StorageNodes[nodeId], new StorageNode.StoreRequest(command));
            }
        }

        void RepairNodes()
        {
            if (this.DataMap.Count == 0)
            {
                return;
            }

            var latestData = this.DataMap.Values.Max();
            var numOfReplicas = this.DataMap.Count(kvp => kvp.Value == latestData);
            if (numOfReplicas >= this.NumberOfReplicas)
            {
                return;
            }

            foreach (var node in this.DataMap)
            {
                if (node.Value != latestData)
                {
                    this.Logger.WriteLine("\n [NodeManager] repairing storage node {0}.\n", node.Key);
                    this.Send(this.StorageNodes[node.Key], new StorageNode.SyncRequest(latestData));
                    numOfReplicas++;
                }

                if (numOfReplicas == this.NumberOfReplicas)
                {
                    break;
                }
            }
        }

        void ProcessSyncReport()
        {
            var nodeId = (this.ReceivedEvent as StorageNode.SyncReport).NodeId;
            var data = (this.ReceivedEvent as StorageNode.SyncReport).Data;

            // LIVENESS BUG: can fail to ever repair again as it thinks there
            // are enough replicas. Enable to introduce a bug fix.
            //if (!this.StorageNodeMap.ContainsKey(nodeId))
            //{
            //    return;
            //}

            if (!this.DataMap.ContainsKey(nodeId))
            {
                this.DataMap.Add(nodeId, 0);
            }

            this.DataMap[nodeId] = data;
        }

        void ProcessFailure()
        {
            var node = (this.ReceivedEvent as NotifyFailure).Node;
            var nodeId = this.StorageNodes.IndexOf(node);
            this.StorageNodeMap.Remove(nodeId);
            this.DataMap.Remove(nodeId);

            this.Logger.WriteLine("\n [NodeManager] storage node {0} failed.\n", nodeId);

            this.CreateNewNode();
        }

        #endregion
    }
}
