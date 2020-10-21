// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Coyote.SystematicTesting
{
    /// <summary>
    /// Represents an abstract asynchronous operation that can be controlled during systematic testing.
    /// </summary>
    internal abstract class AsyncOperation
    {
        /// <summary>
        /// The unique id of the operation.
        /// </summary>
        internal ulong Id { get; }

        /// <summary>
        /// The unique name of the operation.
        /// </summary>
        internal string Name { get; }

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
        /// Initializes a new instance of the <see cref="AsyncOperation"/> class.
        /// </summary>
        internal AsyncOperation(ulong operationId, string name)
        {
            this.Id = operationId;
            this.Name = name;
            this.Status = AsyncOperationStatus.None;
        }

        /// <summary>
        /// Invoked when the operation completes.
        /// </summary>
        internal virtual void OnCompleted()
        {
            this.Status = AsyncOperationStatus.Completed;
        }

        /// <summary>
        /// Tries to enable the operation, if it is not already enabled.
        /// </summary>
        internal virtual bool TryEnable() => false;

        /// <summary>
        /// Checks if the operation is blocked on an uncontrolled dependency.
        /// </summary>
        internal virtual bool IsBlockedOnUncontrolledDependency() => false;

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
