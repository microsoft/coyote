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

        /// <summary>
        /// For tests expecting uncontrolled task assertions, use these as the expectedErrors array.
        /// </summary>
        public static string[] GetUncontrolledTaskErrorMessages()
        {
            return new string[]
            {
                "Controlled task '' is trying to wait for an uncontrolled task or awaiter to complete. Please " +
                "make sure to avoid using concurrency APIs () inside actor handlers. If you are using external " +
                "libraries that are executing concurrently, you will need to mock them during testing.",
                "Uncontrolled task '' invoked a runtime method. Please make sure to avoid using concurrency APIs () " +
                "inside actor handlers or controlled tasks. If you are using external libraries that are executing " +
                "concurrently, you will need to mock them during testing.",
                "Controlled task '' is trying to wait for an uncontrolled task or awaiter to complete. " +
                "Please make sure to use Coyote APIs to express concurrency ()."
            };
        }
    }
}
