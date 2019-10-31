// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Actors;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests
{
    public class WildCardEventTest : BaseTest
    {
        public WildCardEventTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class A : StateMachine
        {
            [Start]
            [OnEventDoAction(typeof(E1), nameof(Foo))]
            [OnEventGotoState(typeof(E2), typeof(S1))]
            [DeferEvents(typeof(WildCardEvent))]
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

            private void Bar()
            {
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

        private class B : StateMachine
        {
            [Start]
            [OnEntry(nameof(Conf))]
            private class Init : State
            {
            }

            private void Conf()
            {
                var a = this.CreateStateMachine(typeof(A));
                this.SendEvent(a, new E3());
                this.SendEvent(a, new E1());
                this.SendEvent(a, new E2());
            }
        }

        [Fact(Timeout=5000)]
        public void TestWildCardEvent()
        {
            this.Test(r =>
            {
                r.CreateStateMachine(typeof(B));
            });
        }
    }
}
