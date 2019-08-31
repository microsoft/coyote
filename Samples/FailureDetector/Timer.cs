using Microsoft.Coyote;

namespace FailureDetector
{
    /// <summary>
    /// This Coyote machine models the operating system timer.
    ///
    /// It fires timeouts in a non-deterministic fashion using the Coyote
    /// method 'Random', rather than using an actual timeout.
    /// </summary>
    internal class Timer : Machine
    {
        internal class Config : Event
        {
            public MachineId Target;

            public Config(MachineId target)
            {
                this.Target = target;
            }
        }

        /// <summary>
        /// Although this event accepts a timeout value, because
        /// this machine models a timer by nondeterministically
        /// triggering a timeout, this value is not used.
        /// </summary>
        internal class StartTimerEvent : Event
        {
            public int Timeout;

            public StartTimerEvent(int timeout)
            {
                this.Timeout = timeout;
            }
        }

        internal class TimeoutEvent : Event { }

        internal class CancelSuccess : Event { }
        internal class CancelFailure : Event { }
        internal class CancelTimerEvent : Event { }

        /// <summary>
        /// Reference to the owner of the timer.
        /// </summary>
        MachineId Target;

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        class Init : MachineState { }

        /// <summary>
        /// When it enters the 'Init' state, the timer receives a reference to
        /// the target machine, and then transitions to the 'WaitForReq' state.
        /// </summary>
        void InitOnEntry()
        {
            this.Target = (this.ReceivedEvent as Config).Target;
            this.Goto<WaitForReq>();
        }

        /// <summary>
        /// The timer waits in the 'WaitForReq' state for a request from the client.
        ///
        /// It responds with a 'CancelFailure' event on a 'CancelTimer' event.
        [OnEventGotoState(typeof(CancelTimerEvent), typeof(WaitForReq), nameof(CancelTimerAction))]
        ///
        /// It transitions to the 'WaitForCancel' state on a 'StartTimerEvent' event.
        [OnEventGotoState(typeof(StartTimerEvent), typeof(WaitForCancel))]
        /// </summary>
        class WaitForReq : MachineState { }

        void CancelTimerAction()
        {
            this.Send(this.Target, new CancelFailure());
        }

        /// <summary>
        /// In the 'WaitForCancel' state, any 'StartTimerEvent' event is dequeued and dropped without any
        /// action (indicated by the 'IgnoreEvents' declaration).
        [IgnoreEvents(typeof(StartTimerEvent))]
        [OnEventGotoState(typeof(CancelTimerEvent), typeof(WaitForReq), nameof(CancelTimerAction2))]
        [OnEventGotoState(typeof(Default), typeof(WaitForReq), nameof(DefaultAction))]
        /// </summary>
        class WaitForCancel : MachineState { }

        void DefaultAction()
        {
            this.Send(this.Target, new TimeoutEvent());
        }

        /// <summary>
        /// The response to a 'CancelTimer' event is nondeterministic. During testing, Coyote will
        /// take control of this source of nondeterminism and explore different execution paths.
        ///
        /// Using this approach, we model the race condition between the arrival of a 'CancelTimer'
        /// event from the target and the elapse of the timer.
        /// </summary>
        void CancelTimerAction2()
        {
            // A nondeterministic choice that is controlled by the Coyote runtime during testing.
            if (this.Random())
            {
                this.Send(this.Target, new CancelSuccess());
            }
            else
            {
                this.Send(this.Target, new CancelFailure());
                this.Send(this.Target, new TimeoutEvent());
            }
        }
    }
}
