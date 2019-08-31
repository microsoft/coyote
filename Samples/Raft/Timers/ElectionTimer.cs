using System;
using System.Collections.Generic;
using Microsoft.Coyote;

namespace Raft
{
    internal class ElectionTimer : Machine
    {
        internal class ConfigureEvent : Event
        {
            public MachineId Target;

            public ConfigureEvent(MachineId id)
                : base()
            {
                this.Target = id;
            }
        }

        internal class StartTimerEvent : Event { }
        internal class CancelTimerEvent : Event { }
        internal class TimeoutEvent : Event { }

        private class TickEvent : Event { }

        MachineId Target;

        [Start]
        [OnEventDoAction(typeof(ConfigureEvent), nameof(Configure))]
        [OnEventGotoState(typeof(StartTimerEvent), typeof(Active))]
        class Init : MachineState { }

        void Configure()
        {
            this.Target = (this.ReceivedEvent as ConfigureEvent).Target;
            //this.Raise(new StartTimerEvent());
        }

        [OnEntry(nameof(ActiveOnEntry))]
        [OnEventDoAction(typeof(TickEvent), nameof(Tick))]
        [OnEventGotoState(typeof(CancelTimerEvent), typeof(Inactive))]
        [IgnoreEvents(typeof(StartTimerEvent))]
        class Active : MachineState { }

        void ActiveOnEntry()
        {
            this.Send(this.Id, new TickEvent());
        }

        void Tick()
        {
            if (this.Random())
            {
                this.Logger.WriteLine("\n [ElectionTimer] " + this.Target + " | timed out\n");
                this.Send(this.Target, new TimeoutEvent());
            }

            //this.Send(this.Id, new TickEvent());
            this.Raise(new CancelTimerEvent());
        }

        [OnEventGotoState(typeof(StartTimerEvent), typeof(Active))]
        [IgnoreEvents(typeof(CancelTimerEvent), typeof(TickEvent))]
        class Inactive : MachineState { }
    }
}
