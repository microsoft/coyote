using System.Collections.Generic;
using Microsoft.Coyote;

namespace ChainReplication
{
    internal class Client : Machine
    {
        #region events

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

        private class Local : Event { }
        private class Done : Event { }

        #endregion

        #region fields

        int ClientId;

        MachineId HeadNode;
        MachineId TailNode;

        int StartIn;
        int Next;

        Dictionary<int, int> KeyValueStore;

        #endregion

        #region states

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        [OnEventGotoState(typeof(Local), typeof(PumpUpdateRequests))]
        class Init : MachineState { }

		void InitOnEntry()
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
        [IgnoreEvents(typeof(ChainReplicationServer.ResponseToUpdate),
            typeof(ChainReplicationServer.ResponseToQuery))]
        class PumpUpdateRequests : MachineState { }

        void PumpUpdateRequestsOnEntry()
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
        [IgnoreEvents(typeof(ChainReplicationServer.ResponseToUpdate),
            typeof(ChainReplicationServer.ResponseToQuery))]
        class PumpQueryRequests : MachineState { }

        void PumpQueryRequestsOnEntry()
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

        void PumpRequestsLocalAction()
        {
            this.Next++;
        }

        void PumpRequestsDoneAction()
        {
            this.Next = 1;
        }

        #endregion
    }
}
