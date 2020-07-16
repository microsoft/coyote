// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Xunit.Abstractions;

namespace Microsoft.Coyote.SystematicTesting.Tests.Actors
{
    public class GetEventGroupIdTests : Microsoft.Coyote.Production.Tests.Actors.GetEventGroupIdTests
    {
        public GetEventGroupIdTests(ITestOutputHelper output)
            : base(output)
        {
        }

        public override bool IsSystematicTest => true;
    }
}
