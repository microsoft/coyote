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
        /// The test execution context across iterations.
        /// </summary>
        private readonly ExecutionContext Context;

        /// <summary>
        /// Initializes a new instance of the <see cref="TraceCycleReducer"/> class.
        /// </summary>
        internal TraceCycleReducer(ExecutionContext context)
        {
            this.Context = context;
        }

        /// <inheritdoc/>
        public void InitializeNextIteration(uint iteration)
        {
        }

        /// <inheritdoc/>
        public IEnumerable<ControlledOperation> ReduceOperations(IEnumerable<ControlledOperation> ops, ControlledOperation current, ulong state)
        {
            // Check if there is any write operation, and if yes do nothing.
            if (!ops.All(op => this.Context.IsOperationGroupReadOnly(op.Group)))
            {
                return ops;
            }

            var highFrequencyOps = ops.Where(op => this.Context.GetStateFrequencyOfOperation(op) > 10).ToArray();
            return ops.Except(highFrequencyOps);
        }
    }
}
