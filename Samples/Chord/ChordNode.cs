using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Coyote;

namespace Chord
{
    internal class ChordNode : Machine
    {
        #region events

        internal class Config : Event
        {
            public int Id;
            public HashSet<int> Keys;
            public List<MachineId> Nodes;
            public List<int> NodeIds;
            public MachineId Manager;

            public Config(int id, HashSet<int> keys, List<MachineId> nodes,
                List<int> nodeIds, MachineId manager)
                : base()
            {
                this.Id = id;
                this.Keys = keys;
                this.Nodes = nodes;
                this.NodeIds = nodeIds;
                this.Manager = manager;
            }
        }

        internal class Join : Event
        {
            public int Id;
            public List<MachineId> Nodes;
            public List<int> NodeIds;
            public int NumOfIds;
            public MachineId Manager;

            public Join(int id, List<MachineId> nodes, List<int> nodeIds,
                int numOfIds, MachineId manager)
                : base()
            {
                this.Id = id;
                this.Nodes = nodes;
                this.NodeIds = nodeIds;
                this.NumOfIds = numOfIds;
                this.Manager = manager;
            }
        }

        internal class FindSuccessor : Event
        {
            public MachineId Sender;
            public int Key;

            public FindSuccessor(MachineId sender, int key)
                : base()
            {
                this.Sender = sender;
                this.Key = key;
            }
        }

        internal class FindSuccessorResp : Event
        {
            public MachineId Node;
            public int Key;

            public FindSuccessorResp(MachineId node, int key)
                : base()
            {
                this.Node = node;
                this.Key = key;
            }
        }

        internal class FindPredecessor : Event
        {
            public MachineId Sender;

            public FindPredecessor(MachineId sender)
                : base()
            {
                this.Sender = sender;
            }
        }

        internal class FindPredecessorResp : Event
        {
            public MachineId Node;

            public FindPredecessorResp(MachineId node)
                : base()
            {
                this.Node = node;
            }
        }

        internal class QueryId : Event
        {
            public MachineId Sender;

            public QueryId(MachineId sender)
                : base()
            {
                this.Sender = sender;
            }
        }

        internal class QueryIdResp : Event
        {
            public int Id;

            public QueryIdResp(int id)
                : base()
            {
                this.Id = id;
            }
        }

        internal class AskForKeys : Event
        {
            public MachineId Node;
            public int Id;

            public AskForKeys(MachineId node, int id)
                : base()
            {
                this.Node = node;
                this.Id = id;
            }
        }

        internal class AskForKeysResp : Event
        {
            public List<int> Keys;

            public AskForKeysResp(List<int> keys)
                : base()
            {
                this.Keys = keys;
            }
        }

        private class NotifySuccessor : Event
        {
            public MachineId Node;

            public NotifySuccessor(MachineId node)
                : base()
            {
                this.Node = node;
            }
        }

        internal class JoinAck : Event { }
        internal class Stabilize : Event { }
        internal class Terminate : Event { }
        private class Local : Event { }

        #endregion

        #region fields

        int NodeId;
        HashSet<int> Keys;
        int NumOfIds;

        Dictionary<int, Finger> FingerTable;
        MachineId Predecessor;

        MachineId Manager;

        #endregion

        #region states

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        [OnEventGotoState(typeof(Local), typeof(Waiting))]
        [OnEventDoAction(typeof(Config), nameof(Configure))]
        [OnEventDoAction(typeof(Join), nameof(JoinCluster))]
        [DeferEvents(typeof(AskForKeys), typeof(NotifySuccessor), typeof(Stabilize))]
        class Init : MachineState { }

		void InitOnEntry()
        {
            this.FingerTable = new Dictionary<int, Finger>();
        }

        void Configure()
        {
            this.NodeId = (this.ReceivedEvent as Config).Id;
            this.Keys = (this.ReceivedEvent as Config).Keys;
            this.Manager = (this.ReceivedEvent as Config).Manager;

            var nodes = (this.ReceivedEvent as Config).Nodes;
            var nodeIds = (this.ReceivedEvent as Config).NodeIds;

            this.NumOfIds = (int)Math.Pow(2, nodes.Count);

            for (var idx = 1; idx <= nodes.Count; idx++)
            {
                var start = (this.NodeId + (int)Math.Pow(2, (idx - 1))) % this.NumOfIds;
                var end = (this.NodeId + (int)Math.Pow(2, idx)) % this.NumOfIds;

                var nodeId = this.GetSuccessorNodeId(start, nodeIds);
                this.FingerTable.Add(start, new Finger(start, end, nodes[nodeId]));
            }

            for (var idx = 0; idx < nodeIds.Count; idx++)
            {
                if (nodeIds[idx] == this.NodeId)
                {
                    this.Predecessor = nodes[this.WrapSubtract(idx, 1, nodeIds.Count)];
                    break;
                }
            }

            this.Raise(new Local());
        }

