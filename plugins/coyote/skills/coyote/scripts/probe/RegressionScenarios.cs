// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Coyote.Specifications;

namespace Probe
{
    internal static class RegressionScenarios
    {
        internal const string LostUpdateMessage = "Lost-update regression: both increments must be preserved.";

        internal static async Task LostUpdate()
        {
            int value = 0;
            await Task.WhenAll(Task.Run(Increment), Task.Run(Increment));
            Specification.Assert(value == 2, LostUpdateMessage);

            async Task Increment()
            {
                int previous = value;
                await Task.Yield();
                value = previous + 1;
            }
        }

        internal static Task UncontrolledConcurrency()
        {
            using var timer = new Timer(_ => { }, null, Timeout.Infinite, Timeout.Infinite);
            return Task.CompletedTask;
        }

        internal static Task UncontrolledData()
        {
            _ = Guid.NewGuid();
            return Task.CompletedTask;
        }
    }
}
