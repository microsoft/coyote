using System.Collections.Generic;
using Microsoft.Coyote;

namespace TwoPhaseCommit
{
    internal class Coordinator : Machine
    {
        internal class Config : Event
        {
            public int NumReplicas;

            public Config(int numReplicas)
                : base()
            {
                this.NumReplicas = numReplicas;
            }
        }

        internal class ReqReplica : Event
        {
            public PendingWriteRequest PendingWriteReq;

            public ReqReplica(PendingWriteRequest req)
                : base()
            {
                this.PendingWriteReq = req;
            }
        }

        internal class GlobalCommit : Event
        {
            public int SeqNum;

            public GlobalCommit(int currSeqNum)
                : base()
            {
                this.SeqNum = currSeqNum;
            }
        }

        internal class GlobalAbort : Event
        {
            public int SeqNum;

            public GlobalAbort(int currSeqNum)
                : base()
            {
                this.SeqNum = currSeqNum;
            }
        }

        internal class ReadSuccess : Event
        {
            public int Idx;

            public ReadSuccess(int idx)
                : base()
            {
                this.Idx = idx;
            }
        }

        internal class WriteSuccess : Event { }
        internal class WriteFail : Event { }
        internal class ReadFail : Event { }
        private class Unit : Event { }

        private Dictionary<int, int> Data;
        private List<MachineId> Replicas;
        private PendingWriteRequest PendingWriteReq;
        private int CurrSeqNum;
        private int Counter;
        private MachineId Timer;

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        [OnEventGotoState(typeof(Unit), typeof(Loop))]
        [OnEventDoAction(typeof(Config), nameof(Configure))]
        class Init : MachineState { }

		void InitOnEntry()
        {
            this.Data = new Dictionary<int, int>();
            this.Replicas = new List<MachineId>();
        }

        void Configure()
        {
            int numReplicas = (this.ReceivedEvent as Config).NumReplicas;
            this.Assert(numReplicas > 0);

            for (int i = 0; i < numReplicas; i++)
            {
                var replica = this.CreateMachine(typeof(Replica));
                this.Replicas.Add(replica);
                this.Send(replica, new Replica.Config(this.Id));
            }

            this.CurrSeqNum = 0;
            this.Counter = numReplicas;

            this.Timer = this.CreateMachine(typeof(Timer));
            this.Send(this.Timer, new Timer.Config(this.Id));

            this.Raise(new Unit());
        }

        [OnEventGotoState(typeof(Unit), typeof(CountingVote))]
        [OnEventDoAction(typeof(Client.WriteReq), nameof(DoWrite))]
        [OnEventDoAction(typeof(Client.ReadReq), nameof(DoRead))]
        [IgnoreEvents(typeof(Replica.RespReplicaCommit), typeof(Replica.RespReplicaAbort))]
        class Loop : MachineState { }

        void DoWrite()
        {
            this.PendingWriteReq = (this.ReceivedEvent as Client.WriteReq).PendingWriteReq;
            this.CurrSeqNum++;

            for (int i = 0; i < this.Replicas.Count; i++)
            {
                this.Send(this.Replicas[i], new ReqReplica(new PendingWriteRequest(this.CurrSeqNum,
                    this.PendingWriteReq.Idx, this.PendingWriteReq.Val)));
            }

            this.Send(this.Timer, new Timer.StartTimerEvent(100));
            this.Raise(new Unit());
        }

        void DoRead()
        {
            var client = (this.ReceivedEvent as Client.ReadReq).Client;
            var idx = (this.ReceivedEvent as Client.ReadReq).Idx;

            if (this.Data.ContainsKey(idx))
            {
                this.Monitor<SafetyMonitor>(new SafetyMonitor.MonitorReadSuccess(idx, this.Data[idx]));
                this.Send(client, new ReadSuccess(this.Data[idx]));
            }
            else
            {
                this.Monitor<SafetyMonitor>(new SafetyMonitor.MonitorReadUnavailable(idx));
                this.Send(client, new ReadFail());
            }
        }

        [OnEntry(nameof(CountingVoteOnEntry))]
        [OnEventGotoState(typeof(Unit), typeof(WaitingForCancelTimerResponse))]
        [OnEventGotoState(typeof(Replica.RespReplicaCommit), typeof(CountingVote), nameof(RespReplicaCommitAction))]
        [OnEventGotoState(typeof(Timer.TimeoutEvent), typeof(Loop), nameof(DoGlobalAbort))]
        [OnEventDoAction(typeof(Replica.RespReplicaAbort), nameof(HandleAbort))]
        [OnEventDoAction(typeof(Client.ReadReq), nameof(DoRead))]
        [DeferEvents(typeof(Client.WriteReq))]
        class CountingVote : MachineState { }

        void CountingVoteOnEntry()
        {
            if (this.Counter == 0)
            {
                for (int i = 0; i < this.Replicas.Count; i++)
                {
                    this.Send(this.Replicas[i], new GlobalCommit(this.CurrSeqNum));
                    this.Counter++;
                }

                //this.Data.Add(this.PendingWriteReq.Idx, this.PendingWriteReq.Val);
                if (!this.Data.ContainsKey(this.PendingWriteReq.Idx))
                {
                    this.Data.Add(this.PendingWriteReq.Idx, this.PendingWriteReq.Val);
                }
                else
                {
                    this.Data[this.PendingWriteReq.Idx] = this.PendingWriteReq.Val;
                }

                this.Monitor<SafetyMonitor>(new SafetyMonitor.MonitorWrite(
                    this.PendingWriteReq.Idx, PendingWriteReq.Val));

                this.Send(this.PendingWriteReq.Client, new WriteSuccess());
                this.Send(this.Timer, new Timer.CancelTimerEvent());

                this.Raise(new Unit());
            }
        }

        void RespReplicaCommitAction()
        {
            var seqNum = (this.ReceivedEvent as Replica.RespReplicaCommit).SeqNum;

            if (this.CurrSeqNum == seqNum)
            {
                this.Counter--;
            }
        }

        void HandleAbort()
        {
            var seqNum = (this.ReceivedEvent as Replica.RespReplicaAbort).SeqNum;
            if (this.CurrSeqNum == seqNum)
            {
                this.DoGlobalAbort();
                this.Send(this.Timer, new Timer.CancelTimerEvent());
                this.Raise(new Unit());
            }
        }

        void DoGlobalAbort()
        {
            for (int i = 0; i < this.Replicas.Count; i++)
            {
                this.Send(this.Replicas[i], new GlobalAbort(this.CurrSeqNum));
            }

            this.Send(this.PendingWriteReq.Client, new WriteFail());
        }

        [OnEventGotoState(typeof(Timer.TimeoutEvent), typeof(Loop))]
        [OnEventGotoState(typeof(Timer.CancelTimerSuccess), typeof(Loop))]
        [OnEventGotoState(typeof(Timer.CancelTimerFailure), typeof(WaitingForTimeout))]
        [DeferEvents(typeof(Client.WriteReq), typeof(Client.ReadReq))]
        [IgnoreEvents(typeof(Replica.RespReplicaCommit), typeof(Replica.RespReplicaAbort))]
        class WaitingForCancelTimerResponse : MachineState { }

        [OnEventGotoState(typeof(Timer.TimeoutEvent), typeof(Loop))]
        [DeferEvents(typeof(Client.WriteReq), typeof(Client.ReadReq))]
        [IgnoreEvents(typeof(Replica.RespReplicaCommit), typeof(Replica.RespReplicaAbort))]
        class WaitingForTimeout : MachineState { }
    }
}
