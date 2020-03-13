// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace Microsoft.Coyote.Runtime.Exploration
{
    /// <summary>
    /// Resource that can be used to synchronize asynchronous operations.
    /// </summary>
    internal readonly struct Resource
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
        /// Initializes a new instance of the <see cref="Resource"/> struct.
        /// </summary>
        private Resource(CoyoteRuntime runtime)
        {
            this.Runtime = runtime;
            this.AwaitingOperations = new HashSet<AsyncOperation>();
        }

        /// <summary>
        /// Creates a new instance of the <see cref="Resource"/> struct.
        /// </summary>
        /// <returns>The resource instance.</returns>
        public static Resource Create() => new Resource(CoyoteRuntime.Current);

        /// <summary>
        /// Notifies that the currently executing asynchronous operation is waiting
        /// for the resource to be released.
        /// </summary>
        public void NotifyWait()
        {
            var op = this.Runtime.GetExecutingOperation<AsyncOperation>();
            op.Status = AsyncOperationStatus.BlockedOnResource;
            this.AwaitingOperations.Add(op);
        }

        /// <summary>
        /// Notifies all waiting asynchronous operations waiting on this resource,
        /// that the resource has been released.
        /// </summary>
        public void NotifyRelease()
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
