// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote;
using Microsoft.Coyote.Actors;

namespace Microsoft.Coyote.Samples.Monitors
{
    /// <summary>
    /// This Coyote machine models a cancellable and non-deterministic operating system timer.
    /// It fires timeouts in a non-deterministic fashion using the Coyote
    /// method 'Random', rather than using an actual timeout.
    /// </summary>
    internal class Timer : StateMachine
    {
        internal class Config : Event
        {
            public ActorId Target;

            public Config(ActorId target)
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
        private ActorId Target;

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        private class Init : State { }

        /// <summary>
        /// When it enters the 'Init' state, the timer receives a reference to
        /// the target machine, and then transitions to the 'WaitForReq' state.
        /// </summary>
        private void InitOnEntry(Event e)
        {
            this.Target = (e as Config).Target;
            this.RaiseGotoStateEvent<WaitForReq>();
        }

        /// <summary>
        /// The timer waits in the 'WaitForReq' state for a request from the client.
        ///
        /// It responds with a 'CancelFailure' event on a 'CancelTimer' event.
        ///
        /// It transitions to the 'WaitForCancel' state on a 'StartTimerEvent' event.
        /// </summary>
        [OnEventGotoState(typeof(CancelTimerEvent), typeof(WaitForReq), nameof(CancelTimerAction))]
        [OnEventGotoState(typeof(StartTimerEvent), typeof(WaitForCancel))]
        private class WaitForReq : State { }

        private void CancelTimerAction()
        {
            this.SendEvent(this.Target, new CancelFailure());
        }

        /// <summary>
        /// In the 'WaitForCancel' state, any 'StartTimerEvent' event is dequeued and dropped without any
        /// action (indicated by the 'IgnoreEvents' declaration).
        /// </summary>
        [IgnoreEvents(typeof(StartTimerEvent))]
        [OnEventGotoState(typeof(CancelTimerEvent), typeof(WaitForReq), nameof(CancelTimerAction2))]
        [OnEventGotoState(typeof(DefaultEvent), typeof(WaitForReq), nameof(DefaultAction))]
        private class WaitForCancel : State { }

        private void DefaultAction()
        {
            this.SendEvent(this.Target, new TimeoutEvent());
        }

        /// <summary>
        /// The response to a 'CancelTimer' event is nondeterministic. During testing, Coyote will
        /// take control of this source of nondeterminism and explore different execution paths.
        ///
        /// Using this approach, we model the race condition between the arrival of a 'CancelTimer'
        /// event from the target and the elapse of the timer.
        /// </summary>
        private void CancelTimerAction2()
        {
            // A nondeterministic choice that is controlled by the Coyote runtime during testing.
            if (this.RandomBoolean())
            {
                this.SendEvent(this.Target, new CancelSuccess());
            }
            else
            {
                this.SendEvent(this.Target, new CancelFailure());
                this.SendEvent(this.Target, new TimeoutEvent());
            }
        }
    }
}
