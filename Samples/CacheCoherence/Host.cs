using System;
using System.Collections.Generic;
using Microsoft.Coyote;

namespace CacheCoherence
{
    internal class Host : Machine
    {
        internal class GrantExcl : Event { }
        internal class GrantShare : Event { }
        internal class Invalidate : Event { }

        private class GrantAccess : Event { }
        private class NeedInvalidate : Event { }
        private class Unit : Event { }

        MachineId CurrentClient;
        Tuple<MachineId, MachineId, MachineId> Clients;
        MachineId CurrentCPU;
        List<MachineId> SharerList;

        bool IsCurrentReqExcl;
        bool IsExclGranted;

		[Start]
        [OnEntry(nameof(InitOnEntry))]
        [OnEventGotoState(typeof(Unit), typeof(Receiving))]
        class Init : MachineState { }

		void InitOnEntry()
        {
            this.SharerList = new List<MachineId>();

            this.Clients = Tuple.Create(
                this.CreateMachine(typeof(Client)),
                this.CreateMachine(typeof(Client)),
                this.CreateMachine(typeof(Client)));

            this.Send(this.Clients.Item1, new Client.Config(this.Id, false));
            this.Send(this.Clients.Item2, new Client.Config(this.Id, false));
            this.Send(this.Clients.Item3, new Client.Config(this.Id, false));

            this.CurrentClient = null;
            this.CurrentCPU = this.CreateMachine(typeof(CPU));
            this.Send(this.CurrentCPU, new CPU.Config(this.Clients));

            this.Raise(new Unit());
        }

        [OnEventGotoState(typeof(Client.ReqShare), typeof(ShareRequest))]
        [OnEventGotoState(typeof(Client.ReqExcl), typeof(ExclRequest))]
        [DeferEvents(typeof(Client.InvalidateAck))]
        class Receiving : MachineState { }

        [OnEntry(nameof(ShareRequestOnEntry))]
        [OnEventGotoState(typeof(Unit), typeof(ProcessingRequest))]
        class ShareRequest : MachineState { }

        void ShareRequestOnEntry()
        {
            this.CurrentClient = (this.ReceivedEvent as Client.ReqShare).Client;
            this.IsCurrentReqExcl = false;
            this.Raise(new Unit());
        }

        [OnEntry(nameof(ExclRequestOnEntry))]
        [OnEventGotoState(typeof(Unit), typeof(ProcessingRequest))]
        class ExclRequest : MachineState { }

        void ExclRequestOnEntry()
        {
            this.CurrentClient = (this.ReceivedEvent as Client.ReqExcl).Client;
            this.IsCurrentReqExcl = true;
            this.Raise(new Unit());
        }

        [OnEntry(nameof(ProcessRequestOnEntry))]
        [OnEventGotoState(typeof(NeedInvalidate), typeof(Invalidating))]
        [OnEventGotoState(typeof(GrantAccess), typeof(GrantingAccess))]
        class ProcessingRequest : MachineState { }

        void ProcessRequestOnEntry()
        {
            if (this.IsCurrentReqExcl || this.IsExclGranted)
            {
                this.Raise(new NeedInvalidate());
            }
            else
            {
                this.Raise(new GrantAccess());
            }
        }

        [OnEntry(nameof(InvalidatingOnEntry))]
        [OnEventGotoState(typeof(GrantAccess), typeof(GrantingAccess))]
        [OnEventDoAction(typeof(Client.InvalidateAck), nameof(RecAck))]
        [DeferEvents(typeof(Client.ReqShare), typeof(Client.ReqExcl))]
        class Invalidating : MachineState { }

        void InvalidatingOnEntry()
        {
            if (this.SharerList.Count == 0)
            {
                this.Raise(new GrantAccess());
            }

            foreach (var m in this.SharerList)
            {
                this.Send(m, new Invalidate());
            }
        }

        void RecAck()
        {
            this.SharerList.RemoveAt(0);
            if (this.SharerList.Count == 0)
            {
                this.Raise(new GrantAccess());
            }
        }

        [OnEntry(nameof(GrantingAccessOnEntry))]
        [OnEventGotoState(typeof(Unit), typeof(Receiving))]
        class GrantingAccess : MachineState { }

        void GrantingAccessOnEntry()
        {
            if (this.IsCurrentReqExcl)
            {
                this.IsExclGranted = true;
                this.Send(this.CurrentClient, new GrantExcl());
            }
            else
            {
                this.Send(this.CurrentClient, new GrantShare());
            }

            this.SharerList.Insert(0, this.CurrentClient);
            this.Raise(new Unit());
        }
    }
}
