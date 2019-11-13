// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Actors;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests.Actors.StateMachines
{
    public class MustHandleEventTest : BaseTest
    {
        public MustHandleEventTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class MustHandleEvent : Event
        {
        }

        private class MoveEvent : Event
        {
        }

        private class M1 : StateMachine
        {
            [Start]
            [IgnoreEvents(typeof(MustHandleEvent))]
            private class Init : State
            {
            }
        }

        [Fact(Timeout = 5000)]
        public void TestMustHandleEventNotTriggered()
        {
            this.Test(r =>
            {
                var m = r.CreateActor(typeof(M1));
                r.SendEvent(m, new MustHandleEvent(), options: new SendOptions(mustHandle: true));
                r.SendEvent(m, new HaltEvent());
            },
            configuration: Configuration.Create().WithNumberOfIterations(100));
        }

        private class M2 : StateMachine
        {
            [Start]
            [DeferEvents(typeof(MustHandleEvent))]
            private class Init : State
            {
            }
        }

        [Fact(Timeout = 5000)]
        public void TestMustHandleDeferredEvent()
        {
            this.TestWithError(r =>
            {
                var m = r.CreateActor(typeof(M2));
                r.SendEvent(m, new MustHandleEvent(), options: new SendOptions(mustHandle: true));
                r.SendEvent(m, new HaltEvent());
            },
            configuration: Configuration.Create().WithNumberOfIterations(1),
            expectedError: "'M2()' halted before dequeueing must-handle event 'MustHandleEvent'.",
            replay: true);
        }

        private class M3 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [DeferEvents(typeof(MustHandleEvent))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.RaiseEvent(new HaltEvent());
            }
        }

        [Fact(Timeout = 5000)]
        public void TestMustHandleEventAfterRaisingHalt()
        {
            this.TestWithError(r =>
            {
                var m = r.CreateActor(typeof(M3));
                r.SendEvent(m, new MustHandleEvent(), options: new SendOptions(mustHandle: true));
            },
            configuration: Configuration.Create().WithNumberOfIterations(500),
            expectedErrors: new string[]
                {
                    "A must-handle event 'MustHandleEvent' was sent to 'M3()' which has halted.",
                    "'M3()' halted before dequeueing must-handle event 'MustHandleEvent'."
                },
            replay: true);
        }

        private class M4 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [DeferEvents(typeof(MustHandleEvent))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.SendEvent(this.Id, new HaltEvent());
            }
        }

        [Fact(Timeout = 5000)]
        public void TestMustHandleEventAfterSendingHalt()
        {
            this.TestWithError(r =>
            {
                var m = r.CreateActor(typeof(M4));
                r.SendEvent(m, new MustHandleEvent(), options: new SendOptions(mustHandle: true));
            },
            configuration: Configuration.Create().WithNumberOfIterations(500),
            expectedErrors: new string[]
                {
                    "A must-handle event 'MustHandleEvent' was sent to 'M4()' which has halted.",
                    "'M4()' halted before dequeueing must-handle event 'MustHandleEvent'."
                },
            replay: true);
        }

        private class M5 : StateMachine
        {
            [Start]
            [DeferEvents(typeof(MustHandleEvent), typeof(HaltEvent))]
            [OnEventGotoState(typeof(MoveEvent), typeof(Next))]
            private class Init : State
            {
            }

            private class Next : State
            {
            }
        }

        [Fact(Timeout = 5000)]
        public void TestMustHandleDeferredEventAfterStateTransition()
        {
            this.TestWithError(r =>
            {
                var m = r.CreateActor(typeof(M5));
                r.SendEvent(m, new HaltEvent());
                r.SendEvent(m, new MustHandleEvent(), options: new SendOptions(mustHandle: true));
                r.SendEvent(m, new MoveEvent());
            },
            configuration: Configuration.Create().WithNumberOfIterations(1),
            expectedError: "'M5()' halted before dequeueing must-handle event 'MustHandleEvent'.",
            replay: true);
        }
    }
}
