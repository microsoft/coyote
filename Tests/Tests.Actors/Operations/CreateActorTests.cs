// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Actors.Tests
{
    public class CreateActorTests : BaseActorTest
    {
        public CreateActorTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private class SetupEvent : Event
        {
            public int Value;

            public SetupEvent(int value)
            {
                this.Value = value;
            }
        }

        private class A : Actor
        {
        }

        [Fact(Timeout = 5000)]
        public void TestCreateActor()
        {
            this.Test(r =>
            {
                var id = r.CreateActor(typeof(A));
                r.Assert(id != null, "The actor id is null.");
            },
            configuration: this.GetConfiguration());
        }

        private class M : StateMachine
        {
            [Start]
            private class S : State
            {
            }
        }

        [Fact(Timeout = 5000)]
        public void TestCreateStateMachine()
        {
            this.Test(r =>
            {
                var id = r.CreateActor(typeof(M));
                r.Assert(id != null, "The actor id is null.");
            },
            configuration: this.GetConfiguration());
        }
    }
}
