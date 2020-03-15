// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Actors;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.SystematicTesting.Tests.Runtime
{
    public class SendEventTests : BaseSystematicTest
    {
        public SendEventTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private class Transfer : Event
        {
            public int Value;

            public Transfer(int value)
            {
                this.Value = value;
            }
        }

        private class M : StateMachine
        {
            [Start]
            [OnEventDoAction(typeof(Transfer), nameof(HandleTransfer))]
            private class Init : State
            {
            }

            private void HandleTransfer(Event e)
            {
                int value = (e as Transfer).Value;
                this.Assert(value > 0, "Value is 0.");
            }
        }

        [Fact(Timeout = 5000)]
        public void TestSendEvent()
        {
            this.TestWithError(r =>
            {
                ActorId m = r.CreateActor(typeof(M));
                r.SendEvent(m, new Transfer(0));
            },
            expectedError: "Value is 0.",
            replay: true);
        }
    }
}
