// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace Microsoft.Coyote.Runtime
{
    /// <summary>
    /// Interface of a reducer that can choose a subset of all available operations
    /// to be scheduled at each scheduling step.
    /// </summary>
    internal interface IScheduleReducer
    {
        /// <summary>
        /// Initializes the next iteration.
        /// </summary>
        /// <param name="iteration">The id of the next iteration.</param>
        void InitializeNextIteration(uint iteration);

        /// <summary>
        /// Returns a subset of all available operations to be scheduled at the next scheduling step.
        /// </summary>
        /// <param name="ops">All available operations to schedule.</param>
        /// <param name="current">The currently scheduled operation.</param>
        /// <returns>The subset of operations to schedule.</returns>
        IEnumerable<ControlledOperation> ReduceOperations(IEnumerable<ControlledOperation> ops, ControlledOperation current);
    }
}
