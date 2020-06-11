// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;

namespace Microsoft.Coyote.Actors
{
    /// <summary>
    /// An object representing a long running operation involving one or more actors.
    /// This operation is passed along automatically during any subsequent CreateActor
    /// or SendEvent calls so that any Actor in a network of actors can complete this
    /// operation.  An actor can find the operation using the CurrentOperation property.
    /// </summary>
    public class Operation
    {
        /// <summary>
        /// The unique id of this operation.
        /// </summary>
        public Guid Id { get; internal set; }

        /// <summary>
        /// An optional name for this operation.
        /// </summary>
        public string Name { get; internal set; }

        /// <summary>
        /// Indicates the operation has been completed.
        /// </summary>
        public bool IsCompleted { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Operation"/> class.
        /// </summary>
        public Operation()
        {
            this.Id = Guid.NewGuid();
        }
    }

    /// <summary>
    /// An object representing a long running operation involving one or more actors.
    /// This operation is passed along automatically during any subsequent CreateActor
    /// or SendEvent calls so that any Actor in a network of actors can complete this
    /// operation.  An actor can find the operation using the CurrentOperation property.
    /// This operation contains a TaskCompletionSource that can be used to wait for the
    /// operation to be completed.
    /// </summary>
    /// <typeparam name="T">The result returned whent the operation is completed.</typeparam>
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
            this.Completion = new TaskCompletionSource<T>();
        }

        /// <summary>
        /// Provided the completed result and set IsCompleted to true.
        /// </summary>
        /// <param name="result">The completed result object.</param>
        public virtual void SetResult(T result)
        {
            this.Completion.SetResult(result);
            this.IsCompleted = true;
        }

        /// <summary>
        /// Provided the completed result and set IsCompleted to true if the
        /// operation is not already completed.
        /// </summary>
        /// <param name="result">The completed result object.</param>
        public virtual void TrySetResult(T result)
        {
            this.Completion.TrySetResult(result);
            this.IsCompleted = true;
        }
    }

    /// <summary>
    /// This is a special operation that is completed when the Actor reaches a quiescent state,
    /// meaning the inbox cannot process any events, either it is empty or everything in it is
    /// deferred.  Quiescence is never reach on a StateMachine that has a DefaultEvent handler.
    /// In that case the default event handler can be considered a quiescent notification.
    /// </summary>
    public class QuiescentOperation : Operation<bool>
    {
    }
}
