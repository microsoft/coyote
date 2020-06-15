// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Xunit.Abstractions;

namespace Microsoft.Coyote.SystematicTesting.Tests.Actors
{
    public class QuiescentOperationTests : Microsoft.Coyote.Production.Tests.Actors.QuiescentOperationTests
    {
        public QuiescentOperationTests(ITestOutputHelper output)
            : base(output)
        {
        }

        public override bool SystematicTest => true;
    }
}
