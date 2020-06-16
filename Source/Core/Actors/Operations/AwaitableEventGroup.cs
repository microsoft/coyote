// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Coyote.Tasks;

namespace Microsoft.Coyote.Actors
{
    /// <summary>
    /// An object representing an awaitable long running context involving one or more actors.
    /// An `AwaitableEventGroup` can be provided as an optional argument in CreateActor and SendEvent.
    /// If a null `AwaitableEventGroup` is passed then the `EventGroup` is inherited from the sender
    /// or target actors (based on which ever one has a <see cref="Actor.CurrentEventGroup"/>).
    /// In this way an `AwaitableEventGroup` is automatically communicated to all actors involved in
    /// completing some larger operation. Each actor involved can find the `AwaitableEventGroup` using
    /// their <see cref="Actor.CurrentEventGroup"/> property.
    /// </summary>
    /// <typeparam name="T">The result returned when the operation is completed.</typeparam>
    public class AwaitableEventGroup<T> : EventGroup
    {
        /// <summary>
        /// A task completion source that can be awaited to get the final result object.
        /// </summary>
        private readonly TaskCompletionSource<T> Tcs;

        /// <summary>
        /// Initializes a new instance of the <see cref="AwaitableEventGroup{T}"/> class.
        /// </summary>
        /// <param name="id">The id for this `AwaitableEventGroup` (defaults to Guid.Empty).</param>
        public AwaitableEventGroup(Guid id = default)
            : base(id)
        {
            this.Tcs = TaskCompletionSource.Create<T>();
        }

        /// <summary>
        /// Gets the task created by this `AwaitableEventGroup`.
        /// </summary>
        public Task<T> Task => this.Tcs.Task;

        /// <summary>
        /// Indicates the `AwaitableEventGroup` has been completed.
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
        /// <returns>True if the `AwaitableEventGroup` was successful; otherwise, false.</returns>
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
        /// <returns>True if the `AwaitableEventGroup` was successful; otherwise, false.</returns>
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
