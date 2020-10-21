// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Coyote.Tests.Common.Events;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Actors.SystematicTesting.Tests
{
    public class PushStateTransitionTests : BaseSystematicActorTest
    {
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

        public PushStateTransitionTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private class M1 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private void InitOnEntry() => this.RaisePushStateEvent<Done>();

            [OnEntry(nameof(EntryDone))]
            private class Done : State
            {
            }

            private void EntryDone()
            {
                // This assert is reachable.
                this.Assert(false, "Reached test assertion.");
            }
        }

        [Fact(Timeout = 5000)]
        public void TestPushStateTransition()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M1));
            },
            expectedError: "Reached test assertion.",
            replay: true);
        }

        private class M2 : StateMachine
        {
            private int cnt = 0;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [IgnoreEvents(typeof(UnitEvent))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.Assert(this.cnt == 0); // called once
                this.cnt++;
                this.RaisePushStateEvent<Done>();
            }

            [OnEntry(nameof(EntryDone))]
            private class Done : State
            {
            }

            private void EntryDone() => this.RaisePopStateEvent();
        }

        [Fact(Timeout = 5000)]
        public void TestPushAndPopStateTransitions()
        {
            this.Test(r =>
            {
                var m = r.CreateActor(typeof(M2));
                r.SendEvent(m, UnitEvent.Instance);
            });
        }

        private class M3 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnExit(nameof(ExitInit))]
            private class Init : State
            {
            }

            private void InitOnEntry() => this.RaisePushStateEvent<Done>();

            private void ExitInit()
            {
                // This assert is not reachable.
                this.Assert(false, "Reached test assertion.");
            }

            private class Done : State
            {
            }
        }

        [Fact(Timeout = 5000)]
        public void TestPushStateTransitionWithOnExitSkipped()
        {
            this.Test(r =>
            {
                r.CreateActor(typeof(M3));
            });
        }

        private class M4a : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private void InitOnEntry() => this.RaisePushStateEvent<M4b.Done>();

            private class Done : State
            {
            }
        }

        private class M4b : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
            }

            internal class Done : State
            {
            }
        }

        [Fact(Timeout = 5000)]
        public void TestPushTransitionToNonExistingState()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M4a));
            },
            expectedError: "M4a() is trying to transition to non-existing state 'Done'.",
            replay: true);
        }

        private class M5a : StateMachine
        {
            [Start]
            [OnEventDoAction(typeof(E1), nameof(Foo))]
            [OnEventPushState(typeof(E2), typeof(S1))]
            private class S0 : State
            {
            }

            [OnEventDoAction(typeof(E3), nameof(Bar))]
            private class S1 : State
            {
            }

            private void Foo()
            {
            }

            private void Bar() => this.RaisePopStateEvent();

            internal static void RunTest(IActorRuntime runtime)
            {
                var a = runtime.CreateActor(typeof(M5a));
                runtime.SendEvent(a, new E2()); // Push(S1)
                runtime.SendEvent(a, new E1()); // Execute foo without popping
                runtime.SendEvent(a, new E3()); // Can handle it because A is still in S1
            }
        }

        [Fact(Timeout = 5000)]
        public void TestPushStateTransitionViaEvent()
        {
            this.Test(r =>
            {
                M5a.RunTest(r);
            });
        }

        private class M6 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnExit(nameof(ExitMethod))]
            private class Init : State
            {
            }

            private void InitOnEntry() => this.RaiseGotoStateEvent<Done>();

            private void ExitMethod() => this.RaisePushStateEvent<Done>();

            private class Done : State
            {
            }
        }

        [Fact(Timeout = 5000)]
        public void TestPushStateTransitionOnExit()
        {
            var expectedError = "M6() has performed a 'PushState' transition from an OnExit action.";
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M6));
            },
            expectedError: expectedError,
            replay: true);
        }

        internal class LogEvent : Event
        {
            public List<string> Log = new List<string>();

            public void WriteLine(string msg, params object[] args)
            {
                this.Log.Add(string.Format(msg, args));
            }
        }

        /// <summary>
        /// Test that GotoState transitions are not inherited by PushState operations.
        /// </summary>
        private class M7 : StateMachine
        {
            private LogEvent Log;

            [Start]
            [OnEntry(nameof(OnInit))]
            [OnEventDoAction(typeof(E1), nameof(HandleE1))]
            [OnEventGotoState(typeof(E2), typeof(Bad))]
            public class Init : State
            {
            }

            private void HandleE1()
            {
                this.Log.WriteLine(string.Format("Handling E1 in state {0}", this.CurrentStateName));
                this.RaisePushStateEvent<Ready>();
            }

            private void OnInit(Event e)
            {
                this.Log = (LogEvent)e;
            }

            [OnEntry(nameof(OnReady))]
            [OnExit(nameof(OnReadyExit))]
            [OnEventPushState(typeof(E1), typeof(Active))]
            [OnEventDoAction(typeof(E3), nameof(HandleE3))]
            public class Ready : State
            {
            }

            private void OnReady()
            {
                this.Log.WriteLine("Entering Ready state");
            }

            private void OnReadyExit()
            {
                this.Log.WriteLine("Exiting Ready state");
            }

            [OnEntry(nameof(OnActive))]
            [OnExit(nameof(OnActiveExit))]
            public class Active : State
            {
            }

            private void OnActive()
            {
                this.Log.WriteLine("Entering Active state");
            }

            private void OnActiveExit()
            {
                this.Log.WriteLine("Exiting Active state");
            }

            private void HandleE3()
            {
                this.Log.WriteLine("Handling E3 in State {0}", this.CurrentState);
            }

            [OnEntry(nameof(OnBad))]
            public class Bad : State
            {
            }

            private void OnBad()
            {
                this.Log.WriteLine("Entering Bad state");
            }

            protected override Task OnEventUnhandledAsync(Event e, string state)
            {
                this.Log.WriteLine("Unhandled event {0} in state {1}", e.GetType().Name, state);
                return base.OnEventUnhandledAsync(e, state);
            }

            public static void RunTest(IActorRuntime runtime, LogEvent initEvent)
            {
                var actor = runtime.CreateActor(typeof(M7), initEvent);
                runtime.SendEvent(actor, new E1()); // should be handled by Init state, and trigger push to Ready
                runtime.SendEvent(actor, new E1()); // should be handled by Ready with OnEventPushState to Active
                runtime.SendEvent(actor, new E2()); // Now OnEventGotoState(E2) should not be inherited so this should pop us back to the Init state.
                runtime.SendEvent(actor, new E3()); // just to prove we are no longer in the Active state, this should raise an unhandled event error.
            }
        }

        [Fact(Timeout = 5000)]
        public void TestPushStateNotInheritGoto()
        {
            string expectedError = "M7() received event 'E3' that cannot be handled.";
            var log = new LogEvent();
            this.TestWithError(r =>
            {
                M7.RunTest(r, log);
            },
            expectedError: expectedError);

            string actual = string.Join(", ", log.Log);
            Assert.Equal(@"Handling E1 in state Init, Entering Ready state, Entering Active state, Exiting Active state, Exiting Ready state, Entering Bad state, Unhandled event E3 in state Bad", actual);
        }

        /// <summary>
        /// Test that PushState transitions are not inherited by PushState operations, and therefore
        /// the event in question will cause the pushed state to pop before handling the event again.
        /// </summary>
        private class M8 : StateMachine
        {
            private LogEvent Log;

            [Start]
            [OnEntry(nameof(OnInit))]
            [OnEventPushState(typeof(E1), typeof(Ready))]
            public class Init : State
            {
            }

            private void OnInit(Event e)
            {
                this.Log = (LogEvent)e;
            }

            [OnEntry(nameof(OnReady))]
            [OnExit(nameof(OnReadyExit))]
            public class Ready : State
            {
            }

            private void OnReady()
            {
                this.Log.WriteLine("Entering Ready state");
            }

            private void OnReadyExit()
            {
                this.Log.WriteLine("Exiting Ready state");
            }

            protected override Task OnEventUnhandledAsync(Event e, string state)
            {
                this.Log.WriteLine("Unhandled event {0} in state {1}", e.GetType().Name, state);
                return base.OnEventUnhandledAsync(e, state);
            }

            public static void RunTest(IActorRuntime runtime, LogEvent initEvent)
            {
                var actor = runtime.CreateActor(typeof(M8), initEvent);
                runtime.SendEvent(actor, new E1()); // should be handled by Init state, and trigger push to Ready
                runtime.SendEvent(actor, new E1()); // should pop Active and go back to Init where it will be handled.
            }
        }

        [Fact(Timeout = 5000)]
        public void TestPushStateNotInheritPush()
        {
            var log = new LogEvent();
            this.Test(r =>
            {
                M8.RunTest(r, log);
            });

            string actual = string.Join(", ", log.Log);
            Assert.Equal(@"Entering Ready state, Exiting Ready state, Entering Ready state", actual);
        }
    }
}
