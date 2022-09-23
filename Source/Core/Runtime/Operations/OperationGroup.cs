// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Microsoft.Coyote.Runtime
{
    /// <summary>
    /// Represents a group of controlled operations that can be scheduled together during testing.
    /// </summary>
    internal class OperationGroup : IEnumerable<ControlledOperation>, IEquatable<OperationGroup>
    {
        /// <summary>
        /// Provides access to the operation group associated with each async local context,
        /// or null if the current async local context has no associated group.
        /// </summary>
        private protected static readonly AsyncLocal<OperationGroup> AsyncLocalGroup =
            new AsyncLocal<OperationGroup>();

        /// <summary>
        /// The operation group associated with the current execution context, if any.
        /// </summary>
        internal static OperationGroup Current => AsyncLocalGroup.Value;

        /// <summary>
        /// The unique id of this group.
        /// </summary>
        internal readonly Guid Id;

        /// <summary>
        /// The controlled operation that owns this group.
        /// </summary>
        internal readonly ControlledOperation Owner;

        /// <summary>
        /// The controlled operations that are members of this group.
        /// </summary>
        private readonly HashSet<ControlledOperation> Members;

        /// <summary>
        /// Initializes a new instance of the <see cref="OperationGroup"/> class.
        /// </summary>
        private OperationGroup(Guid? id, ControlledOperation owner)
        {
            this.Id = id ?? Guid.NewGuid();
            this.Owner = owner;
            this.Members = new HashSet<ControlledOperation>();
        }

        /// <summary>
        /// Creates a new <see cref="OperationGroup"/> instance.
        /// </summary>
        internal static OperationGroup Create(ControlledOperation owner) => Create(null, owner);

        /// <summary>
        /// Creates a new <see cref="OperationGroup"/> instance with the specified id.
        /// </summary>
        internal static OperationGroup Create(Guid? id, ControlledOperation owner) => new OperationGroup(id, owner);

        /// <summary>
        /// Registers the specified operation as a member of this group.
        /// </summary>
        internal void RegisterMember(ControlledOperation member) => this.Members.Add(member);

        /// <summary>
        /// Returns an enumerator that iterates through the members of this group.
        /// </summary>
        public IEnumerator<ControlledOperation> GetEnumerator()
        {
            foreach (ControlledOperation op in this.Members)
            {
                yield return op;
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through the members of this group.
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        /// <summary>
        /// Returns true if the specified operation is a member of this group, else false.
        /// </summary>
        internal bool IsMember(ControlledOperation operation) => this.Members.Contains(operation);

        /// <summary>
        /// Determines whether all members of this group have completed.
        /// </summary>
        internal bool IsCompleted() => this.Members.All(op => op.Status is OperationStatus.Completed);

        /// <summary>
        /// Associates the specified operation group with the currently executing thread,
        /// allowing future retrieval in the same thread, as well as across threads that
        /// share the same asynchronous control flow.
        /// </summary>
        internal static void SetCurrent(OperationGroup group)
        {
            if (group != null)
            {
                AsyncLocalGroup.Value = group;
            }
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj is OperationGroup op)
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
        /// Returns a string that represents the current group id.
        /// </summary>
        public override string ToString() => this.Id.ToString();

        /// <summary>
        /// Indicates whether the specified <see cref="OperationGroup"/> is equal
        /// to the current <see cref="OperationGroup"/>.
        /// </summary>
        public bool Equals(OperationGroup other) => this.Equals((object)other);

        /// <summary>
        /// Indicates whether the specified <see cref="OperationGroup"/> is equal
        /// to the current <see cref="OperationGroup"/>.
        /// </summary>
        bool IEquatable<OperationGroup>.Equals(OperationGroup other) => this.Equals(other);

        /// <summary>
        /// Removes this operation group from the local context.
        /// </summary>
        internal static void RemoveFromContext()
        {
            AsyncLocalGroup.Value = null;
        }
    }
}
