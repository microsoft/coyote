﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Xunit.Abstractions;

namespace Microsoft.Coyote.SystematicTesting.Tests.Actors
{
    public class OperationGroupingTests : Microsoft.Coyote.Production.Tests.Actors.OperationGroupingTests
    {
        public OperationGroupingTests(ITestOutputHelper output)
            : base(output)
        {
        }

        public override bool SystematicTest => true;
    }
}
