// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Coyote.Coverage;

namespace Microsoft.Coyote.Runtime
{
    /// <summary>
    /// Interface of a reducer that can choose a subset of all available operations
    /// to be scheduled at each scheduling step.
    /// </summary>
    internal interface IScheduleReducer
    {
        /// <summary>
        /// Returns a subset of all available operations to be scheduled at the next scheduling step.
        /// </summary>
        /// <param name="ops">All available operations to schedule.</param>
        /// <param name="current">The currently scheduled operation.</param>
        /// <param name="coverageInfo">Stores coverage data across multiple test iterations.</param>
        /// <returns>The subset of operations to schedule.</returns>
        IEnumerable<ControlledOperation> ReduceOperations(IEnumerable<ControlledOperation> ops, ControlledOperation current, CoverageInfo coverageInfo);
    }
}
