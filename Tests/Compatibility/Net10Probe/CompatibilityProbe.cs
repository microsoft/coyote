// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.SystematicTesting;
using Microsoft.Coyote.Specifications;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Coyote.Compatibility.Net10
{
    public static class CompatibilityProbe
    {
        [Test]
        public static async Task Execute()
        {
            var sync = new Lock();
            int count = 0;
            Task first = Task.Run(() =>
            {
                lock (sync)
                {
                    count++;
                }
            });
            Task second = Task.Run(() =>
            {
                lock (sync)
                {
                    count++;
                }
            });

            await Task.WhenAll(first, second);
            Specification.Assert(count is 2, "Expected both tasks to execute.");
        }
    }
}
