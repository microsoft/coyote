// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
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

        private class A : Actor
        {
        }

        [Fact(Timeout = 5000)]
        public void TestReuseActorId()
        {
            this.TestWithException<InvalidOperationException>(r =>
            {
                ActorId id = r.CreateActor(typeof(A));
                r.CreateActor(id, typeof(A));
            },
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestReuseNamedActorId()
        {
            this.TestWithException<InvalidOperationException>(r =>
            {
                ActorId id = r.CreateActorIdFromName(typeof(A), "NamedActor");
                r.CreateActor(id, typeof(A));
                r.CreateActor(id, typeof(A));
            },
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestReuseActorIdWithHaltRace()
        {
            this.TestWithException<InvalidOperationException>(r =>
            {
                ActorId id = r.CreateActor(typeof(A));

                // Sending a halt event can race with the subsequent actor creation.
                r.SendEvent(id, HaltEvent.Instance);
                r.CreateActor(id, typeof(A));
            },
            configuration: this.GetConfiguration().WithTestingIterations(100),
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestReuseNamedActorIdWithHaltRace()
        {
            this.TestWithException<InvalidOperationException>(r =>
            {
                ActorId id = r.CreateActorIdFromName(typeof(A), "NamedActor");
                r.CreateActor(id, typeof(A));

                // Sending a halt event can race with the subsequent actor creation.
                r.SendEvent(id, HaltEvent.Instance);
                r.CreateActor(id, typeof(A));
            },
            configuration: this.GetConfiguration().WithTestingIterations(100),
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestReuseActorIdAfterHalt()
        {
            this.Test(async r =>
            {
                ActorId id = r.CreateActor(typeof(A));
                while (true)
                {
                    try
                    {
                        // Halt the actor before trying to reuse its id.
                        r.SendEvent(id, HaltEvent.Instance);

                        // Trying to bring up a halted actor,
                        // but this is racy and can fail.
                        id = r.CreateActor(id, typeof(A));
                        break;
                    }
                    catch (InvalidOperationException ex)
                    {
                        if (ex.Message.Contains("already exists"))
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
        public void TestReuseNamedActorIdAfterHalt()
        {
            this.Test(async r =>
            {
                ActorId id = r.CreateActorIdFromName(typeof(A), "NamedActor");
                r.CreateActor(id, typeof(A));
                while (true)
                {
                    try
                    {
                        // Halt the actor before trying to reuse its id.
                        r.SendEvent(id, HaltEvent.Instance);

                        // Trying to bring up a halted actor,
                        // but this is racy and can fail.
                        id = r.CreateActor(id, typeof(A));
                        break;
                    }
                    catch (InvalidOperationException ex)
                    {
                        if (ex.Message.Contains("already exists"))
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
    }
}
