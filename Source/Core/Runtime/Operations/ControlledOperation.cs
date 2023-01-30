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
        /// The creation sequence id of this operation.
        /// </summary>
        internal ulong SequenceId { get; }

        /// <summary>
        /// The name of this operation.
        /// </summary>
        internal string Name { get; }

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
        /// The creation sequence of this operation.
        /// </summary>
        private readonly List<ulong> Sequence;

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
        /// The count of operations created by this operation.
        /// </summary>
        internal ulong OperationCreationCount;

        /// <summary>
        /// True if the source of this operation is uncontrolled, else false.
        /// </summary>
        internal bool IsSourceUncontrolled;

        /// <summary>
        /// True if the dependency is uncontrolled, else false.
        /// </summary>
        internal bool IsDependencyUncontrolled;

        /// <summary>
        /// The length of the creation sequence of this operation.
        /// </summary>
        internal int SequenceLength => this.Sequence?.Count ?? 0;

        /// <summary>
        /// The debug information of this operation.
        /// </summary>
        internal string DebugInfo { get; }

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
        internal ControlledOperation(ulong operationId, string name, OperationGroup group, CoyoteRuntime runtime)
        {
            this.Runtime = runtime;
            this.Id = operationId;
            this.Name = name;
            this.Status = OperationStatus.None;
            this.Group = group ?? OperationGroup.Create(this);
            this.Continuations = new Queue<Action>();
            this.SyncEvent = new ManualResetEventSlim(false);
            this.LastSchedulingPoint = SchedulingPointType.Start;
            this.LastHashedProgramState = 0;
            this.LastAccessedSharedState = string.Empty;
            this.LastAccessedSharedStateComparer = null;
            this.OperationCreationCount = 0;
            this.IsSourceUncontrolled = false;
            this.IsDependencyUncontrolled = false;

            // Only compute the sequence if the runtime scheduler is controlled.
            if (this.Runtime.SchedulingPolicy is SchedulingPolicy.None)
            {
                this.SequenceId = 0;
            }
            else
            {
                // If no parent operation is found, and this is not the root operation, then assign the root operation as the
                // parent operation. This is just an approximation that is applied in the case of uncontrolled threads.
                ControlledOperation parent = this.Runtime.GetExecutingOperationUnsafe() ?? this.Runtime.GetOperationWithId(0);
                this.Sequence = GetSequenceFromParent(operationId, parent);
                this.SequenceId = this.GetSequenceHash();
                if (this.SequenceLength > 100)
                {
                    this.Runtime.LogWriter.LogDebug("[coyote::debug] New operation {0} has '{1}' parents: {2}",
                        this.Name, this.SequenceLength, new System.Diagnostics.StackTrace());
                }
            }

            // Set the debug information for this operation.
            this.DebugInfo = $"'{this.Name}' with sequence id '{this.SequenceId}' and group id '{this.Group.Id}'";

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
        /// Returns the creation sequence based on the specified parent operation.
        /// </summary>
        private static List<ulong> GetSequenceFromParent(ulong operationId, ControlledOperation parent)
        {
            var sequence = new List<ulong>();
            if (operationId is 0)
            {
                // If this is the root operation, then the sequence only contains the root operation itself.
                sequence.Add(0);
            }
            else
            {
                sequence.AddRange(parent.Sequence);
                sequence.Add(parent.OperationCreationCount);
                parent.OperationCreationCount++;
            }

            return sequence;
        }

        /// <summary>
        /// Returns the hash of the creation sequence.
        /// </summary>
        private ulong GetSequenceHash()
        {
            // Iterate the creation sequence and create a low collision rate hash.
            ulong hash = (ulong)this.Sequence.Count;
            foreach (ulong element in this.Sequence)
            {
                ulong seq = ((element >> 16) ^ element) * 0x45d9f3b;
                seq = ((seq >> 16) ^ seq) * 0x45d9f3b;
                seq = (seq >> 16) ^ seq;
                hash ^= seq + 0x9e3779b9 + (hash << 6) + (hash >> 2);
            }

            return hash;
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
