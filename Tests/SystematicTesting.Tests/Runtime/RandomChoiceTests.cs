// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Actors;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.SystematicTesting.Tests.Runtime
{
    public class RandomChoiceTests : BaseSystematicTest
    {
        public RandomChoiceTests(ITestOutputHelper output)
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

        [Fact(Timeout = 5000)]
        public void TestRandomChoice()
        {
            this.Test(r =>
            {
                if (r.Random())
                {
                    r.CreateActor(typeof(M));
                }
            });
        }
    }
}
