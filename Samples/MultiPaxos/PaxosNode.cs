using System;
using System.Collections.Generic;
using Microsoft.Coyote;

namespace MultiPaxos
{
    internal class PaxosNode : Machine
    {
        internal class Config : Event
        {
            public int MyRank;

            public Config(int id)
            {
                this.MyRank = id;
            }
        }

        internal class AllNodes : Event
        {
            public List<MachineId> Nodes;

            public AllNodes(List<MachineId> nodes)
            {
                this.Nodes = nodes;
            }
        }

        internal class Prepare : Event
        {
            public MachineId Node;
            public int NextSlotForProposer;
            public Tuple<int, int> NextProposal;
            public int MyRank;

            public Prepare(MachineId id, int nextSlot, Tuple<int, int> nextProposal, int myRank)
            {
                this.Node = id;
                this.NextSlotForProposer = nextSlot;
                this.NextProposal = nextProposal;
                this.MyRank = myRank;
            }
        }

        internal class Accepted : Event
        {
            public int Slot;
            public int Round;
            public int Server;
            public int Value;

            public Accepted(int slot, int round, int server, int value)
            {
                this.Slot = slot;
                this.Round = round;
                this.Server = server;
                this.Value = value;
            }
        }

        internal class Chosen : Event
        {
            public int Slot;
            public int Round;
            public int Server;
            public int Value;

            public Chosen(int slot, int round, int server, int value)
            {
                this.Slot = slot;
                this.Round = round;
                this.Server = server;
                this.Value = value;
            }
        }

        internal class Agree : Event
        {
            public int Slot;
            public int Round;
            public int Server;
            public int Value;

            public Agree(int slot, int round, int server, int value)
            {
                this.Slot = slot;
                this.Round = round;
                this.Server = server;
                this.Value = value;
            }
        }

        internal class Accept : Event
        {
            public MachineId Node;
            public int NextSlotForProposer;
            public Tuple<int, int> NextProposal;
            public int ProposeVal;

            public Accept(MachineId id, int nextSlot, Tuple<int, int> nextProposal, int proposeVal)
            {
                this.Node = id;
                this.NextSlotForProposer = nextSlot;
                this.NextProposal = nextProposal;
                this.ProposeVal = proposeVal;
            }
        }

        internal class Reject : Event
        {
            public int Round;
            public Tuple<int, int> Proposal;

            public Reject(int round, Tuple<int, int> proposal)
            {
                this.Round = round;
                this.Proposal = proposal;
            }
        }

        internal class Update : Event
        {
            public int V1;
            public int V2;

            public Update(int v1, int v2)
            {
                this.V1 = v1;
                this.V2 = v2;
            }
        }

        Tuple<int, MachineId> CurrentLeader;
        MachineId LeaderElectionService;

        List<MachineId> Acceptors;
        int CommitValue;
        int ProposeVal;
        int Majority;
        int MyRank;
        Tuple<int, int> NextProposal;
        Tuple<int, int, int> ReceivedAgree;
        int MaxRound;
        int AcceptCount;
        int AgreeCount;
        MachineId Timer;
        int NextSlotForProposer;

        Dictionary<int, Tuple<int, int, int>> AcceptorSlots;

        Dictionary<int, Tuple<int, int, int>> LearnerSlots;
        int LastExecutedSlot;

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        [OnEventGotoState(typeof(local), typeof(PerformOperation))]
        [OnEventDoAction(typeof(PaxosNode.Config), nameof(Configure))]
        [OnEventDoAction(typeof(PaxosNode.AllNodes), nameof(UpdateAcceptors))]
        [DeferEvents(typeof(LeaderElection.Ping))]
        class Init : MachineState { }

        void InitOnEntry()
        {
            this.Acceptors = new List<MachineId>();
            this.AcceptorSlots = new Dictionary<int, Tuple<int, int, int>>();
            this.LearnerSlots = new Dictionary<int, Tuple<int, int, int>>();
        }

