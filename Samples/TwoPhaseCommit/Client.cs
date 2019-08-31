using Microsoft.Coyote;

namespace TwoPhaseCommit
{
    internal class Client : Machine
    {
        internal class Config : Event
        {
            public MachineId Coordinator;

            public Config(MachineId coordinator)
                : base()
            {
                this.Coordinator = coordinator;
            }
        }

        internal class WriteReq : Event
        {
            public PendingWriteRequest PendingWriteReq;

            public WriteReq(PendingWriteRequest req)
                : base()
            {
                this.PendingWriteReq = req;
            }
        }

        internal class ReadReq : Event
        {
            public MachineId Client;
            public int Idx;

            public ReadReq(MachineId client, int idx)
                : base()
            {
                this.Client = client;
                this.Idx = idx;
            }
        }

        private class Unit : Event { }

        private MachineId Coordinator;
        private int Idx;
        private int Val;

        [Start]
        [OnEventGotoState(typeof(Unit), typeof(DoWrite))]
        [OnEventDoAction(typeof(Config), nameof(Configure))]
        class Init : MachineState { }

		void Configure()
        {
            this.Coordinator = (this.ReceivedEvent as Config).Coordinator;
            this.Raise(new Unit());
        }

        [OnEntry(nameof(DoWriteOnEntry))]
        [OnEventGotoState(typeof(Coordinator.WriteSuccess), typeof(DoRead))]
        [OnEventGotoState(typeof(Coordinator.WriteFail), typeof(End))]
        class DoWrite : MachineState { }

        void DoWriteOnEntry()
        {
            this.Idx = this.ChooseIndex();
            this.Val = this.ChooseValue();

            this.Send(this.Coordinator, new WriteReq(new PendingWriteRequest(
                this.Id, this.Idx, this.Val)));
        }

        int ChooseIndex()
        {
            if (this.Random())
            {
                return 0;
            }
            else
            {
                return 1;
            }
        }

        int ChooseValue()
        {
            if (this.Random())
            {
                return 0;
            }
            else
            {
                return 1;
            }
        }

        [OnEntry(nameof(DoReadOnEntry))]
        [OnEventGotoState(typeof(Coordinator.ReadSuccess), typeof(End))]
        [OnEventGotoState(typeof(Coordinator.ReadFail), typeof(End))]
        class DoRead : MachineState { }

        void DoReadOnEntry()
        {
            this.Send(this.Coordinator, new ReadReq(this.Id, this.Idx));
        }

        class End : MachineState { }
    }
}
