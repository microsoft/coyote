// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.SystematicTesting;
using TaskCanceledException = System.Threading.Tasks.TaskCanceledException;
using TaskStatus = System.Threading.Tasks.TaskStatus;

namespace Microsoft.Coyote.Tasks
{
    /// <summary>
    /// Represents the producer side of a task unbound to a delegate, providing access to the consumer
    /// side through the <see cref="TaskCompletionSource{TResult}.Task"/> property.
    /// </summary>
    public static class TaskCompletionSource
    {
        /// <summary>
        /// Creates a new <see cref="TaskCompletionSource{TResult}"/> instance.
        /// </summary>
        /// <typeparam name="TResult">The type of the result value assocatied with this task completion source.</typeparam>
        /// <returns>The task completion source.</returns>
        public static TaskCompletionSource<TResult> Create<TResult>() => CoyoteRuntime.IsExecutionControlled ?
            new Mock<TResult>() : new TaskCompletionSource<TResult>(false);

        /// <summary>
        /// Mock implementation of <see cref="TaskCompletionSource{TResult}"/> that
        /// can be controlled during systematic testing.
        /// </summary>
        private class Mock<TResult> : TaskCompletionSource<TResult>
        {
            /// <summary>
            /// The resource associated with this task completion source.
            /// </summary>
            private readonly Resource Resource;

            /// <summary>
            /// The cancellation token source.
            /// </summary>
            private readonly CancellationTokenSource CancellationTokenSource;

            /// <summary>
            /// A task that can be awaited until the task completion source completes.
            /// </summary>
            /// <remarks>
            /// Cached to avoid allocating a new task on each <see cref="Task"/> access.
            /// </remarks>
            private Task<TResult> AwaiterTask;

            /// <summary>
            /// Gets the task created by this task completion source.
            /// </summary>
            public override Task<TResult> Task
            {
                get
                {
                    // Optimization: if the task completion source is already completed,
                    // just return a completed task, no need to run a new task.
                    if (this.ResultTask is null)
                    {
                        // Else, return a task that will complete once the task completion source also completes.
                        if (this.AwaiterTask is null)
                        {
                            this.AwaiterTask = Tasks.Task.Run(() =>
                            {
                                if (this.ResultTask is null)
                                {
                                    // The resource is not available yet, notify the scheduler that the executing
                                    // asynchronous operation is blocked, so that it cannot be scheduled during
                                    // systematic testing exploration, which could deadlock.
                                    this.Resource.Wait();
                                }

                                if (this.CancellationTokenSource.IsCancellationRequested)
                                {
                                    this.CancellationTokenSource.Token.ThrowIfCancellationRequested();
                                }
                                else if (this.ResultTask.Status is TaskStatus.Faulted)
                                {
                                    Exception ex = this.ResultTask.Exception;
                                    throw ex is AggregateException aex ? aex.InnerException : ex;
                                }

                                return this.ResultTask.Result;
                            }, this.CancellationTokenSource.Token);
                        }

                        return this.AwaiterTask;
                    }

                    return this.ResultTask;
                }
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="Mock{TResult}"/> class.
            /// </summary>
            internal Mock()
                : base(true)
            {
                this.Resource = new Resource();
                this.CancellationTokenSource = new CancellationTokenSource();
            }

            /// <inheritdoc/>
            public override void SetResult(TResult result) =>
                this.CompleteWithStatus(TaskStatus.RanToCompletion, result, default);

            /// <inheritdoc/>
            public override bool TrySetResult(TResult result) =>
                this.TryCompleteWithStatus(TaskStatus.RanToCompletion, result, default);

            /// <summary>
            /// Transitions the underlying task into the <see cref="TaskStatus.Canceled"/> state.
            /// </summary>
            public override void SetCanceled() =>
                this.CompleteWithStatus(TaskStatus.Canceled, default, default);

            /// <inheritdoc/>
            public override bool TrySetCanceled() =>
                this.TryCompleteWithStatus(TaskStatus.Canceled, default, default);

            /// <inheritdoc/>
            public override void SetException(Exception exception) =>
                this.CompleteWithStatus(TaskStatus.Faulted, default, exception);

            /// <inheritdoc/>
            public override bool TrySetException(Exception exception) =>
                this.TryCompleteWithStatus(TaskStatus.Faulted, default, exception);

            /// <summary>
            /// Completes the task completion source with the specified status.
            /// </summary>
            private void CompleteWithStatus(TaskStatus status, TResult result, Exception exception)
            {
                if (!this.TryCompleteWithStatus(status, result, exception))
                {
                    throw new InvalidOperationException("The underlying Task<TResult> is already in one " +
                        "of the three final states: RanToCompletion, Faulted, or Canceled.");
                }
            }

            /// <summary>
            /// Tries to complete the task completion source with the specified status.
            /// </summary>
            private bool TryCompleteWithStatus(TaskStatus status, TResult result, Exception exception)
            {
                if (this.ResultTask is null)
                {
                    if (status is TaskStatus.RanToCompletion)
                    {
                        this.ResultTask = Tasks.Task.FromResult(result);
                    }
                    else if (status is TaskStatus.Canceled)
                    {
                        this.CancellationTokenSource.Cancel();
                        this.ResultTask = Tasks.Task.FromCanceled<TResult>(this.CancellationTokenSource.Token);
                    }
                    else if (status is TaskStatus.Faulted)
                    {
                        this.ResultTask = Tasks.Task.FromException<TResult>(exception);
                    }

                    // Release the resource and notify any awaiting asynchronous operations.
                    this.Resource.SignalAll();

                    return true;
                }

                return false;
            }
        }
    }

