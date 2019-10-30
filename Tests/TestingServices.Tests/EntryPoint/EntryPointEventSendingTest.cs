// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Machines;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests
{
    public class EntryPointEventSendingTest : BaseTest
    {
        public EntryPointEventSendingTest(ITestOutputHelper output)
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
            private class Init : MachineState
            {
            }

            private void HandleTransfer()
            {
                int value = (this.ReceivedEvent as Transfer).Value;
                this.Assert(value > 0, "Value is 0.");
            }
        }

        [Fact(Timeout=5000)]
        public void TestEntryPointEventSending()
        {
            this.TestWithError(r =>
            {
                MachineId m = r.CreateMachine(typeof(M));
                r.SendEvent(m, new Transfer(0));
            },
            expectedError: "Value is 0.",
            replay: true);
        }
    }
}
