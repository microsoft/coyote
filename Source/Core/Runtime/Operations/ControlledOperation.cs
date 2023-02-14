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
        /// The unique id of the parent of this operation.
        /// </summary>
        internal ulong ParentId { get; }

        /// <summary>
        /// The name of this operation.
        /// </summary>
        internal string Name { get; }

        /// <summary>
        /// The group where this operation has membership. This can be used
        /// by the scheduler to optimize exploration.
        /// </summary>
        internal readonly OperationGroup Group;

        /// <summary>
        /// The status of this operation. An operation can be scheduled only
        /// if it is <see cref="OperationStatus.Enabled"/>.
        /// </summary>
        internal OperationStatus Status;

        /// <summary>
        /// Synchronization mechanism for controlling the execution of this operation.
        /// </summary>
        private ManualResetEventSlim SyncEvent;

        /// <summary>
        /// Sequence of visited call sites during execution of this operation.
        /// </summary>
        internal readonly List<string> CallSiteSequence;

        /// <summary>
        /// Sequence of invoked scheduling points during execution of this operation.
        /// </summary>
        internal readonly List<SchedulingPointType> SchedulingPointSequence;

        /// <summary>
        /// Queue of continuations that this operation must execute before it completes.
        /// </summary>
        private readonly Queue<Action> Continuations;

        /// <summary>
        /// Dependency that must get resolved before this operation can resume executing.
        /// </summary>
        private Func<bool> Dependency;

        /// <summary>
        /// A value that represents the hashed state when this operation last executed.
        /// </summary>
        internal ulong LastHashedState { get; private set; }

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
        /// The hash of the call sites that this operation has visited.
        /// </summary>
        private ulong CallSiteSequenceHash;

        /// <summary>
        /// The hash of the scheduling points that this operation has invoked.
        /// </summary>
        private ulong SchedulingPointSequenceHash;

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
        /// The call site that was last executed by this operation.
        /// </summary>
        internal string LastCallSite => this.CallSiteSequence.Count is 0 ?
            string.Empty : this.CallSiteSequence[this.CallSiteSequence.Count - 1];

        /// <summary>
        /// The type of the last invoked scheduling point.
        /// </summary>
        internal SchedulingPointType LastSchedulingPoint => this.SchedulingPointSequence[this.SchedulingPointSequence.Count - 1];

        /// <summary>
        /// The debug information of this operation.
        /// </summary>
        internal string DebugInfo { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ControlledOperation"/> class.
        /// </summary>
        internal ControlledOperation(ulong operationId, string name, CoyoteRuntime runtime)
        {
            this.Runtime = runtime;
            this.Id = operationId;
            this.ParentId = 0;
            this.Name = name;
            this.Status = OperationStatus.None;
            this.SyncEvent = new ManualResetEventSlim(false);
            this.CallSiteSequence = new List<string>();
            this.SchedulingPointSequence = new List<SchedulingPointType> { SchedulingPointType.Start };
            this.Continuations = new Queue<Action>();
            this.LastAccessedSharedState = string.Empty;
            this.LastAccessedSharedStateComparer = null;
            this.CallSiteSequenceHash = 17;
            this.SchedulingPointSequenceHash = 17;
            this.IsSourceUncontrolled = false;
            this.IsDependencyUncontrolled = false;

            // Assign the operation group from the execution context. If there is no scheduling policy,
            // assign the default operation group, else assign a group based on the runtime context.
            this.Group = runtime.SchedulingPolicy is SchedulingPolicy.None ? OperationGroup.Default :
                runtime.Context.AssignOperationGroup(this);
            this.LastHashedState = (ulong)this.Group.Id.GetHashCode();
            // this.Runtime.LogWriter.LogImportant($">>> ASSIGN GROUP FOR {this.Id} (STATE:{this.LastHashedState}): {this.Id}");

            // Set the debug information for this operation.
            this.DebugInfo = $"'{this.Name}' with group id '{this.Group.Id}'";

            // Register this operation with the runtime.
            this.Runtime.RegisterNewOperation(this);
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
        /// Sets a callback that executes the next continuation of this operation.
        /// </summary>
        internal void SetContinuationCallback(Action callback) => this.Continuations.Enqueue(callback);

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
        /// Registers the specified call site as visited.
        /// </summary>
        internal void RegisterCallSite(string callSite)
        {
            this.CallSiteSequenceHash = (this.CallSiteSequenceHash * 31) + (ulong)callSite.GetHashCode();
            this.CallSiteSequence.Add(callSite);
        }

        /// <summary>
        /// Registers the specified scheduling point as invoked.
        /// </summary>
        internal void RegisterSchedulingPoint(SchedulingPointType sp)
        {
            this.SchedulingPointSequenceHash = (this.SchedulingPointSequenceHash * 31) + (ulong)sp.GetHashCode();
            this.SchedulingPointSequence.Add(sp);
        }

        /// <summary>
        /// Computes the latest hashed state of this operation for the specified policy.
        /// </summary>
        /// <summary>
        /// Compute the latest hashed state of this operation for the specified policy.
        /// </summary>
        internal ulong ComputeHashedState(SchedulingPolicy policy)
        {
            this.LastHashedState = this.GetLatestHashedState(policy);
            return this.LastHashedState;
        }

        /// <summary>
        /// Returns the latest hashed state of this operation for the specified policy.
        /// </summary>
        protected virtual ulong GetLatestHashedState(SchedulingPolicy policy)
        {
            unchecked
            {
                ulong hash = 17;
                if (policy != SchedulingPolicy.None)
                {
                    hash = (hash * 31) + this.CallSiteSequenceHash;
                    hash = (hash * 31) + this.SchedulingPointSequenceHash;
                    hash ^= (ulong)this.Group.Id.GetHashCode();
                }

                return hash;
            }
        }

        /// <summary>
        /// Returns the group-agnostic latest hashed state of this operation.
        /// </summary>
        internal ulong GetGroupAgnosticLatestHashedState() => this.LastHashedState ^ (ulong)this.Group.Id.GetHashCode();

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
