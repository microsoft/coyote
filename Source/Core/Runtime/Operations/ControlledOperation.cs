// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;

namespace Microsoft.Coyote.Runtime
{
    /// <summary>
    /// Represents an operation that can be controlled during testing.
    /// </summary>
    internal class ControlledOperation : IEquatable<ControlledOperation>, IDisposable
    {
        /// <summary>
        /// The runtime managing this operation.
        /// </summary>
        internal readonly CoyoteRuntime Runtime;

        /// <summary>
        /// The unique id of this operation.
        /// </summary>
        internal ulong Id { get; }

        /// <summary>
        /// The name of this operation.
        /// </summary>
        internal string Name { get; }

        /// <summary>
        /// The description of this operation.
        /// </summary>
        internal string Description { get; }

        /// <summary>
        /// The status of this operation. An operation can be scheduled only
        /// if it is <see cref="OperationStatus.Enabled"/>.
        /// </summary>
        internal OperationStatus Status;

        /// <summary>
        /// The group where this operation has membership. This can be used
        /// by the scheduler to optimize exploration.
        /// </summary>
        internal readonly OperationGroup Group;

        /// <summary>
        /// Queue of continuations that this operation must execute before it completes.
        /// </summary>
        private readonly Queue<Action> Continuations;

        /// <summary>
        /// Dependency that must get resolved before this operation can resume executing.
        /// </summary>
        private Func<bool> Dependency;

        /// <summary>
        /// Synchronization mechanism for controlling the execution of this operation.
        /// </summary>
        private ManualResetEventSlim SyncEvent;

        /// <summary>
        /// The type of the last encountered scheduling point.
        /// </summary>
        internal SchedulingPointType LastSchedulingPoint;

        /// <summary>
        /// A value that represents the hashed program state when this operation last executed.
        /// </summary>
        internal int LastHashedProgramState;

        /// <summary>
        /// A value that represents the shared state being accessed when this
        /// operation last executed, if there was any such state access.
        /// </summary>
        internal string LastAccessedSharedState;

        /// <summary>
        /// A comparer that checks if the shared state being accessed when this
        /// operation last executed is equal with another shared state that is
        /// being accessed by some other operation.
        /// </summary>
        internal IEqualityComparer<string> LastAccessedSharedStateComparer;

        /// <summary>
        /// True if the source of this operation is uncontrolled, else false.
        /// </summary>
        internal bool IsSourceUncontrolled;

        /// <summary>
        /// True if the dependency is uncontrolled, else false.
        /// </summary>
        internal bool IsDependencyUncontrolled;

        /// <summary>
        /// True if this is the root operation, else false.
        /// </summary>
        internal bool IsRoot => this.Id is 0;

        /// <summary>
        /// True if this operation is currently paused, else false.
        /// </summary>
        internal bool IsPaused =>
            this.Status is OperationStatus.Paused ||
            this.Status is OperationStatus.PausedOnDelay ||
            this.Status is OperationStatus.PausedOnResource ||
            this.Status is OperationStatus.PausedOnReceive;

        /// <summary>
        /// Initializes a new instance of the <see cref="ControlledOperation"/> class.
        /// </summary>
        internal ControlledOperation(ulong operationId, string name, string description, OperationGroup group, CoyoteRuntime runtime)
        {
            this.Runtime = runtime;
            this.Id = operationId;
            this.Name = name;
            this.Description = description ?? string.Empty;
            this.Status = OperationStatus.None;
            this.Group = group ?? OperationGroup.Create(this);
            this.Continuations = new Queue<Action>();
            this.SyncEvent = new ManualResetEventSlim(false);
            this.LastSchedulingPoint = SchedulingPointType.Start;
            this.LastHashedProgramState = 0;
            this.LastAccessedSharedState = string.Empty;
            this.LastAccessedSharedStateComparer = null;
            this.IsSourceUncontrolled = false;
            this.IsDependencyUncontrolled = false;

            // Register this operation with the runtime.
            this.Runtime.RegisterNewOperation(this);
        }

        /// <summary>
        /// Executes all continuations of this operation in order, if there are any.
        /// </summary>
        internal void ExecuteContinuations()
        {
            // New continuations can be added while executing a continuation,
            // so keep executing them until the queue is drained.
            while (this.Continuations.Count > 0)
            {
                var nextContinuation = this.Continuations.Dequeue();
                nextContinuation();
            }
        }

        /// <summary>
        /// Pauses the execution of this operation until it receives a signal.
        /// </summary>
        /// <remarks>
        /// It is assumed that this method is invoked by the same thread executing this operation.
        /// </remarks>
        internal void WaitSignal()
        {
            try
            {
                this.SyncEvent.Wait();
                this.SyncEvent.Reset();
            }
            catch (ObjectDisposedException)
            {
                // The handler was disposed, so we can ignore this exception.
            }
        }

        /// <summary>
        /// Signals this operation to resume its execution.
        /// </summary>
        internal void Signal() => this.SyncEvent.Set();

        /// <summary>
        /// Sets a callback that executes the next continuation of this operation.
        /// </summary>
        internal void SetContinuationCallback(Action callback) => this.Continuations.Enqueue(callback);

        /// <summary>
        /// Pauses this operation and sets a callback that returns true when the
        /// dependency causing the pause has been resolved.
        /// </summary>
        internal void PauseWithDependency(Func<bool> callback, bool isControlled)
        {
            this.Status = OperationStatus.Paused;
            this.Dependency = callback;
            this.IsDependencyUncontrolled = !isControlled;
        }

        /// <summary>
        /// Tries to enable this operation if its dependency has been resolved.
        /// </summary>
        internal bool TryEnable()
        {
            if (this.Status is OperationStatus.Paused && (this.Dependency?.Invoke() ?? true))
            {
                this.Dependency = null;
                this.IsDependencyUncontrolled = false;
                this.Status = OperationStatus.Enabled;
            }

            return this.Status is OperationStatus.Enabled;
        }

        /// <summary>
        /// Returns the hashed state of this operation for the specified policy.
        /// </summary>
        internal virtual int GetHashedState(SchedulingPolicy policy) => this.LastSchedulingPoint.GetHashCode();

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
        /// Releases any held resources.
        /// </summary>
        public void Dispose()
        {
            this.SyncEvent.Dispose();
        }
    }
}
