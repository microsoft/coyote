// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.TestingServices;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Core.Tests.Actors.StateMachines
{
    /// <summary>
    /// Tests that StateMachines can also fall back on class level OnEventDoActions that
    /// all Actors can define.
    /// </summary>
    public class ClassHandlerTests : BaseTest
    {
        public ClassHandlerTests(ITestOutputHelper output)
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

        [OnEventDoAction(typeof(E1), nameof(HandleE1))]
        private class M1 : StateMachine
        {
            public int Count;

            [Start]
            private class Init : State
            {
            }

            private void HandleE1()
            {
                this.Count++;
            }
        }

        [OnEventDoAction(typeof(E1), nameof(HandleE1))]
        private class M2 : StateMachine
        {
            public int Count;

            [Start]
            [OnEventDoAction(typeof(E1), nameof(HandleInitE1))]
            private class Init : State
            {
            }

            private void HandleE1()
            {
                this.Count--;
            }

            private void HandleInitE1()
            {
                this.Count++;
            }
        }

        [OnEventDoAction(typeof(E1), nameof(HandleE1))]
        private class M3 : StateMachine
        {
            public int Count;

            [Start]
            [OnEntry(nameof(OnInitEntry))]
            [DeferEvents(typeof(E1))]
            private class Init : State
            {
            }

            private void OnInitEntry()
            {
                this.RaiseGotoStateEvent<Active>();
            }

            [OnEventDoAction(typeof(E1), nameof(HandleActiveE1))]
            private class Active : State
            {
            }

            private void HandleE1()
            {
                this.Count--;
            }

            private void HandleActiveE1()
            {
                this.Count++;
            }
        }

        [OnEventDoAction(typeof(E1), nameof(HandleE1))]
        private class M4 : StateMachine
        {
            public int Count;

            [Start]
            [OnEventDoAction(typeof(WildCardEvent), nameof(HandleWildCard))]
            private class Init : State
            {
            }

            private void HandleE1()
            {
                this.Count--;
            }

            private void HandleWildCard()
            {
                this.Count++;
            }
        }

        [Fact(Timeout = 5000)]
        public async Task TestClassEventHandler()
        {
            var configuration = GetConfiguration();
            var test = new ActorTestKit<M1>(configuration: configuration);

            await test.StartActorAsync();
            await test.SendEventAsync(new E1());
            test.Assert(test.ActorInstance.Count == 1, "HandleE1 was not called");
        }

        [Fact(Timeout = 5000)]
        public async Task TestClassEventHandlerOverride()
        {
            var configuration = GetConfiguration();
            var test = new ActorTestKit<M2>(configuration: configuration);

            await test.StartActorAsync();
            await test.SendEventAsync(new E1());
            test.Assert(test.ActorInstance.Count == 1, "HandleInitE1 was not called");
        }

        [Fact(Timeout = 5000)]
        public async Task TestClassEventHandlerDeferOverride()
        {
            var configuration = GetConfiguration();
            var test = new ActorTestKit<M3>(configuration: configuration);

            await test.StartActorAsync();
            await test.SendEventAsync(new E1());
            test.Assert(test.ActorInstance.Count == 1, "HandleActiveE1 was not called");
        }

        [Fact(Timeout = 5000)]
        public async Task TestClassEventHandlerWildcardOverride()
        {
            var configuration = GetConfiguration();
            var test = new ActorTestKit<M3>(configuration: configuration);

            await test.StartActorAsync();
            await test.SendEventAsync(new E1());
            test.Assert(test.ActorInstance.Count == 1, "HandleWildCard was not called");
        }
    }
}
