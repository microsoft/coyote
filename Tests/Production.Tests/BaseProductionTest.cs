// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Tests.Common;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Production.Tests
{
    public abstract class BaseProductionTest : BaseTest
    {
        public BaseProductionTest(ITestOutputHelper output)
            : base(output)
        {
        }
    }
}
