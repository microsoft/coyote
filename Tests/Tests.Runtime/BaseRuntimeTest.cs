// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.Tests.Common;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Runtime.Tests
{
    public abstract class BaseRuntimeTest : BaseTest
    {
        public BaseRuntimeTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private protected override SchedulingPolicy SchedulingPolicy
        {
            get
            {
                return SchedulingPolicy.Interleaving;
            }
        }
    }
}
