// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Specifications;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.SystematicTesting.Tests.Tasks
{
    public class TaskInterfaceTests : BaseSystematicTest
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