        void JoinCluster()
        {
            this.NodeId = (this.ReceivedEvent as Join).Id;
            this.Manager = (this.ReceivedEvent as Join).Manager;
            this.NumOfIds = (this.ReceivedEvent as Join).NumOfIds;

            var nodes = (this.ReceivedEvent as Join).Nodes;
            var nodeIds = (this.ReceivedEvent as Join).NodeIds;

            for (var idx = 1; idx <= nodes.Count; idx++)
            {
                var start = (this.NodeId + (int)Math.Pow(2, (idx - 1))) % this.NumOfIds;
                var end = (this.NodeId + (int)Math.Pow(2, idx)) % this.NumOfIds;

                var nodeId = this.GetSuccessorNodeId(start, nodeIds);
                this.FingerTable.Add(start, new Finger(start, end, nodes[nodeId]));
            }

            var successor = this.FingerTable[(this.NodeId + 1) % this.NumOfIds].Node;

            this.Send(this.Manager, new JoinAck());
            this.Send(successor, new NotifySuccessor(this.Id));
        }

        [OnEventDoAction(typeof(FindSuccessor), nameof(ProcessFindSuccessor))]
        [OnEventDoAction(typeof(FindSuccessorResp), nameof(ProcessFindSuccessorResp))]
        [OnEventDoAction(typeof(FindPredecessor), nameof(ProcessFindPredecessor))]
        [OnEventDoAction(typeof(FindPredecessorResp), nameof(ProcessFindPredecessorResp))]
        [OnEventDoAction(typeof(QueryId), nameof(ProcessQueryId))]
        [OnEventDoAction(typeof(AskForKeys), nameof(SendKeys))]
        [OnEventDoAction(typeof(AskForKeysResp), nameof(UpdateKeys))]
        [OnEventDoAction(typeof(NotifySuccessor), nameof(UpdatePredecessor))]
        [OnEventDoAction(typeof(Stabilize), nameof(ProcessStabilize))]
        [OnEventDoAction(typeof(Terminate), nameof(ProcessTerminate))]
        class Waiting : MachineState { }

        void ProcessFindSuccessor()
        {
            var sender = (this.ReceivedEvent as FindSuccessor).Sender;
            var key = (this.ReceivedEvent as FindSuccessor).Key;

            if (this.Keys.Contains(key))
            {
                this.Send(sender, new FindSuccessorResp(this.Id, key));
            }
            else if (this.FingerTable.ContainsKey(key))
            {
                this.Send(sender, new FindSuccessorResp(this.FingerTable[key].Node, key));
            }
            else if (this.NodeId.Equals(key))
            {
                this.Send(sender, new FindSuccessorResp(
                    this.FingerTable[(this.NodeId + 1) % this.NumOfIds].Node, key));
            }
            else
            {
                int idToAsk = -1;
                foreach (var finger in this.FingerTable)
                {
                    if (((finger.Value.Start > finger.Value.End) &&
                        (finger.Value.Start <= key || key < finger.Value.End)) ||
                        ((finger.Value.Start < finger.Value.End) &&
                        (finger.Value.Start <= key && key < finger.Value.End)))
                    {
                        idToAsk = finger.Key;
                    }
                }

                if (idToAsk < 0)
                {
                    idToAsk = (this.NodeId + 1) % this.NumOfIds;
                }

                if (this.FingerTable[idToAsk].Node.Equals(this.Id))
                {
                    foreach (var finger in this.FingerTable)
                    {
                        if (finger.Value.End == idToAsk ||
                            finger.Value.End == idToAsk - 1)
                        {
                            idToAsk = finger.Key;
                            break;
                        }
                    }

                    this.Assert(!this.FingerTable[idToAsk].Node.Equals(this.Id),
                        "Cannot locate successor of {0}.", key);
                }

                this.Send(this.FingerTable[idToAsk].Node, new FindSuccessor(sender, key));
            }
        }

        void ProcessFindPredecessor()
        {
            var sender = (this.ReceivedEvent as FindPredecessor).Sender;
            if (this.Predecessor != null)
            {
                this.Send(sender, new FindPredecessorResp(this.Predecessor));
            }
        }

