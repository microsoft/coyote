// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Xunit.Abstractions;

namespace Microsoft.Coyote.SystematicTesting.Tests.Tasks
{
    public class TestMethodSignatures : Microsoft.Coyote.Production.Tests.Tasks.TestMethodSignatures
    {
        public TestMethodSignatures(ITestOutputHelper output)
            : base(output)
        {
        }

        public override bool SystematicTest => true;
    }
}