        void Configure()
        {
            this.MyRank = (this.ReceivedEvent as PaxosNode.Config).MyRank;

            this.CurrentLeader = Tuple.Create(this.MyRank, this.Id);
            this.MaxRound = 0;

            this.Timer = this.CreateMachine(typeof(Timer));
            this.Send(this.Timer, new Timer.Config(this.Id, 10));

            this.LastExecutedSlot = -1;
            this.NextSlotForProposer = 0;
        }

        [OnEventPushState(typeof(goPropose), typeof(ProposeValuePhase1))]
        [OnEventPushState(typeof(PaxosNode.Chosen), typeof(RunLearner))]
        [OnEventDoAction(typeof(PaxosNode.Update), nameof(CheckIfLeader))]
        [OnEventDoAction(typeof(PaxosNode.Prepare), nameof(PrepareAction))]
        [OnEventDoAction(typeof(PaxosNode.Accept), nameof(AcceptAction))]
        [OnEventDoAction(typeof(LeaderElection.Ping), nameof(ForwardToLE))]
        [OnEventDoAction(typeof(LeaderElection.NewLeader), nameof(UpdateLeader))]
        [IgnoreEvents(typeof(PaxosNode.Agree), typeof(PaxosNode.Accepted), typeof(Timer.TimeoutEvent), typeof(PaxosNode.Reject))]
        class PerformOperation : MachineState { }

        [OnEntry(nameof(ProposeValuePhase1OnEntry))]
        [OnEventGotoState(typeof(PaxosNode.Reject), typeof(ProposeValuePhase1), nameof(ProposeValuePhase1RejectAction))]
        [OnEventGotoState(typeof(success), typeof(ProposeValuePhase2), nameof(ProposeValuePhase1SuccessAction))]
        [OnEventGotoState(typeof(Timer.TimeoutEvent), typeof(ProposeValuePhase1))]
        [OnEventDoAction(typeof(PaxosNode.Agree), nameof(CountAgree))]
        [IgnoreEvents(typeof(PaxosNode.Accepted))]
        class ProposeValuePhase1 : MachineState { }

        void ProposeValuePhase1OnEntry()
        {
            this.AgreeCount = 0;
            this.NextProposal = this.GetNextProposal(this.MaxRound);
            this.ReceivedAgree = Tuple.Create(-1, -1, -1);

            foreach (var acceptor in this.Acceptors)
            {
                this.Send(acceptor, new PaxosNode.Prepare(this.Id, this.NextSlotForProposer, this.NextProposal, this.MyRank));
            }

            this.Monitor<ValidityCheck>(new ValidityCheck.monitor_proposer_sent(this.ProposeVal));
            this.Send(this.Timer, new Timer.StartTimerEvent());
        }

        void ProposeValuePhase1RejectAction()
        {
            var round = (this.ReceivedEvent as PaxosNode.Reject).Round;

            if (this.NextProposal.Item1 <= round)
            {
                this.MaxRound = round;
            }

            this.Send(this.Timer, new Timer.CancelTimerEvent());
        }

        void ProposeValuePhase1SuccessAction()
        {
            this.Send(this.Timer, new Timer.CancelTimerEvent());
        }

        [OnEntry(nameof(ProposeValuePhase2OnEntry))]
        [OnExit(nameof(ProposeValuePhase2OnExit))]
        [OnEventGotoState(typeof(PaxosNode.Reject), typeof(ProposeValuePhase1), nameof(ProposeValuePhase2RejectAction))]
        [OnEventGotoState(typeof(Timer.TimeoutEvent), typeof(ProposeValuePhase1))]
        [OnEventDoAction(typeof(PaxosNode.Accepted), nameof(CountAccepted))]
        [IgnoreEvents(typeof(PaxosNode.Agree))]
        class ProposeValuePhase2 : MachineState { }

        void ProposeValuePhase2OnEntry()
        {
            this.AcceptCount = 0;
            this.ProposeVal = this.GetHighestProposedValue();

            this.Monitor<BasicPaxosInvariant_P2b>(new BasicPaxosInvariant_P2b.monitor_valueProposed(
                this.Id, this.NextSlotForProposer, this.NextProposal, this.ProposeVal));
            this.Monitor<ValidityCheck>(new ValidityCheck.monitor_proposer_sent(this.ProposeVal));

            foreach (var acceptor in this.Acceptors)
            {
                this.Send(acceptor, new PaxosNode.Accept(this.Id, this.NextSlotForProposer, this.NextProposal, this.ProposeVal));
            }

            this.Send(this.Timer, new Timer.StartTimerEvent());
        }

