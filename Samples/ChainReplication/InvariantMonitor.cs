using System.Collections.Generic;
using Microsoft.Coyote;

namespace ChainReplication
{
    internal class InvariantMonitor : Monitor
    {
        #region events

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

        private class Local : Event { }

        #endregion

        #region fields

        List<MachineId> Servers;

        Dictionary<MachineId, List<int>> History;
        Dictionary<MachineId, List<int>> SentHistory;
        List<int> TempSeq;

        MachineId Next;
        MachineId Prev;

        #endregion

        #region states

        [Start]
        [OnEventGotoState(typeof(Local), typeof(WaitForUpdateMessage))]
        [OnEventDoAction(typeof(Config), nameof(Configure))]
        class Init : MonitorState { }

        void Configure()
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
        class WaitForUpdateMessage : MonitorState { }

        void CheckUpdatePropagationInvariant()
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

        void CheckInprocessRequestsInvariant()
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

        void GetNext(MachineId curr)
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

        void GetPrev(MachineId curr)
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

        void ExtractSeqId(List<SentLog> seq)
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

        void MergeSeq(List<int> seq1, List<int> seq2)
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

        void IsSorted(List<int> seq)
        {
            for (int i = 0; i < seq.Count - 1; i++)
            {
                this.Assert(seq[i] < seq[i + 1], "Sequence is not sorted.");
            }
        }

        void CheckLessOrEqualThan(List<int> seq1, List<int> seq2)
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

        void CheckEqual(List<int> seq1, List<int> seq2)
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

        void ClearTempSeq()
        {
            this.Assert(this.TempSeq.Count <= 6, "Temp sequence has more than 6 elements.");
            this.TempSeq.Clear();
            this.Assert(this.TempSeq.Count == 0, "Temp sequence is not cleared.");
        }

        void ProcessUpdateServers()
        {
            this.Servers = (this.ReceivedEvent as UpdateServers).Servers;
        }

        #endregion
    }
}