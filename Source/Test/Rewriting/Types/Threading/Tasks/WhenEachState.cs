// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if NET10_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.Runtime.CompilerServices;
using SystemCancellationToken = System.Threading.CancellationToken;
using SystemEnumeratorCancellation = System.Runtime.CompilerServices.EnumeratorCancellationAttribute;
using SystemTask = System.Threading.Tasks.Task;
using SystemTaskContinuationOptions = System.Threading.Tasks.TaskContinuationOptions;
using SystemTaskScheduler = System.Threading.Tasks.TaskScheduler;

namespace Microsoft.Coyote.Rewriting.Types.Threading.Tasks
{
    /// <summary>
    /// Stores the state required to asynchronously enumerate a collection of tasks in the
    /// order that they complete during systematic testing.
    /// </summary>
    /// <remarks>
    /// The uncontrolled <see cref="SystemTask.WhenEach(SystemTask[])"/> methods signal the
    /// enumeration from an uncontrolled thread pool thread, which the runtime is unable to
    /// observe, so awaiting the enumeration can result in a false deadlock. The enumeration
    /// records completions synchronously and uses controlled operations to pause until
    /// the next completed task is available.
    /// </remarks>
    internal sealed class WhenEachState
    {
        /// <summary>
        /// Responsible for controlling the enumeration of the tasks.
        /// </summary>
        private readonly CoyoteRuntime Runtime;

        /// <summary>
        /// Synchronizes access to the enumerated tasks.
        /// </summary>
        private readonly object SyncObject;

        /// <summary>
        /// The number of tasks that have not completed yet.
        /// </summary>
        private int PendingCount;

        /// <summary>
        /// The tasks that have completed, but have not been yielded yet, in completion order.
        /// </summary>
        private readonly Queue<SystemTask> Completed;

        /// <summary>
        /// Value 0 if this state has never been enumerated, else 1.
        /// </summary>
        private int Enumerated;

        /// <summary>
        /// True if all tasks have been yielded, else false.
        /// </summary>
        private bool IsEnumerationCompleted
        {
            get
            {
                lock (this.SyncObject)
                {
                    return this.PendingCount is 0 && this.Completed.Count is 0;
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WhenEachState"/> class.
        /// </summary>
        private WhenEachState(CoyoteRuntime runtime, IReadOnlyCollection<SystemTask> tasks)
        {
            this.Runtime = runtime;
            this.SyncObject = new object();
            this.PendingCount = tasks.Count;
            this.Completed = new Queue<SystemTask>();
            this.Enumerated = 0;
            var scheduler = new CompletionTaskScheduler();
            foreach (SystemTask task in tasks)
            {
                _ = task.ContinueWith(this.EnqueueCompletedTask, SystemCancellationToken.None,
                    SystemTaskContinuationOptions.ExecuteSynchronously, scheduler);
            }
        }

        /// <summary>
        /// Creates the state for enumerating the specified tasks, or null if there are no tasks.
        /// </summary>
        internal static WhenEachState Create<TTask>(CoyoteRuntime runtime, ReadOnlySpan<TTask> tasks)
            where TTask : SystemTask
        {
            if (tasks.Length is 0)
            {
                return null;
            }

            var pending = new List<SystemTask>(tasks.Length);
            foreach (TTask task in tasks)
            {
                if (task is null)
                {
                    throw new ArgumentException("The tasks argument included a null value.", nameof(tasks));
                }

                pending.Add(task);
            }

            return new WhenEachState(runtime, pending);
        }

        /// <summary>
        /// Creates the state for enumerating the specified tasks, or null if there are no tasks.
        /// </summary>
        internal static WhenEachState Create<TTask>(CoyoteRuntime runtime, IEnumerable<TTask> tasks)
            where TTask : SystemTask
        {
            ArgumentNullException.ThrowIfNull(tasks);

            var pending = new List<SystemTask>();
            foreach (TTask task in tasks)
            {
                if (task is null)
                {
                    throw new ArgumentException("The tasks argument included a null value.", nameof(tasks));
                }

                pending.Add(task);
            }

            return pending.Count is 0 ? null : new WhenEachState(runtime, pending);
        }

        /// <summary>
        /// Asynchronously enumerates the tasks of the specified state as they complete.
        /// </summary>
        internal static async IAsyncEnumerable<TTask> Iterate<TTask>(WhenEachState state,
            [SystemEnumeratorCancellation] SystemCancellationToken cancellationToken = default)
            where TTask : SystemTask
        {
            // No matter how many times the enumerable is enumerated, each task is yielded only once,
            // which is the same behavior as the uncontrolled 'Task.WhenEach' methods.
            if (state?.TryStartEnumeration() is not true)
            {
                yield break;
            }

            while (true)
            {
                if (state.TryDequeueCompletedTask(out SystemTask next))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    yield return (TTask)next;
                    continue;
                }

                if (state.IsEnumerationCompleted)
                {
                    yield break;
                }

                cancellationToken.ThrowIfCancellationRequested();

                // Pause the current operation until the next task completes, or until cancellation is
                // requested, so that the runtime remains in control of the asynchronous enumeration.
                await AsyncConditionAwaiterStateMachine.RunAsync(state.Runtime,
                    () => state.HasCompletedTask() || cancellationToken.IsCancellationRequested,
                    debugMsg: "any of the enumerated tasks to complete");
                cancellationToken.ThrowIfCancellationRequested();
            }
        }

        /// <summary>
        /// Returns true if this state has not been enumerated before, else false.
        /// </summary>
        private bool TryStartEnumeration() => Interlocked.Exchange(ref this.Enumerated, 1) is 0;

        /// <summary>
        /// Tries to dequeue the next task that completed, but has not been yielded yet.
        /// </summary>
        private bool TryDequeueCompletedTask(out SystemTask task)
        {
            lock (this.SyncObject)
            {
                if (this.Completed.Count > 0)
                {
                    task = this.Completed.Dequeue();
                    return true;
                }
            }

            task = null;
            return false;
        }

        /// <summary>
        /// Returns true if there is at least one task that completed, but has not been yielded yet.
        /// </summary>
        private bool HasCompletedTask()
        {
            lock (this.SyncObject)
            {
                return this.Completed.Count > 0;
            }
        }

        /// <summary>
        /// Records a task at the point that it completes.
        /// </summary>
        private void EnqueueCompletedTask(SystemTask task)
        {
            lock (this.SyncObject)
            {
                this.Completed.Enqueue(task);
                this.PendingCount--;
            }
        }

        /// <summary>
        /// Runs only internal completion bookkeeping synchronously, even when the source
        /// task requests asynchronous continuations.
        /// </summary>
        private sealed class CompletionTaskScheduler : SystemTaskScheduler
        {
            /// <inheritdoc/>
            protected override void QueueTask(SystemTask task) => this.TryExecuteTask(task);

            /// <inheritdoc/>
            protected override bool TryExecuteTaskInline(SystemTask task, bool taskWasPreviouslyQueued) =>
                this.TryExecuteTask(task);

            /// <inheritdoc/>
            protected override IEnumerable<SystemTask> GetScheduledTasks() => Array.Empty<SystemTask>();
        }
    }
}
#endif
