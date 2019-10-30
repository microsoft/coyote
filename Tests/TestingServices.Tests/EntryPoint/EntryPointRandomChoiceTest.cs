// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Machines;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests
{
    public class EntryPointRandomChoiceTest : BaseTest
    {
        public EntryPointRandomChoiceTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class M : StateMachine
        {
            [Start]
            private class Init : MachineState
            {
            }
        }

        [Fact(Timeout=5000)]
        public void TestEntryPointRandomChoice()
        {
            this.Test(r =>
            {
                if (r.Random())
                {
                    r.CreateMachine(typeof(M));
                }
            });
        }
    }
}
