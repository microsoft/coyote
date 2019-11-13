// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Actors;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests.Actors.StateMachines
{
    public class PushStateTest : BaseTest
    {
        public PushStateTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class A : StateMachine
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

            private void Bar()
            {
                this.Pop();
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

        private class B : StateMachine
        {
            [Start]
            [OnEntry(nameof(Conf))]
            private class Init : State
            {
            }

            private void Conf()
            {
                var a = this.CreateActor(typeof(A));
                this.SendEvent(a, new E2()); // push(S1)
                this.SendEvent(a, new E1()); // execute foo without popping
                this.SendEvent(a, new E3()); // can handle it because A is still in S1
            }
        }

        [Fact(Timeout=5000)]
        public void TestPushStateEvent()
        {
            this.Test(r =>
            {
                r.CreateActor(typeof(B));
            });
        }
    }
}
