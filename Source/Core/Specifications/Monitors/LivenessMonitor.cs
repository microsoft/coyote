// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Coyote.SystematicTesting;

namespace Microsoft.Coyote.Specifications
{
    /// <summary>
    /// A monitor that checks if a liveness property associated with a <see cref="TaskOperation"/> is eventually satisfied.
    /// </summary>
    internal sealed class LivenessMonitor
    {
        /// <summary>
        /// The operation waiting for the liveness property to get satisfied.
        /// </summary>
        internal readonly TaskOperation Operation;

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
        /// True if the liveness property has been checked since the last time the hash was updated, else false.
        /// </summary>
        private bool IsPropertyChecked;

        /// <summary>
        /// True if the liveness property is satisfied, else false.
        /// </summary>
        internal bool IsSatisfied { get; private set; }

        /// <summary>
        /// Trace used for debugging purposes.
        /// </summary>
        internal StackTrace StackTrace { get; private set; }

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
            this.IsPropertyChecked = false;
            this.IsSatisfied = false;
        }

        /// <summary>
        /// Checks if there is progress related to the liveness property.
        /// </summary>
        internal void CheckProgress()
        {
            if (!this.IsPropertyChecked)
            {
                int hash = this.HashingFunction();
                if (this.Hash != hash)
                {
                    // There is progress, so unblock the task waiting on the resource.
                    this.Resource.SignalAll();
                    this.Hash = hash;
                }
            }
        }

        // BUG: Hash resets during a context switch between 1 and 4 below!!!

        /// <summary>
        /// Waits until the liveness property gets satisfied.
        /// </summary>
        internal void Wait()
        {
            // Get the initial hash.
            this.Hash = this.HashingFunction();
            this.StackTrace = new StackTrace();

            while (true)
            {
                this.IsPropertyChecked = true;

                Task<bool> task = this.Predicate();
                if (!task.IsCompleted)
                {
                    this.Operation.BlockUntilTaskCompletes(task);
                }

                if (task.Result)
                {
                    this.IsSatisfied = true;
                    break;
                }

                // Block on the resource until there is progress.
                this.IsPropertyChecked = false;
                this.Resource.Wait();
            }
        }
    }
}
