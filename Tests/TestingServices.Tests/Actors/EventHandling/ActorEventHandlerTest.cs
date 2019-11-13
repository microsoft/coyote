// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Tests.Common.Actors;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests.Actors
{
    public class ActorEventHandlerTest : BaseTest
    {
        public ActorEventHandlerTest(ITestOutputHelper output)
            : base(output)
        {
        }

        [OnEventDoAction(typeof(UnitEvent), nameof(HandleUnitEvent))]
        private class A1 : Actor
        {
            private void HandleUnitEvent()
            {
                this.Assert(false, "Reached test assertion.");
            }
        }

        [Fact(Timeout=5000)]
        public void TestEventHandler()
        {
            this.TestWithError(r =>
            {
                var id = r.CreateActor(typeof(A1));
                r.SendEvent(id, new UnitEvent());
            },
            configuration: GetConfiguration(),
            expectedError: "Reached test assertion.",
            replay: true);
        }
    }
}
