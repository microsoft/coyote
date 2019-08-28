// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests
{
    /// <summary>
    /// A single-process implementation of the chain replication protocol.
    ///
    /// The chain replication protocol is described in the following paper:
    /// http://www.cs.cornell.edu/home/rvr/papers/OSDI04.pdf
    ///
    /// This test contains a bug that leads to a safety assertion failure.
    /// </summary>
    public class ChainReplicationTest : BaseTest
    {
        public ChainReplicationTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class SentLog
        {
            public int NextSeqId;
            public MachineId Client;
            public int Key;
            public int Value;

            public SentLog(int nextSeqId, MachineId client, int key, int val)
            {
                this.NextSeqId = nextSeqId;
                this.Client = client;
                this.Key = key;
                this.Value = val;
            }
        }

        private class Environment : Machine
        {
            private List<MachineId> Servers;
            private List<MachineId> Clients;

            private int NumOfServers;

            private MachineId ChainReplicationMaster;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Servers = new List<MachineId>();
                this.Clients = new List<MachineId>();

                this.NumOfServers = 3;

                for (int i = 0; i < this.NumOfServers; i++)
                {
                    MachineId server = null;

                    if (i == 0)
                    {
                        server = this.CreateMachine(
                            typeof(ChainReplicationServer),
                            new ChainReplicationServer.Config(i, true, false));
                    }
                    else if (i == this.NumOfServers - 1)
                    {
                        server = this.CreateMachine(
                            typeof(ChainReplicationServer),
                            new ChainReplicationServer.Config(i, false, true));
                    }
                    else
                    {
                        server = this.CreateMachine(
                            typeof(ChainReplicationServer),
                            new ChainReplicationServer.Config(i, false, false));
                    }

                    this.Servers.Add(server);
                }

                this.Monitor<InvariantMonitor>(
                    new InvariantMonitor.Config(this.Servers));
                this.Monitor<ServerResponseSeqMonitor>(
                    new ServerResponseSeqMonitor.Config(this.Servers));

                for (int i = 0; i < this.NumOfServers; i++)
                {
                    MachineId pred = null;
                    MachineId succ = null;

                    if (i > 0)
                    {
                        pred = this.Servers[i - 1];
                    }
                    else
                    {
                        pred = this.Servers[0];
                    }

                    if (i < this.NumOfServers - 1)
                    {
                        succ = this.Servers[i + 1];
                    }
                    else
                    {
                        succ = this.Servers[this.NumOfServers - 1];
                    }

                    this.Send(this.Servers[i], new ChainReplicationServer.PredSucc(pred, succ));
                }

                this.Clients.Add(this.CreateMachine(
                    typeof(Client),
                    new Client.Config(0, this.Servers[0], this.Servers[this.NumOfServers - 1], 1)));

                this.Clients.Add(this.CreateMachine(
                    typeof(Client),
                    new Client.Config(1, this.Servers[0], this.Servers[this.NumOfServers - 1], 100)));

                this.ChainReplicationMaster = this.CreateMachine(
                    typeof(ChainReplicationMaster),
                    new ChainReplicationMaster.Config(this.Servers, this.Clients));

                this.Raise(new Halt());
            }
        }

        private class FailureDetector : Machine
        {
            internal class Config : Event
            {
                public MachineId Master;
                public List<MachineId> Servers;

                public Config(MachineId master, List<MachineId> servers)
                    : base()
                {
                    this.Master = master;
                    this.Servers = servers;
                }
            }

            internal class FailureDetected : Event
            {
                public MachineId Server;

                public FailureDetected(MachineId server)
                    : base()
                {
                    this.Server = server;
                }
            }

            internal class FailureCorrected : Event
            {
                public List<MachineId> Servers;

                public FailureCorrected(List<MachineId> servers)
                    : base()
                {
                    this.Servers = servers;
                }
            }

            internal class Ping : Event
            {
                public MachineId Target;

                public Ping(MachineId target)
                    : base()
                {
                    this.Target = target;
                }
            }

            internal class Pong : Event
            {
            }

            private class InjectFailure : Event
            {
            }

            private class Local : Event
            {
            }

            private MachineId Master;
            private List<MachineId> Servers;

            private int CheckNodeIdx;
            private int Failures;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventGotoState(typeof(Local), typeof(StartMonitoring))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Master = (this.ReceivedEvent as Config).Master;
                this.Servers = (this.ReceivedEvent as Config).Servers;

                this.CheckNodeIdx = 0;
                this.Failures = 100;

                this.Raise(new Local());
            }

            [OnEntry(nameof(StartMonitoringOnEntry))]
            [OnEventGotoState(typeof(Pong), typeof(StartMonitoring), nameof(HandlePong))]
            [OnEventGotoState(typeof(InjectFailure), typeof(HandleFailure))]
            private class StartMonitoring : MachineState
            {
            }

            private void StartMonitoringOnEntry()
            {
                if (this.Failures < 1)
                {
                    this.Raise(new Halt());
                }
                else
                {
                    this.Send(this.Servers[this.CheckNodeIdx], new Ping(this.Id));

                    if (this.Servers.Count > 1)
                    {
                        if (this.Random())
                        {
                            this.Send(this.Id, new InjectFailure());
                        }
                        else
                        {
                            this.Send(this.Id, new Pong());
                        }
                    }
                    else
                    {
                        this.Send(this.Id, new Pong());
                    }

                    this.Failures--;
                }
            }

            private void HandlePong()
            {
                this.CheckNodeIdx++;
                if (this.CheckNodeIdx == this.Servers.Count)
                {
                    this.CheckNodeIdx = 0;
                }
            }

            [OnEntry(nameof(HandleFailureOnEntry))]
            [OnEventGotoState(typeof(FailureCorrected), typeof(StartMonitoring), nameof(ProcessFailureCorrected))]
            [IgnoreEvents(typeof(Pong), typeof(InjectFailure))]
            private class HandleFailure : MachineState
            {
            }

            private void HandleFailureOnEntry()
            {
                this.Send(this.Master, new FailureDetected(this.Servers[this.CheckNodeIdx]));
            }

            private void ProcessFailureCorrected()
            {
                this.CheckNodeIdx = 0;
                this.Servers = (this.ReceivedEvent as FailureCorrected).Servers;
            }
        }

        private class ChainReplicationMaster : Machine
        {
            internal class Config : Event
            {
                public List<MachineId> Servers;
                public List<MachineId> Clients;

                public Config(List<MachineId> servers, List<MachineId> clients)
                    : base()
                {
                    this.Servers = servers;
                    this.Clients = clients;
                }
            }

            internal class BecomeHead : Event
            {
                public MachineId Target;

                public BecomeHead(MachineId target)
                    : base()
                {
                    this.Target = target;
                }
            }

            internal class BecomeTail : Event
            {
                public MachineId Target;

                public BecomeTail(MachineId target)
                    : base()
                {
                    this.Target = target;
                }
            }

            internal class Success : Event
            {
            }

            internal class HeadChanged : Event
            {
            }

            internal class TailChanged : Event
            {
            }

            private class HeadFailed : Event
            {
            }

            private class TailFailed : Event
            {
            }

            private class ServerFailed : Event
            {
            }

            private class FixSuccessor : Event
            {
            }

            private class FixPredecessor : Event
            {
            }

            private class Local : Event
            {
            }

            private class Done : Event
            {
            }

            private List<MachineId> Servers;
            private List<MachineId> Clients;

            private MachineId FailureDetector;

            private MachineId Head;
            private MachineId Tail;

            private int FaultyNodeIndex;
            private int LastUpdateReceivedSucc;
            private int LastAckSent;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventGotoState(typeof(Local), typeof(WaitForFailure))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Servers = (this.ReceivedEvent as Config).Servers;
                this.Clients = (this.ReceivedEvent as Config).Clients;

                this.FailureDetector = this.CreateMachine(
                    typeof(FailureDetector),
                    new FailureDetector.Config(this.Id, this.Servers));

                this.Head = this.Servers[0];
                this.Tail = this.Servers[this.Servers.Count - 1];

                this.Raise(new Local());
            }

            [OnEventGotoState(typeof(HeadFailed), typeof(CorrectHeadFailure))]
            [OnEventGotoState(typeof(TailFailed), typeof(CorrectTailFailure))]
            [OnEventGotoState(typeof(ServerFailed), typeof(CorrectServerFailure))]
            [OnEventDoAction(typeof(FailureDetector.FailureDetected), nameof(CheckWhichNodeFailed))]
            private class WaitForFailure : MachineState
            {
            }

            private void CheckWhichNodeFailed()
            {
                this.Assert(this.Servers.Count > 1, "All nodes have failed.");

                var failedServer = (this.ReceivedEvent as FailureDetector.FailureDetected).Server;

                if (this.Head.Equals(failedServer))
                {
                    this.Raise(new HeadFailed());
                }
                else if (this.Tail.Equals(failedServer))
                {
                    this.Raise(new TailFailed());
                }
                else
                {
                    for (int i = 0; i < this.Servers.Count - 1; i++)
                    {
                        if (this.Servers[i].Equals(failedServer))
                        {
                            this.FaultyNodeIndex = i;
                        }
                    }

                    this.Raise(new ServerFailed());
                }
            }

            [OnEntry(nameof(CorrectHeadFailureOnEntry))]
            [OnEventGotoState(typeof(Done), typeof(WaitForFailure), nameof(UpdateFailureDetector))]
            [OnEventDoAction(typeof(HeadChanged), nameof(UpdateClients))]
            private class CorrectHeadFailure : MachineState
            {
            }

            private void CorrectHeadFailureOnEntry()
            {
                this.Servers.RemoveAt(0);

                this.Monitor<InvariantMonitor>(
                    new InvariantMonitor.UpdateServers(this.Servers));
                this.Monitor<ServerResponseSeqMonitor>(
                    new ServerResponseSeqMonitor.UpdateServers(this.Servers));

                this.Head = this.Servers[0];

                this.Send(this.Head, new BecomeHead(this.Id));
            }

            private void UpdateClients()
            {
                for (int i = 0; i < this.Clients.Count; i++)
                {
                    this.Send(this.Clients[i], new Client.UpdateHeadTail(this.Head, this.Tail));
                }

                this.Raise(new Done());
            }

            private void UpdateFailureDetector()
            {
                this.Send(this.FailureDetector, new FailureDetector.FailureCorrected(this.Servers));
            }

            [OnEntry(nameof(CorrectTailFailureOnEntry))]
            [OnEventGotoState(typeof(Done), typeof(WaitForFailure), nameof(UpdateFailureDetector))]
            [OnEventDoAction(typeof(TailChanged), nameof(UpdateClients))]
            private class CorrectTailFailure : MachineState
            {
            }

            private void CorrectTailFailureOnEntry()
            {
                this.Servers.RemoveAt(this.Servers.Count - 1);

                this.Monitor<InvariantMonitor>(
                    new InvariantMonitor.UpdateServers(this.Servers));
                this.Monitor<ServerResponseSeqMonitor>(
                    new ServerResponseSeqMonitor.UpdateServers(this.Servers));

                this.Tail = this.Servers[this.Servers.Count - 1];

                this.Send(this.Tail, new BecomeTail(this.Id));
            }

            [OnEntry(nameof(CorrectServerFailureOnEntry))]
            [OnEventGotoState(typeof(Done), typeof(WaitForFailure), nameof(UpdateFailureDetector))]
            [OnEventDoAction(typeof(FixSuccessor), nameof(UpdateClients))]
            [OnEventDoAction(typeof(FixPredecessor), nameof(ProcessFixPredecessor))]
            [OnEventDoAction(typeof(ChainReplicationServer.NewSuccInfo), nameof(SetLastUpdate))]
            [OnEventDoAction(typeof(Success), nameof(ProcessSuccess))]
            private class CorrectServerFailure : MachineState
            {
            }

            private void CorrectServerFailureOnEntry()
            {
                this.Servers.RemoveAt(this.FaultyNodeIndex);

                this.Monitor<InvariantMonitor>(
                    new InvariantMonitor.UpdateServers(this.Servers));
                this.Monitor<ServerResponseSeqMonitor>(
                    new ServerResponseSeqMonitor.UpdateServers(this.Servers));

                this.Raise(new FixSuccessor());
            }

            private void ProcessFixSuccessor()
            {
                this.Send(this.Servers[this.FaultyNodeIndex], new ChainReplicationServer.NewPredecessor(
                    this.Id, this.Servers[this.FaultyNodeIndex - 1]));
            }

            private void ProcessFixPredecessor()
            {
                this.Send(this.Servers[this.FaultyNodeIndex - 1], new ChainReplicationServer.NewSuccessor(
                    this.Id, this.Servers[this.FaultyNodeIndex], this.LastAckSent, this.LastUpdateReceivedSucc));
            }

            private void SetLastUpdate()
            {
                this.LastUpdateReceivedSucc = (this.ReceivedEvent as
                    ChainReplicationServer.NewSuccInfo).LastUpdateReceivedSucc;
                this.LastAckSent = (this.ReceivedEvent as
                    ChainReplicationServer.NewSuccInfo).LastAckSent;
                this.Raise(new FixPredecessor());
            }

            private void ProcessSuccess()
            {
                this.Raise(new Done());
            }
        }

        private class ChainReplicationServer : Machine
        {
            internal class Config : Event
            {
                public int Id;
                public bool IsHead;
                public bool IsTail;

                public Config(int id, bool isHead, bool isTail)
                    : base()
                {
                    this.Id = id;
                    this.IsHead = isHead;
                    this.IsTail = isTail;
                }
            }

            internal class PredSucc : Event
            {
                public MachineId Predecessor;
                public MachineId Successor;

                public PredSucc(MachineId pred, MachineId succ)
                    : base()
                {
                    this.Predecessor = pred;
                    this.Successor = succ;
                }
            }

            internal class ForwardUpdate : Event
            {
                public MachineId Predecessor;
                public int NextSeqId;
                public MachineId Client;
                public int Key;
                public int Value;

                public ForwardUpdate(MachineId pred, int nextSeqId, MachineId client, int key, int val)
                    : base()
                {
                    this.Predecessor = pred;
                    this.NextSeqId = nextSeqId;
                    this.Client = client;
                    this.Key = key;
                    this.Value = val;
                }
            }

            internal class BackwardAck : Event
            {
                public int NextSeqId;

                public BackwardAck(int nextSeqId)
                    : base()
                {
                    this.NextSeqId = nextSeqId;
                }
            }

            internal class NewPredecessor : Event
            {
                public MachineId Master;
                public MachineId Predecessor;

                public NewPredecessor(MachineId master, MachineId pred)
                    : base()
                {
                    this.Master = master;
                    this.Predecessor = pred;
                }
            }

            internal class NewSuccessor : Event
            {
                public MachineId Master;
                public MachineId Successor;
                public int LastUpdateReceivedSucc;
                public int LastAckSent;

                public NewSuccessor(MachineId master, MachineId succ,
                    int lastUpdateReceivedSucc, int lastAckSent)
                    : base()
                {
                    this.Master = master;
                    this.Successor = succ;
                    this.LastUpdateReceivedSucc = lastUpdateReceivedSucc;
                    this.LastAckSent = lastAckSent;
                }
            }

            internal class NewSuccInfo : Event
            {
                public int LastUpdateReceivedSucc;
                public int LastAckSent;

                public NewSuccInfo(int lastUpdateReceivedSucc, int lastAckSent)
                    : base()
                {
                    this.LastUpdateReceivedSucc = lastUpdateReceivedSucc;
                    this.LastAckSent = lastAckSent;
                }
            }

            internal class ResponseToQuery : Event
            {
                public int Value;

                public ResponseToQuery(int val)
                    : base()
                {
                    this.Value = val;
                }
            }

            internal class ResponseToUpdate : Event
            {
            }

            private class Local : Event
            {
            }

            private int ServerId;
            private bool IsHead;
            private bool IsTail;

            private MachineId Predecessor;
            private MachineId Successor;

            private Dictionary<int, int> KeyValueStore;
            private List<int> History;
            private List<SentLog> SentHistory;

            private int NextSeqId;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventGotoState(typeof(Local), typeof(WaitForRequest))]
            [OnEventDoAction(typeof(PredSucc), nameof(SetupPredSucc))]
            [DeferEvents(typeof(Client.Update), typeof(Client.Query),
                typeof(BackwardAck), typeof(ForwardUpdate))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.ServerId = (this.ReceivedEvent as Config).Id;
                this.IsHead = (this.ReceivedEvent as Config).IsHead;
                this.IsTail = (this.ReceivedEvent as Config).IsTail;

                this.KeyValueStore = new Dictionary<int, int>();
                this.History = new List<int>();
                this.SentHistory = new List<SentLog>();

                this.NextSeqId = 0;
            }

            private void SetupPredSucc()
            {
                this.Predecessor = (this.ReceivedEvent as PredSucc).Predecessor;
                this.Successor = (this.ReceivedEvent as PredSucc).Successor;
                this.Raise(new Local());
            }

            [OnEventGotoState(typeof(Client.Update), typeof(ProcessUpdate), nameof(ProcessUpdateAction))]
            [OnEventGotoState(typeof(ForwardUpdate), typeof(ProcessFwdUpdate))]
            [OnEventGotoState(typeof(BackwardAck), typeof(ProcessBckAck))]
            [OnEventDoAction(typeof(Client.Query), nameof(ProcessQueryAction))]
            [OnEventDoAction(typeof(NewPredecessor), nameof(UpdatePredecessor))]
            [OnEventDoAction(typeof(NewSuccessor), nameof(UpdateSuccessor))]
            [OnEventDoAction(typeof(ChainReplicationMaster.BecomeHead), nameof(ProcessBecomeHead))]
            [OnEventDoAction(typeof(ChainReplicationMaster.BecomeTail), nameof(ProcessBecomeTail))]
            [OnEventDoAction(typeof(FailureDetector.Ping), nameof(SendPong))]
            private class WaitForRequest : MachineState
            {
            }

            private void ProcessUpdateAction()
            {
                this.NextSeqId++;
                this.Assert(this.IsHead, "Server {0} is not head", this.ServerId);
            }

            private void ProcessQueryAction()
            {
                var client = (this.ReceivedEvent as Client.Query).Client;
                var key = (this.ReceivedEvent as Client.Query).Key;

                this.Assert(this.IsTail, "Server {0} is not tail", this.Id);

                if (this.KeyValueStore.ContainsKey(key))
                {
                    this.Monitor<ServerResponseSeqMonitor>(new ServerResponseSeqMonitor.ResponseToQuery(
                        this.Id, key, this.KeyValueStore[key]));

                    this.Send(client, new ResponseToQuery(this.KeyValueStore[key]));
                }
                else
                {
                    this.Send(client, new ResponseToQuery(-1));
                }
            }

            private void ProcessBecomeHead()
            {
                this.IsHead = true;
                this.Predecessor = this.Id;

                var target = (this.ReceivedEvent as ChainReplicationMaster.BecomeHead).Target;
                this.Send(target, new ChainReplicationMaster.HeadChanged());
            }

            private void ProcessBecomeTail()
            {
                this.IsTail = true;
                this.Successor = this.Id;

                for (int i = 0; i < this.SentHistory.Count; i++)
                {
                    this.Monitor<ServerResponseSeqMonitor>(new ServerResponseSeqMonitor.ResponseToUpdate(
                        this.Id, this.SentHistory[i].Key, this.SentHistory[i].Value));

                    this.Send(this.SentHistory[i].Client, new ResponseToUpdate());
                    this.Send(this.Predecessor, new BackwardAck(this.SentHistory[i].NextSeqId));
                }

                var target = (this.ReceivedEvent as ChainReplicationMaster.BecomeTail).Target;
                this.Send(target, new ChainReplicationMaster.TailChanged());
            }

            private void SendPong()
            {
                var target = (this.ReceivedEvent as FailureDetector.Ping).Target;
                this.Send(target, new FailureDetector.Pong());
            }

            private void UpdatePredecessor()
            {
                var master = (this.ReceivedEvent as NewPredecessor).Master;
                this.Predecessor = (this.ReceivedEvent as NewPredecessor).Predecessor;

                if (this.History.Count > 0)
                {
                    if (this.SentHistory.Count > 0)
                    {
                        this.Send(master, new NewSuccInfo(
                            this.History[this.History.Count - 1],
                            this.SentHistory[0].NextSeqId));
                    }
                    else
                    {
                        this.Send(master, new NewSuccInfo(
                            this.History[this.History.Count - 1],
                            this.History[this.History.Count - 1]));
                    }
                }
            }

            private void UpdateSuccessor()
            {
                var master = (this.ReceivedEvent as NewSuccessor).Master;
                this.Successor = (this.ReceivedEvent as NewSuccessor).Successor;
                var lastUpdateReceivedSucc = (this.ReceivedEvent as NewSuccessor).LastUpdateReceivedSucc;
                var lastAckSent = (this.ReceivedEvent as NewSuccessor).LastAckSent;

                if (this.SentHistory.Count > 0)
                {
                    for (int i = 0; i < this.SentHistory.Count; i++)
                    {
                        if (this.SentHistory[i].NextSeqId > lastUpdateReceivedSucc)
                        {
                            this.Send(this.Successor, new ForwardUpdate(this.Id, this.SentHistory[i].NextSeqId,
                                this.SentHistory[i].Client, this.SentHistory[i].Key, this.SentHistory[i].Value));
                        }
                    }

                    int tempIndex = -1;
                    for (int i = this.SentHistory.Count - 1; i >= 0; i--)
                    {
                        if (this.SentHistory[i].NextSeqId == lastAckSent)
                        {
                            tempIndex = i;
                        }
                    }

                    for (int i = 0; i < tempIndex; i++)
                    {
                        this.Send(this.Predecessor, new BackwardAck(this.SentHistory[0].NextSeqId));
                        this.SentHistory.RemoveAt(0);
                    }
                }

                this.Send(master, new ChainReplicationMaster.Success());
            }

            [OnEntry(nameof(ProcessUpdateOnEntry))]
            [OnEventGotoState(typeof(Local), typeof(WaitForRequest))]
            private class ProcessUpdate : MachineState
            {
            }

            private void ProcessUpdateOnEntry()
            {
                var client = (this.ReceivedEvent as Client.Update).Client;
                var key = (this.ReceivedEvent as Client.Update).Key;
                var value = (this.ReceivedEvent as Client.Update).Value;

                if (this.KeyValueStore.ContainsKey(key))
                {
                    this.KeyValueStore[key] = value;
                }
                else
                {
                    this.KeyValueStore.Add(key, value);
                }

                this.History.Add(this.NextSeqId);

                this.Monitor<InvariantMonitor>(
                    new InvariantMonitor.HistoryUpdate(this.Id, new List<int>(this.History)));

                this.SentHistory.Add(new SentLog(this.NextSeqId, client, key, value));
                this.Monitor<InvariantMonitor>(
                    new InvariantMonitor.SentUpdate(this.Id, new List<SentLog>(this.SentHistory)));

                this.Send(this.Successor, new ForwardUpdate(this.Id, this.NextSeqId, client, key, value));

                this.Raise(new Local());
            }

            [OnEntry(nameof(ProcessFwdUpdateOnEntry))]
            [OnEventGotoState(typeof(Local), typeof(WaitForRequest))]
            private class ProcessFwdUpdate : MachineState
            {
            }

            private void ProcessFwdUpdateOnEntry()
            {
                var pred = (this.ReceivedEvent as ForwardUpdate).Predecessor;
                var nextSeqId = (this.ReceivedEvent as ForwardUpdate).NextSeqId;
                var client = (this.ReceivedEvent as ForwardUpdate).Client;
                var key = (this.ReceivedEvent as ForwardUpdate).Key;
                var value = (this.ReceivedEvent as ForwardUpdate).Value;

                if (pred.Equals(this.Predecessor))
                {
                    this.NextSeqId = nextSeqId;

                    if (this.KeyValueStore.ContainsKey(key))
                    {
                        this.KeyValueStore[key] = value;
                    }
                    else
                    {
                        this.KeyValueStore.Add(key, value);
                    }

                    if (!this.IsTail)
                    {
                        this.History.Add(nextSeqId);

                        this.Monitor<InvariantMonitor>(
                            new InvariantMonitor.HistoryUpdate(this.Id, new List<int>(this.History)));

                        this.SentHistory.Add(new SentLog(this.NextSeqId, client, key, value));
                        this.Monitor<InvariantMonitor>(
                            new InvariantMonitor.SentUpdate(this.Id, new List<SentLog>(this.SentHistory)));

                        this.Send(this.Successor, new ForwardUpdate(this.Id, this.NextSeqId, client, key, value));
                    }
                    else
                    {
                        if (!this.IsHead)
                        {
                            this.History.Add(nextSeqId);
                        }

                        this.Monitor<ServerResponseSeqMonitor>(new ServerResponseSeqMonitor.ResponseToUpdate(
                            this.Id, key, value));

                        this.Send(client, new ResponseToUpdate());
                        this.Send(this.Predecessor, new BackwardAck(nextSeqId));
                    }
                }

                this.Raise(new Local());
            }

            [OnEntry(nameof(ProcessBckAckOnEntry))]
            [OnEventGotoState(typeof(Local), typeof(WaitForRequest))]
            private class ProcessBckAck : MachineState
            {
            }

            private void ProcessBckAckOnEntry()
            {
                var nextSeqId = (this.ReceivedEvent as BackwardAck).NextSeqId;

                this.RemoveItemFromSent(nextSeqId);

                if (!this.IsHead)
                {
                    this.Send(this.Predecessor, new BackwardAck(nextSeqId));
                }

                this.Raise(new Local());
            }

            private void RemoveItemFromSent(int seqId)
            {
                int removeIdx = -1;

                for (int i = this.SentHistory.Count - 1; i >= 0; i--)
                {
                    if (seqId == this.SentHistory[i].NextSeqId)
                    {
                        removeIdx = i;
                    }
                }

                if (removeIdx != -1)
                {
                    this.SentHistory.RemoveAt(removeIdx);
                }
            }
        }

        private class Client : Machine
        {
            internal class Config : Event
            {
                public int Id;
                public MachineId HeadNode;
                public MachineId TailNode;
                public int Value;

                public Config(int id, MachineId head, MachineId tail, int val)
                    : base()
                {
                    this.Id = id;
                    this.HeadNode = head;
                    this.TailNode = tail;
                    this.Value = val;
                }
            }

            internal class UpdateHeadTail : Event
            {
                public MachineId Head;
                public MachineId Tail;

                public UpdateHeadTail(MachineId head, MachineId tail)
                    : base()
                {
                    this.Head = head;
                    this.Tail = tail;
                }
            }

            internal class Update : Event
            {
                public MachineId Client;
                public int Key;
                public int Value;

                public Update(MachineId client, int key, int value)
                    : base()
                {
                    this.Client = client;
                    this.Key = key;
                    this.Value = value;
                }
            }

            internal class Query : Event
            {
                public MachineId Client;
                public int Key;

                public Query(MachineId client, int key)
                    : base()
                {
                    this.Client = client;
                    this.Key = key;
                }
            }

            private class Local : Event
            {
            }

            private class Done : Event
            {
            }

            private int ClientId;

            private MachineId HeadNode;
            private MachineId TailNode;

            private int StartIn;
            private int Next;

            private Dictionary<int, int> KeyValueStore;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventGotoState(typeof(Local), typeof(PumpUpdateRequests))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.ClientId = (this.ReceivedEvent as Config).Id;

                this.HeadNode = (this.ReceivedEvent as Config).HeadNode;
                this.TailNode = (this.ReceivedEvent as Config).TailNode;

                this.StartIn = (this.ReceivedEvent as Config).Value;
                this.Next = 1;

                this.KeyValueStore = new Dictionary<int, int>();
                this.KeyValueStore.Add(1 * this.StartIn, 100);
                this.KeyValueStore.Add(2 * this.StartIn, 200);
                this.KeyValueStore.Add(3 * this.StartIn, 300);
                this.KeyValueStore.Add(4 * this.StartIn, 400);

                this.Raise(new Local());
            }

            [OnEntry(nameof(PumpUpdateRequestsOnEntry))]
            [OnEventGotoState(typeof(Local), typeof(PumpUpdateRequests), nameof(PumpRequestsLocalAction))]
            [OnEventGotoState(typeof(Done), typeof(PumpQueryRequests), nameof(PumpRequestsDoneAction))]
            [IgnoreEvents(typeof(ChainReplicationServer.ResponseToUpdate), typeof(ChainReplicationServer.ResponseToQuery))]
            private class PumpUpdateRequests : MachineState
            {
            }

            private void PumpUpdateRequestsOnEntry()
            {
                this.Send(this.HeadNode, new Update(this.Id, this.Next * this.StartIn,
                    this.KeyValueStore[this.Next * this.StartIn]));

                if (this.Next >= 3)
                {
                    this.Raise(new Done());
                }
                else
                {
                    this.Raise(new Local());
                }
            }

            [OnEntry(nameof(PumpQueryRequestsOnEntry))]
            [OnEventGotoState(typeof(Local), typeof(PumpQueryRequests), nameof(PumpRequestsLocalAction))]
            [IgnoreEvents(typeof(ChainReplicationServer.ResponseToUpdate), typeof(ChainReplicationServer.ResponseToQuery))]
            private class PumpQueryRequests : MachineState
            {
            }

            private void PumpQueryRequestsOnEntry()
            {
                this.Send(this.TailNode, new Query(this.Id, this.Next * this.StartIn));

                if (this.Next >= 3)
                {
                    this.Raise(new Halt());
                }
                else
                {
                    this.Raise(new Local());
                }
            }

            private void PumpRequestsLocalAction()
            {
                this.Next++;
            }

            private void PumpRequestsDoneAction()
            {
                this.Next = 1;
            }
        }

        private class InvariantMonitor : Monitor
        {
            internal class Config : Event
            {
                public List<MachineId> Servers;

                public Config(List<MachineId> servers)
                    : base()
                {
                    this.Servers = servers;
                }
            }

            internal class UpdateServers : Event
            {
                public List<MachineId> Servers;

                public UpdateServers(List<MachineId> servers)
                    : base()
                {
                    this.Servers = servers;
                }
            }

            internal class HistoryUpdate : Event
            {
                public MachineId Server;
                public List<int> History;

                public HistoryUpdate(MachineId server, List<int> history)
                    : base()
                {
                    this.Server = server;
                    this.History = history;
                }
            }

            internal class SentUpdate : Event
            {
                public MachineId Server;
                public List<SentLog> SentHistory;

                public SentUpdate(MachineId server, List<SentLog> sentHistory)
                    : base()
                {
                    this.Server = server;
                    this.SentHistory = sentHistory;
                }
            }

            private class Local : Event
            {
            }

            private List<MachineId> Servers;

            private Dictionary<MachineId, List<int>> History;
            private Dictionary<MachineId, List<int>> SentHistory;
            private List<int> TempSeq;

            private MachineId Next;
            private MachineId Prev;

            [Start]
            [OnEventGotoState(typeof(Local), typeof(WaitForUpdateMessage))]
            [OnEventDoAction(typeof(Config), nameof(Configure))]
            private class Init : MonitorState
            {
            }

            private void Configure()
            {
                this.Servers = (this.ReceivedEvent as Config).Servers;
                this.History = new Dictionary<MachineId, List<int>>();
                this.SentHistory = new Dictionary<MachineId, List<int>>();
                this.TempSeq = new List<int>();

                this.Raise(new Local());
            }

            [OnEventDoAction(typeof(HistoryUpdate), nameof(CheckUpdatePropagationInvariant))]
            [OnEventDoAction(typeof(SentUpdate), nameof(CheckInprocessRequestsInvariant))]
            [OnEventDoAction(typeof(UpdateServers), nameof(ProcessUpdateServers))]
            private class WaitForUpdateMessage : MonitorState
            {
            }

            private void CheckUpdatePropagationInvariant()
            {
                var server = (this.ReceivedEvent as HistoryUpdate).Server;
                var history = (this.ReceivedEvent as HistoryUpdate).History;

                this.IsSorted(history);

                if (this.History.ContainsKey(server))
                {
                    this.History[server] = history;
                }
                else
                {
                    this.History.Add(server, history);
                }

                // HIST(i+1) <= HIST(i)
                this.GetNext(server);
                if (this.Next != null && this.History.ContainsKey(this.Next))
                {
                    this.CheckLessOrEqualThan(this.History[this.Next], this.History[server]);
                }

                // HIST(i) <= HIST(i-1)
                this.GetPrev(server);
                if (this.Prev != null && this.History.ContainsKey(this.Prev))
                {
                    this.CheckLessOrEqualThan(this.History[server], this.History[this.Prev]);
                }
            }

            private void CheckInprocessRequestsInvariant()
            {
                this.ClearTempSeq();

                var server = (this.ReceivedEvent as SentUpdate).Server;
                var sentHistory = (this.ReceivedEvent as SentUpdate).SentHistory;

                this.ExtractSeqId(sentHistory);

                if (this.SentHistory.ContainsKey(server))
                {
                    this.SentHistory[server] = this.TempSeq;
                }
                else
                {
                    this.SentHistory.Add(server, this.TempSeq);
                }

                this.ClearTempSeq();

                // HIST(i) == HIST(i+1) + SENT(i)
                this.GetNext(server);
                if (this.Next != null && this.History.ContainsKey(this.Next))
                {
                    this.MergeSeq(this.History[this.Next], this.SentHistory[server]);
                    this.CheckEqual(this.History[server], this.TempSeq);
                }

                this.ClearTempSeq();

                // HIST(i-1) == HIST(i) + SENT(i-1)
                this.GetPrev(server);
                if (this.Prev != null && this.History.ContainsKey(this.Prev))
                {
                    this.MergeSeq(this.History[server], this.SentHistory[this.Prev]);
                    this.CheckEqual(this.History[this.Prev], this.TempSeq);
                }

                this.ClearTempSeq();
            }

            private void GetNext(MachineId curr)
            {
                this.Next = null;

                for (int i = 1; i < this.Servers.Count; i++)
                {
                    if (this.Servers[i - 1].Equals(curr))
                    {
                        this.Next = this.Servers[i];
                    }
                }
            }

            private void GetPrev(MachineId curr)
            {
                this.Prev = null;

                for (int i = 1; i < this.Servers.Count; i++)
                {
                    if (this.Servers[i].Equals(curr))
                    {
                        this.Prev = this.Servers[i - 1];
                    }
                }
            }

            private void ExtractSeqId(List<SentLog> seq)
            {
                this.ClearTempSeq();

                for (int i = seq.Count - 1; i >= 0; i--)
                {
                    if (this.TempSeq.Count > 0)
                    {
                        this.TempSeq.Insert(0, seq[i].NextSeqId);
                    }
                    else
                    {
                        this.TempSeq.Add(seq[i].NextSeqId);
                    }
                }

                this.IsSorted(this.TempSeq);
            }

            private void MergeSeq(List<int> seq1, List<int> seq2)
            {
                this.ClearTempSeq();
                this.IsSorted(seq1);

                if (seq1.Count == 0)
                {
                    this.TempSeq = seq2;
                }
                else if (seq2.Count == 0)
                {
                    this.TempSeq = seq1;
                }
                else
                {
                    for (int i = 0; i < seq1.Count; i++)
                    {
                        if (seq1[i] < seq2[0])
                        {
                            this.TempSeq.Add(seq1[i]);
                        }
                    }

                    for (int i = 0; i < seq2.Count; i++)
                    {
                        this.TempSeq.Add(seq2[i]);
                    }
                }

                this.IsSorted(this.TempSeq);
            }

            private void IsSorted(List<int> seq)
            {
                for (int i = 0; i < seq.Count - 1; i++)
                {
                    this.Assert(seq[i] < seq[i + 1], "Sequence is not sorted.");
                }
            }

            private void CheckLessOrEqualThan(List<int> seq1, List<int> seq2)
            {
                this.IsSorted(seq1);
                this.IsSorted(seq2);

                for (int i = 0; i < seq1.Count; i++)
                {
                    if ((i == seq1.Count) || (i == seq2.Count))
                    {
                        break;
                    }

                    this.Assert(seq1[i] <= seq2[i], "{0} not less or equal than {1}.", seq1[i], seq2[i]);
                }
            }

            private void CheckEqual(List<int> seq1, List<int> seq2)
            {
                this.IsSorted(seq1);
                this.IsSorted(seq2);

                for (int i = 0; i < seq1.Count; i++)
                {
                    if ((i == seq1.Count) || (i == seq2.Count))
                    {
                        break;
                    }

                    this.Assert(seq1[i] == seq2[i], "{0} not equal with {1}.", seq1[i], seq2[i]);
                }
            }

            private void ClearTempSeq()
            {
                this.Assert(this.TempSeq.Count <= 6, "Temp sequence has more than 6 elements.");
                this.TempSeq.Clear();
                this.Assert(this.TempSeq.Count == 0, "Temp sequence is not cleared.");
            }

            private void ProcessUpdateServers()
            {
                this.Servers = (this.ReceivedEvent as UpdateServers).Servers;
            }
        }

        private class ServerResponseSeqMonitor : Monitor
        {
            internal class Config : Event
            {
                public List<MachineId> Servers;

                public Config(List<MachineId> servers)
                    : base()
                {
                    this.Servers = servers;
                }
            }

            internal class UpdateServers : Event
            {
                public List<MachineId> Servers;

                public UpdateServers(List<MachineId> servers)
                    : base()
                {
                    this.Servers = servers;
                }
            }

            internal class ResponseToUpdate : Event
            {
                public MachineId Tail;
                public int Key;
                public int Value;

                public ResponseToUpdate(MachineId tail, int key, int val)
                    : base()
                {
                    this.Tail = tail;
                    this.Key = key;
                    this.Value = val;
                }
            }

            internal class ResponseToQuery : Event
            {
                public MachineId Tail;
                public int Key;
                public int Value;

                public ResponseToQuery(MachineId tail, int key, int val)
                    : base()
                {
                    this.Tail = tail;
                    this.Key = key;
                    this.Value = val;
                }
            }

            private class Local : Event
            {
            }

            private List<MachineId> Servers;
            private Dictionary<int, int> LastUpdateResponse;

            [Start]
            [OnEventGotoState(typeof(Local), typeof(Wait))]
            [OnEventDoAction(typeof(Config), nameof(Configure))]
            private class Init : MonitorState
            {
            }

            private void Configure()
            {
                this.Servers = (this.ReceivedEvent as Config).Servers;
                this.LastUpdateResponse = new Dictionary<int, int>();
                this.Raise(new Local());
            }

            [OnEventDoAction(typeof(ResponseToUpdate), nameof(ResponseToUpdateAction))]
            [OnEventDoAction(typeof(ResponseToQuery), nameof(ResponseToQueryAction))]
            [OnEventDoAction(typeof(UpdateServers), nameof(ProcessUpdateServers))]
            private class Wait : MonitorState
            {
            }

            private void ResponseToUpdateAction()
            {
                var tail = (this.ReceivedEvent as ResponseToUpdate).Tail;
                var key = (this.ReceivedEvent as ResponseToUpdate).Key;
                var value = (this.ReceivedEvent as ResponseToUpdate).Value;

                if (this.Servers.Contains(tail))
                {
                    if (this.LastUpdateResponse.ContainsKey(key))
                    {
                        this.LastUpdateResponse[key] = value;
                    }
                    else
                    {
                        this.LastUpdateResponse.Add(key, value);
                    }
                }
            }

            private void ResponseToQueryAction()
            {
                var tail = (this.ReceivedEvent as ResponseToQuery).Tail;
                var key = (this.ReceivedEvent as ResponseToQuery).Key;
                var value = (this.ReceivedEvent as ResponseToQuery).Value;

                if (this.Servers.Contains(tail))
                {
                    this.Assert(value == this.LastUpdateResponse[key], "Value {0} is not " +
                        "equal to {1}", value, this.LastUpdateResponse[key]);
                }
            }

            private void ProcessUpdateServers()
            {
                this.Servers = (this.ReceivedEvent as UpdateServers).Servers;
            }
        }

        [Theory(Timeout = 5000)]
        // [ClassData(typeof(SeedGenerator))]
        [InlineData(90)]
        public void TestSequenceNotSortedInChainReplicationProtocol(int seed)
        {
            var configuration = GetConfiguration();
            configuration.SchedulingStrategy = Utilities.SchedulingStrategy.FairPCT;
            configuration.PrioritySwitchBound = 1;
            configuration.MaxSchedulingSteps = 100;
            configuration.RandomSchedulingSeed = seed;
            configuration.SchedulingIterations = 2;

            this.TestWithError(r =>
            {
                r.RegisterMonitor(typeof(InvariantMonitor));
                r.RegisterMonitor(typeof(ServerResponseSeqMonitor));
                r.CreateMachine(typeof(Environment));
            },
            configuration: configuration,
            expectedError: "Sequence is not sorted.",
            replay: true);
        }
    }
}
