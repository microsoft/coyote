// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Runtime;

namespace Microsoft.Coyote.Rewriting.Types.Collections.Concurrent
{
    /// <summary>
    /// Provides methods that are common for all concurrent collections during testing.
    /// </summary>
    internal class Helper
    {
        internal static void Interleave()
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.Systematic)
            {
                runtime.ScheduleNextOperation(SchedulingPointType.Default);
            }
            else if (runtime.SchedulingPolicy is SchedulingPolicy.Fuzzing)
            {
                runtime.DelayOperation();
            }
        }
    }
}
