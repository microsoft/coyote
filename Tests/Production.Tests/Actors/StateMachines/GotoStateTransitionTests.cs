// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Actors.UnitTesting;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Production.Tests.Actors.StateMachines
{
    public class GotoStateTransitionTests : BaseProductionTest
    {
        public GotoStateTransitionTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private class Message : Event
        {
        }

        private class M1 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private void InitOnEntry() => this.RaiseGotoStateEvent<Final>();

            private class Final : State
            {
            }
        }

        private class M2 : StateMachine
        {
            [Start]
            [OnEventGotoState(typeof(Message), typeof(Final))]
            private class Init : State
            {
            }

            private class Final : State
            {
            }
        }

        private class M3 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventGotoState(typeof(Message), typeof(Final))]
            private class Init : State
            {
            }

            private void InitOnEntry() => this.RaiseEvent(new Message());

            private class Final : State
            {
            }
        }

        [Fact(Timeout = 5000)]
        public async Task TestGotoStateTransition()
        {
            var configuration = GetConfiguration();
            var test = new ActorTestKit<M1>(configuration: configuration);
            await test.StartActorAsync();
            test.AssertStateTransition("Final");
        }

        [Fact(Timeout = 5000)]
        public async Task TestGotoStateTransitionAfterSend()
        {
            var configuration = GetConfiguration();
            var test = new ActorTestKit<M2>(configuration: configuration);
            await test.StartActorAsync();
            test.AssertStateTransition("Init");

            await test.SendEventAsync(new Message());
            test.AssertStateTransition("Final");
        }

        [Fact(Timeout = 5000)]
        public async Task TestGotoStateTransitionAfterRaise()
        {
            var configuration = GetConfiguration();
            var test = new ActorTestKit<M3>(configuration: configuration);
            await test.StartActorAsync();
            test.AssertStateTransition("Final");
        }
    }
}
