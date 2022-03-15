// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Coyote.Runtime;

namespace Microsoft.Coyote.Testing.Reduction
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
        /// <param name="current">The currently scheduled operation.</param>
        /// <param name="ops">The set of all available operations to schedule.</param>
        /// <returns>The reduced set of operations to schedule.</returns>
        List<ControlledOperation> ReduceOperations(ControlledOperation current, List<ControlledOperation> ops);
    }
}
