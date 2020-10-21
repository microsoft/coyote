// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Xunit.Abstractions;

namespace Microsoft.Coyote.SystematicTesting.Tests.Actors
{
    public class CreateActorIdFromNameTests : Coyote.Actors.Tests.CreateActorIdFromNameTests
    {
        public CreateActorIdFromNameTests(ITestOutputHelper output)
            : base(output)
        {
        }

        protected override bool IsSystematicTest => true;
    }
}
