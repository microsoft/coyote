// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Xunit.Abstractions;

namespace Microsoft.Coyote.Tests.Common
{
    public abstract class BaseTest
    {
        protected readonly ITestOutputHelper TestOutput;

        public BaseTest(ITestOutputHelper output)
        {
            this.TestOutput = output;
        }
    }
}