    /// <summary>
    /// Represents the producer side of a task unbound to a delegate, providing access to the consumer
    /// side through the <see cref="TaskCompletionSource{TResult}.Task"/> property.
    /// </summary>
    /// <typeparam name="TResult">The type of the result value assocatied with this task completion source.</typeparam>
    public class TaskCompletionSource<TResult>
    {
        /// <summary>
        /// The internal task completion source.
        /// </summary>
        private readonly System.Threading.Tasks.TaskCompletionSource<TResult> Instance;

        /// <summary>
        /// The task containing the result.
        /// </summary>
        /// <remarks>
        /// Cached to avoid allocating a new task wrapper on each <see cref="Task"/> access.
        /// </remarks>
        private protected Task<TResult> ResultTask;

        /// <summary>
        /// Gets the task created by this task completion source.
        /// </summary>
        public virtual Task<TResult> Task => this.ResultTask;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskCompletionSource{TResult}"/> class.
        /// </summary>
        internal TaskCompletionSource(bool isMocked)
        {
            if (!isMocked)
            {
                this.Instance = new System.Threading.Tasks.TaskCompletionSource<TResult>();
                this.ResultTask = this.Instance.Task.WrapInControlledTask();
            }
        }

        /// <summary>
        /// Transitions the underlying task into the <see cref="TaskStatus.RanToCompletion"/> state.
        /// </summary>
        /// <param name="result">The result value to bind to this task.</param>
        /// <exception cref="InvalidOperationException">The underlying <see cref="Task{TResult}"/>
        /// is already in one of the three final states: <see cref="TaskStatus.RanToCompletion"/>,
        /// <see cref="TaskStatus.Faulted"/>, or <see cref="TaskStatus.Canceled"/>.</exception>
        public virtual void SetResult(TResult result) => this.Instance.SetResult(result);

        /// <summary>
        /// Attempts to transition the underlying task into the <see cref="TaskStatus.RanToCompletion"/> state.
        /// </summary>
        /// <param name="result">The result value to bind to this task.</param>
        /// <returns>True if the operation was successful; otherwise, false.</returns>
        public virtual bool TrySetResult(TResult result) => this.Instance.TrySetResult(result);

        /// <summary>
        /// Transitions the underlying task into the <see cref="TaskStatus.Canceled"/> state.
        /// </summary>
        /// <exception cref="InvalidOperationException">The underlying <see cref="Task{TResult}"/>
        /// is already in one of the three final states: <see cref="TaskStatus.RanToCompletion"/>,
        /// <see cref="TaskStatus.Faulted"/>, or <see cref="TaskStatus.Canceled"/>.</exception>
        public virtual void SetCanceled() => this.Instance.SetCanceled();

        /// <summary>
        /// Attempts to transition the underlying task into the <see cref="TaskStatus.Canceled"/> state.
        /// </summary>
        /// <returns>True if the operation was successful; otherwise, false.</returns>
        public virtual bool TrySetCanceled() => this.Instance.TrySetCanceled();

        /// <summary>
        /// Transitions the underlying task into the <see cref="TaskStatus.Faulted"/> state
        /// and binds it to the specified exception.
        /// </summary>
        /// <param name="exception">The exception to bind to this task.</param>
        /// <exception cref="InvalidOperationException">The underlying <see cref="Task{TResult}"/>
        /// is already in one of the three final states: <see cref="TaskStatus.RanToCompletion"/>,
        /// <see cref="TaskStatus.Faulted"/>, or <see cref="TaskStatus.Canceled"/>.</exception>
        public virtual void SetException(Exception exception) => this.Instance.SetException(exception);

        /// <summary>
        /// Attempts to transition the underlying task into the <see cref="TaskStatus.Faulted"/> state
        /// and binds it to the specified exception.
        /// </summary>
        /// <param name="exception">The exception to bind to this task.</param>
        /// <returns>True if the operation was successful; otherwise, false.</returns>
        public virtual bool TrySetException(Exception exception) => this.Instance.TrySetException(exception);
    }
}
