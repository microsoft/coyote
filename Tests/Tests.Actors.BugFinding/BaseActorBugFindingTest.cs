// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.BugFinding.Tests;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Actors.BugFinding.Tests
{
    public abstract class BaseActorBugFindingTest : BaseBugFindingTest
    {
        public BaseActorBugFindingTest(ITestOutputHelper output)
            : base(output)
        {
        }
    }
}
