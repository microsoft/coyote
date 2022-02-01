﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Microsoft.Coyote.Runtime
{
    /// <summary>
    /// Represents an abstract asynchronous operation that can be controlled during systematic testing.
    /// </summary>
    internal abstract class AsyncOperation : IEquatable<AsyncOperation>
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
        /// The type of the last encountered scheduling point.
        /// </summary>
        internal SchedulingPointType SchedulingPoint;

        /// <summary>
        /// A value that represents the hashed program state when
        /// this operation last executed.
        /// </summary>
        internal int HashedProgramState;

        internal int Thread;

        internal string Msg;

        internal string StackTrace;

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncOperation"/> class.
        /// </summary>
        internal AsyncOperation(ulong operationId, string name)
        {
            this.Id = operationId;
            this.Name = name;
            this.Status = AsyncOperationStatus.None;
            this.SchedulingPoint = SchedulingPointType.Start;
            this.Thread = -1;
            this.Msg = CoyoteRuntime.AsyncLocalDebugInfo.Value ?? string.Empty;
            if (this.Msg.Length > 0)
            {
                IO.Debug.WriteLine($"---> Created Op '{this.Id}' with msg '{this.Msg}' from thread '{System.Threading.Thread.CurrentThread.ManagedThreadId}'");
            }
            else
            {
                IO.Debug.WriteLine($"---> Created Op '{this.Id}' from thread '{System.Threading.Thread.CurrentThread.ManagedThreadId}'");
            }

            this.StackTrace = new System.Diagnostics.StackTrace().ToString();
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
            if (obj is AsyncOperation op)
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
        /// Indicates whether the specified <see cref="AsyncOperation"/> is equal
        /// to the current <see cref="AsyncOperation"/>.
        /// </summary>
        public bool Equals(AsyncOperation other) => this.Equals((object)other);

        /// <summary>
        /// Indicates whether the specified <see cref="AsyncOperation"/> is equal
        /// to the current <see cref="AsyncOperation"/>.
        /// </summary>
        bool IEquatable<AsyncOperation>.Equals(AsyncOperation other) => this.Equals(other);
    }
}
