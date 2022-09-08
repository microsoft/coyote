// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace Microsoft.Coyote.Runtime
{
    /// <summary>
    /// Resource for modeling a synchronization primitive that allows access to a bounded number of operations.
    /// </summary>
    internal class BoundedAccessResource
    {
        /// <summary>
        /// The runtime associated with this resource.
        /// </summary>
        internal readonly CoyoteRuntime Runtime;

        /// <summary>
        /// Set of asynchronous operations that are waiting on the resource to be released.
        /// </summary>
        private readonly HashSet<ControlledOperation> AwaitingOperations;

        /// <summary>
        /// Initializes a new instance of the <see cref="BoundedAccessResource"/> class.
        /// </summary>
        internal BoundedAccessResource()
        {
            this.Runtime = CoyoteRuntime.Current;
            this.AwaitingOperations = new HashSet<ControlledOperation>();
        }

        /// <summary>
        /// Acquires the resource.
        /// </summary>
        internal void Acquire()
        {
            var op = this.Runtime.GetExecutingOperation();
            op.Status = OperationStatus.BlockedOnResource;
            this.AwaitingOperations.Add(op);
            this.Runtime.ScheduleNextOperation(SchedulingPointType.Wait);
        }

        /// <summary>
        /// Signals the specified waiting operation that the resource has been released.
        /// </summary>
        internal void Signal(ControlledOperation op)
        {
            if (this.AwaitingOperations.Contains(op))
            {
                op.Status = OperationStatus.Enabled;
                this.AwaitingOperations.Remove(op);
            }
        }

        /// <summary>
        /// Signals all waiting operations that the resource has been released.
        /// </summary>
        internal void SignalAll()
        {
            foreach (var op in this.AwaitingOperations)
            {
                op.Status = OperationStatus.Enabled;
            }

            // We need to clear the whole set, because we signal all awaiting asynchronous
            // operations to wake up, else we could set as enabled an operation that is not
            // any more waiting for this resource at a future point.
            this.AwaitingOperations.Clear();
        }

        /// <summary>
        /// The status of the resource.
        /// </summary>
        private enum Status
        {
            Released = 0,
            Acquired
        };
    }
}