        void ProcessQueryId()
        {
            var sender = (this.ReceivedEvent as QueryId).Sender;
            this.Send(sender, new QueryIdResp(this.NodeId));
        }

        void SendKeys()
        {
            var sender = (this.ReceivedEvent as AskForKeys).Node;
            var senderId = (this.ReceivedEvent as AskForKeys).Id;

            this.Assert(this.Predecessor.Equals(sender), "Predecessor is corrupted.");

            List<int> keysToSend = new List<int>();
            foreach (var key in this.Keys)
            {
                if (key <= senderId)
                {
                    keysToSend.Add(key);
                }
            }

            if (keysToSend.Count > 0)
            {
                foreach (var key in keysToSend)
                {
                    this.Keys.Remove(key);
                }

                this.Send(sender, new AskForKeysResp(keysToSend));
            }
        }

        void ProcessStabilize()
        {
            var successor = this.FingerTable[(this.NodeId + 1) % this.NumOfIds].Node;
            this.Send(successor, new FindPredecessor(this.Id));

            foreach (var finger in this.FingerTable)
            {
                if (!finger.Value.Node.Equals(successor))
                {
                    this.Send(successor, new FindSuccessor(this.Id, finger.Key));
                }
            }
        }

        void ProcessFindSuccessorResp()
        {
            var successor = (this.ReceivedEvent as FindSuccessorResp).Node;
            var key = (this.ReceivedEvent as FindSuccessorResp).Key;

            this.Assert(this.FingerTable.ContainsKey(key),
                "Finger table of {0} does not contain {1}.", this.NodeId, key);
            this.FingerTable[key] = new Finger(this.FingerTable[key].Start,
                this.FingerTable[key].End, successor);
        }

        void ProcessFindPredecessorResp()
        {
            var successor = (this.ReceivedEvent as FindPredecessorResp).Node;
            if (!successor.Equals(this.Id))
            {
                this.FingerTable[(this.NodeId + 1) % this.NumOfIds] =
                    new Finger(this.FingerTable[(this.NodeId + 1) % this.NumOfIds].Start,
                    this.FingerTable[(this.NodeId + 1) % this.NumOfIds].End,
                    successor);

                this.Send(successor, new NotifySuccessor(this.Id));
                this.Send(successor, new AskForKeys(this.Id, this.NodeId));
            }
        }

        void UpdatePredecessor()
        {
            var predecessor = (this.ReceivedEvent as NotifySuccessor).Node;
            if (!predecessor.Equals(this.Id))
            {
                this.Predecessor = predecessor;
            }
        }

        void UpdateKeys()
        {
            var keys = (this.ReceivedEvent as AskForKeysResp).Keys;
            foreach (var key in keys)
            {
                this.Keys.Add(key);
            }
        }

        void ProcessTerminate()
        {
            this.Raise(new Halt());
        }

        int GetSuccessorNodeId(int start, List<int> nodeIds)
        {
            var candidate = -1;
            foreach (var id in nodeIds.Where(v => v >= start))
            {
                if (candidate < 0 || id < candidate)
                {
                    candidate = id;
                }
            }

            if (candidate < 0)
            {
                foreach (var id in nodeIds.Where(v => v < start))
                {
                    if (candidate < 0 || id < candidate)
                    {
                        candidate = id;
                    }
                }
            }

            for (int idx = 0; idx < nodeIds.Count; idx++)
            {
                if (nodeIds[idx] == candidate)
                {
                    candidate = idx;
                    break;
                }
            }

            return candidate;
        }

        int WrapAdd(int left, int right, int ceiling)
        {
            int result = left + right;
            if (result > ceiling)
            {
                result = ceiling - result;
            }

            return result;
        }

        int WrapSubtract(int left, int right, int ceiling)
        {
            int result = left - right;
            if (result < 0)
            {
                result = ceiling + result;
            }

            return result;
        }

        void EmitFingerTableAndKeys()
        {
            this.Logger.WriteLine(" ... Printing finger table of node {0}:", this.NodeId);
            foreach (var finger in this.FingerTable)
            {
                this.Logger.WriteLine("  >> " + finger.Key + " | [" + finger.Value.Start +
                    ", " + finger.Value.End + ") | " + finger.Value.Node);
            }

            this.Logger.WriteLine(" ... Printing keys of node {0}:", this.NodeId);
            foreach (var key in this.Keys)
            {
                this.Logger.WriteLine("  >> Key-" + key);
            }
        }

        #endregion
    }
}
