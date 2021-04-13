// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading;
using Microsoft.Coyote.Runtime;

namespace Microsoft.Coyote.Tasks
{
    /// <summary>
    /// A semaphore that limits the number of tasks that can access a resource. During testing,
    /// the semaphore is automatically replaced with a controlled mocked version.
    /// </summary>
    public class Semaphore : IDisposable
    {
        /// <summary>
        /// Limits the number of tasks that can access a resource.
        /// </summary>
        private readonly SemaphoreSlim Instance;

        /// <summary>
        /// Number of remaining tasks that can enter the semaphore.
        /// </summary>
        public virtual int CurrentCount => this.Instance.CurrentCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="Semaphore"/> class.
        /// </summary>
        protected Semaphore(SemaphoreSlim semaphore)
        {
            this.Instance = semaphore;
        }

        /// <summary>
        /// Creates a new semaphore.
        /// </summary>
        /// <returns>The semaphore.</returns>
        public static Semaphore Create(int initialCount, int maxCount) => CoyoteRuntime.IsExecutionControlled ?
            new Mock(initialCount, maxCount) : new Semaphore(new SemaphoreSlim(initialCount, maxCount));

        /// <summary>
        /// Blocks the current task until it can enter the semaphore.
        /// </summary>
        public virtual void Wait() => this.Instance.Wait();

        /// <summary>
        /// Blocks the current task until it can enter the semaphore, using a <see cref="TimeSpan"/>
        /// that specifies the timeout.
        /// </summary>
        /// <param name="timeout">
        /// A <see cref="TimeSpan"/> that represents the number of milliseconds to wait, a <see cref="TimeSpan"/>
        /// that represents -1 milliseconds to wait indefinitely, or a <see cref="TimeSpan"/> that represents
        /// 0 milliseconds to test the wait handle and return immediately.
        /// </param>
        /// <returns>True if the current task successfully entered the semaphore, else false.</returns>
        public virtual bool Wait(TimeSpan timeout) => this.Instance.Wait(timeout);

        /// <summary>
        /// Blocks the current task until it can enter the semaphore, using a 32-bit signed integer
        /// that specifies the timeout.
        /// </summary>
        /// <param name="millisecondsTimeout">
        /// The number of milliseconds to wait, <see cref="Timeout.Infinite"/> (-1) to wait indefinitely,
        /// or zero to test the state of the wait handle and return immediately.
        /// </param>
        /// <returns>True if the current task successfully entered the semaphore, else false.</returns>
        public virtual bool Wait(int millisecondsTimeout) => this.Instance.Wait(millisecondsTimeout);

        /// <summary>
        /// Blocks the current task until it can enter the semaphore, while observing a <see cref="CancellationToken"/>.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe.</param>
        public virtual void Wait(CancellationToken cancellationToken) => this.Instance.Wait(cancellationToken);

        /// <summary>
        /// Blocks the current task until it can enter the semaphore, using a <see cref="TimeSpan"/>
        /// that specifies the timeout, while observing a <see cref="CancellationToken"/>.
        /// </summary>
        /// <param name="timeout">
        /// A <see cref="TimeSpan"/> that represents the number of milliseconds to wait, a <see cref="TimeSpan"/>
        /// that represents -1 milliseconds to wait indefinitely, or a <see cref="TimeSpan"/> that represents
        /// 0 milliseconds to test the wait handle and return immediately.
        /// </param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe.</param>
        /// <returns>True if the current task successfully entered the semaphore, else false.</returns>
        public virtual bool Wait(TimeSpan timeout, CancellationToken cancellationToken) =>
            this.Instance.Wait(timeout, cancellationToken);

        /// <summary>
        /// Blocks the current task until it can enter the semaphore, using a 32-bit signed integer
        /// that specifies the timeout, while observing a <see cref="CancellationToken"/>.
        /// </summary>
        /// <param name="millisecondsTimeout">
        /// The number of milliseconds to wait, <see cref="Timeout.Infinite"/> (-1) to wait indefinitely,
        /// or zero to test the state of the wait handle and return immediately.
        /// </param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe.</param>
        /// <returns>True if the current task successfully entered the semaphore, else false.</returns>
        public virtual bool Wait(int millisecondsTimeout, CancellationToken cancellationToken) =>
            this.Instance.Wait(millisecondsTimeout, cancellationToken);

        /// <summary>
        /// Asynchronously waits to enter the semaphore.
        /// </summary>
        /// <returns>A task that will complete when the semaphore has been entered.</returns>
        public virtual Task WaitAsync() => this.Instance.WaitAsync().WrapInControlledTask();

        /// <summary>
        /// Asynchronously waits to enter the semaphore, using a <see cref="TimeSpan"/>
        /// that specifies the timeout.
        /// </summary>
        /// <param name="timeout">
        /// A <see cref="TimeSpan"/> that represents the number of milliseconds to wait, a <see cref="TimeSpan"/>
        /// that represents -1 milliseconds to wait indefinitely, or a <see cref="TimeSpan"/> that represents
        /// 0 milliseconds to test the wait handle and return immediately.
        /// </param>
        /// <returns>
        /// A task that will complete with a result of true if the current thread successfully entered
        /// the semaphore, otherwise with a result of false.
        /// </returns>
        public virtual Task<bool> WaitAsync(TimeSpan timeout) => this.Instance.WaitAsync(timeout).WrapInControlledTask();

