// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.SystematicTesting.Tests;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Actors.SystematicTesting.Tests
{
    public abstract class BaseActorSystematicTest : BaseSystematicTest
    {
        public BaseActorSystematicTest(ITestOutputHelper output)
            : base(output)
        {
        }
    }
}
