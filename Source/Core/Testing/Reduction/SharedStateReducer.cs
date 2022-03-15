// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Coyote.Runtime;

namespace Microsoft.Coyote.Testing.Reduction
{
    /// <summary>
    /// A reducer that analyzes shared state accesses to reduce the set of operations
    /// to be scheduled at each scheduling step.
    /// </summary>
    internal sealed class SharedStateReducer : IScheduleReducer
    {
        /// <summary>
        /// Set of values corresponding to shared state that has been accessed
        /// by 'READ' operations across all iterations.
        /// </summary>
        private readonly HashSet<string> ReadAccesses;

        /// <summary>
        /// Set of values corresponding to shared state that has been accessed
        /// by 'WRITE' operations across all iterations.
        /// </summary>
        private readonly HashSet<string> WriteAccesses;

        /// <summary>
        /// Initializes a new instance of the <see cref="SharedStateReducer"/> class.
        /// </summary>
        internal SharedStateReducer()
        {
            this.ReadAccesses = new HashSet<string>();
            this.WriteAccesses = new HashSet<string>();
        }

        /// <inheritdoc/>
        public List<ControlledOperation> ReduceOperations(ControlledOperation current, List<ControlledOperation> ops)
        {
            // Find all operations that are not accessing any shared state.
            var noStateAccessOps = ops.Where(op => op.LastSchedulingPoint != SchedulingPointType.Read &&
                op.LastSchedulingPoint != SchedulingPointType.Write);
            if (noStateAccessOps.Any())
            {
                // There are operations that are not accessing any shared state, so prioritize them.
                ops = noStateAccessOps.ToList();
            }
            else
            {
                // Split the operations that are accessing shared state into a 'READ' and 'WRITE' group.
                var readAccessOps = ops.Where(op => op.LastSchedulingPoint is SchedulingPointType.Read);
                var writeAccessOps = ops.Where(op => op.LastSchedulingPoint is SchedulingPointType.Write);

                // Update the known 'READ' and 'WRITE' accesses so far.
                this.ReadAccesses.UnionWith(readAccessOps.Select(op => op.LastAccessedState));
                this.WriteAccesses.UnionWith(writeAccessOps.Select(op => op.LastAccessedState));

                // Find if there are any read-only accesses. Note that this is just an approximation
                // based on current knowledge. An access that is considered read-only might not be
                // considered anymore in later steps or iterations.
                var readOnlyAccessOps = readAccessOps.Where(op => !this.WriteAccesses.Any(
                    state => state == op.LastAccessedState));
                if (readOnlyAccessOps.Any())
                {
                    // Prioritize any read-only access operation.
                    ops = readOnlyAccessOps.ToList();
                }

                // else
                // {
                //     // There are operations writing to shared state.
                //     var stateAccessingGroups = result.Select(op => op.Group).Distinct();
                //     if (stateAccessingGroups.Any(group => !group.IsReadOnly))
                //     {
                //         // this.TryChangeGroupPriorities(result);
                //     }
                // }
            }

            // if (this.GetPrioritizedOperationGroup(result, out OperationGroup nextGroup))
            // {
            //     if (nextGroup == current.Group)
            //     {
            //         // Maybe dont need this ...
            //         // Choose the current operation, if it is enabled.
            //         var currentGroupOps = result.Where(op => op.Id == current.Id).ToList();
            //         if (currentGroupOps.Count is 1)
            //         {
            //             result = currentGroupOps;
            //             return true;
            //         }
            //     }
            //     result = result.Where(op => nextGroup.IsMember(op)).ToList();
            //     foreach (var op in result)
            //     {
            //         System.Console.WriteLine($">>>>>>>> op: {op.Name} (sp: {op.LastSchedulingPoint} | o: {op.Group.Owner} | g: {op.Group})");
            //     }
            // }

            return ops;
        }
    }
}
