// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Tests.Common.Actors;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Production.Tests.Actors.StateMachines
{
    public class PushPopStateTests : BaseProductionTest
    {
        public PushPopStateTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private class E1 : Event
        {
        }

        private class E2 : Event
        {
        }

        private class E3 : Event
        {
        }

        private class E4 : Event
        {
        }

        private class M1 : TraceableStateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.Trace("InitOnEntry");
                this.RaisePushStateEvent<Final>();
            }

            [OnEntry(nameof(OnFinal))]
            private class Final : State
            {
            }

            private void OnFinal()
            {
                this.Trace("OnFinal");
                this.OnFinalEvent();
            }
        }

        [Fact(Timeout = 5000)]
        public void TestPushStateTransition()
        {
            this.Test(async (IActorRuntime runtime) =>
            {
                var op = new EventGroupList();
                runtime.CreateActor(typeof(M1), null, op);
                await this.GetResultAsync(op.Task);
                var actual = op.ToString();
                Assert.Equal("InitOnEntry, CurrentState=Final, OnFinal", actual);
            });
        }

        private class M2 : TraceableStateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventPushState(typeof(E1), typeof(Final))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.TraceOp.AddItem("InitOnEntry");
            }

            [OnEntry(nameof(OnFinal))]
            private class Final : State
            {
            }

            private void OnFinal()
            {
                this.TraceOp.AddItem("OnFinal");
                this.OnFinalEvent();
            }
        }

        [Fact(Timeout = 5000)]
        public void TestPushStateTransitionAfterSend()
        {
            this.Test(async (IActorRuntime runtime) =>
            {
                var op = new EventGroupList();
                var id = runtime.CreateActor(typeof(M2), null, op);
                runtime.SendEvent(id, new E1());
                await this.GetResultAsync(op.Task);
                var actual = op.ToString();
                Assert.Equal("InitOnEntry, CurrentState=Final, OnFinal", actual);
            });
        }

        private class M3 : TraceableStateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventPushState(typeof(E1), typeof(Final))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.Trace("InitOnEntry");
                this.Trace("RaiseEvent");
                this.RaiseEvent(new E1());
            }

            [OnEntry(nameof(OnFinal))]
            private class Final : State
            {
            }

            private void OnFinal()
            {
                this.Trace("OnFinal");
                this.OnFinalEvent();
            }
        }

        [Fact(Timeout = 5000)]
        public void TestPushStateTransitionAfterRaise()
        {
            this.Test(async (IActorRuntime runtime) =>
            {
                var op = new EventGroupList();
                var id = runtime.CreateActor(typeof(M3), null, op);
                await this.GetResultAsync(op.Task);
                var actual = op.ToString();
                Assert.Equal("InitOnEntry, RaiseEvent, CurrentState=Final, OnFinal", actual);
            });
        }

        private class M4 : TraceableStateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventPushState(typeof(E1), typeof(Final))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.Trace("InitOnEntry");
            }

            [OnEventDoAction(typeof(E2), nameof(Pop))]
            private class Final : State
            {
            }

            private void Pop()
            {
                this.Trace("Pop");
                this.RaisePopStateEvent();
                this.OnFinalEvent();
            }
        }

        [Fact(Timeout = 5000)]
        public void TestPushPopTransition()
        {
            this.Test(async (IActorRuntime runtime) =>
            {
                var op = new EventGroupList();
                var id = runtime.CreateActor(typeof(M4), null, op);
                runtime.SendEvent(id, new E1());
                runtime.SendEvent(id, new E2());
                await this.GetResultAsync(op.Task);
                var actual = op.ToString();
                // the CurrentState=Init that happens as a result of Pop is timing sensitive and
                // there is no state machine call back to tell us when this happens, so we just use
                // a Contains test here to deal with that non-determinism.
                Assert.Contains("InitOnEntry, CurrentState=Final, Pop", actual);
            });
        }

        /// <summary>
        /// Test that Defer, Ignore and DoAction can be inherited.
        /// </summary>
        private class M5 : TraceableStateMachine
        {
            [Start]
            [OnEventPushState(typeof(E1), typeof(Final))]
            [DeferEvents(typeof(E2))]
            [IgnoreEvents(typeof(E3))]
            [OnEventDoAction(typeof(E4), nameof(FinalEvent))]
            private class Init : State
            {
            }

            [OnEventDoAction(typeof(E2), nameof(HandleEvent))]
            private class Final : State
            {
            }

            private void HandleEvent()
            {
                this.Trace("HandleEvent");
            }

            private void FinalEvent()
            {
                this.Trace("FinalEvent");
                this.OnFinalEvent();
            }
        }

        [Fact(Timeout = 5000)]
        public void TestPushStateInheritance()
        {
            this.Test(async (IActorRuntime runtime) =>
            {
                var op = new EventGroupList();
                var id = runtime.CreateActor(typeof(M5), null, op);
                runtime.SendEvent(id, new E2()); // should be deferred
                runtime.SendEvent(id, new E1()); // push
                runtime.SendEvent(id, new E3()); // ignored
                runtime.SendEvent(id, new E4()); // inherited handler
                await this.GetResultAsync(op.Task);
                var actual = op.ToString();
                Assert.Equal("CurrentState=Final, HandleEvent, FinalEvent", actual);
            });
        }

        /// <summary>
        /// Test you cannot have duplicated handlers for the same event.
        /// </summary>
        private class M6 : StateMachine
        {
            public List<string> Log = new List<string>();

            [Start]
            [DeferEvents(typeof(E2))]
            [IgnoreEvents(typeof(E2))]
            private class Init : State
            {
            }
        }

        [Fact(Timeout = 5000)]
        public void TestDuplicateHandler()
        {
            this.TestWithError((IActorRuntime runtime) =>
            {
                runtime.CreateActor(typeof(M6));
            },
            expectedError: "M6(0) declared multiple handlers for event 'E2' in state 'M6+Init'.");
        }

        /// <summary>
        /// Test you cannot have duplicated handlers for the same event.
        /// </summary>
        private class M7 : StateMachine
        {
            public List<string> Log = new List<string>();

            [Start]
            [DeferEvents(typeof(E2))]
            [OnEventGotoState(typeof(E2), typeof(Final))]
            private class Init : State
            {
            }

            private class Final : State
            {
            }
        }

        [Fact(Timeout = 5000)]
        public void TestDuplicateHandler2()
        {
            this.TestWithError((IActorRuntime runtime) =>
            {
                runtime.CreateActor(typeof(M7));
            },
            expectedError: "M7(0) declared multiple handlers for event 'E2' in state 'M7+Init'.");
        }

        /// <summary>
        /// Test if you have duplicate handlers for the same event on inherited State class then the
        /// most derrived handler wins.
        /// </summary>
        [DeferEvents(typeof(E1))]
        [IgnoreEvents(typeof(E2))]
        [OnEventGotoState(typeof(E3), typeof(StateMachine.State))]
        [OnEventPushState(typeof(E4), typeof(StateMachine.State))]
        private class BaseState : StateMachine.State
        {
        }

        private class M8 : StateMachine
        {
            public List<string> Log = new List<string>();

            [Start]
            [OnEventGotoState(typeof(E1), typeof(Final))]
            private class Init : BaseState
            {
            }

            [OnEventDoAction(typeof(E1), nameof(HandleEvent))]
            private class Final : State
            {
            }

            private void HandleEvent()
            {
                this.Assert(false, "HandleEvent not happen!");
            }
        }

        [Fact(Timeout = 5000)]
        public void TestInheritedDuplicateHandler()
        {
            this.TestWithError((IActorRuntime runtime) =>
            {
                runtime.CreateActor(typeof(M8));
            },
            expectedError: "M8(0) inherited multiple handlers for event 'E1' from state 'BaseState' in state 'M8+Init'.");
        }

        private class M9 : TraceableStateMachine
        {
            [Start]
            [DeferEvents(typeof(E1))]
            [IgnoreEvents(typeof(E2))]
            [OnEventGotoState(typeof(E3), typeof(Final))]
            private class Init : BaseState
            {
            }

            [OnEventDoAction(typeof(E1), nameof(HandleEvent))]
            private class Final : State
            {
            }

            private void HandleEvent(Event e)
            {
                this.Trace("HandleEvent:{0}", e.GetType().Name);
                this.OnFinalEvent();
            }
        }

        [Fact(Timeout = 5000)]
        public void TestInheritedDuplicateDeferHandler()
        {
            this.Test(async (IActorRuntime runtime) =>
            {
                var op = new EventGroupList();
                var id = runtime.CreateActor(typeof(M9), null, op);
                runtime.SendEvent(id, new E1()); // should be deferred
                runtime.SendEvent(id, new E2()); // should be ignored
                runtime.SendEvent(id, new E3()); // should trigger goto, where deferred E1 can be handled.
                await this.GetResultAsync(op.Task);
                var actual = op.ToString();
                Assert.Equal("CurrentState=Final, HandleEvent:E1", actual);
            });
        }
    }
}
