// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.Tests.Common;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Rewriting.Tests
{
    public abstract class BaseRewritingTest : BaseTest
    {
        public BaseRewritingTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private protected override SchedulingPolicy SchedulingPolicy
        {
            get
            {
                var assembly = this.GetType().Assembly;
                bool result = RewritingEngine.IsAssemblyRewrittenWithCurrentVersion(assembly);
                Assert.True(result, $"Expected the '{assembly}' assembly to be rewritten.");
                return SchedulingPolicy.Systematic;
            }
        }
    }
}
