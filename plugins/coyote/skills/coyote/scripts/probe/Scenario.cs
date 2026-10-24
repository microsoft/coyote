// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Coyote.Specifications;

namespace Probe
{
    public static class Scenario
    {
        public static int CompletedIterations { get; set; }

        public static async Task Run()
        {
            int value = 0;
            await Task.WhenAll(Task.Run(Work), Task.Run(Work));
            Specification.Assert(value == 2, "Atomic updates must not be lost.");
            CompletedIterations++;

            async Task Work()
            {
                // Replace this block with the focused API usage you want to investigate.
                await Task.Delay(1);
                Thread.Sleep(1);
                Interlocked.Increment(ref value);
                await Task.Yield();
            }
        }
    }
}
