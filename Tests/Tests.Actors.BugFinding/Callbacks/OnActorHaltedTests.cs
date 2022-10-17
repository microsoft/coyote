// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Actors.BugFinding.Tests
{
    public class OnActorHaltedTests : BaseActorBugFindingTest
    {
        public OnActorHaltedTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private class A : Actor
        {
        }

        [Fact(Timeout = 5000)]
        public void TestOnActorHalted()
        {
            var called = false;
            this.Test(r =>
            {
                var actorId = r.CreateActor(typeof(A));
                r.OnActorHalted += (id) =>
                {
                    called = true;
                    Assert.Equal(actorId, id);
                };

                r.SendEvent(actorId, HaltEvent.Instance);
            });

            Assert.True(called);
        }
    }
}
