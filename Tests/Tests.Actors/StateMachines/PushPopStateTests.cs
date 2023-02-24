// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Coyote.Actors.UnitTesting;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Actors.Tests.StateMachines
{
    public class PushPopStateTests : BaseActorTest
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
        public async Task TestPushStateTransition()
        {
            var configuration = this.GetConfiguration();
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
        public async Task TestPushStateTransitionAfterSend()
        {
            var configuration = this.GetConfiguration();
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
        public async Task TestPushStateTransitionAfterRaise()
        {
            var configuration = this.GetConfiguration();
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
        public async Task TestPushPopTransition()
        {
            var configuration = this.GetConfiguration();
            var test = new ActorTestKit<M4>(configuration: configuration);
            await test.StartActorAsync();
            await test.SendEventAsync(new E1());
            await test.SendEventAsync(new E2());
            test.AssertStateTransition("Init");
            test.Assert(test.ActorInstance.Count == 1, "Did not reach the Final state.");
        }

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
        public async Task TestPushStateInheritance()
        {
            var configuration = this.GetConfiguration();
            var test = new ActorTestKit<M5>(configuration: configuration);
            await test.StartActorAsync();
            await test.SendEventAsync(new E2());
            await test.SendEventAsync(new E1());
            await test.SendEventAsync(new E3());
            await test.SendEventAsync(new E4());
            test.AssertStateTransition("Final");
            string actual = $"{string.Join(", ", test.ActorInstance.Log)}.";
            Assert.Equal("E2 in state Final, E4 in state Final.", actual);
        }

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
                var configuration = this.GetConfiguration();
                var test = new ActorTestKit<M6>(configuration);
            }
            catch (Exception e)
            {
                string name = typeof(PushPopStateTests).FullName;
                actual = e.Message.Replace(name + "+", string.Empty);
            }

            Assert.Equal("M6(0) declared multiple handlers for event 'E2' in state 'M6+Init'.", actual);
        }

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
                var configuration = this.GetConfiguration();
                var test = new ActorTestKit<M7>(configuration);
            }
            catch (Exception e)
            {
                string name = typeof(PushPopStateTests).FullName;
                actual = e.Message.Replace(name + "+", string.Empty);
            }

            Assert.Equal("M7(0) declared multiple handlers for event 'E2' in state 'M7+Init'.", actual);
        }

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
                this.Assert(false, "HandleEvent did not happen!");
            }
        }

        [Fact(Timeout = 5000)]
        public void TestInheritedDuplicateHandler()
        {
            string actual = null;
            try
            {
                var configuration = this.GetConfiguration();
                var test = new ActorTestKit<M8>(configuration);
            }
            catch (Exception e)
            {
                string name = typeof(PushPopStateTests).FullName;
                actual = e.Message.Replace(name + "+", string.Empty);
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
        public async Task TestInheritedDuplicateDeferHandler()
        {
            var configuration = this.GetConfiguration();
            var test = new ActorTestKit<M9>(configuration: configuration);
            await test.StartActorAsync();
            await test.SendEventAsync(new E1());
            await test.SendEventAsync(new E2());
            await test.SendEventAsync(new E3());
            test.AssertStateTransition("Final");
        }
    }
}
