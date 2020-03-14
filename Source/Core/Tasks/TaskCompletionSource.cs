// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.SystematicTesting;

namespace Microsoft.Coyote.Tasks
{
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
        /// Gets the task created by this task completion source.
        /// </summary>
        public virtual ControlledTask<TResult> Task => this.Instance.Task.ToControlledTask();

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskCompletionSource{TResult}"/> class.
        /// </summary>
        protected TaskCompletionSource(System.Threading.Tasks.TaskCompletionSource<TResult> tcs)
        {
            this.Instance = tcs;
        }

        /// <summary>
        /// Creates a new <see cref="TaskCompletionSource{TResult}"/> instance.
        /// </summary>
        /// <returns>The task completion source.</returns>
#pragma warning disable CA1000 // Do not declare static members on generic types
        public static TaskCompletionSource<TResult> Create() => CoyoteRuntime.IsExecutionControlled ?
#pragma warning restore CA1000 // Do not declare static members on generic types
            new Mock() : new TaskCompletionSource<TResult>(new System.Threading.Tasks.TaskCompletionSource<TResult>());

        /// <summary>
        /// Transitions the underlying task into the <see cref="TaskStatus.RanToCompletion"/> state.
        /// </summary>
        /// <param name="result">The result value to bind to this task.</param>
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
        public virtual void SetException(Exception exception) => this.Instance.SetException(exception);

        /// <summary>
        /// Attempts to transition the underlying task into the <see cref="TaskStatus.Faulted"/> state
        /// and binds it to the specified exception.
        /// </summary>
        /// <param name="exception">The exception to bind to this task.</param>
        /// <returns>True if the operation was successful; otherwise, false.</returns>
        public virtual bool TrySetException(Exception exception) => this.Instance.TrySetException(exception);

        /// <summary>
        /// Mock implementation of <see cref="TaskCompletionSource{TResult}"/> that
        /// can be controlled during systematic testing.
        /// </summary>
        private class Mock : TaskCompletionSource<TResult>
        {
            /// <summary>
            /// The resource associated with this task completion source.
            /// </summary>
            private readonly Resource Resource;

            /// <summary>
            /// True if the task completion source is completed, else false.
            /// </summary>
            private TaskStatus Status;

            /// <summary>
            /// The task that provides access to the result.
            /// </summary>
            private ControlledTask<TResult> ResultTask;

            /// <summary>
            /// The result value.
            /// </summary>
            private TResult Result;

            /// <summary>
            /// The bound exception, if any.
            /// </summary>
            private Exception Exception;

            /// <summary>
            /// The cancellation token source.
            /// </summary>
            private readonly CancellationTokenSource CancellationTokenSource;

            /// <summary>
            /// Gets the task created by this task completion source.
            /// </summary>
            public override ControlledTask<TResult> Task
            {
                get
                {
                    if (this.ResultTask is null)
                    {
                        // Optimization: if the task completion source is already completed,
                        // just return a completed task, no need to run a new task.
                        if (this.Status is TaskStatus.RanToCompletion)
                        {
                            this.ResultTask = ControlledTask.FromResult(this.Result);
                        }
                        else if (this.Status is TaskStatus.Canceled)
                        {
                            this.ResultTask = ControlledTask.FromCanceled<TResult>(this.CancellationTokenSource.Token);
                        }
                        else if (this.Status is TaskStatus.Faulted)
                        {
                            this.ResultTask = ControlledTask.FromException<TResult>(this.Exception);
                        }
                        else
                        {
                            // Else, return a task that will complete once the task completion source also completes.
                            this.ResultTask = ControlledTask.Run(() =>
                            {
                                if (this.Status is TaskStatus.Created)
                                {
                                    // The resource is not available yet, notify the scheduler that the executing
                                    // asynchronous operation is blocked, so that it cannot be scheduled during
                                    // systematic testing exploration, which could deadlock.
                                    this.Resource.NotifyWait();
                                    this.Resource.Runtime.ScheduleNextOperation();
                                }

                                if (this.Status is TaskStatus.Canceled)
                                {
                                    this.CancellationTokenSource.Token.ThrowIfCancellationRequested();
                                }
                                else if (this.Status is TaskStatus.Faulted)
                                {
                                    throw this.Exception;
                                }

                                return this.Result;
                            }, this.CancellationTokenSource.Token);
                        }
                    }

                    return this.ResultTask;
                }
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="Mock"/> class.
            /// </summary>
            internal Mock()
                : base(default)
            {
                this.Resource = Resource.Create();
                this.Status = TaskStatus.Created;
                this.CancellationTokenSource = new CancellationTokenSource();
            }

            /// <inheritdoc/>
            public override void SetResult(TResult result)
            {
                this.Resource.Runtime.Assert(this.Status is TaskStatus.Created,
                    "The underlying task is already in the {0} state.", this.Status);
                this.CompleteWithStatus(TaskStatus.RanToCompletion, result, default);
            }

            /// <inheritdoc/>
            public override bool TrySetResult(TResult result)
            {
                if (this.Status is TaskStatus.Created)
                {
                    this.CompleteWithStatus(TaskStatus.RanToCompletion, result, default);
                    return true;
                }

                return false;
            }

            /// <summary>
            /// Transitions the underlying task into the <see cref="TaskStatus.Canceled"/> state.
            /// </summary>
            public override void SetCanceled()
            {
                this.Resource.Runtime.Assert(this.Status is TaskStatus.Created,
                    "The underlying task is already in the {0} state.", this.Status);
                this.CompleteWithStatus(TaskStatus.Canceled, default, default);
            }

            /// <inheritdoc/>
            public override bool TrySetCanceled()
            {
                if (this.Status is TaskStatus.Created)
                {
                    this.CompleteWithStatus(TaskStatus.Canceled, default, default);
                    return true;
                }

                return false;
            }

            /// <inheritdoc/>
            public override void SetException(Exception exception)
            {
                this.Resource.Runtime.Assert(this.Status is TaskStatus.Created,
                    "The underlying task is already in the {0} state.", this.Status);
                this.CompleteWithStatus(TaskStatus.Faulted, default, exception);
            }

            /// <inheritdoc/>
            public override bool TrySetException(Exception exception)
            {
                if (this.Status is TaskStatus.Created)
                {
                    this.CompleteWithStatus(TaskStatus.Faulted, default, exception);
                    return true;
                }

                return false;
            }

            /// <summary>
            /// Completes the task completion source with the specified status.
            /// </summary>
            private void CompleteWithStatus(TaskStatus status, TResult result, Exception exception)
            {
                this.Status = status;
                if (status is TaskStatus.RanToCompletion)
                {
                    this.Result = result;
                }
                else if (status is TaskStatus.Canceled)
                {
                    this.CancellationTokenSource.Cancel();
                    this.Exception = new TaskCanceledException();
                }
                else if (status is TaskStatus.Faulted)
                {
                    this.Exception = exception;
                }

                // Release the resource and notify any awaiting asynchronous operations.
                this.Resource.NotifyRelease();
                this.Resource.Runtime.ScheduleNextOperation();
            }
        }
    }
}
