// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Xunit.Abstractions;

namespace Microsoft.Coyote.SystematicTesting.Tests.Actors
{
    public class CreateActorTests : Microsoft.Coyote.Actors.Tests.Actors.CreateActorTests
    {
        public CreateActorTests(ITestOutputHelper output)
            : base(output)
        {
        }

        public override bool IsSystematicTest => true;
    }
}
