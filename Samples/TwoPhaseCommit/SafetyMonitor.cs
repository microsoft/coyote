using System.Collections.Generic;
using Microsoft.Coyote;

namespace TwoPhaseCommit
{
    internal class SafetyMonitor : Monitor
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

        internal class MonitorWrite : Event
        {
            public int Idx;
            public int Val;

            public MonitorWrite(int idx, int val)
                : base()
            {
                this.Idx = idx;
                this.Val = val;
            }
        }

        internal class MonitorReadSuccess : Event
        {
            public int Idx;
            public int Val;

            public MonitorReadSuccess(int idx, int val)
                : base()
            {
                this.Idx = idx;
                this.Val = val;
            }
        }

        internal class MonitorReadUnavailable : Event
        {
            public int Idx;

            public MonitorReadUnavailable(int idx)
                : base()
            {
                this.Idx = idx;
            }
        }

        private class Unit : Event { }

        private Dictionary<int, int> Data;

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        [OnEventDoAction(typeof(MonitorWrite), nameof(MonitorWriteAction))]
        [OnEventDoAction(typeof(MonitorReadSuccess), nameof(MonitorReadSuccessAction))]
        [OnEventDoAction(typeof(MonitorReadUnavailable), nameof(MonitorReadUnavailableAction))]
        class Init : MonitorState { }

        void InitOnEntry()
        {
            this.Data = new Dictionary<int, int>();
        }

        void MonitorWriteAction()
        {
            var idx = (this.ReceivedEvent as MonitorWrite).Idx;
            var val = (this.ReceivedEvent as MonitorWrite).Val;

            if (!this.Data.ContainsKey(idx))
            {
                this.Data.Add(idx, val);
            }
            else
            {
                this.Data[idx] = val;
            }
        }

        void MonitorReadSuccessAction()
        {
            var idx = (this.ReceivedEvent as MonitorReadSuccess).Idx;
            var val = (this.ReceivedEvent as MonitorReadSuccess).Val;
            this.Assert(this.Data.ContainsKey(idx));
            this.Assert(this.Data[idx] == val);
        }

        void MonitorReadUnavailableAction()
        {
            var idx = (this.ReceivedEvent as MonitorReadUnavailable).Idx;
            this.Assert(!this.Data.ContainsKey(idx));
        }
    }
}
