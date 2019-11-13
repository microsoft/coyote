// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Tests.Common.Actors;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests.Actors.StateMachines
{
    public class ReceiveTest : BaseTest
    {
        public ReceiveTest(ITestOutputHelper output)
            : base(output)
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
                this.SendEvent(this.Id, new UnitEvent());
                await this.ReceiveEventAsync(typeof(UnitEvent));
                this.Assert(false);
            }
        }

        [Fact(Timeout=5000)]
        public void TestAsyncReceiveEvent()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M));
            },
            expectedError: "Detected an assertion failure.",
            replay: true);
        }
    }
}
