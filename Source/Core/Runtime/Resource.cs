// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace Microsoft.Coyote.Runtime
{
    /// <summary>
    /// Resource that can be used to synchronize asynchronous operations.
    /// </summary>
    internal class Resource
    {
        /// <summary>
        /// The runtime associated with this resource.
        /// </summary>
        internal readonly CoyoteRuntime Runtime;

        /// <summary>
        /// Set of asynchronous operations that are waiting on the resource to be released.
        /// </summary>
        private readonly HashSet<AsyncOperation> AwaitingOperations;

        /// <summary>
        /// Initializes a new instance of the <see cref="Resource"/> class.
        /// </summary>
        internal Resource()
        {
            this.Runtime = CoyoteRuntime.Current;
            this.AwaitingOperations = new HashSet<AsyncOperation>();
        }

        /// <summary>
        /// Waits for the resource to be released.
        /// </summary>
        internal void Wait()
        {
            var op = this.Runtime.GetExecutingOperation<AsyncOperation>();
            op.Status = AsyncOperationStatus.BlockedOnResource;
            this.AwaitingOperations.Add(op);
            this.Runtime.ScheduleNextOperation(false, true);
        }

        /// <summary>
        /// Signals the specified waiting operation that the resource has been released.
        /// </summary>
        internal void Signal(AsyncOperation op)
        {
            if (this.AwaitingOperations.Contains(op))
            {
                op.Status = AsyncOperationStatus.Enabled;
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
                op.Status = AsyncOperationStatus.Enabled;
            }

            // We need to clear the whole set, because we signal all awaiting asynchronous
            // operations to wake up, else we could set as enabled an operation that is not
            // any more waiting for this resource at a future point.
            this.AwaitingOperations.Clear();
        }
    }
}
