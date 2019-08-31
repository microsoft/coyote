using System;
using System.Collections.Generic;
using Microsoft.Coyote;

namespace MultiPaxos
{
    internal class BasicPaxosInvariant_P2b : Monitor
    {
        internal class monitor_valueProposed : Event
        {
            public MachineId Node;
            public int NextSlotForProposer;
            public Tuple<int, int> NextProposal;
            public int ProposeVal;

            public monitor_valueProposed(MachineId id, int nextSlot, Tuple<int, int> nextProposal, int proposeVal)
            {
                this.Node = id;
                this.NextSlotForProposer = nextSlot;
                this.NextProposal = nextProposal;
                this.ProposeVal = proposeVal;
            }
        }

        internal class monitor_valueChosen : Event
        {
            public MachineId Node;
            public int NextSlotForProposer;
            public Tuple<int, int> NextProposal;
            public int ProposeVal;

            public monitor_valueChosen(MachineId id, int nextSlot, Tuple<int, int> nextProposal, int proposeVal)
            {
                this.Node = id;
                this.NextSlotForProposer = nextSlot;
                this.NextProposal = nextProposal;
                this.ProposeVal = proposeVal;
            }
        }

        Dictionary<int, Tuple<int, int, int>> LastValueChosen;

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        class Init : MonitorState { }

        void InitOnEntry()
        {
            this.LastValueChosen = new Dictionary<int, Tuple<int, int, int>>();
            this.Goto<WaitForValueChosen>();
        }

        [OnEventGotoState(typeof(BasicPaxosInvariant_P2b.monitor_valueChosen), typeof(CheckValueProposed), nameof(WaitForValueChosenAction))]
        [IgnoreEvents(typeof(BasicPaxosInvariant_P2b.monitor_valueProposed))]
        class WaitForValueChosen : MonitorState { }

        void WaitForValueChosenAction()
        {
            var slot = (this.ReceivedEvent as BasicPaxosInvariant_P2b.monitor_valueChosen).NextSlotForProposer;
            var proposal = Tuple.Create((this.ReceivedEvent as BasicPaxosInvariant_P2b.monitor_valueChosen).NextProposal.Item1,
                (this.ReceivedEvent as BasicPaxosInvariant_P2b.monitor_valueChosen).NextProposal.Item2,
                (this.ReceivedEvent as BasicPaxosInvariant_P2b.monitor_valueChosen).ProposeVal);
            this.LastValueChosen.Add(slot, proposal);
        }

        [OnEventGotoState(typeof(BasicPaxosInvariant_P2b.monitor_valueChosen), typeof(CheckValueProposed), nameof(ValueChosenAction))]
        [OnEventGotoState(typeof(BasicPaxosInvariant_P2b.monitor_valueProposed), typeof(CheckValueProposed), nameof(ValueProposedAction))]
        class CheckValueProposed : MonitorState { }

        void ValueChosenAction()
        {
            var slot = (this.ReceivedEvent as BasicPaxosInvariant_P2b.monitor_valueChosen).NextSlotForProposer;
            var proposal = Tuple.Create((this.ReceivedEvent as BasicPaxosInvariant_P2b.monitor_valueChosen).NextProposal.Item1,
                (this.ReceivedEvent as BasicPaxosInvariant_P2b.monitor_valueChosen).NextProposal.Item2,
                (this.ReceivedEvent as BasicPaxosInvariant_P2b.monitor_valueChosen).ProposeVal);

            this.Assert(this.LastValueChosen[slot].Item3 == proposal.Item3, "ValueChosenAction");
        }

        void ValueProposedAction()
        {
            var slot = (this.ReceivedEvent as BasicPaxosInvariant_P2b.monitor_valueProposed).NextSlotForProposer;
            var proposal = Tuple.Create((this.ReceivedEvent as BasicPaxosInvariant_P2b.monitor_valueProposed).NextProposal.Item1,
                (this.ReceivedEvent as BasicPaxosInvariant_P2b.monitor_valueProposed).NextProposal.Item2,
                (this.ReceivedEvent as BasicPaxosInvariant_P2b.monitor_valueProposed).ProposeVal);

            if (this.LessThan(this.LastValueChosen[slot].Item1, this.LastValueChosen[slot].Item2,
                proposal.Item1, proposal.Item2))
            {
                this.Assert(this.LastValueChosen[slot].Item3 == proposal.Item3, "ValueProposedAction");
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