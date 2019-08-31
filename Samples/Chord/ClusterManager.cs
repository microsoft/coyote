using System;
using System.Collections.Generic;
using Microsoft.Coyote;

namespace Chord
{
    internal class ClusterManager : Machine
    {
        #region events

        internal class CreateNewNode : Event { }
        internal class TerminateNode : Event { }
        private class Local : Event { }

        #endregion

        #region fields

        int NumOfNodes;
        int NumOfIds;

        List<MachineId> ChordNodes;

        List<int> Keys;
        List<int> NodeIds;

        MachineId Client;

        #endregion

        #region states

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        [OnEventGotoState(typeof(Local), typeof(Waiting))]
        class Init : MachineState { }

        void InitOnEntry()
        {
            this.NumOfNodes = 3;
            this.NumOfIds = (int)Math.Pow(2, this.NumOfNodes);

            this.ChordNodes = new List<MachineId>();
            this.NodeIds = new List<int> { 0, 1, 3 };
            this.Keys = new List<int> { 1, 2, 6 };

            for (int idx = 0; idx < this.NodeIds.Count; idx++)
            {
                this.ChordNodes.Add(this.CreateMachine(typeof(ChordNode)));
            }

            var nodeKeys = this.AssignKeysToNodes();
            for (int idx = 0; idx < this.ChordNodes.Count; idx++)
            {
                var keys = nodeKeys[this.NodeIds[idx]];
                this.Send(this.ChordNodes[idx], new ChordNode.Config(this.NodeIds[idx], new HashSet<int>(keys),
                    new List<MachineId>(this.ChordNodes), new List<int>(this.NodeIds), this.Id));
            }

            this.Client = this.CreateMachine(typeof(Client),
                new Client.Config(this.Id, new List<int>(this.Keys)));

            this.Raise(new Local());
        }

        [OnEventDoAction(typeof(ChordNode.FindSuccessor), nameof(ForwardFindSuccessor))]
        [OnEventDoAction(typeof(CreateNewNode), nameof(ProcessCreateNewNode))]
        [OnEventDoAction(typeof(TerminateNode), nameof(ProcessTerminateNode))]
        [OnEventDoAction(typeof(ChordNode.JoinAck), nameof(QueryStabilize))]
        class Waiting : MachineState { }

        void ForwardFindSuccessor()
        {
            this.Send(this.ChordNodes[0], this.ReceivedEvent);
        }

        void ProcessCreateNewNode()
        {
            int newId = -1;
            while ((newId < 0 || this.NodeIds.Contains(newId)) &&
                this.NodeIds.Count < this.NumOfIds)
            {
                for (int i = 0; i < this.NumOfIds; i++)
                {
                    if (this.Random())
                    {
                        newId = i;
                    }
                }
            }

            this.Assert(newId >= 0, "Cannot create a new node, no ids available.");

            var newNode = this.CreateMachine(typeof(ChordNode));

            this.NumOfNodes++;
            this.NodeIds.Add(newId);
            this.ChordNodes.Add(newNode);

            this.Send(newNode, new ChordNode.Join(newId, new List<MachineId>(this.ChordNodes),
                new List<int>(this.NodeIds), this.NumOfIds, this.Id));
        }

        void ProcessTerminateNode()
        {
            int endId = -1;
            while ((endId < 0 || !this.NodeIds.Contains(endId)) &&
                this.NodeIds.Count > 0)
            {
                for (int i = 0; i < this.ChordNodes.Count; i++)
                {
                    if (this.Random())
                    {
                        endId = i;
                    }
                }
            }

            this.Assert(endId >= 0, "Cannot find a node to terminate.");

            var endNode = this.ChordNodes[endId];

            this.NumOfNodes--;
            this.NodeIds.Remove(endId);
            this.ChordNodes.Remove(endNode);

            this.Send(endNode, new ChordNode.Terminate());
        }

        void QueryStabilize()
        {
            foreach (var node in this.ChordNodes)
            {
                this.Send(node, new ChordNode.Stabilize());
            }
        }

        Dictionary<int, List<int>> AssignKeysToNodes()
        {
            var nodeKeys = new Dictionary<int, List<int>>();
            for (int i = this.Keys.Count - 1; i >= 0; i--)
            {
                bool assigned = false;
                for (int j = 0; j < this.NodeIds.Count; j++)
                {
                    if (this.Keys[i] <= this.NodeIds[j])
                    {
                        if (nodeKeys.ContainsKey(this.NodeIds[j]))
                        {
                            nodeKeys[this.NodeIds[j]].Add(this.Keys[i]);
                        }
                        else
                        {
                            nodeKeys.Add(this.NodeIds[j], new List<int>());
                            nodeKeys[this.NodeIds[j]].Add(this.Keys[i]);
                        }

                        assigned = true;
                        break;
                    }
                }

                if (!assigned)
                {
                    if (nodeKeys.ContainsKey(this.NodeIds[0]))
                    {
                        nodeKeys[this.NodeIds[0]].Add(this.Keys[i]);
                    }
                    else
                    {
                        nodeKeys.Add(this.NodeIds[0], new List<int>());
                        nodeKeys[this.NodeIds[0]].Add(this.Keys[i]);
                    }
                }
            }

            return nodeKeys;
        }

        #endregion
    }
}