        void ProposeValuePhase2OnExit()
        {
            if (this.ReceivedEvent.GetType() == typeof(PaxosNode.Chosen))
            {
                this.Monitor<BasicPaxosInvariant_P2b>(new BasicPaxosInvariant_P2b.monitor_valueChosen(
                    this.Id, this.NextSlotForProposer, this.NextProposal, this.ProposeVal));

                this.Send(this.Timer, new Timer.CancelTimerEvent());

                this.Monitor<ValidityCheck>(new ValidityCheck.monitor_proposer_chosen(this.ProposeVal));

                this.NextSlotForProposer++;
            }
        }

        void ProposeValuePhase2RejectAction()
        {
            var round = (this.ReceivedEvent as PaxosNode.Reject).Round;

            if (this.NextProposal.Item1 <= round)
            {
                this.MaxRound = round;
            }

            this.Send(this.Timer, new Timer.CancelTimerEvent());
        }

        [OnEntry(nameof(RunLearnerOnEntry))]
        [IgnoreEvents(typeof(PaxosNode.Agree), typeof(PaxosNode.Accepted), typeof(Timer.TimeoutEvent),
            typeof(PaxosNode.Prepare), typeof(PaxosNode.Reject), typeof(PaxosNode.Accept))]
        [DeferEvents(typeof(LeaderElection.NewLeader))]
        class RunLearner : MachineState { }

        void RunLearnerOnEntry()
        {
            var slot = (this.ReceivedEvent as PaxosNode.Chosen).Slot;
            var round = (this.ReceivedEvent as PaxosNode.Chosen).Round;
            var server = (this.ReceivedEvent as PaxosNode.Chosen).Server;
            var value = (this.ReceivedEvent as PaxosNode.Chosen).Value;

            this.LearnerSlots[slot] = Tuple.Create(round, server, value);

            if (this.CommitValue == value)
            {
                this.Pop();
            }
            else
            {
                this.ProposeVal = this.CommitValue;
                this.Raise(new goPropose());
            }
        }

        void UpdateAcceptors()
        {
            var acceptors = (this.ReceivedEvent as PaxosNode.AllNodes).Nodes;

            this.Acceptors = acceptors;

            this.Majority = this.Acceptors.Count / 2 + 1;
            this.Assert(this.Majority == 2, "Majority is not 2");

            this.LeaderElectionService = this.CreateMachine(typeof(LeaderElection));
            this.Send(this.LeaderElectionService, new LeaderElection.Config(this.Acceptors, this.Id, this.MyRank));

            this.Raise(new local());
        }

        void CheckIfLeader()
        {
            var e = this.ReceivedEvent as PaxosNode.Update;
            if (this.CurrentLeader.Item1 == this.MyRank)
            {
                this.CommitValue = e.V2;
                this.ProposeVal = this.CommitValue;
                this.Raise(new goPropose());
            }
            else
            {
                this.Send(this.CurrentLeader.Item2, new PaxosNode.Update(e.V1, e.V2));
            }
        }

        void PrepareAction()
        {
            var proposer = (this.ReceivedEvent as PaxosNode.Prepare).Node;
            var slot = (this.ReceivedEvent as PaxosNode.Prepare).NextSlotForProposer;
            var round = (this.ReceivedEvent as PaxosNode.Prepare).NextProposal.Item1;
            var server = (this.ReceivedEvent as PaxosNode.Prepare).NextProposal.Item2;

            if (!this.AcceptorSlots.ContainsKey(slot))
            {
                this.Send(proposer, new PaxosNode.Agree(slot, -1, -1, -1));
                return;
            }

            if (this.LessThan(round, server, this.AcceptorSlots[slot].Item1, this.AcceptorSlots[slot].Item2))
            {
                this.Send(proposer, new PaxosNode.Reject(slot, Tuple.Create(this.AcceptorSlots[slot].Item1,
                    this.AcceptorSlots[slot].Item2)));
            }
            else
            {
                this.Send(proposer, new PaxosNode.Agree(slot, this.AcceptorSlots[slot].Item1,
                    this.AcceptorSlots[slot].Item2, this.AcceptorSlots[slot].Item3));
                this.AcceptorSlots[slot] = Tuple.Create(this.AcceptorSlots[slot].Item1, this.AcceptorSlots[slot].Item2, -1);
            }
        }

