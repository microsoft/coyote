// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Coyote.Runtime
{
    /// <summary>
    /// A reducer that prioritizes non-racy access scheduling decisions.
    /// </summary>
    internal sealed class RacyAccessReducer : IScheduleReducer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RacyAccessReducer"/> class.
        /// </summary>
        internal RacyAccessReducer()
        {
        }

        /// <inheritdoc/>
        public IEnumerable<ControlledOperation> ReduceOperations(IEnumerable<ControlledOperation> ops, ControlledOperation current)
        {
            // Find all operations that are not invoking a 'READ' or 'WRITE' scheduling decision,
            // and if there are any, then return them. This effectively helps racy scheduling
            // decisions to happen as close to each other as possible, which helps to find bugs
            // that are caused by the interleaving of these operations.
            var noReadOrWriteSchedulingOps = ops.Where(op => !SchedulingPoint.IsReadOrWrite(op.LastSchedulingPoint));
            if (noReadOrWriteSchedulingOps.Any())
            {
                return noReadOrWriteSchedulingOps.ToArray();
            }

            return ops;
        }
    }
}
