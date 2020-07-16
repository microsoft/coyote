// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Xunit.Abstractions;

namespace Microsoft.Coyote.SystematicTesting.Tests.Tasks
{
    public class MethodSignaturesTests : Microsoft.Coyote.Production.Tests.Tasks.MethodSignaturesTests
    {
        public MethodSignaturesTests(ITestOutputHelper output)
            : base(output)
        {
        }

        public override bool IsSystematicTest => true;
    }
}
