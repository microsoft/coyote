using System.Collections.Generic;
using Microsoft.Coyote;

namespace ChainReplication
{
    internal class ServerResponseSeqMonitor : Monitor
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

        private class Local : Event { }

        #endregion

        #region fields

        List<MachineId> Servers;
        Dictionary<int, int> LastUpdateResponse;

        #endregion

        #region states

        [Start]
        [OnEventGotoState(typeof(Local), typeof(Wait))]
        [OnEventDoAction(typeof(Config), nameof(Configure))]
        class Init : MonitorState { }

        void Configure()
        {
            this.Servers = (this.ReceivedEvent as Config).Servers;
            this.LastUpdateResponse = new Dictionary<int, int>();
            this.Raise(new Local());
        }

        [OnEventDoAction(typeof(ResponseToUpdate), nameof(ResponseToUpdateAction))]
        [OnEventDoAction(typeof(ResponseToQuery), nameof(ResponseToQueryAction))]
        [OnEventDoAction(typeof(UpdateServers), nameof(ProcessUpdateServers))]
        class Wait : MonitorState { }

        void ResponseToUpdateAction()
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

        void ResponseToQueryAction()
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

        #endregion
    }
}