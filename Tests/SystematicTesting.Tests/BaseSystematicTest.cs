// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if BINARY_REWRITE
using System.Threading.Tasks;
#else
using Microsoft.Coyote.Tasks;
#endif
using Microsoft.Coyote.Tests.Common;
using Xunit.Abstractions;

#if BINARY_REWRITE
namespace Microsoft.Coyote.BinaryRewriting.Tests
#else
namespace Microsoft.Coyote.SystematicTesting.Tests
#endif
{
    public abstract class BaseSystematicTest : BaseTest
    {
        public BaseSystematicTest(ITestOutputHelper output)
            : base(output)
        {
        }

        public override bool IsSystematicTest => true;

        public class SharedEntry
        {
            public volatile int Value = 0;

            public async Task<int> GetWriteResultAsync(int value)
            {
                this.Value = value;
                await Task.CompletedTask;
                return this.Value;
            }

            public async Task<int> GetWriteResultWithDelayAsync(int value)
            {
                this.Value = value;
                await Task.Delay(1);
                return this.Value;
            }
        }
    }
}