        void AcceptAction()
        {
            var e = this.ReceivedEvent as PaxosNode.Accept;

            var proposer = e.Node;
            var slot = e.NextSlotForProposer;
            var round = e.NextProposal.Item1;
            var server = e.NextProposal.Item2;
            var value = e.ProposeVal;

            if (this.AcceptorSlots.ContainsKey(slot))
            {
                if (!this.IsEqual(round, server, this.AcceptorSlots[slot].Item1, this.AcceptorSlots[slot].Item2))
                {
                    this.Send(proposer, new PaxosNode.Reject(slot, Tuple.Create(this.AcceptorSlots[slot].Item1,
                        this.AcceptorSlots[slot].Item2)));
                }
                else
                {
                    this.AcceptorSlots[slot] = Tuple.Create(round, server, value);
                    this.Send(proposer, new PaxosNode.Accepted(slot, round, server, value));
                }
            }
        }

        void ForwardToLE()
        {
            this.Send(this.LeaderElectionService, this.ReceivedEvent);
        }

        void UpdateLeader()
        {
            var e = this.ReceivedEvent as LeaderElection.NewLeader;
            this.CurrentLeader = Tuple.Create(e.Rank, e.CurrentLeader);
        }

        void CountAgree()
        {
            var slot = (this.ReceivedEvent as PaxosNode.Agree).Slot;
            var round = (this.ReceivedEvent as PaxosNode.Agree).Round;
            var server = (this.ReceivedEvent as PaxosNode.Agree).Server;
            var value = (this.ReceivedEvent as PaxosNode.Agree).Value;

            if (slot == this.NextSlotForProposer)
            {
                this.AgreeCount++;
                if (this.LessThan(this.ReceivedAgree.Item1, this.ReceivedAgree.Item2, round, server))
                {
                    this.ReceivedAgree = Tuple.Create(round, server, value);
                }

                if (this.AgreeCount == this.Majority)
                {
                    this.Raise(new success());
                }
            }
        }

        void CountAccepted()
        {
            var e = this.ReceivedEvent as PaxosNode.Accepted;

            var slot = e.Slot;
            var round = e.Round;
            var server = e.Server;

            if (slot == this.NextSlotForProposer)
            {
                if (this.IsEqual(round, server, this.NextProposal.Item1, this.NextProposal.Item2))
                {
                    this.AcceptCount++;
                }

                if (this.AcceptCount == this.Majority)
                {
                    this.Raise(new PaxosNode.Chosen(e.Slot, e.Round, e.Server, e.Value));
                }
            }
        }

        void RunReplicatedMachine()
        {
            while (true)
            {
                if (this.LearnerSlots.ContainsKey(this.LastExecutedSlot + 1))
                {
                    this.LastExecutedSlot++;
                }
                else
                {
                    return;
                }
            }
        }

        int GetHighestProposedValue()
        {
            if (this.ReceivedAgree.Item2 != -1)
            {
                return this.ReceivedAgree.Item2;
            }
            else
            {
                return this.CommitValue;
            }
        }

        Tuple<int, int> GetNextProposal(int maxRound)
        {
            return Tuple.Create(maxRound + 1, this.MyRank);
        }

        bool IsEqual(int round1, int server1, int round2, int server2)
        {
            if (round1 == round2 && server1 == server2)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        bool LessThan(int round1, int server1, int round2, int server2)
        {
            if (round1 < round2)
            {
                return true;
            }
            else if (round1 == round2)
            {
                if (server1 < server2)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
    }
}
