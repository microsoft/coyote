// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.Tests.Common.Actors;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests.Actors
{
    public class WildCardEventTests : BaseTest
    {
        public WildCardEventTests(ITestOutputHelper output)
            : base(output)
        {
        }

        internal class LogEvent : Event
        {
            public List<string> Result = new List<string>();

            public void WriteLine(string msg, params object[] args)
            {
                this.Result.Add(string.Format(msg, args));
            }

            public override string ToString()
            {
                return string.Join(",", this.Result);
            }
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

        [OnEventDoAction(typeof(WildCardEvent), nameof(Foo))]
        private class Aa : Actor
        {
            private LogEvent Config;

            internal override Task InitializeAsync(Event initialEvent)
            {
                this.Config = (LogEvent)initialEvent;
                return base.InitializeAsync(initialEvent);
            }

            private void Foo(Event e)
            {
                if (e is E2)
                {
                    this.Config.WriteLine("E2");
                }
                else if (e is UnitEvent)
                {
                    this.Config.WriteLine("UnitEvent");
                }
                else if (e is E1)
                {
                    this.Config.WriteLine("E1");
                }
            }

            public static void RunTest(IActorRuntime r, LogEvent config)
            {
                var a = r.CreateActor(typeof(Aa), config);
                r.SendEvent(a, new E2());
                r.SendEvent(a, UnitEvent.Instance);
                r.SendEvent(a, new E1());
            }
        }

        [Fact(Timeout = 5000)]
        public void TestWildCardEventInActor()
        {
            var config = new LogEvent();
            this.Test(r =>
            {
                Aa.RunTest(r, config);
            });

            string actual = config.ToString();
            Assert.True(actual == "E2,UnitEvent,E1");
        }

        private class Ma : StateMachine
        {
            private LogEvent Config;

            protected override Task OnInitializeAsync(Event initialEvent)
            {
                this.Config = (LogEvent)initialEvent;
                return base.OnInitializeAsync(initialEvent);
            }

            [Start]
            [OnEventDoAction(typeof(UnitEvent), nameof(Foo))]
            [OnEventGotoState(typeof(E1), typeof(S1))]
            [DeferEvents(typeof(WildCardEvent))]
            private class S0 : State
            {
            }

            [OnEntry(nameof(OnS1))]
            [OnEventDoAction(typeof(E2), nameof(Bar))]
            private class S1 : State
            {
            }

            private void OnS1()
            {
                this.Config.WriteLine("Enter S1");
            }

            private void Foo()
            {
                this.Config.WriteLine("Foo");
            }

            private void Bar()
            {
                this.Config.WriteLine("Bar");
            }

            public static void RunTest(IActorRuntime r, LogEvent config)
            {
                var a = r.CreateActor(typeof(Ma), config);
                r.SendEvent(a, new E2());
                r.SendEvent(a, UnitEvent.Instance);
                r.SendEvent(a, new E1());
            }
        }

        [Fact(Timeout = 5000)]
        public void TestWildCardEventInStateMachine()
        {
            var config = new LogEvent();
            this.Test(r =>
            {
                Ma.RunTest(r, config);
            });
            string actual = config.ToString();
            Assert.True(actual == "Foo,Enter S1,Bar");
        }

        /// <summary>
        /// Test that we can "do something specific for E1, but goto state for everything else".
        /// In otherwords that WildCardEvent does not take precedence over a more specific
        /// even typed action if defined on the same state.
        /// </summary>
        internal class W : StateMachine
        {
            private LogEvent Config;

            [Start]
            [OnEntry(nameof(OnInitEntry))]
            [OnEventDoAction(typeof(E1), nameof(HandleE1))]
            [OnEventGotoState(typeof(WildCardEvent), typeof(CatchAll))]
            public class Init : State
            {
            }

            public void OnInitEntry(Event initialEvent)
            {
                this.Config = (LogEvent)initialEvent;
            }

            private void HandleE1()
            {
                this.Config.WriteLine("handle E1");
            }

            [OnEntry(nameof(OnCatchAll))]
            public class CatchAll : State
            {
            }

            private void OnCatchAll(Event e)
            {
                this.Config.WriteLine("catch " + e.GetType().Name);
            }

            public static void RunTest(IActorRuntime r, LogEvent config)
            {
                var actor = r.CreateActor(typeof(W), config);
                r.SendEvent(actor, new E1());
                r.SendEvent(actor, new E2());
            }
        }

        [Fact(Timeout = 5000)]
        public void TestWildGotoInStateMachine()
        {
            var config = new LogEvent();
            this.Test(r =>
            {
                W.RunTest(r, config);
            });

            string actual = config.ToString();
            Assert.True(actual == "handle E1,catch E2");
        }

        /// <summary>
        /// Test that wildcard can be overridden by push
        /// </summary>
        internal class X : StateMachine
        {
            private LogEvent Config;

            [Start]
            [OnEntry(nameof(OnInit))]
            [OnEventDoAction(typeof(E1), nameof(HandleEvent))]
            [OnEventDoAction(typeof(WildCardEvent), nameof(CatchAll))]

            public class Init : State
            {
            }

            public void OnInit(Event initialEvent)
            {
                this.Config = (LogEvent)initialEvent;
            }

            private void HandleEvent(Event e)
            {
                this.Config.WriteLine("handle " + e.GetType().Name);
            }

            private void CatchAll(Event e)
            {
                this.Config.WriteLine("catch " + e.GetType().Name);
                if (e.GetType() == typeof(E2))
                {
                    // test specific handler for E3 takes over from wildcard
                    this.RaisePushStateEvent(typeof(Ready));
                }
                else if (e.GetType() == typeof(E4))
                {
                    // test wild card is re-instated for E3.
                    this.RaisePopStateEvent();
                }
            }

            [OnEventDoAction(typeof(E3), nameof(HandleEvent))]
            public class Ready : State
            {
            }

            internal static void RunTest(IActorRuntime runtime, LogEvent config)
            {
                var actor = runtime.CreateActor(typeof(X), config);
                runtime.SendEvent(actor, new E1()); // handle
                runtime.SendEvent(actor, new E3()); // catch
                runtime.SendEvent(actor, new E2()); // catch & push to ready
                runtime.SendEvent(actor, new E3()); // handled by Ready (overriding wildcard)
                runtime.SendEvent(actor, new E4()); // catch, wildcard still in effect
                runtime.SendEvent(actor, new E3()); // catch, wildcard still in effect
            }
        }

        [Fact(Timeout = 5000)]
        public void TestWildcardPushInStateMachine()
        {
            var config = new LogEvent();
            this.Test(r =>
            {
                X.RunTest(r, config);
            });

            string actual = config.ToString();
            Assert.True(actual == "handle E1,catch E3,catch E2,handle E3,catch E4,catch E3");
        }

        /// <summary>
        /// Test that wildcard can override inherited action.
        /// </summary>
        internal class X2 : StateMachine
        {
            private LogEvent Config;

            [Start]
            [OnEntry(nameof(OnInit))]
            [OnEventDoAction(typeof(E1), nameof(HandleE1))]
            public class Init : State
            {
            }

            public void OnInit(Event initialEvent)
            {
                this.Config = (LogEvent)initialEvent;
            }

            private void HandleE1()
            {
                this.Config.WriteLine("Handling E1 in State {0}", this.CurrentStateName);
                this.RaisePushStateEvent<Active>();
            }

            [OnEntry(nameof(OnActive))]
            [OnEventDoAction(typeof(E2), nameof(HandleE2))]
            [OnEventDoAction(typeof(WildCardEvent), nameof(CatchAll))]
            public class Active : State
            {
            }

            private void OnActive()
            {
                this.Config.WriteLine("Active");
            }

            private void HandleE2()
            {
                this.Config.WriteLine("Handling E2 in State {0}", this.CurrentStateName);
            }

            private void CatchAll(Event e)
            {
                this.Config.WriteLine("Catch " + e.GetType().Name);
            }

            internal static void RunTest(IActorRuntime runtime, LogEvent config)
            {
                var actor = runtime.CreateActor(typeof(X2), config);
                runtime.SendEvent(actor, new E1()); // handle E1 & push active
                runtime.SendEvent(actor, new E1()); // catch E1, by wildcard
                runtime.SendEvent(actor, new E2()); // handle E2, specific handler wins
            }
        }

        [Fact(Timeout = 5000)]
        public void TestWildcardOverrideActionStateMachine()
        {
            var config = new LogEvent();
            this.Test(r =>
            {
                X2.RunTest(r, config);
            });

            string actual = config.ToString();
            Assert.True(actual == "Handling E1 in State Init,Active,Catch E1,Handling E2 in State Active");
        }

        /// <summary>
        /// Test that wildcard can override deferred event action using a pushed state.
        /// </summary>
        internal class X3 : StateMachine
        {
            private LogEvent Config;

            [Start]
            [OnEntry(nameof(OnInit))]
            [DeferEvents(typeof(E1))]
            [OnEventPushState(typeof(E2), typeof(Active))]
            public class Init : State
            {
            }

            public void OnInit(Event initialEvent)
            {
                this.Config = (LogEvent)initialEvent;
                this.Config.WriteLine("Init");
            }

            [OnEntry(nameof(OnActive))]
            [OnEventDoAction(typeof(WildCardEvent), nameof(CatchAll))]
            public class Active : State
            {
            }

            private void OnActive()
            {
                this.Config.WriteLine("Active");
            }

            private void CatchAll(Event e)
            {
                this.Config.WriteLine("Catch {0} in State {1}", e.GetType().Name, this.CurrentStateName);
            }

            internal static void RunTest(IActorRuntime runtime, LogEvent config)
            {
                var actor = runtime.CreateActor(typeof(X3), config);
                runtime.SendEvent(actor, new E1()); // deferred
                runtime.SendEvent(actor, new E2()); // push state Active, and allow handling of deferred event.
            }
        }

        [Fact(Timeout = 5000)]
        public void TestWildcardOverrideDeferStateMachine()
        {
            var config = new LogEvent();
            this.Test(r =>
            {
                X3.RunTest(r, config);
            });

            string actual = config.ToString();
            Assert.True(actual == "Init,Active,Catch E1 in State Active");
        }

        /// <summary>
        /// Test that wildcard can override ignored event action using a pushed state.
        /// </summary>
        internal class X4 : StateMachine
        {
            private LogEvent Config;

            [Start]
            [OnEntry(nameof(OnInit))]
            [IgnoreEvents(typeof(E1))]
            [OnEventPushState(typeof(E2), typeof(Active))]
            public class Init : State
            {
            }

            public void OnInit(Event initialEvent)
            {
                this.Config = (LogEvent)initialEvent;
                this.Config.WriteLine("Init");
            }

            [OnEntry(nameof(OnActive))]
            [OnEventDoAction(typeof(WildCardEvent), nameof(CatchAll))]
            public class Active : State
            {
            }

            private void OnActive()
            {
                this.Config.WriteLine("Active");
            }

            private void CatchAll(Event e)
            {
                this.Config.WriteLine("Catch {0} in State {1}", e.GetType().Name, this.CurrentStateName);
            }

            internal static void RunTest(IActorRuntime runtime, LogEvent config)
            {
                var actor = runtime.CreateActor(typeof(X3), config);
                runtime.SendEvent(actor, new E1()); // ignored (and therefore dropped)
                runtime.SendEvent(actor, new E2()); // push state Active.
                runtime.SendEvent(actor, new E1()); // Catch by wildcard (overriding inherited IgnoreEvents)
            }
        }

        [Fact(Timeout = 5000)]
        public void TestWildcardOverrideIgnoreStateMachine()
        {
            var config = new LogEvent();
            this.Test(r =>
            {
                X3.RunTest(r, config);
            });

            string actual = config.ToString();
            Assert.True(actual == "Init,Active,Catch E1 in State Active");
        }
    }
}
