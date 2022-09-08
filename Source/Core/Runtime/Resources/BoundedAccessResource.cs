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
        /// The current number of operations that have access to the resource.
        /// </summary>
        private uint CurrentCount;

        /// <summary>
        /// The max number of operations that can access the resource.
        /// </summary>
        private readonly uint MaxCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="BoundedAccessResource"/> class.
        /// </summary>
        internal BoundedAccessResource(CoyoteRuntime runtime, uint initialCount, uint maxCount)
        {
            this.Runtime = runtime;
            this.AwaitingOperations = new HashSet<ControlledOperation>();
            this.CurrentCount = initialCount;
            this.MaxCount = maxCount;
        }

        /// <summary>
        /// Invoked when an operation is trying to acquire the resource.
        /// </summary>
        internal void Acquire()
        {
            var op = this.Runtime.GetExecutingOperation();
            if (this.CurrentCount == this.MaxCount)
            {

            }
            else
            {
                
                this.CurrentCount++;
            }

            op.Status = OperationStatus.BlockedOnResource;
            this.AwaitingOperations.Add(op);
            this.Runtime.ScheduleNextOperation(SchedulingPointType.Wait);
        }

        /// <summary>
        /// Invoked when an operation releases the resource.
        /// </summary>
        internal void Release(ControlledOperation op)
        {
            if (this.AwaitingOperations.Contains(op))
            {
                op.Status = OperationStatus.Enabled;
                this.AwaitingOperations.Remove(op);
            }
        }

        /// <summary>
        /// Returns true if the resource is currently available, else false.
        /// </summary>
        private bool IsAvailable => this.CurrentCount < this.MaxCount;

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
