// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Xunit.Abstractions;

namespace Microsoft.Coyote.SystematicTesting.Tests.Actors
{
    public class CustomActorRuntimeLogTests : Coyote.Actors.Tests.CustomActorRuntimeLogTests
    {
        public CustomActorRuntimeLogTests(ITestOutputHelper output)
            : base(output)
        {
        }

        protected override bool IsSystematicTest => true;
    }
}
