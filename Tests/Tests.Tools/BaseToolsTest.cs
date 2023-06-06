// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.Tests.Common;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Tools.Tests
{
    public abstract class BaseToolsTest : BaseTest
    {
        public BaseToolsTest(ITestOutputHelper output)
            : base(output)
        {
        }
    }
}
