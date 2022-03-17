// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Runtime;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Actors.BugFinding.Tests
{
    public class CreateActorIdFromNameTests : Actors.Tests.CreateActorIdFromNameTests
    {
        public CreateActorIdFromNameTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private protected override SchedulingPolicy SchedulingPolicy => SchedulingPolicy.Interleaving;
    }
}
