// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Tests.Common.Events;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.SystematicTesting.Tests.Actors
{
    public class StateInheritanceTests : Microsoft.Coyote.Production.Tests.Actors.StateMachines.StateInheritanceTests
    {
        public StateInheritanceTests(ITestOutputHelper output)
            : base(output)
        {
        }

        public override bool IsSystematicTest => true;
    }
}
