// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Xunit.Abstractions;

namespace Microsoft.Coyote.SystematicTesting.Tests.Actors
{
    public class StateInheritanceTests : Coyote.Actors.Tests.StateMachines.StateInheritanceTests
    {
        public StateInheritanceTests(ITestOutputHelper output)
            : base(output)
        {
        }

        protected override bool IsSystematicTest => true;
    }
}
