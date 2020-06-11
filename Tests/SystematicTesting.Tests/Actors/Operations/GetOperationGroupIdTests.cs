// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Xunit.Abstractions;

namespace Microsoft.Coyote.SystematicTesting.Tests.Actors
{
    public class GetOperationGroupIdTests : Microsoft.Coyote.Production.Tests.Actors.GetOperationGroupIdTests
    {
        public GetOperationGroupIdTests(ITestOutputHelper output)
            : base(output)
        {
        }

        public override bool SystematicTest => true;
    }
}
