// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Xunit.Abstractions;

namespace Microsoft.Coyote.SystematicTesting.Tests.Actors
{
    public class LargeEventGroupTest : Microsoft.Coyote.Production.Tests.Actors.LargeEventGroupTest
    {
        public LargeEventGroupTest(ITestOutputHelper output)
            : base(output)
        {
        }

        public override bool IsSystematicTest => true;
    }
}
