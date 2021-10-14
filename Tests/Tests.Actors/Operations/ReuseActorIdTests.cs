// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Runtime;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Actors.Tests
{
    public class ReuseActorIdTests : BaseActorTest
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
            expectedError: "An actor with id '' was already created.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestReuseActorIdAfterHalt()
        {
            this.Test(async r =>
            {
                var id = r.CreateActor(typeof(M));
                while (true)
                {
                    try
                    {
                        // Halt the actor before trying to reuse its id.
                        r.SendEvent(id, HaltEvent.Instance);

                        // Trying to bring up a halted actor,
                        // but this is racy and can fail.
                        id = r.CreateActor(id, typeof(M));
                        break;
                    }
                    catch (AssertionFailureException ex)
                    {
                        if (ex.Message.Contains("was already created"))
                        {
                            // Retry.
                            await Task.Delay(10);
                            continue;
                        }

                        throw;
                    }
                }
            },
            configuration: this.GetConfiguration().WithTestingIterations(100));
        }

        [Fact(Timeout = 5000)]
        public void TestReuseActorIdWithHaltRace()
        {
            this.TestWithError(r =>
            {
                ActorId id = r.CreateActor(typeof(M));

                // Sending a halt event can race with the subsequent actor creation.
                r.SendEvent(id, HaltEvent.Instance);
                r.CreateActor(id, typeof(M));
            },
            configuration: this.GetConfiguration().WithTestingIterations(100),
            expectedError: "An actor with id '' was already created.",
            replay: true);
        }
    }
}
