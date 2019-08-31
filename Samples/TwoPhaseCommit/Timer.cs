using Microsoft.Coyote;

namespace TwoPhaseCommit
{
    internal class Timer : Machine
    {
        internal class Config : Event
        {
            public MachineId Target;

            public Config(MachineId id)
                : base()
            {
                this.Target = id;
            }
        }

        internal class StartTimerEvent : Event
        {
            public int Timeout;

            public StartTimerEvent(int timeout)
                : base()
            {
                this.Timeout = timeout;
            }
        }

        internal class TimeoutEvent : Event { }
        internal class CancelTimerEvent : Event { }
        internal class CancelTimerSuccess : Event { }
        internal class CancelTimerFailure : Event { }
        private class Unit : Event { }

        MachineId Target;

        [Start]
        [OnEventGotoState(typeof(Unit), typeof(Loop))]
        [OnEventDoAction(typeof(Config), nameof(Configure))]
        class Init : MachineState { }

        void Configure()
        {
            this.Target = (this.ReceivedEvent as Config).Target;
            this.Raise(new Unit());
        }

        [OnEventGotoState(typeof(StartTimerEvent), typeof(TimerStarted))]
        [IgnoreEvents(typeof(CancelTimerEvent))]
        class Loop : MachineState { }


        [OnEntry(nameof(TimerStartedOnEntry))]
        [OnEventGotoState(typeof(Unit), typeof(Loop))]
        [OnEventDoAction(typeof(CancelTimerEvent), nameof(CancelingTimer))]
        class TimerStarted : MachineState { }

        void TimerStartedOnEntry()
        {
            if (this.Random())
            {
                this.Send(this.Target, new Timer.TimeoutEvent());
                this.Raise(new Unit());
            }
        }

        void CancelingTimer()
        {
            if (this.Random())
            {
                this.Send(this.Target, new CancelTimerFailure());
                this.Send(this.Target, new TimeoutEvent());
            }
            else
            {
                this.Send(this.Target, new CancelTimerSuccess());
            }

            this.Raise(new Unit());
        }
    }
}
