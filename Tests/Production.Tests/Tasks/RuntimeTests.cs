// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Production.Tests.Tasks
{
    public class RuntimeTests : BaseProductionTest
    {
        public RuntimeTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestExploreContextSwitchIsDisabled()
        {
            Task.ExploreContextSwitch();
        }
    }
}
