// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Coyote.SystematicTesting;

namespace Microsoft.Coyote.Specifications
{
    /// <summary>
    /// A monitor that checks if a liveness property has been eventually satisfied.
    /// </summary>
    internal sealed class LivenessMonitor
    {
        /// <summary>
        /// The operation waiting for the liveness property to get satisfied.
        /// </summary>
        private readonly TaskOperation Operation;

        /// <summary>
        /// The resource associated with this property.
        /// </summary>
        private readonly Resource Resource;

        /// <summary>
        /// Predicate that must hold to satisfy the liveness property.
        /// </summary>
        private readonly Func<Task<bool>> Predicate;

        /// <summary>
        /// Function that returns a hash used to track progress related to the liveness property.
        /// </summary>
        private readonly Func<int> HashingFunction;

        /// <summary>
        /// Hash used to track progress related to the liveness property.
        /// </summary>
        private int Hash;

        /// <summary>
        /// True if the liveness property is satisfied, else false.
        /// </summary>
        internal bool IsSatisfied { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LivenessMonitor"/> class.
        /// </summary>
        internal LivenessMonitor(TaskOperation op, Func<Task<bool>> predicate, Func<int> hashingFunction)
        {
            this.Operation = op;
            this.Resource = new Resource();
            this.Predicate = predicate;
            this.HashingFunction = hashingFunction;
            this.Hash = 0;
            this.IsSatisfied = false;
        }

        /// <summary>
        /// Checks if there is progress related to the liveness property.
        /// </summary>
        internal void CheckProgress()
        {
            int hash = this.HashingFunction();
            if (this.Hash != hash)
            {
                // There is progress, so unblock the task waiting on the resource.
                this.Resource.SignalAll();
                this.Hash = hash;
            }
        }

        /// <summary>
        /// Waits until the liveness property gets satisfied.
        /// </summary>
        internal void Wait()
        {
            // Get the initial hash.
            this.Hash = this.HashingFunction();

            while (true)
            {
                Task<bool> task = this.Predicate();
                if (!task.IsCompleted)
                {
                    this.Operation.BlockUntilTaskCompletes(task);
                }

                if (task.Result)
                {
                    break;
                }

                // Block on the resource until there is progress.
                this.Resource.Wait();
            }
        }
    }
}
