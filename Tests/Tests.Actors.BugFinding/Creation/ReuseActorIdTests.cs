// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Specifications;
using Microsoft.Coyote.Tests.Common.Events;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Actors.BugFinding.Tests
{
    public class ReuseActorIdTests : BaseActorBugFindingTest
    {
        public ReuseActorIdTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private class M : StateMachine
        {
            [Start]
            private class S : State
            {
            }
        }

        [Fact(Timeout = 5000)]
        public void TestReuseActorId()
        {
            this.TestWithError(r =>
            {
                ActorId id = r.CreateActor(typeof(M));
                r.CreateActor(id, typeof(M));
            },
            expectedError: "Actor id '' is used by an existing or previously halted actor.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestReuseActorIdAfterHalt()
        {
            this.TestWithError(r =>
            {
                bool isEventDropped = false;
                r.OnEventDropped += (Event e, ActorId target) =>
                {
                    isEventDropped = true;
                };

                ActorId id = r.CreateActor(typeof(M));
                while (!isEventDropped)
                {
                    // Make sure the actor halts before trying to reuse its id.
                    r.SendEvent(id, HaltEvent.Instance);
                }

                // Trying to bring up a halted actor.
                r.CreateActor(id, typeof(M2));
            },
            configuration: this.GetConfiguration(),
            expectedError: "Actor id '' is used by an existing or previously halted actor.");
        }
    }
}
