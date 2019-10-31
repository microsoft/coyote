// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Actors;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests
{
    public class EntryPointMachineCreationTest : BaseTest
    {
        public EntryPointMachineCreationTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class M : StateMachine
        {
            [Start]
            private class Init : State
            {
            }
        }

        private class N : StateMachine
        {
            [Start]
            private class Init : State
            {
            }
        }

        [Fact(Timeout=5000)]
        public void TestEntryPointMachineCreation()
        {
            this.Test(r =>
            {
                ActorId m = r.CreateStateMachine(typeof(M));
                ActorId n = r.CreateStateMachine(typeof(N));
                r.Assert(m != null && m != null, "Machine ids are null.");
            });
        }
    }
}
