// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Coyote.Tests.Common.Tasks
{
    /// <summary>
    /// Helper class for task rewriting tests.
    /// </summary>
    /// <remarks>
    /// We do not rewrite this class in purpose to test scenarios with partially rewritten code.
    /// </remarks>
    public static class AsyncProvider
    {
        public static Task DelayAsync(int duration) => Task.Delay(duration);

        public static async Task YieldAsync() => await Task.Yield();

        public static IEnumerable<Task<int>> GetPendingTasks() => new[] { new TaskCompletionSource<int>().Task };

        public static IEnumerable<Task<int>> GetPendingTasks(out Action complete)
        {
            var source = new TaskCompletionSource<int>();
            complete = () => source.SetResult(1);
            return new[] { source.Task };
        }

        public static IEnumerable<Task> GetDelayedTasks(int duration) => new[] { DelayAsync(duration) };
    }
}
