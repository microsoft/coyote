// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !DEBUG
using System.Diagnostics;
#endif

namespace Microsoft.Coyote.SystematicTesting
{
    /// <summary>
    /// An abstract asynchronous operation that can be controlled during testing.
    /// </summary>
#if !DEBUG
    [DebuggerStepThrough]
#endif
    internal abstract class AsyncOperation
    {
        /// <summary>
        /// The unique id of the operation.
        /// </summary>
        internal abstract ulong Id { get; }

        /// <summary>
        /// The unique name of the operation.
        /// </summary>
        internal abstract string Name { get; }

        /// <summary>
        /// The status of the operation. An operation can be scheduled only
        /// if it is <see cref="AsyncOperationStatus.Enabled"/>.
        /// </summary>
        internal AsyncOperationStatus Status;

        /// <summary>
        /// A value that represents the hashed program state when
        /// this operation last executed.
        /// </summary>
        internal int HashedProgramState;

        /// <summary>
        /// True if the handler of the source of the operation is running, else false.
        /// </summary>
        internal bool IsHandlerRunning; // TODO: figure out if this can be replaced by status.

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncOperation"/> class.
        /// </summary>
        internal AsyncOperation()
        {
            this.Status = AsyncOperationStatus.None;
            this.IsHandlerRunning = false;
        }

        /// <summary>
        /// Invoked when the operation has been enabled.
        /// </summary>
        internal void OnEnabled()
        {
            this.Status = AsyncOperationStatus.Enabled;
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
