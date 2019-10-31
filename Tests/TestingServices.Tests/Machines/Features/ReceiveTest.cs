// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Actors;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests
{
    public class ReceiveTest : BaseTest
    {
        public ReceiveTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class E : Event
        {
        }

        private class M : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private async Task InitOnEntry()
            {
                this.SendEvent(this.Id, new E());
                await this.ReceiveEventAsync(typeof(E));
                this.Assert(false);
            }
        }

        [Fact(Timeout=5000)]
        public void TestAsyncReceiveEvent()
        {
            this.TestWithError(r =>
            {
                r.CreateStateMachine(typeof(M));
            },
            expectedError: "Detected an assertion failure.",
            replay: true);
        }
    }
}
