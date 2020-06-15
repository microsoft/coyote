// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Coyote.Tasks;

namespace Microsoft.Coyote.Actors
{
    /// <summary>
    /// An object representing a long running operation involving one or more actors.
    /// This operation is passed along automatically during any subsequent CreateActor
    /// or SendEvent calls so that any Actor in a network of actors can complete this
    /// operation.  An actor can find the operation using the <see cref="Actor.CurrentOperation"/>
    /// property.
    /// </summary>
    public class Operation
    {
        /// <summary>
        /// The unique id of this operation, automatically initialized with Guid.NewGiud.
        /// </summary>
        public Guid Id { get; internal set; }

        /// <summary>
        /// An optional friendly name for this operation.
        /// </summary>
        public string Name { get; internal set; }

        /// <summary>
        /// Indicates the operation has been completed.
        /// </summary>
        public bool IsCompleted { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Operation"/> class.
        /// </summary>
        public Operation()
        {
            this.Id = Guid.NewGuid();
        }

        /// <summary>
        /// Call this method to mark the operation as completed.
        /// </summary>
        public virtual void NotifyComlete()
        {
            this.IsCompleted = true;
        }

        /// <summary>
        /// A special null operation that can be used to stop the CurrentOperation from
        /// being passed along in a CreateActor or SendEvent call.
        /// </summary>
        public static Operation NullOperation = new Operation();
    }

    /// <summary>
    /// An object representing a long running operation involving one or more actors.
    /// This operation is passed along automatically during any subsequent CreateActor
    /// or SendEvent calls so that any Actor in a network of actors can complete this
    /// operation.  An actor can find the operation using the <see cref="Actor.CurrentOperation"/> property.
    /// This operation contains a <see cref="TaskCompletionSource"/> that can be used to wait for the
    /// operation to be completed.
    /// </summary>
    /// <typeparam name="T">The result returned when the operation is completed.</typeparam>
    public class Operation<T> : Operation
    {
        /// <summary>
        /// A task completion source that can be awaited to get the final result object.
        /// </summary>
        public TaskCompletionSource<T> Completion { get; internal set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Operation{T}"/> class.
        /// </summary>
        public Operation()
        {
            this.Completion = TaskCompletionSource.Create<T>();
        }

        /// <summary>
        /// Provided the completed result and set <see cref="Operation.IsCompleted"/> to true.
        /// </summary>
        /// <param name="result">The completed result object.</param>
        public virtual void SetResult(T result)
        {
            this.Completion.SetResult(result);
            this.NotifyComlete();
        }

        /// <summary>
        /// Provided the completed result and set IsCompleted to true if the
        /// operation is not already completed.
        /// </summary>
        /// <param name="result">The completed result object.</param>
        public virtual void TrySetResult(T result)
        {
            this.Completion.TrySetResult(result);
            this.NotifyComlete();
        }
    }
}
