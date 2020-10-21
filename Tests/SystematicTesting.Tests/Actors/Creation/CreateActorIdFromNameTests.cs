// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Xunit.Abstractions;

namespace Microsoft.Coyote.SystematicTesting.Tests.Actors
{
    public class CreateActorIdFromNameTests : Microsoft.Coyote.Actors.Tests.Actors.CreateActorIdFromNameTests
    {
        public CreateActorIdFromNameTests(ITestOutputHelper output)
            : base(output)
        {
        }

        public override bool IsSystematicTest => true;
    }
}
