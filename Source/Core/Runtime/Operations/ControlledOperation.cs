// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace Microsoft.Coyote.Runtime
{
    /// <summary>
    /// Represents an operation that can be controlled during testing.
    /// </summary>
    internal class ControlledOperation : IEquatable<ControlledOperation>, IDisposable
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
        /// The group that this operation has membership. This can be used
        /// by the scheduler to optimize exploration.
        /// </summary>
        internal readonly OperationGroup Group;

        /// <summary>
        /// Set of dependencies that must get satisfied before this operation can resume executing.
        /// </summary>
        internal readonly HashSet<object> Dependencies;

        /// <summary>
        /// The type of the last encountered scheduling point.
        /// </summary>
        internal SchedulingPointType LastSchedulingPoint;

        /// <summary>
        /// A value that represents the hashed program state when this operation last executed.
        /// </summary>
        internal int LastHashedProgramState;

        /// <summary>
        /// A value that represents the state being accessed when this operation last executed.
        /// </summary>
        internal string LastAccessedState;

        /// <summary>
        /// True if this is a read-only operation that cannot modify shared state, else false.
        /// </summary>
        internal readonly bool IsReadOnly;

        /// <summary>
        /// True if the source of this operation is uncontrolled, else false.
        /// </summary>
        internal bool IsSourceUncontrolled;

        /// <summary>
        /// True if at least one of the dependencies is uncontrolled, else false.
        /// </summary>
        internal bool IsAnyDependencyUncontrolled;

        /// <summary>
        /// True if this is the root operation, else false.
        /// </summary>
        internal bool IsRoot => this.Id is 0;

        /// <summary>
        /// True if this operation is currently blocked, else false.
        /// </summary>
        internal bool IsBlocked =>
            this.Status is OperationStatus.BlockedOnWaitAll ||
            this.Status is OperationStatus.BlockedOnWaitAny ||
            this.Status is OperationStatus.BlockedOnReceive ||
            this.Status is OperationStatus.BlockedOnResource;

        /// <summary>
        /// Initializes a new instance of the <see cref="ControlledOperation"/> class.
        /// </summary>
        internal ControlledOperation(ulong operationId, string name, OperationGroup group = null, bool isReadOnly = true)
        {
            this.Id = operationId;
            this.Name = name;
            this.Status = OperationStatus.None;
            this.Group = group ?? OperationGroup.Create(this);
            this.Dependencies = new HashSet<object>();
            this.LastSchedulingPoint = SchedulingPointType.Start;
            this.LastHashedProgramState = 0;
            this.LastAccessedState = string.Empty;
            // TODO: set by default to false, unless the HTTP abstraction is set.
            this.IsReadOnly = isReadOnly;
            this.IsSourceUncontrolled = false;
            this.IsAnyDependencyUncontrolled = false;

            // Assign this operation as a member of this group.
            this.Group.RegisterMember(this);
        }

        /// <summary>
        /// Sets the specified dependency.
        /// </summary>
        internal void SetDependency(object dependency, bool isControlled)
        {
            this.Dependencies.Add(dependency);
            this.IsAnyDependencyUncontrolled |= !isControlled;
        }

        /// <summary>
        /// Unblocks the operation by clearing its dependencies.
        /// </summary>
        internal void Unblock()
        {
            this.Dependencies.Clear();
            this.IsAnyDependencyUncontrolled = false;
            this.Status = OperationStatus.Enabled;
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
        /// Returns a string that represents the current operation id.
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

        /// <summary>
        /// Disposes the contents of this object.
        /// </summary>
        public void Dispose()
        {
            this.Dependencies.Clear();
        }
    }
}
