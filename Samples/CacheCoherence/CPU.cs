using System;
using Microsoft.Coyote;

namespace CacheCoherence
{
    internal class CPU : Machine
    {
        internal class Config : Event
        {
            public Tuple<MachineId, MachineId, MachineId> Clients;

            public Config(Tuple<MachineId, MachineId, MachineId> clients)
                : base()
            {
                this.Clients = clients;
            }
        }

        internal class AskShare : Event { }
        internal class AskExcl : Event { }
        private class Unit : Event { }

        Tuple<MachineId, MachineId, MachineId> Cache;

        [Start]
        [OnEventDoAction(typeof(Config), nameof(Configure))]
        [OnEventGotoState(typeof(Unit), typeof(MakingReq))]
        class Init : MachineState { }

        void Configure()
        {
            this.Cache = (this.ReceivedEvent as Config).Clients;
            this.Raise(new Unit());
        }

        [OnEntry(nameof(MakingReqOnEntry))]
        [OnEventGotoState(typeof(Unit), typeof(MakingReq))]
        class MakingReq : MachineState { }

        void MakingReqOnEntry()
        {
            if (this.Random())
            {
                if (this.Random())
                {
                    this.Send(this.Cache.Item1, new AskShare());
                }
                else
                {
                    this.Send(this.Cache.Item1, new AskExcl());
                }
            }
            else if (this.Random())
            {
                if (this.Random())
                {
                    this.Send(this.Cache.Item2, new AskShare());
                }
                else
                {
                    this.Send(this.Cache.Item2, new AskExcl());
                }
            }
            else
            {
                if (this.Random())
                {
                    this.Send(this.Cache.Item3, new AskShare());
                }
                else
                {
                    this.Send(this.Cache.Item3, new AskExcl());
                }
            }

            this.Raise(new Unit());
        }
    }
}
