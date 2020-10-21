// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Xunit.Abstractions;

namespace Microsoft.Coyote.SystematicTesting.Tests.Actors
{
    public class GetEventGroupIdTests : Coyote.Actors.Tests.GetEventGroupIdTests
    {
        public GetEventGroupIdTests(ITestOutputHelper output)
            : base(output)
        {
        }

        protected override bool IsSystematicTest => true;
    }
}
