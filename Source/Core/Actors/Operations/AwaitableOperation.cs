// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Coyote.Tasks;

namespace Microsoft.Coyote.Actors
{
    /// <summary>
    /// An object representing a long running operation involving one or more actors.
    /// This operation is passed along automatically during any subsequent CreateActor
    /// or SendEvent calls so that any actor in a network of actors can complete this
    /// operation.  An actor can find the operation using the <see cref="Actor.CurrentOperation"/> property.
    /// This operation contains a <see cref="TaskCompletionSource"/> that can be used to wait for the
    /// operation to be completed.
    /// </summary>
    /// <typeparam name="T">The result returned when the operation is completed.</typeparam>
    public class AwaitableOperation<T> : Operation
    {
        /// <summary>
        /// A task completion source that can be awaited to get the final result object.
        /// </summary>
        private readonly TaskCompletionSource<T> Tcs;

        /// <summary>
        /// Initializes a new instance of the <see cref="AwaitableOperation{T}"/> class.
        /// </summary>
        /// <param name="id">The id for this operation (defaults to Guid.Empty).</param>
        public AwaitableOperation(Guid id = default)
            : base(id)
        {
            this.Tcs = TaskCompletionSource.Create<T>();
        }

        /// <summary>
        /// Gets the task created by this operation.
        /// </summary>
        public Task<T> Task => this.Tcs.Task;

        /// <summary>
        /// Indicates the operation has been completed.
        /// </summary>
        public bool IsCompleted => this.Tcs.Task.IsCompleted;

        /// <summary>
        /// Value that indicates whether the task completed execution due to being canceled.
        /// </summary>
        public bool IsCanceled => this.Tcs.Task.IsCanceled;

        /// <summary>
        /// Value that indicates whether the task completed due to an unhandled exception.
        /// </summary>
        public bool IsFaulted => this.Tcs.Task.IsFaulted;

        /// <summary>
        /// Transitions the underlying task into the <see cref="System.Threading.Tasks.TaskStatus.RanToCompletion"/> state.
        /// </summary>
        /// <param name="result">The completed result object.</param>
        public virtual void SetResult(T result)
        {
            this.Tcs.SetResult(result);
        }

        /// <summary>
        /// Attempts to transition the underlying task into the <see cref="System.Threading.Tasks.TaskStatus.RanToCompletion"/> state.
        /// </summary>
        /// <param name="result">The completed result object.</param>
        public virtual void TrySetResult(T result)
        {
            this.Tcs.TrySetResult(result);
        }

        /// <summary>
        /// Transitions the underlying task into the <see cref="System.Threading.Tasks.TaskStatus.Canceled"/> state.
        /// </summary>
        public virtual void SetCancelled() => this.Tcs.SetCanceled();

        /// <summary>
        /// Attempts to transition the underlying task into the <see cref="System.Threading.Tasks.TaskStatus.Canceled"/> state.
        /// </summary>
        /// <returns>True if the operation was successful; otherwise, false.</returns>
        public virtual bool TrySetCanceled() => this.Tcs.TrySetCanceled();

        /// <summary>
        /// Transitions the underlying task into the <see cref="System.Threading.Tasks.TaskStatus.Faulted"/> state
        /// and binds it to the specified exception.
        /// </summary>
        /// <param name="exception">The exception to bind to this task.</param>
        public virtual void SetException(Exception exception) => this.Tcs.SetException(exception);

        /// <summary>
        /// Attempts to transition the underlying task into the <see cref="System.Threading.Tasks.TaskStatus.Faulted"/> state
        /// and binds it to the specified exception.
        /// </summary>
        /// <param name="exception">The exception to bind to this task.</param>
        /// <returns>True if the operation was successful; otherwise, false.</returns>
        public virtual bool TrySetException(Exception exception) => this.Tcs.TrySetException(exception);

        /// <summary>
        /// Gets an awaiter for this awaitable.
        /// </summary>
        public TaskAwaiter<T> GetAwaiter()
        {
            return this.Tcs.Task.GetAwaiter();
        }
    }
}
