// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Runtime;

namespace Microsoft.Coyote.Interception
{
    /// <summary>
    /// Provides methods that are common for all concurrent collections during testing.
    /// </summary>
    /// <remarks>This type is intended for concurrent collections only.</remarks>
    internal class ConcurrentCollectionHelper
    {
        internal static void Interleave()
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.Systematic)
            {
                runtime.ScheduleNextOperation(AsyncOperationType.Default);
            }
            else if (runtime.SchedulingPolicy is SchedulingPolicy.Fuzzing)
            {
                runtime.DelayOperation();
            }
        }
    }
}