        /// <summary>
        /// Asynchronously waits to enter the semaphore, using a 32-bit signed integer
        /// that specifies the timeout.
        /// </summary>
        /// <param name="millisecondsTimeout">
        /// The number of milliseconds to wait, <see cref="Timeout.Infinite"/> (-1) to wait indefinitely,
        /// or zero to test the state of the wait handle and return immediately.
        /// </param>
        /// <returns>
        /// A task that will complete with a result of true if the current thread successfully entered
        /// the semaphore, otherwise with a result of false.
        /// </returns>
        public virtual Task<bool> WaitAsync(int millisecondsTimeout) =>
            this.Instance.WaitAsync(millisecondsTimeout).WrapInControlledTask();

        /// <summary>
        /// Asynchronously waits to enter the semaphore, while observing a <see cref="CancellationToken"/>.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe.</param>
        /// <returns>A task that will complete when the semaphore has been entered.</returns>
        public virtual Task WaitAsync(CancellationToken cancellationToken) =>
            this.Instance.WaitAsync(cancellationToken).WrapInControlledTask();

        /// <summary>
        /// Asynchronously waits to enter the semaphore, using a <see cref="TimeSpan"/>
        /// that specifies the timeout, while observing a <see cref="CancellationToken"/>.
        /// </summary>
        /// <param name="timeout">
        /// A <see cref="TimeSpan"/> that represents the number of milliseconds to wait, a <see cref="TimeSpan"/>
        /// that represents -1 milliseconds to wait indefinitely, or a <see cref="TimeSpan"/> that represents
        /// 0 milliseconds to test the wait handle and return immediately.
        /// </param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe.</param>
        /// <returns>
        /// A task that will complete with a result of true if the current thread successfully entered
        /// the semaphore, otherwise with a result of false.
        /// </returns>
        public virtual Task<bool> WaitAsync(TimeSpan timeout, CancellationToken cancellationToken) =>
            this.Instance.WaitAsync(timeout, cancellationToken).WrapInControlledTask();

        /// <summary>
        /// Asynchronously waits to enter the semaphore, using a 32-bit signed integer
        /// that specifies the timeout, while observing a <see cref="CancellationToken"/>.
        /// </summary>
        /// <param name="millisecondsTimeout">
        /// The number of milliseconds to wait, <see cref="Timeout.Infinite"/> (-1) to wait indefinitely,
        /// or zero to test the state of the wait handle and return immediately.
        /// </param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe.</param>
        /// <returns>
        /// A task that will complete with a result of true if the current thread successfully entered
        /// the semaphore, otherwise with a result of false.
        /// </returns>
        public virtual Task<bool> WaitAsync(int millisecondsTimeout, CancellationToken cancellationToken) =>
            this.Instance.WaitAsync(millisecondsTimeout, cancellationToken).WrapInControlledTask();

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
        /// Mock implementation of <see cref="Semaphore"/> that can be controlled during systematic testing.
        /// </summary>
        private sealed class Mock : Semaphore
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
            public override void Wait() => this.Wait(Timeout.Infinite, default);

            /// <inheritdoc/>
            public override bool Wait(TimeSpan timeout) => this.Wait(timeout, default);

            /// <inheritdoc/>
            public override bool Wait(int millisecondsTimeout) => this.Wait(millisecondsTimeout, default);

            /// <inheritdoc/>
            public override void Wait(CancellationToken cancellationToken) => this.Wait(Timeout.Infinite, cancellationToken);

            /// <inheritdoc/>
            public override bool Wait(TimeSpan timeout, CancellationToken cancellationToken)
            {
                long totalMilliseconds = (long)timeout.TotalMilliseconds;
                if (totalMilliseconds < -1 || totalMilliseconds > int.MaxValue)
                {
                    throw new ArgumentOutOfRangeException(nameof(timeout));
                }

                return this.Wait((int)totalMilliseconds, default);
            }

            /// <inheritdoc/>
            public override bool Wait(int millisecondsTimeout, CancellationToken cancellationToken)
            {
                // TODO: support cancellations during testing.
                this.Resource.Runtime.ScheduleNextOperation(AsyncOperationType.Join, false, true);

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
            public override Task WaitAsync() => this.WaitAsync(Timeout.Infinite, default);

            /// <inheritdoc/>
            public override Task<bool> WaitAsync(TimeSpan timeout) => this.WaitAsync(timeout, default);

            /// <inheritdoc/>
            public override Task<bool> WaitAsync(int millisecondsTimeout) => this.WaitAsync(millisecondsTimeout, default);

            /// <inheritdoc/>
            public override Task WaitAsync(CancellationToken cancellationToken) => this.WaitAsync(Timeout.Infinite, cancellationToken);

            /// <inheritdoc/>
            public override Task<bool> WaitAsync(TimeSpan timeout, CancellationToken cancellationToken) =>
                Task.FromResult(this.Wait(timeout, cancellationToken));

            /// <inheritdoc/>
            public override Task<bool> WaitAsync(int millisecondsTimeout, CancellationToken cancellationToken) =>
                Task.FromResult(this.Wait(millisecondsTimeout, cancellationToken));

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
                this.Resource.Runtime.ScheduleNextOperation(AsyncOperationType.Release, false, true);
            }
        }
    }
}
