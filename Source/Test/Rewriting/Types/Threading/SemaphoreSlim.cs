// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Coyote.Runtime;
using SystemTasks = System.Threading.Tasks;

using SystemCancellationToken = System.Threading.CancellationToken;
using SystemSemaphoreSlim = System.Threading.SemaphoreSlim;
using SystemTask = System.Threading.Tasks.Task;
using SystemTimeout = System.Threading.Timeout;

namespace Microsoft.Coyote.Rewriting.Types.Threading
{
    /// <summary>
    /// A semaphore that limits the number of tasks that can access a resource. During testing,
    /// the semaphore is automatically replaced with a controlled mocked version.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public class SemaphoreSlim : IDisposable
    {
        /// <summary>
        /// Limits the number of tasks that can access a resource.
        /// </summary>
        private readonly SystemSemaphoreSlim Instance;

        /// <summary>
        /// Number of remaining tasks that can enter the semaphore.
        /// </summary>
        public virtual int CurrentCount => this.Instance.CurrentCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="SemaphoreSlim"/> class.
        /// </summary>
        protected SemaphoreSlim(SystemSemaphoreSlim semaphore)
        {
            this.Instance = semaphore;
        }

        /// <summary>
        /// Creates a new semaphore.
        /// </summary>
        public static SemaphoreSlim Create(int initialCount, int maxCount) =>
            CoyoteRuntime.IsExecutionControlled ?
            new Mock(initialCount, maxCount) :
            new SemaphoreSlim(new SystemSemaphoreSlim(initialCount, maxCount));

        /// <summary>
        /// Blocks the current task until it can enter the semaphore.
        /// </summary>
        public virtual void Wait() => this.Instance.Wait();

        /// <summary>
        /// Blocks the current task until it can enter the semaphore, using a timespan
        /// that specifies the timeout.
        /// </summary>
        public virtual bool Wait(TimeSpan timeout) => this.Instance.Wait(timeout);

        /// <summary>
        /// Blocks the current task until it can enter the semaphore, using a 32-bit signed integer
        /// that specifies the timeout.
        /// </summary>
        public virtual bool Wait(int millisecondsTimeout) => this.Instance.Wait(millisecondsTimeout);

        /// <summary>
        /// Blocks the current task until it can enter the semaphore, while observing a cancellation token.
        /// </summary>
        public virtual void Wait(SystemCancellationToken cancellationToken) => this.Instance.Wait(cancellationToken);

        /// <summary>
        /// Blocks the current task until it can enter the semaphore, using a timespan
        /// that specifies the timeout, while observing a cancellation token.
        /// </summary>
        public virtual bool Wait(TimeSpan timeout, SystemCancellationToken cancellationToken) =>
            this.Instance.Wait(timeout, cancellationToken);

        /// <summary>
        /// Blocks the current task until it can enter the semaphore, using a 32-bit signed integer
        /// that specifies the timeout, while observing a cancellation token.
        /// </summary>
        public virtual bool Wait(int millisecondsTimeout, SystemCancellationToken cancellationToken) =>
            this.Instance.Wait(millisecondsTimeout, cancellationToken);

        /// <summary>
        /// Asynchronously waits to enter the semaphore.
        /// </summary>
        public virtual SystemTask WaitAsync() => this.Instance.WaitAsync();

        /// <summary>
        /// Asynchronously waits to enter the semaphore, using a timespan
        /// that specifies the timeout.
        /// </summary>
        public virtual SystemTasks.Task<bool> WaitAsync(TimeSpan timeout) => this.Instance.WaitAsync(timeout);

        /// <summary>
        /// Asynchronously waits to enter the semaphore, using a 32-bit signed integer
        /// that specifies the timeout.
        /// </summary>
        public virtual SystemTasks.Task<bool> WaitAsync(int millisecondsTimeout) =>
            this.Instance.WaitAsync(millisecondsTimeout);

        /// <summary>
        /// Asynchronously waits to enter the semaphore, while observing a cancellation token.
        /// </summary>
        public virtual SystemTask WaitAsync(SystemCancellationToken cancellationToken) =>
            this.Instance.WaitAsync(cancellationToken);

        /// <summary>
        /// Asynchronously waits to enter the semaphore, using a timespan
        /// that specifies the timeout, while observing a cancellation token.
        /// </summary>
        public virtual SystemTasks.Task<bool> WaitAsync(TimeSpan timeout, SystemCancellationToken cancellationToken) =>
            this.Instance.WaitAsync(timeout, cancellationToken);

        /// <summary>
        /// Asynchronously waits to enter the semaphore, using a 32-bit signed integer
        /// that specifies the timeout, while observing a cancellation token.
        /// </summary>
        public virtual SystemTasks.Task<bool> WaitAsync(int millisecondsTimeout, SystemCancellationToken cancellationToken) =>
            this.Instance.WaitAsync(millisecondsTimeout, cancellationToken);

        /// <summary>
        /// Releases the semaphore.
        /// </summary>
        public virtual void Release() => this.Instance.Release();

        /// <summary>
        /// Releases resources used by the semaphore.
        /// </summary>
        private void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            this.Instance?.Dispose();
        }

