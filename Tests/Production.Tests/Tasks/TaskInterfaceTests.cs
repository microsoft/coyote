// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if BINARY_REWRITE
using System.Threading.Tasks;
#else
using Microsoft.Coyote.Tasks;
#endif
using Microsoft.Coyote.Specifications;
using Xunit;
using Xunit.Abstractions;

#if BINARY_REWRITE
namespace Microsoft.Coyote.BinaryRewriting.Tests.Tasks
#else
namespace Microsoft.Coyote.Production.Tests.Tasks
#endif
{
    public class TaskInterfaceTests : BaseProductionTest
    {
        public TaskInterfaceTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private interface IAsyncSender
        {
            Task<bool> SendEventAsync();
        }

        private class AsyncSender : IAsyncSender
        {
            public async Task<bool> SendEventAsync()
            {
                // Model sending some event.
                await Task.Delay(1);
                return true;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestAsyncInterfaceMethodCall()
        {
            this.Test(async () =>
            {
                IAsyncSender sender = new AsyncSender();
                bool result = await sender.SendEventAsync();
                Specification.Assert(result, "Unexpected result.");
            },
            configuration: GetConfiguration());
        }
    }
}
