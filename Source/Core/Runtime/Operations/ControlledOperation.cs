// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace Microsoft.Coyote.Runtime
{
    /// <summary>
    /// Represents an operation that can be controlled during testing.
    /// </summary>
    internal class ControlledOperation : IEquatable<ControlledOperation>
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
        /// if it is <see cref="OperationStatus.Enabled"/>.
        /// </summary>
        internal OperationStatus Status;

        /// <summary>
        /// The type of the last encountered scheduling point.
        /// </summary>
        internal SchedulingPointType SchedulingPoint;

        /// <summary>
        /// Set of dependencies that must get satisfied before this operation can resume executing.
        /// </summary>
        internal readonly HashSet<object> Dependencies;

        /// <summary>
        /// True if at least one of the dependencies is uncontrolled, else false.
        /// </summary>
        internal bool IsDependencyUncontrolled;

        /// <summary>
        /// A value that represents the hashed program state when
        /// this operation last executed.
        /// </summary>
        internal int HashedProgramState;

        /// <summary>
        /// Initializes a new instance of the <see cref="ControlledOperation"/> class.
        /// </summary>
        internal ControlledOperation(ulong operationId, string name)
        {
            this.Id = operationId;
            this.Name = name;
            this.Status = OperationStatus.None;
            this.SchedulingPoint = SchedulingPointType.Start;
            this.Dependencies = new HashSet<object>();
        }

        /// <summary>
        /// Returns the hashed state of this operation for the specified policy.
        /// </summary>
        internal virtual int GetHashedState(SchedulingPolicy policy) => 0;

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj is ControlledOperation op)
            {
                return this.Id == op.Id;
            }

            return false;
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        public override int GetHashCode() => this.Id.GetHashCode();

        /// <summary>
        /// Returns a string that represents the current actor id.
        /// </summary>
        public override string ToString() => this.Name;

        /// <summary>
        /// Indicates whether the specified <see cref="ControlledOperation"/> is equal
        /// to the current <see cref="ControlledOperation"/>.
        /// </summary>
        public bool Equals(ControlledOperation other) => this.Equals((object)other);

        /// <summary>
        /// Indicates whether the specified <see cref="ControlledOperation"/> is equal
        /// to the current <see cref="ControlledOperation"/>.
        /// </summary>
        bool IEquatable<ControlledOperation>.Equals(ControlledOperation other) => this.Equals(other);
    }
}
