// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Runtime;
using SystemThread = System.Threading.Thread;

namespace Microsoft.Coyote.Rewriting.Types.Threading
{
    /// <summary>
    /// Provides methods for creating threads that can be controlled during testing.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static class Thread
    {
        /// <summary>
        /// Causes the calling thread to yield execution to another thread that is ready
        /// to run on the current processor.
        /// </summary>
        public static bool Yield()
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.Interleaving &&
                runtime.TryGetExecutingOperation(out ControlledOperation current))
            {
                return runtime.ScheduleNextOperation(current, SchedulingPointType.Yield, true, true);
            }

            return SystemThread.Yield();
        }
    }
}
