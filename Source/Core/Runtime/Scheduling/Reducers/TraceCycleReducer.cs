// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Coyote.Runtime
{
    /// <summary>
    /// A reducer that analyzes cycles in the execution trace to reduce the set of operations
    /// to be scheduled at each scheduling step.
    /// </summary>
    internal sealed class TraceCycleReducer : IScheduleReducer
    {
        /// <summary>
        /// Set of states that have been 'READ' accessed. States are removed from the set
        /// when they are 'WRITE' accessed.
        /// </summary>
        private readonly HashSet<string> RepeatedReadAccesses;

        /// <summary>
        /// Initializes a new instance of the <see cref="TraceCycleReducer"/> class.
        /// </summary>
        internal TraceCycleReducer()
        {
            this.RepeatedReadAccesses = new HashSet<string>();
        }

        /// <inheritdoc/>
        public IEnumerable<ControlledOperation> ReduceOperations(IEnumerable<ControlledOperation> ops, ControlledOperation current)
        {
            // Find all operations that perform 'WRITE' accesses.
            var writeAccessOps = ops.Where(op => op.LastSchedulingPoint is SchedulingPointType.Write).ToArray();

            // Filter out all 'READ' operations that are repeatedly 'READ' accessing shared state when there is a 'WRITE' access.
            var filteredOps = ops.Where(op => op.LastSchedulingPoint is SchedulingPointType.Read &&
                this.RepeatedReadAccesses.Any(state => op.LastAccessedSharedState == state) &&
                writeAccessOps.Any(wop =>
                    wop.LastAccessedSharedStateComparer?.Equals(wop.LastAccessedSharedState, op.LastAccessedSharedState) ??
                    wop.LastAccessedSharedState == op.LastAccessedSharedState)).ToArray();

            if (current.LastSchedulingPoint is SchedulingPointType.Read)
            {
                // The current operation is a 'READ' access, so add it to the set of repeated read accesses.
                this.RepeatedReadAccesses.Add(current.LastAccessedSharedState);
            }
            else if (current.LastSchedulingPoint is SchedulingPointType.Write)
            {
                // The current operation is a 'WRITE' access, so remove it from the set of repeated read accesses.
                this.RepeatedReadAccesses.RemoveWhere(state =>
                    current.LastAccessedSharedStateComparer?.Equals(current.LastAccessedSharedState, state) ??
                    current.LastAccessedSharedState == state);
            }

            return ops.Except(filteredOps);
        }
    }
}
