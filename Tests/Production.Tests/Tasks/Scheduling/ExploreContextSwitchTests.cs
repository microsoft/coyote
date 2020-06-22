// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Production.Tests.Tasks.Scheduling
{
    public class ExploreContextSwitchTests : BaseProductionTest
    {
        public ExploreContextSwitchTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestExploreContextSwitchIsDisabledInProduction()
        {
            Task.ExploreContextSwitch();
        }
    }
}
