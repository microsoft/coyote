// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Actors;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests.Actors
{
    public class RaiseEventTest : BaseTest
    {
        public RaiseEventTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class E1 : Event
        {
        }

        private class E2 : Event
        {
        }

        private class M : StateMachine
        {
            [Start]
            [OnEventDoAction(typeof(E1), nameof(HandleE1))]
            [OnEventDoAction(typeof(E2), nameof(HandleE2))]
            private class Init : State
            {
            }

            private void HandleE1()
            {
                this.RaiseEvent(new E2());
            }

            private void HandleE2()
            {
                this.Assert(false, "Reached test assertion.");
            }
        }

        [Fact(Timeout=5000)]
        public void TestRaiseEvent()
        {
            this.TestWithError(r =>
            {
                var id = r.CreateActor(typeof(M));
                r.SendEvent(id, new E1());
            },
            configuration: GetConfiguration(),
            expectedError: "Reached test assertion.",
            replay: true);
        }
    }
}