        /// <summary>
        /// Releases resources used by the semaphore.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Mock implementation of a semaphore that can be controlled during systematic testing.
        /// </summary>
        private sealed class Mock : SemaphoreSlim
        {
            /// <summary>
            /// The resource associated with this semaphore.
            /// </summary>
            private readonly Resource Resource;

            /// <summary>
            /// The maximum number of requests that can be granted concurrently.
            /// </summary>
            private readonly int MaxCount;

            /// <summary>
            /// The number of requests that have been granted concurrently.
            /// </summary>
            private int NumAcquired;

            /// <summary>
            /// Number of remaining tasks that can enter the semaphore.
            /// </summary>
            public override int CurrentCount => this.MaxCount - this.NumAcquired;

            /// <summary>
            /// Initializes a new instance of the <see cref="Mock"/> class.
            /// </summary>
            internal Mock(int initialCount, int maxCount)
                : base(default)
            {
                this.Resource = new Resource();
                this.Resource.Runtime.Assert(initialCount >= 0,
                    "Cannot create semaphore with initial count of {0}. The count must be equal or greater than 0.", initialCount);
                this.Resource.Runtime.Assert(initialCount <= maxCount,
                    "Cannot create semaphore with initial count of {0}. The count be equal or less than max count of {1}.",
                    initialCount, maxCount);
                this.Resource.Runtime.Assert(maxCount > 0,
                    "Cannot create semaphore with max count of {0}. The count must be greater than 0.", maxCount);
                this.MaxCount = maxCount;
                this.NumAcquired = maxCount - initialCount;
            }

            /// <inheritdoc/>
            public override void Wait() => this.Wait(SystemTimeout.Infinite, default);

            /// <inheritdoc/>
            public override bool Wait(TimeSpan timeout) => this.Wait(timeout, default);

            /// <inheritdoc/>
            public override bool Wait(int millisecondsTimeout) => this.Wait(millisecondsTimeout, default);

            /// <inheritdoc/>
            public override void Wait(SystemCancellationToken cancellationToken) =>
                this.Wait(SystemTimeout.Infinite, cancellationToken);

            /// <inheritdoc/>
            public override bool Wait(TimeSpan timeout, SystemCancellationToken cancellationToken)
            {
                long totalMilliseconds = (long)timeout.TotalMilliseconds;
                if (totalMilliseconds < -1 || totalMilliseconds > int.MaxValue)
                {
                    throw new ArgumentOutOfRangeException(nameof(timeout));
                }

                return this.Wait((int)totalMilliseconds, default);
            }

            /// <inheritdoc/>
            public override bool Wait(int millisecondsTimeout, SystemCancellationToken cancellationToken)
            {
                // TODO: support cancellations during testing.
                this.Resource.Runtime.ScheduleNextOperation(AsyncOperationType.Join);

                // We need this loop, because when a resource gets released it notifies all asynchronous
                // operations waiting to acquire it, even if such an operation is still blocked.
                while (this.CurrentCount is 0)
                {
                    // The resource is not available yet, notify the scheduler that the executing
                    // asynchronous operation is blocked, so that it cannot be scheduled during
                    // systematic testing exploration, which could deadlock.
                    this.Resource.Wait();
                }

                this.NumAcquired++;

                // TODO: support timeouts during testing, this would become false if there is a timeout.
                return true;
            }

            /// <inheritdoc/>
            public override SystemTask WaitAsync() => this.WaitAsync(SystemTimeout.Infinite, default);

            /// <inheritdoc/>
            public override SystemTasks.Task<bool> WaitAsync(TimeSpan timeout) => this.WaitAsync(timeout, default);

            /// <inheritdoc/>
            public override SystemTasks.Task<bool> WaitAsync(int millisecondsTimeout) =>
                this.WaitAsync(millisecondsTimeout, default);

            /// <inheritdoc/>
            public override SystemTask WaitAsync(SystemCancellationToken cancellationToken) =>
                this.WaitAsync(SystemTimeout.Infinite, cancellationToken);

            /// <inheritdoc/>
            public override SystemTasks.Task<bool> WaitAsync(TimeSpan timeout, SystemCancellationToken cancellationToken) =>
                SystemTask.FromResult(this.Wait(timeout, cancellationToken));

            /// <inheritdoc/>
            public override SystemTasks.Task<bool> WaitAsync(int millisecondsTimeout, SystemCancellationToken cancellationToken) =>
                SystemTask.FromResult(this.Wait(millisecondsTimeout, cancellationToken));

            /// <inheritdoc/>
            public override void Release()
            {
                this.NumAcquired--;
                this.Resource.Runtime.Assert(this.NumAcquired >= 0,
                    "Cannot release semaphore as it has reached max count of {0}.", this.MaxCount);

                // Release the semaphore and notify any awaiting asynchronous operations.
                this.Resource.SignalAll();

                // This must be called outside the context of the semaphore, because it notifies
                // the scheduler to try schedule another asynchronous operation that could in turn
                // try to acquire this semaphore causing a deadlock.
                this.Resource.Runtime.ScheduleNextOperation(AsyncOperationType.Release);
            }
        }
    }
}
