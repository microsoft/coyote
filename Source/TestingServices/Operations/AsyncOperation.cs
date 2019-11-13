// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Threading;

namespace Microsoft.Coyote.TestingServices.Scheduling
{
    /// <summary>
    /// Contains information about an asynchronous operation
    /// that can be controlled during testing.
    /// </summary>
    [DebuggerStepThrough]
    internal abstract class AsyncOperation : IAsyncOperation
    {
        /// <summary>
        /// Stores the unique scheduler id that executes this operation.
        /// </summary>
        internal static readonly AsyncLocal<Guid> SchedulerId = new AsyncLocal<Guid>();

        /// <summary>
        /// The scheduler executing this operation.
        /// </summary>
        internal readonly OperationScheduler Scheduler;

        /// <summary>
        /// The unique id of the operation.
        /// </summary>
        public abstract ulong Id { get; }

        /// <summary>
        /// The unique name of the operation.
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// The status of the operation. An operation can be scheduled only
        /// if it is <see cref="AsyncOperationStatus.Enabled"/>.
        /// </summary>
        public AsyncOperationStatus Status { get; internal set; }

        /// <summary>
        /// Is the source of the operation active.
        /// </summary>
        internal bool IsActive;

        /// <summary>
        /// True if the handler of the source of the operation is running, else false.
        /// </summary>
        internal bool IsHandlerRunning;

        /// <summary>
        /// True if the next awaiter is controlled, else false.
        /// </summary>
        internal bool IsAwaiterControlled;

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncOperation"/> class.
        /// </summary>
        internal AsyncOperation(OperationScheduler scheduler)
        {
            this.Scheduler = scheduler;
            this.Status = AsyncOperationStatus.None;
            this.IsActive = false;
            this.IsHandlerRunning = false;
            this.IsAwaiterControlled = false;
        }

        /// <summary>
        /// Invoked when the operation has been enabled.
        /// </summary>
        internal void OnEnabled()
        {
            this.Status = AsyncOperationStatus.Enabled;
            this.IsActive = false;
            this.IsHandlerRunning = false;
        }

        /// <summary>
        /// Invoked when the operation completes.
        /// </summary>
        internal virtual void OnCompleted()
        {
            this.Status = AsyncOperationStatus.Completed;
            this.IsHandlerRunning = false;
        }

        /// <summary>
        /// Tries to enable the operation, if it was not already enabled.
        /// </summary>
        internal virtual void TryEnable()
        {
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj is AsyncOperation op)
            {
                return this.Id == op.Id;
            }

            return false;
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        public override int GetHashCode() => (int)this.Id;
    }
}
