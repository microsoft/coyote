// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Coyote.Samples.CoffeeMachineTasks
{
    /// <summary>
    /// A non-reentrant mutual exclusion lock that can be acquired asynchronously
    /// in a first-in first-out order. During testing, Coyote will automatically
    /// take control of it and explore various interleavings. This is possible
    /// because the lock is implemented on top of a TaskCompletionSource, which
    /// Coyote knows how to rewrite and control during testing.
    /// </summary>
    public class AsyncLock
    {
        /// <summary>
        /// Queue of tasks awaiting to acquire the lock.
        /// </summary>
        protected readonly Queue<TaskCompletionSource<object>> Awaiters;

        /// <summary>
        /// True if the lock has been acquired, else false.
        /// </summary>
        protected internal bool IsAcquired { get; protected set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncLock"/> class.
        /// </summary>
        public AsyncLock()
        {
            this.Awaiters = new Queue<TaskCompletionSource<object>>();
            this.IsAcquired = false;
        }

        /// <summary>
        /// Tries to acquire the lock asynchronously, and returns a task that completes
        /// when the lock has been acquired. The returned task contains a releaser that
        /// releases the lock when disposed. This is not a reentrant operation.
        /// </summary>
        public virtual async Task<Releaser> AcquireAsync()
        {
            TaskCompletionSource<object> awaiter;
            lock (this.Awaiters)
            {
                if (this.IsAcquired)
                {
                    awaiter = new TaskCompletionSource<object>();
                    this.Awaiters.Enqueue(awaiter);
                }
                else
                {
                    this.IsAcquired = true;
                    awaiter = null;
                }
            }

            if (awaiter != null)
            {
                await awaiter.Task;
            }

            return new Releaser(this);
        }

        /// <summary>
        /// Releases the lock.
        /// </summary>
        protected virtual void Release()
        {
            TaskCompletionSource<object> awaiter = null;
            lock (this.Awaiters)
            {
                if (this.Awaiters.Count > 0)
                {
                    awaiter = this.Awaiters.Dequeue();
                }
                else
                {
                    this.IsAcquired = false;
                }
            }

            awaiter?.SetResult(null);
        }

        /// <summary>
        /// Releases the acquired <see cref="AsyncLock"/> when disposed.
        /// </summary>
        public struct Releaser : IDisposable
        {
            /// <summary>
            /// The acquired lock.
            /// </summary>
            private readonly AsyncLock AsyncLock;

            /// <summary>
            /// Initializes a new instance of the <see cref="Releaser"/> struct.
            /// </summary>
            internal Releaser(AsyncLock asyncLock)
            {
                this.AsyncLock = asyncLock;
            }

            /// <summary>
            /// Releases the acquired lock.
            /// </summary>
            public void Dispose() => this.AsyncLock?.Release();
        }
    }
}
