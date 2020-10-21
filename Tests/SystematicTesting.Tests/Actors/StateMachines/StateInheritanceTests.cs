// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Xunit.Abstractions;

namespace Microsoft.Coyote.SystematicTesting.Tests.Actors
{
    public class StateInheritanceTests : Microsoft.Coyote.Actors.Tests.Actors.StateMachines.StateInheritanceTests
    {
        public StateInheritanceTests(ITestOutputHelper output)
            : base(output)
        {
        }

        public override bool IsSystematicTest => true;
    }
}
