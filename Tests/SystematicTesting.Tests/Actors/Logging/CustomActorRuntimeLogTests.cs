// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Xunit.Abstractions;

namespace Microsoft.Coyote.SystematicTesting.Tests.Actors
{
    public class CustomActorRuntimeLogTests : Microsoft.Coyote.Production.Tests.Actors.CustomActorRuntimeLogTests
    {
        public CustomActorRuntimeLogTests(ITestOutputHelper output)
            : base(output)
        {
        }

        public override bool IsSystematicTest => true;
    }
}
