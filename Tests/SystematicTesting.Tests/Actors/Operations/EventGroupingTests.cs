// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Xunit.Abstractions;

namespace Microsoft.Coyote.SystematicTesting.Tests.Actors
{
    public class EventGroupingTests : Coyote.Actors.Tests.EventGroupingTests
    {
        public EventGroupingTests(ITestOutputHelper output)
            : base(output)
        {
        }

        protected override bool IsSystematicTest => true;
    }
}
