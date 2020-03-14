// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Actors;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.SystematicTesting.Tests.Actors
{
    public class CreateActorTests : BaseSystematicTest
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
            configuration: GetConfiguration());
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
            configuration: GetConfiguration());
        }
    }
}
