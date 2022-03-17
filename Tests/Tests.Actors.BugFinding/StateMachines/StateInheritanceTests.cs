// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Runtime;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Actors.BugFinding.Tests
{
    public class StateInheritanceTests : Actors.Tests.StateMachines.StateInheritanceTests
    {
        public StateInheritanceTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private protected override SchedulingPolicy SchedulingPolicy => SchedulingPolicy.Interleaving;
    }
}
