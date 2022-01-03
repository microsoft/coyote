// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Coyote.Runtime;
using SystemThreading = System.Threading;

namespace Microsoft.Coyote.Rewriting.Types.Threading
{
    /// <summary>
    /// Provides methods for monitors that can be controlled during testing.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static class Monitor
    {
        /// <summary>
        /// Acquires an exclusive lock on the specified object.
        /// </summary>
        public static void Enter(object obj)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                SynchronizedBlock.Mock.Create(obj).Lock();
            }
            else
            {
                SystemThreading.Monitor.Enter(obj);
            }
        }

        /// <summary>
        /// Acquires an exclusive lock on the specified object.
        /// </summary>
        public static void Enter(object obj, ref bool lockTaken)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                var block = SynchronizedBlock.Mock.Create(obj);
                block.Lock();
                lockTaken = block.IsLockTaken;
            }
            else
            {
                SystemThreading.Monitor.Enter(obj, ref lockTaken);
            }
        }

        /// <summary>
        /// Releases an exclusive lock on the specified object.
        /// </summary>
        public static void Exit(object obj)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                var mock = SynchronizedBlock.Mock.Find(obj);
                if (mock is null)
                {
                    throw new SystemThreading.SynchronizationLockException();
                }

                mock.Exit();
            }
            else
            {
                SystemThreading.Monitor.Exit(obj);
            }
        }

        /// <summary>
        /// Determines whether the current thread holds the lock on the specified object.
        /// </summary>
        public static bool IsEntered(object obj)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                var mock = SynchronizedBlock.Mock.Find(obj);
                if (mock is null)
                {
                    throw new SystemThreading.SynchronizationLockException();
                }

                return mock.IsEntered();
            }

            return SystemThreading.Monitor.IsEntered(obj);
        }

        /// <summary>
        /// Notifies a thread in the waiting queue of a change in the locked object's state.
        /// </summary>
        public static void Pulse(object obj)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                var mock = SynchronizedBlock.Mock.Find(obj);
                if (mock is null)
                {
                    throw new SystemThreading.SynchronizationLockException();
                }

                mock.Pulse();
            }
            else
            {
                SystemThreading.Monitor.Pulse(obj);
            }
        }

        /// <summary>
        /// Notifies all waiting threads of a change in the object's state.
        /// </summary>
        public static void PulseAll(object obj)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                var mock = SynchronizedBlock.Mock.Find(obj);
                if (mock is null)
                {
                    throw new SystemThreading.SynchronizationLockException();
                }

                mock.PulseAll();
            }
            else
            {
                SystemThreading.Monitor.PulseAll(obj);
            }
        }

        /// <summary>
        /// Attempts, for the specified amount of time, to acquire an exclusive lock on the specified object,
        /// and atomically sets a value that indicates whether the lock was taken.
        /// </summary>
        public static void TryEnter(object obj, TimeSpan timeout, ref bool lockTaken)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                // TODO: how to implement this timeout?
                var block = SynchronizedBlock.Mock.Create(obj);
                block.Lock();
                lockTaken = block.IsLockTaken;
            }
            else
            {
                SystemThreading.Monitor.TryEnter(obj, timeout, ref lockTaken);
            }
        }

        /// <summary>
        /// Attempts, for the specified amount of time, to acquire an exclusive lock on the specified object,
        /// and atomically sets a value that indicates whether the lock was taken.
        /// </summary>
        public static bool TryEnter(object obj, TimeSpan timeout)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                // TODO: how to implement this timeout?
                var block = SynchronizedBlock.Mock.Create(obj);
                block.Lock();
                return block.IsLockTaken;
            }

            return SystemThreading.Monitor.TryEnter(obj, timeout);
        }

        /// <summary>
        /// Attempts, for the specified number of milliseconds, to acquire an exclusive lock on the specified object,
        /// and atomically sets a value that indicates whether the lock was taken.
        /// </summary>
        public static void TryEnter(object obj, int millisecondsTimeout, ref bool lockTaken)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                // TODO: how to implement this timeout?
                var block = SynchronizedBlock.Mock.Create(obj);
                block.Lock();
                lockTaken = block.IsLockTaken;
            }
            else
            {
                SystemThreading.Monitor.TryEnter(obj, millisecondsTimeout, ref lockTaken);
            }
        }

        /// <summary>
        /// Attempts to acquire an exclusive lock on the specified object, and atomically
        /// sets a value that indicates whether the lock was taken.
        /// </summary>
        public static void TryEnter(object obj, ref bool lockTaken)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                // TODO: how to implement this timeout?
                var block = SynchronizedBlock.Mock.Create(obj);
                block.Lock();
                lockTaken = block.IsLockTaken;
            }
            else
            {
                SystemThreading.Monitor.TryEnter(obj, ref lockTaken);
            }
        }

        /// <summary>
        /// Attempts to acquire an exclusive lock on the specified object.
        /// </summary>
        public static bool TryEnter(object obj)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                var block = SynchronizedBlock.Mock.Create(obj);
                block.Lock();
                return block.IsLockTaken;
            }

            return SystemThreading.Monitor.TryEnter(obj);
        }

        /// <summary>
        /// Releases the lock on an object and blocks the current thread until it reacquires the lock.
        /// </summary>
        public static bool Wait(object obj)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                var mock = SynchronizedBlock.Mock.Find(obj);
                if (mock is null)
                {
                    throw new SystemThreading.SynchronizationLockException();
                }

                return mock.Wait();
            }

            return SystemThreading.Monitor.Wait(obj);
        }

        /// <summary>
        /// Releases the lock on an object and blocks the current thread until it reacquires the lock.
        /// If the specified time-out interval elapses, the thread enters the ready queue.
        /// </summary>
        public static bool Wait(object obj, int millisecondsTimeout)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                var mock = SynchronizedBlock.Mock.Find(obj);
                if (mock is null)
                {
                    throw new SystemThreading.SynchronizationLockException();
                }

                return mock.Wait(millisecondsTimeout);
            }

            return SystemThreading.Monitor.Wait(obj, millisecondsTimeout);
        }

        /// <summary>
        /// Releases the lock on an object and blocks the current thread until it reacquires the lock. If the
        /// specified time-out interval elapses, the thread enters the ready queue. This method also specifies
        /// whether the synchronization domain for the context (if in a synchronized context) is exited before
        /// the wait and reacquired afterward.
        /// </summary>
        public static bool Wait(object obj, int millisecondsTimeout, bool exitContext)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                var mock = SynchronizedBlock.Mock.Find(obj);
                if (mock is null)
                {
                    throw new SystemThreading.SynchronizationLockException();
                }

                // TODO: implement exitContext.
                return mock.Wait(millisecondsTimeout);
            }

            return SystemThreading.Monitor.Wait(obj, millisecondsTimeout, exitContext);
        }

        /// <summary>
        /// Releases the lock on an object and blocks the current thread until it reacquires the lock.
        /// If the specified time-out interval elapses, the thread enters the ready queue.
        /// </summary>
        public static bool Wait(object obj, TimeSpan timeout)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                var mock = SynchronizedBlock.Mock.Find(obj);
                if (mock is null)
                {
                    throw new SystemThreading.SynchronizationLockException();
                }

                return mock.Wait(timeout);
            }

            return SystemThreading.Monitor.Wait(obj, timeout);
        }

        /// <summary>
        /// Releases the lock on an object and blocks the current thread until it reacquires the lock.
        /// If the specified time-out interval elapses, the thread enters the ready queue. Optionally
        /// exits the synchronization domain for the synchronized context before the wait and reacquires
        /// the domain afterward.
        /// </summary>
        public static bool Wait(object obj, TimeSpan timeout, bool exitContext)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                var mock = SynchronizedBlock.Mock.Find(obj);
                if (mock is null)
                {
                    throw new SystemThreading.SynchronizationLockException();
                }

                // TODO: implement exitContext.
                return mock.Wait(timeout);
            }

            return SystemThreading.Monitor.Wait(obj, timeout, exitContext);
        }
    }
}
