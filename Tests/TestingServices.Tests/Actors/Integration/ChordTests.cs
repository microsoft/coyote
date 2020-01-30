// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Specifications;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests.Actors
{
    /// <summary>
    /// A single-process implementation of the chord peer-to-peer look up service.
    ///
    /// The Chord protocol is described in the following paper:
    /// https://pdos.csail.mit.edu/papers/chord:sigcomm01/chord_sigcomm.pdf
    ///
    /// This test contains a bug that leads to a liveness assertion failure.
    /// </summary>
    public class ChordTests : BaseTest
    {
        public ChordTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private class Finger
        {
            public int Start;
            public int End;
            public ActorId Node;

            public Finger(int start, int end, ActorId node)
            {
                this.Start = start;
                this.End = end;
                this.Node = node;
            }
        }

        private class ClusterManager : StateMachine
        {
            internal class CreateNewNode : Event
            {
            }

            internal class TerminateNode : Event
            {
            }

            private class Local : Event
            {
            }

            private int NumOfNodes;
            private int NumOfIds;

            private List<ActorId> ChordNodes;

            private List<int> Keys;
            private List<int> NodeIds;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventGotoState(typeof(Local), typeof(Waiting))]
            private class Init : State
            {
            }

            private Transition InitOnEntry()
            {
                this.NumOfNodes = 3;
                this.NumOfIds = (int)Math.Pow(2, this.NumOfNodes);

                this.ChordNodes = new List<ActorId>();
                this.NodeIds = new List<int> { 0, 1, 3 };
                this.Keys = new List<int> { 1, 2, 6 };

                for (int idx = 0; idx < this.NodeIds.Count; idx++)
                {
                    this.ChordNodes.Add(this.CreateActor(typeof(ChordNode)));
                }

                var nodeKeys = this.AssignKeysToNodes();
                for (int idx = 0; idx < this.ChordNodes.Count; idx++)
                {
                    var keys = nodeKeys[this.NodeIds[idx]];
                    this.SendEvent(this.ChordNodes[idx], new ChordNode.SetupEvent(this.NodeIds[idx], new HashSet<int>(keys),
                        new List<ActorId>(this.ChordNodes), new List<int>(this.NodeIds), this.Id));
                }

                this.CreateActor(typeof(Client), new Client.SetupEvent(this.Id, new List<int>(this.Keys)));
                return this.RaiseEvent(new Local());
            }

            [OnEventDoAction(typeof(ChordNode.FindSuccessor), nameof(ForwardFindSuccessor))]
            [OnEventDoAction(typeof(CreateNewNode), nameof(ProcessCreateNewNode))]
            [OnEventDoAction(typeof(TerminateNode), nameof(ProcessTerminateNode))]
            [OnEventDoAction(typeof(ChordNode.JoinAck), nameof(QueryStabilize))]
            private class Waiting : State
            {
            }

            private void ForwardFindSuccessor(Event e)
            {
                this.SendEvent(this.ChordNodes[0], e);
            }

            private void ProcessCreateNewNode()
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

                var newNode = this.CreateActor(typeof(ChordNode));

                this.NumOfNodes++;
                this.NodeIds.Add(newId);
                this.ChordNodes.Add(newNode);

                this.SendEvent(newNode, new ChordNode.Join(newId, new List<ActorId>(this.ChordNodes),
                    new List<int>(this.NodeIds), this.NumOfIds, this.Id));
            }

            private void ProcessTerminateNode()
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

                this.SendEvent(endNode, new ChordNode.Terminate());
            }

            private void QueryStabilize()
            {
                foreach (var node in this.ChordNodes)
                {
                    this.SendEvent(node, new ChordNode.Stabilize());
                }
            }

            private Dictionary<int, List<int>> AssignKeysToNodes()
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
        }

        private class ChordNode : StateMachine
        {
            internal class SetupEvent : Event
            {
                public int Id;
                public HashSet<int> Keys;
                public List<ActorId> Nodes;
                public List<int> NodeIds;
                public ActorId ManagerId;

                public SetupEvent(int id, HashSet<int> keys, List<ActorId> nodes,
                    List<int> nodeIds, ActorId managerId)
                    : base()
                {
                    this.Id = id;
                    this.Keys = keys;
                    this.Nodes = nodes;
                    this.NodeIds = nodeIds;
                    this.ManagerId = managerId;
                }
            }

            internal class Join : Event
            {
                public int Id;
                public List<ActorId> Nodes;
                public List<int> NodeIds;
                public int NumOfIds;
                public ActorId ManagerId;

                public Join(int id, List<ActorId> nodes, List<int> nodeIds,
                    int numOfIds, ActorId managerId)
                    : base()
                {
                    this.Id = id;
                    this.Nodes = nodes;
                    this.NodeIds = nodeIds;
                    this.NumOfIds = numOfIds;
                    this.ManagerId = managerId;
                }
            }

            internal class FindSuccessor : Event
            {
                public ActorId Sender;
                public int Key;

                public FindSuccessor(ActorId sender, int key)
                    : base()
                {
                    this.Sender = sender;
                    this.Key = key;
                }
            }

            internal class FindSuccessorResp : Event
            {
                public ActorId Node;
                public int Key;

                public FindSuccessorResp(ActorId node, int key)
                    : base()
                {
                    this.Node = node;
                    this.Key = key;
                }
            }

            internal class FindPredecessor : Event
            {
                public ActorId Sender;

                public FindPredecessor(ActorId sender)
                    : base()
                {
                    this.Sender = sender;
                }
            }

            internal class FindPredecessorResp : Event
            {
                public ActorId Node;

                public FindPredecessorResp(ActorId node)
                    : base()
                {
                    this.Node = node;
                }
            }

            internal class QueryId : Event
            {
                public ActorId Sender;

                public QueryId(ActorId sender)
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
                public ActorId Node;
                public int Id;

                public AskForKeys(ActorId node, int id)
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
                public ActorId Node;

                public NotifySuccessor(ActorId node)
                    : base()
                {
                    this.Node = node;
                }
            }

            internal class JoinAck : Event
            {
            }

            internal class Stabilize : Event
            {
            }

            internal class Terminate : Event
            {
            }

            private class Local : Event
            {
            }

            private int NodeId;
            private HashSet<int> Keys;
            private int NumOfIds;

            private Dictionary<int, Finger> FingerTable;
            private ActorId Predecessor;

            private ActorId ManagerId;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventGotoState(typeof(Local), typeof(Waiting))]
            [OnEventDoAction(typeof(SetupEvent), nameof(Setup))]
            [OnEventDoAction(typeof(Join), nameof(JoinCluster))]
            [DeferEvents(typeof(AskForKeys), typeof(NotifySuccessor), typeof(Stabilize))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.FingerTable = new Dictionary<int, Finger>();
            }

            private Transition Setup(Event e)
            {
                this.NodeId = (e as SetupEvent).Id;
                this.Keys = (e as SetupEvent).Keys;
                this.ManagerId = (e as SetupEvent).ManagerId;

                var nodes = (e as SetupEvent).Nodes;
                var nodeIds = (e as SetupEvent).NodeIds;

                this.NumOfIds = (int)Math.Pow(2, nodes.Count);

                for (var idx = 1; idx <= nodes.Count; idx++)
                {
                    var start = (this.NodeId + (int)Math.Pow(2, idx - 1)) % this.NumOfIds;
                    var end = (this.NodeId + (int)Math.Pow(2, idx)) % this.NumOfIds;

                    var nodeId = GetSuccessorNodeId(start, nodeIds);
                    this.FingerTable.Add(start, new Finger(start, end, nodes[nodeId]));
                }

                for (var idx = 0; idx < nodeIds.Count; idx++)
                {
                    if (nodeIds[idx] == this.NodeId)
                    {
                        this.Predecessor = nodes[WrapSubtract(idx, 1, nodeIds.Count)];
                        break;
                    }
                }

                return this.RaiseEvent(new Local());
            }

            private void JoinCluster(Event e)
            {
                this.NodeId = (e as Join).Id;
                this.ManagerId = (e as Join).ManagerId;
                this.NumOfIds = (e as Join).NumOfIds;

                var nodes = (e as Join).Nodes;
                var nodeIds = (e as Join).NodeIds;

                for (var idx = 1; idx <= nodes.Count; idx++)
                {
                    var start = (this.NodeId + (int)Math.Pow(2, idx - 1)) % this.NumOfIds;
                    var end = (this.NodeId + (int)Math.Pow(2, idx)) % this.NumOfIds;

                    var nodeId = GetSuccessorNodeId(start, nodeIds);
                    this.FingerTable.Add(start, new Finger(start, end, nodes[nodeId]));
                }

                var successor = this.FingerTable[(this.NodeId + 1) % this.NumOfIds].Node;

                this.SendEvent(this.ManagerId, new JoinAck());
                this.SendEvent(successor, new NotifySuccessor(this.Id));
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
            private class Waiting : State
            {
            }

            private void ProcessFindSuccessor(Event e)
            {
                var sender = (e as FindSuccessor).Sender;
                var key = (e as FindSuccessor).Key;

                if (this.Keys.Contains(key))
                {
                    this.SendEvent(sender, new FindSuccessorResp(this.Id, key));
                }
                else if (this.FingerTable.ContainsKey(key))
                {
                    this.SendEvent(sender, new FindSuccessorResp(this.FingerTable[key].Node, key));
                }
                else if (this.NodeId.Equals(key))
                {
                    this.SendEvent(sender, new FindSuccessorResp(
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
                            finger.Value.Start <= key && key < finger.Value.End))
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

                        this.Assert(!this.FingerTable[idToAsk].Node.Equals(this.Id), "Cannot locate successor of {0}.", key);
                    }

                    this.SendEvent(this.FingerTable[idToAsk].Node, new FindSuccessor(sender, key));
                }
            }

            private void ProcessFindPredecessor(Event e)
            {
                var sender = (e as FindPredecessor).Sender;
                if (this.Predecessor != null)
                {
                    this.SendEvent(sender, new FindPredecessorResp(this.Predecessor));
                }
            }

            private void ProcessQueryId(Event e)
            {
                var sender = (e as QueryId).Sender;
                this.SendEvent(sender, new QueryIdResp(this.NodeId));
            }

            private void SendKeys(Event e)
            {
                var sender = (e as AskForKeys).Node;
                var senderId = (e as AskForKeys).Id;

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

                    this.SendEvent(sender, new AskForKeysResp(keysToSend));
                }
            }

            private void ProcessStabilize()
            {
                var successor = this.FingerTable[(this.NodeId + 1) % this.NumOfIds].Node;
                this.SendEvent(successor, new FindPredecessor(this.Id));

                foreach (var finger in this.FingerTable)
                {
                    if (!finger.Value.Node.Equals(successor))
                    {
                        this.SendEvent(successor, new FindSuccessor(this.Id, finger.Key));
                    }
                }
            }

            private void ProcessFindSuccessorResp(Event e)
            {
                var successor = (e as FindSuccessorResp).Node;
                var key = (e as FindSuccessorResp).Key;

                this.Assert(this.FingerTable.ContainsKey(key), "Finger table of {0} does not contain {1}.", this.NodeId, key);
                this.FingerTable[key] = new Finger(this.FingerTable[key].Start, this.FingerTable[key].End, successor);
            }

            private void ProcessFindPredecessorResp(Event e)
            {
                var successor = (e as FindPredecessorResp).Node;
                if (!successor.Equals(this.Id))
                {
                    this.FingerTable[(this.NodeId + 1) % this.NumOfIds] = new Finger(
                        this.FingerTable[(this.NodeId + 1) % this.NumOfIds].Start,
                        this.FingerTable[(this.NodeId + 1) % this.NumOfIds].End,
                        successor);

                    this.SendEvent(successor, new NotifySuccessor(this.Id));
                    this.SendEvent(successor, new AskForKeys(this.Id, this.NodeId));
                }
            }

            private void UpdatePredecessor(Event e)
            {
                var predecessor = (e as NotifySuccessor).Node;
                if (!predecessor.Equals(this.Id))
                {
                    this.Predecessor = predecessor;
                }
            }

            private void UpdateKeys(Event e)
            {
                var keys = (e as AskForKeysResp).Keys;
                foreach (var key in keys)
                {
                    this.Keys.Add(key);
                }
            }

            private Transition ProcessTerminate() => this.Halt();

            private static int GetSuccessorNodeId(int start, List<int> nodeIds)
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

            private static int WrapSubtract(int left, int right, int ceiling)
            {
                int result = left - right;
                if (result < 0)
                {
                    result = ceiling + result;
                }

                return result;
            }
        }

        private class Client : StateMachine
        {
            internal class SetupEvent : Event
            {
                public ActorId ClusterManager;
                public List<int> Keys;

                public SetupEvent(ActorId clusterManager, List<int> keys)
                    : base()
                {
                    this.ClusterManager = clusterManager;
                    this.Keys = keys;
                }
            }

            private class Local : Event
            {
            }

            private ActorId ClusterManager;

            private List<int> Keys;
            private int QueryCounter;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventGotoState(typeof(Local), typeof(Querying))]
            private class Init : State
            {
            }

            private Transition InitOnEntry(Event e)
            {
                this.ClusterManager = (e as SetupEvent).ClusterManager;
                this.Keys = (e as SetupEvent).Keys;

                // LIVENESS BUG: can never detect the key, and keeps looping without
                // exiting the process. Enable to introduce the bug.
                this.Keys.Add(17);

                this.QueryCounter = 0;

                return this.RaiseEvent(new Local());
            }

            [OnEntry(nameof(QueryingOnEntry))]
            [OnEventGotoState(typeof(Local), typeof(Waiting))]
            private class Querying : State
            {
            }

            private Transition QueryingOnEntry()
            {
                if (this.QueryCounter < 5)
                {
                    if (this.Random())
                    {
                        var key = this.GetNextQueryKey();
                        this.Logger.WriteLine($"<ChordLog> Client is searching for successor of key '{key}'.");
                        this.SendEvent(this.ClusterManager, new ChordNode.FindSuccessor(this.Id, key));
                        this.Monitor<LivenessMonitor>(new LivenessMonitor.NotifyClientRequest(key));
                    }
                    else if (this.Random())
                    {
                        this.SendEvent(this.ClusterManager, new ClusterManager.CreateNewNode());
                    }
                    else
                    {
                        this.SendEvent(this.ClusterManager, new ClusterManager.TerminateNode());
                    }

                    this.QueryCounter++;
                }

                return this.RaiseEvent(new Local());
            }

            private int GetNextQueryKey()
            {
                int keyIndex = -1;
                while (keyIndex < 0)
                {
                    for (int i = 0; i < this.Keys.Count; i++)
                    {
                        if (this.Random())
                        {
                            keyIndex = i;
                            break;
                        }
                    }
                }

                return this.Keys[keyIndex];
            }

            [OnEventGotoState(typeof(Local), typeof(Querying))]
            [OnEventDoAction(typeof(ChordNode.FindSuccessorResp), nameof(ProcessFindSuccessorResp))]
            [OnEventDoAction(typeof(ChordNode.QueryIdResp), nameof(ProcessQueryIdResp))]
            private class Waiting : State
            {
            }

            private void ProcessFindSuccessorResp(Event e)
            {
                var successor = (e as ChordNode.FindSuccessorResp).Node;
                var key = (e as ChordNode.FindSuccessorResp).Key;
                this.Monitor<LivenessMonitor>(new LivenessMonitor.NotifyClientResponse(key));
                this.SendEvent(successor, new ChordNode.QueryId(this.Id));
            }

            private Transition ProcessQueryIdResp() => this.RaiseEvent(new Local());
        }

        private class LivenessMonitor : Monitor
        {
            public class NotifyClientRequest : Event
            {
                public int Key;

                public NotifyClientRequest(int key)
                    : base()
                {
                    this.Key = key;
                }
            }

            public class NotifyClientResponse : Event
            {
                public int Key;

                public NotifyClientResponse(int key)
                    : base()
                {
                    this.Key = key;
                }
            }

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private Transition InitOnEntry() => this.GotoState<Responded>();

            [Cold]
            [OnEventGotoState(typeof(NotifyClientRequest), typeof(Requested))]
            private class Responded : State
            {
            }

            [Hot]
            [OnEventGotoState(typeof(NotifyClientResponse), typeof(Responded))]
            private class Requested : State
            {
            }
        }

        [Theory(Timeout = 10000)]
        [InlineData(0)]
        public void TestLivenessBugInChordProtocol(int seed)
        {
            var configuration = GetConfiguration();
            configuration.MaxUnfairSchedulingSteps = 200;
            configuration.MaxFairSchedulingSteps = 2000;
            configuration.LivenessTemperatureThreshold = 1000;
            configuration.RandomSchedulingSeed = seed;
            configuration.SchedulingIterations = 1;

            this.TestWithError(r =>
            {
                r.RegisterMonitor(typeof(LivenessMonitor));
                r.CreateActor(typeof(ClusterManager));
            },
            configuration: configuration,
            expectedError: "LivenessMonitor detected potential liveness bug in hot state 'Requested'.",
            replay: true);
        }
    }
}
