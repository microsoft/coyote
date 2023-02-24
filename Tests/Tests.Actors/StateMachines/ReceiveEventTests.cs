// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Actors.UnitTesting;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Actors.Tests.StateMachines
{
    public class ReceiveEventTests : BaseActorTest
    {
        public ReceiveEventTests(ITestOutputHelper output)
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

        private class M1 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private async Task InitOnEntry()
            {
                await this.ReceiveEventAsync(typeof(E1));
            }
        }

        private class M2 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private async Task InitOnEntry()
            {
                await this.ReceiveEventAsync(typeof(E1));
                await this.ReceiveEventAsync(typeof(E2));
                await this.ReceiveEventAsync(typeof(E3));
            }
        }

        private class M3 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private async Task InitOnEntry()
            {
                await this.ReceiveEventAsync(typeof(E1), typeof(E2), typeof(E3));
            }
        }

        private class M4 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private async Task InitOnEntry()
            {
                await this.ReceiveEventAsync(typeof(E1), typeof(E2), typeof(E3));
                await this.ReceiveEventAsync(typeof(E1), typeof(E2), typeof(E3));
                await this.ReceiveEventAsync(typeof(E1), typeof(E2), typeof(E3));
            }
        }

        [Fact(Timeout = 5000)]
        public async Task TestReceiveEventStatement()
        {
            var configuration = GetConfiguration();
            var test = new ActorTestKit<M1>(configuration: configuration);

            await test.StartActorAsync();
            test.AssertIsWaitingToReceiveEvent(true);

            await test.SendEventAsync(new E1());
            test.AssertIsWaitingToReceiveEvent(false);
            test.AssertInboxSize(0);
        }

        [Fact(Timeout = 5000)]
        public async Task TestMultipleReceiveEventStatements()
        {
            var configuration = GetConfiguration();
            var test = new ActorTestKit<M2>(configuration: configuration);

            await test.StartActorAsync();
            test.AssertIsWaitingToReceiveEvent(true);

            await test.SendEventAsync(new E1());
            test.AssertIsWaitingToReceiveEvent(true);

            await test.SendEventAsync(new E2());
            test.AssertIsWaitingToReceiveEvent(true);

            await test.SendEventAsync(new E3());
            test.AssertIsWaitingToReceiveEvent(false);
            test.AssertInboxSize(0);
        }

        [Fact(Timeout = 5000)]
        public async Task TestMultipleReceiveEventStatementsUnordered()
        {
            var configuration = GetConfiguration();
            var test = new ActorTestKit<M2>(configuration: configuration);

            await test.StartActorAsync();
            test.AssertIsWaitingToReceiveEvent(true);

            await test.SendEventAsync(new E2());
            test.AssertIsWaitingToReceiveEvent(true);
            test.AssertInboxSize(1);

            await test.SendEventAsync(new E3());
            test.AssertIsWaitingToReceiveEvent(true);
            test.AssertInboxSize(2);

            await test.SendEventAsync(new E1());
            test.AssertIsWaitingToReceiveEvent(false);
            test.AssertInboxSize(0);
        }

        [Fact(Timeout = 5000)]
        public async Task TestReceiveEventStatementWithMultipleTypes()
        {
            var configuration = GetConfiguration();
            var test = new ActorTestKit<M3>(configuration: configuration);

            await test.StartActorAsync();
            test.AssertIsWaitingToReceiveEvent(true);

            await test.SendEventAsync(new E1());
            test.AssertIsWaitingToReceiveEvent(false);
            test.AssertInboxSize(0);
        }

        [Fact(Timeout = 5000)]
        public async Task TestMultipleReceiveEventStatementsWithMultipleTypes()
        {
            var configuration = GetConfiguration();
            var test = new ActorTestKit<M4>(configuration: configuration);

            await test.StartActorAsync();
            test.AssertIsWaitingToReceiveEvent(true);

            await test.SendEventAsync(new E1());
            test.AssertIsWaitingToReceiveEvent(true);

            await test.SendEventAsync(new E2());
            test.AssertIsWaitingToReceiveEvent(true);

            await test.SendEventAsync(new E3());
            test.AssertIsWaitingToReceiveEvent(false);
            test.AssertInboxSize(0);
        }
    }
}
