// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Actors.UnitTesting;
using Xunit;
using Xunit.Abstractions;
using SystemTasks = System.Threading.Tasks;

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

        private class M1 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private void InitOnEntry() => this.RaisePushStateEvent<Final>();

            private class Final : State
            {
            }
        }

        [Fact(Timeout = 5000)]
        public async SystemTasks.Task TestPushStateTransition()
        {
            var configuration = GetConfiguration();
            var test = new ActorTestKit<M1>(configuration: configuration);
            await test.StartActorAsync();
            test.AssertStateTransition("Final");
        }

        private class M2 : StateMachine
        {
            [Start]
            [OnEventPushState(typeof(E1), typeof(Final))]
            private class Init : State
            {
            }

            private class Final : State
            {
            }
        }

        [Fact(Timeout = 5000)]
        public async SystemTasks.Task TestPushStateTransitionAfterSend()
        {
            var configuration = GetConfiguration();
            var test = new ActorTestKit<M2>(configuration: configuration);
            await test.StartActorAsync();
            test.AssertStateTransition("Init");

            await test.SendEventAsync(new E1());
            test.AssertStateTransition("Final");
        }

        private class M3 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventPushState(typeof(E1), typeof(Final))]
            private class Init : State
            {
            }

            private void InitOnEntry() => this.RaiseEvent(new E1());

            private class Final : State
            {
            }
        }

        [Fact(Timeout = 5000)]
        public async SystemTasks.Task TestPushStateTransitionAfterRaise()
        {
            var configuration = GetConfiguration();
            var test = new ActorTestKit<M3>(configuration: configuration);
            await test.StartActorAsync();
            test.AssertStateTransition("Final");
        }

        private class M4 : StateMachine
        {
            public int Count;

            [Start]
            [OnEventPushState(typeof(E1), typeof(Final))]
            private class Init : State
            {
            }

            [OnEventDoAction(typeof(E2), nameof(Pop))]
            private class Final : State
            {
            }

            private void Pop()
            {
                this.Count++;
                this.RaisePopStateEvent();
            }
        }

        [Fact(Timeout = 5000)]
        public async SystemTasks.Task TestPushPopTransition()
        {
            var configuration = GetConfiguration();
            var test = new ActorTestKit<M4>(configuration: configuration);
            await test.StartActorAsync();
            await test.SendEventAsync(new E1());
            await test.SendEventAsync(new E2());
            test.AssertStateTransition("Init");
            test.Assert(test.ActorInstance.Count == 1, "Did not reach Final state");
        }

        /// <summary>
        /// Test that Defer, Ignore and DoAction can be inherited.
        /// </summary>
        private class M5 : StateMachine
        {
            public List<string> Log = new List<string>();

            [Start]
            [OnEventPushState(typeof(E1), typeof(Final))]
            [DeferEvents(typeof(E2))]
            [IgnoreEvents(typeof(E3))]
            [OnEventDoAction(typeof(E4), nameof(HandleEvent))]
            private class Init : State
            {
            }

            [OnEventDoAction(typeof(E2), nameof(HandleEvent))]
            private class Final : State
            {
            }

            private void HandleEvent(Event e)
            {
                this.Log.Add(string.Format("{0} in state {1}", e.GetType().Name, this.CurrentStateName));
            }
        }

        [Fact(Timeout = 5000)]
        public async SystemTasks.Task TestPushStateInheritance()
        {
            var configuration = GetConfiguration();
            var test = new ActorTestKit<M5>(configuration: configuration);
            await test.StartActorAsync();
            await test.SendEventAsync(new E2()); // should be deferred
            await test.SendEventAsync(new E1()); // push
            await test.SendEventAsync(new E3()); // ignored
            await test.SendEventAsync(new E4()); // inherited handler
            test.AssertStateTransition("Final");
            string actual = string.Join(", ", test.ActorInstance.Log);
            string expected = "E2 in state Final, E4 in state Final";
            Assert.Equal(expected, actual);
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
            string actual = null;
            try
            {
                var test = new ActorTestKit<M6>(GetConfiguration());
            }
            catch (Exception e)
            {
                string fullname = typeof(PushPopStateTests).FullName;
                actual = e.Message.Replace(fullname + "+", string.Empty);
            }

            Assert.Equal("M6(0) declared multiple handlers for event 'E2' in state 'M6+Init'.", actual);
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
            string actual = null;
            try
            {
                var test = new ActorTestKit<M7>(GetConfiguration());
            }
            catch (Exception e)
            {
                string fullname = typeof(PushPopStateTests).FullName;
                actual = e.Message.Replace(fullname + "+", string.Empty);
            }

            Assert.Equal("M7(0) declared multiple handlers for event 'E2' in state 'M7+Init'.", actual);
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
            string actual = null;
            try
            {
                var test = new ActorTestKit<M8>(GetConfiguration());
            }
            catch (Exception e)
            {
                string fullname = typeof(PushPopStateTests).FullName;
                actual = e.Message.Replace(fullname + "+", string.Empty);
            }

            Assert.Equal("M8(0) inherited multiple handlers for event 'E1' from state 'BaseState' in state 'M8+Init'.", actual);
        }

        private class M9 : StateMachine
        {
            public int Count;

            [Start]
            [DeferEvents(typeof(E1))]
            [IgnoreEvents(typeof(E2))]
            [OnEventGotoState(typeof(E3), typeof(Final))]
            [OnEventPushState(typeof(E4), typeof(Final))]
            private class Init : BaseState
            {
            }

            [OnEventDoAction(typeof(E1), nameof(HandleEvent))]
            private class Final : State
            {
            }

            private void HandleEvent()
            {
                this.Count++;
            }
        }

        [Fact(Timeout = 5000)]
        public async SystemTasks.Task TestInheritedDuplicateDeferHandler()
        {
            // inherited state can re-define a DeferEvent, IgnoreEvent or a Goto if it wants to, this is not an error.
            var configuration = GetConfiguration();
            var test = new ActorTestKit<M9>(configuration: configuration);
            await test.StartActorAsync();
            await test.SendEventAsync(new E1()); // should be deferred
            await test.SendEventAsync(new E2()); // should be ignored
            await test.SendEventAsync(new E3()); // should trigger goto, where deferred E1 can be handled.
            test.AssertStateTransition("Final");
        }
    }
}
