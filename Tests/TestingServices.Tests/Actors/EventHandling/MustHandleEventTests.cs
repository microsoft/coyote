// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Actors;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests.Actors
{
    public class MustHandleEventTests : BaseTest
    {
        public MustHandleEventTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private class MustHandleEvent : Event
        {
        }

        private class MoveEvent : Event
        {
        }

        private class A1 : Actor
        {
            protected override Task OnInitializeAsync(Event initialEvent)
            {
                this.IgnoreEvent(typeof(MustHandleEvent));
                return Task.CompletedTask;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestMustHandleEventNotTriggeredInActor()
        {
            this.Test(r =>
            {
                var m = r.CreateActor(typeof(A1));
                r.SendEvent(m, new MustHandleEvent(), options: new SendOptions(mustHandle: true));
                r.SendEvent(m, HaltEvent.Instance);
            },
            configuration: Configuration.Create().WithNumberOfIterations(100));
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
        public void TestMustHandleEventNotTriggeredInStateMachine()
        {
            this.Test(r =>
            {
                var m = r.CreateActor(typeof(M1));
                r.SendEvent(m, new MustHandleEvent(), options: new SendOptions(mustHandle: true));
                r.SendEvent(m, HaltEvent.Instance);
            },
            configuration: Configuration.Create().WithNumberOfIterations(100));
        }

        private class A2 : Actor
        {
            protected override Task OnInitializeAsync(Event initialEvent)
            {
                this.DeferEvent(typeof(MustHandleEvent));
                return Task.CompletedTask;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestMustHandleDeferredEventInActor()
        {
            this.TestWithError(r =>
            {
                var m = r.CreateActor(typeof(A2));
                r.SendEvent(m, new MustHandleEvent(), options: new SendOptions(mustHandle: true));
                r.SendEvent(m, HaltEvent.Instance);
            },
            configuration: Configuration.Create().WithNumberOfIterations(1),
            expectedError: "A2() halted before dequeueing must-handle event 'MustHandleEvent'.",
            replay: true);
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
        public void TestMustHandleDeferredEventInStateMachine()
        {
            this.TestWithError(r =>
            {
                var m = r.CreateActor(typeof(M2));
                r.SendEvent(m, new MustHandleEvent(), options: new SendOptions(mustHandle: true));
                r.SendEvent(m, HaltEvent.Instance);
            },
            configuration: Configuration.Create().WithNumberOfIterations(1),
            expectedError: "M2() halted before dequeueing must-handle event 'MustHandleEvent'.",
            replay: true);
        }

        private class A3 : Actor
        {
            protected override Task OnInitializeAsync(Event initialEvent)
            {
                this.DeferEvent(typeof(MustHandleEvent));
                this.SendEvent(this.Id, HaltEvent.Instance);
                return Task.CompletedTask;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestMustHandleEventAfterHaltInActor()
        {
            this.TestWithError(r =>
            {
                var m = r.CreateActor(typeof(A3));
                r.SendEvent(m, new MustHandleEvent(), options: new SendOptions(mustHandle: true));
            },
            configuration: Configuration.Create().WithNumberOfIterations(500),
            expectedErrors: new string[]
                {
                    "A must-handle event 'MustHandleEvent' was sent to A3() which has halted.",
                    "A3() halted before dequeueing must-handle event 'MustHandleEvent'."
                },
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

            private Transition InitOnEntry() => this.Halt();
        }

        [Fact(Timeout = 5000)]
        public void TestMustHandleEventAfterHaltInStateMachine()
        {
            this.TestWithError(r =>
            {
                var m = r.CreateActor(typeof(M3));
                r.SendEvent(m, new MustHandleEvent(), options: new SendOptions(mustHandle: true));
            },
            configuration: Configuration.Create().WithNumberOfIterations(500),
            expectedErrors: new string[]
                {
                    "A must-handle event 'MustHandleEvent' was sent to M3() which has halted.",
                    "M3() halted before dequeueing must-handle event 'MustHandleEvent'."
                },
            replay: true);
        }

        private class A4 : Actor
        {
            protected override Task OnInitializeAsync(Event initialEvent)
            {
                this.DeferEvent(typeof(MustHandleEvent));
                this.SendEvent(this.Id, HaltEvent.Instance);
                return Task.CompletedTask;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestMustHandleEventAfterSendingHaltInActor()
        {
            this.TestWithError(r =>
            {
                var m = r.CreateActor(typeof(A4));
                r.SendEvent(m, new MustHandleEvent(), options: new SendOptions(mustHandle: true));
            },
            configuration: Configuration.Create().WithNumberOfIterations(500),
            expectedErrors: new string[]
                {
                    "A must-handle event 'MustHandleEvent' was sent to A4() which has halted.",
                    "A4() halted before dequeueing must-handle event 'MustHandleEvent'."
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
                this.SendEvent(this.Id, HaltEvent.Instance);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestMustHandleEventAfterSendingHaltInStateMachine()
        {
            this.TestWithError(r =>
            {
                var m = r.CreateActor(typeof(M4));
                r.SendEvent(m, new MustHandleEvent(), options: new SendOptions(mustHandle: true));
            },
            configuration: Configuration.Create().WithNumberOfIterations(500),
            expectedErrors: new string[]
                {
                    "A must-handle event 'MustHandleEvent' was sent to M4() which has halted.",
                    "M4() halted before dequeueing must-handle event 'MustHandleEvent'."
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
                r.SendEvent(m, HaltEvent.Instance);
                r.SendEvent(m, new MustHandleEvent(), options: new SendOptions(mustHandle: true));
                r.SendEvent(m, new MoveEvent());
            },
            configuration: Configuration.Create().WithNumberOfIterations(1),
            expectedError: "M5() halted before dequeueing must-handle event 'MustHandleEvent'.",
            replay: true);
        }
    }
}
