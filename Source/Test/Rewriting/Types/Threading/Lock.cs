// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if NET10_0_OR_GREATER
using System;
using System.Runtime.CompilerServices;
using Microsoft.Coyote.Runtime;
using SystemLock = System.Threading.Lock;
using SystemSynchronizationLockException = System.Threading.SynchronizationLockException;

namespace Microsoft.Coyote.Rewriting.Types.Threading
{
    /// <summary>
    /// Provides methods for locks that can be controlled during testing.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static class Lock
    {
        /// <summary>
        /// Maps each lock to a synchronization identity distinct from its object monitor.
        /// </summary>
        private static readonly ConditionalWeakTable<SystemLock, object> SyncObjects =
            new ConditionalWeakTable<SystemLock, object>();

        /// <summary>
        /// Scope that releases a controlled lock when disposed.
        /// </summary>
        public ref struct Scope
        {
            private SystemLock Instance;

            internal Scope(SystemLock instance)
            {
                this.Instance = instance;
            }

            /// <summary>
            /// Releases the lock.
            /// </summary>
            public void Dispose()
            {
                SystemLock instance = this.Instance;
                if (instance != null)
                {
                    this.Instance = null;
                    Exit(instance);
                }
            }
        }

        /// <summary>
        /// Gets a value that indicates whether the current controlled operation holds the lock.
        /// </summary>
#pragma warning disable CA1707 // Identifiers should not contain underscores
#pragma warning disable SA1300 // Element should begin with upper-case letter
#pragma warning disable IDE1006 // Naming Styles
        public static bool get_IsHeldByCurrentThread(SystemLock instance)
#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore SA1300 // Element should begin with upper-case letter
#pragma warning restore CA1707 // Identifiers should not contain underscores
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.Interleaving)
            {
                var block = Monitor.SynchronizedBlock.Find(GetSyncObject(instance));
                return block != null && block.IsEntered();
            }

            return instance.IsHeldByCurrentThread;
        }

        /// <summary>
        /// Acquires the lock.
        /// </summary>
        public static void Enter(SystemLock instance)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.Interleaving)
            {
                Monitor.SynchronizedBlock.Lock(GetSyncObject(instance));
            }
            else
            {
                DelayOperation(runtime);
                instance.Enter();
            }
        }

        /// <summary>
        /// Acquires the lock and returns a scope that releases it when disposed.
        /// </summary>
        public static Scope EnterScope(SystemLock instance)
        {
            Enter(instance);
            return new Scope(instance);
        }

        /// <summary>
        /// Tries to acquire the lock without blocking.
        /// </summary>
        public static bool TryEnter(SystemLock instance) => TryEnter(instance, 0);

        /// <summary>
        /// Tries to acquire the lock within the specified timeout.
        /// </summary>
        public static bool TryEnter(SystemLock instance, int millisecondsTimeout)
        {
            if (millisecondsTimeout < -1)
            {
                throw new ArgumentOutOfRangeException(nameof(millisecondsTimeout));
            }

            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.Interleaving)
            {
                if (Monitor.SynchronizedBlock.TryLock(GetSyncObject(instance)))
                {
                    return true;
                }

                if (millisecondsTimeout is 0)
                {
                    return false;
                }

                if (millisecondsTimeout > 0)
                {
                    runtime.NotifyAssertionFailure(
                        "Invoking 'Lock.TryEnter' with a finite timeout is not supported in systematic testing.");
                    return false;
                }

                Monitor.SynchronizedBlock.Lock(GetSyncObject(instance));
                return true;
            }

            DelayOperation(runtime);
            return instance.TryEnter(millisecondsTimeout);
        }

        /// <summary>
        /// Tries to acquire the lock within the specified timeout.
        /// </summary>
        public static bool TryEnter(SystemLock instance, TimeSpan timeout)
        {
            long totalMilliseconds = (long)timeout.TotalMilliseconds;
            if (totalMilliseconds < -1 || totalMilliseconds > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(timeout));
            }

            return TryEnter(instance, (int)totalMilliseconds);
        }

        /// <summary>
        /// Releases the lock.
        /// </summary>
        public static void Exit(SystemLock instance)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.Interleaving)
            {
                var block = Monitor.SynchronizedBlock.Find(GetSyncObject(instance));
                if (block is null || !block.IsEntered())
                {
                    throw new SystemSynchronizationLockException();
                }

                block.Exit();
            }
            else
            {
                instance.Exit();
            }
        }

        private static object GetSyncObject(SystemLock instance) =>
            SyncObjects.GetValue(instance, _ => new object());

        private static void DelayOperation(CoyoteRuntime runtime)
        {
            if (runtime.SchedulingPolicy is SchedulingPolicy.Fuzzing &&
                runtime.TryGetExecutingOperation(out ControlledOperation current))
            {
                runtime.DelayOperation(current);
            }
        }
    }
}
#endif
