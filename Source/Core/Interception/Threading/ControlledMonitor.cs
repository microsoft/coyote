// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading;
using Microsoft.Coyote.Runtime;
using SystemThreading = System.Threading;

namespace Microsoft.Coyote.Interception
{
    /// <summary>
    /// Provides methods for monitors that can be controlled during testing.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static class ControlledMonitor
    {
        /// <summary>
        /// Acquires an exclusive lock on the specified object.
        /// </summary>
        /// <param name="obj">The object on which to acquire the monitor lock.</param>
        public static void Enter(object obj)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                SynchronizedBlock.Mock.Create(obj).Lock();
            }
            else
            {
                Monitor.Enter(obj);
            }
        }

        /// <summary>
        /// Acquires an exclusive lock on the specified object.
        /// </summary>
        /// <param name="obj">The object on which to acquire the monitor lock.</param>
        /// <param name="lockTaken">
        /// The result of the attempt to acquire the lock, passed by reference. The input must be false.
        /// The output is true if the lock is acquired; otherwise, the output is false. The output is set
        /// even if an exception occurs during the attempt to acquire the lock. Note that if no exception
        /// occurs, the output of this method is always true.
        /// </param>
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
                Monitor.Enter(obj, ref lockTaken);
            }
        }

        /// <summary>
        /// Releases an exclusive lock on the specified object.
        /// </summary>
        /// <param name="obj">The object on which to release the lock.</param>
        public static void Exit(object obj)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                var mock = SynchronizedBlock.Mock.Find(obj);
                if (mock is null)
                {
                    throw new SynchronizationLockException();
                }

                mock.Exit();
            }
            else
            {
                Monitor.Exit(obj);
            }
        }

        /// <summary>
        /// Determines whether the current thread holds the lock on the specified object.
        /// </summary>
        /// <param name="obj">The object to test.</param>
        /// <returns>True if the current thread holds the lock on obj, else false.</returns>
        public static bool IsEntered(object obj)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                var mock = SynchronizedBlock.Mock.Find(obj);
                if (mock is null)
                {
                    throw new SynchronizationLockException();
                }

                return mock.IsEntered();
            }

            return Monitor.IsEntered(obj);
        }

        /// <summary>
        /// Notifies a thread in the waiting queue of a change in the locked object's state.
        /// </summary>
        /// <param name="obj">The object that sends the pulse.</param>
        public static void Pulse(object obj)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                var mock = SynchronizedBlock.Mock.Find(obj);
                if (mock is null)
                {
                    throw new SynchronizationLockException();
                }

                mock.Pulse();
            }
            else
            {
                Monitor.Pulse(obj);
            }
        }

        /// <summary>
        /// Notifies all waiting threads of a change in the object's state.
        /// </summary>
        /// <param name="obj">The object that sends the pulse.</param>
        public static void PulseAll(object obj)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                var mock = SynchronizedBlock.Mock.Find(obj);
                if (mock is null)
                {
                    throw new SynchronizationLockException();
                }

                mock.PulseAll();
            }
            else
            {
                Monitor.PulseAll(obj);
            }
        }

        /// <summary>
        /// Attempts, for the specified amount of time, to acquire an exclusive lock on the specified object,
        /// and atomically sets a value that indicates whether the lock was taken.
        /// </summary>
        /// <param name="obj">The object on which to acquire the lock.</param>
        /// <param name="timeout">
        /// The amount of time to wait for the lock. A value of –1 millisecond specifies an infinite wait.
        /// </param>
        /// <param name="lockTaken">
        /// The result of the attempt to acquire the lock, passed by reference. The input must be false.
        /// The output is true if the lock is acquired; otherwise, the output is false. The output is set
        /// even if an exception occurs during the attempt to acquire the lock. Note that if no exception
        /// occurs, the output of this method is always true.
        /// </param>
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
                Monitor.TryEnter(obj, timeout, ref lockTaken);
            }
        }

        /// <summary>
        /// Attempts, for the specified amount of time, to acquire an exclusive lock on the specified object,
        /// and atomically sets a value that indicates whether the lock was taken.
        /// </summary>
        /// <param name="obj">The object on which to acquire the lock.</param>
        /// <param name="timeout">
        /// The amount of time to wait for the lock. A value of –1 millisecond specifies an infinite wait.
        /// </param>
        public static bool TryEnter(object obj, TimeSpan timeout)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                // TODO: how to implement this timeout?
                var block = SynchronizedBlock.Mock.Create(obj);
                block.Lock();
                return block.IsLockTaken;
            }

            return Monitor.TryEnter(obj, timeout);
        }

        /// <summary>
        /// Attempts, for the specified number of milliseconds, to acquire an exclusive lock on the specified object,
        /// and atomically sets a value that indicates whether the lock was taken.
        /// </summary>
        /// <param name="obj">The object on which to acquire the lock.</param>
        /// <param name="millisecondsTimeout">The number of milliseconds to wait for the lock.</param>
        /// <param name="lockTaken">
        /// The result of the attempt to acquire the lock, passed by reference. The input must be false.
        /// The output is true if the lock is acquired; otherwise, the output is false. The output is set
        /// even if an exception occurs during the attempt to acquire the lock.
        /// </param>
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
                Monitor.TryEnter(obj, millisecondsTimeout, ref lockTaken);
            }
        }

        /// <summary>
        /// Attempts to acquire an exclusive lock on the specified object, and atomically
        /// sets a value that indicates whether the lock was taken.
        /// </summary>
        /// <param name="obj">The object on which to acquire the lock.</param>
        /// <param name="lockTaken">
        /// The result of the attempt to acquire the lock, passed by reference. The input must be false.
        /// The output is true if the lock is acquired; otherwise, the output is false. The output is set
        /// even if an exception occurs during the attempt to acquire the lock.
        /// </param>
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
                Monitor.TryEnter(obj, ref lockTaken);
            }
        }

        /// <summary>
        /// Attempts to acquire an exclusive lock on the specified object.
        /// </summary>
        /// <param name="obj">The object on which to acquire the lock.</param>
        public static bool TryEnter(object obj)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                var block = SynchronizedBlock.Mock.Create(obj);
                block.Lock();
                return block.IsLockTaken;
            }

            return Monitor.TryEnter(obj);
        }

        /// <summary>
        /// Releases the lock on an object and blocks the current thread until it reacquires the lock.
        /// </summary>
        /// <param name="obj">The object on which to wait.</param>
        /// <returns>
        /// True if the call returned because the caller reacquired the lock for the specified
        /// object. This method does not return if the lock is not reacquired.
        /// </returns>
        public static bool Wait(object obj)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                var mock = SynchronizedBlock.Mock.Find(obj);
                if (mock is null)
                {
                    throw new SynchronizationLockException();
                }

                return mock.Wait();
            }

            return Monitor.Wait(obj);
        }

        /// <summary>
        /// Releases the lock on an object and blocks the current thread until it reacquires the lock.
        /// If the specified time-out interval elapses, the thread enters the ready queue.
        /// </summary>
        /// <param name="obj">The object on which to wait.</param>
        /// <param name="millisecondsTimeout">
        /// The number of milliseconds to wait before the thread enters the ready queue.
        /// </param>
        /// <returns>
        /// True if the lock was reacquired before the specified time elapsed, else false if the
        /// lock was reacquired after the specified time elapsed. The method does not return until
        /// the lock is reacquired.
        /// </returns>
        public static bool Wait(object obj, int millisecondsTimeout)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                var mock = SynchronizedBlock.Mock.Find(obj);
                if (mock is null)
                {
                    throw new SynchronizationLockException();
                }

                return mock.Wait(millisecondsTimeout);
            }

            return Monitor.Wait(obj, millisecondsTimeout);
        }

        /// <summary>
        /// Releases the lock on an object and blocks the current thread until it reacquires the lock. If the
        /// specified time-out interval elapses, the thread enters the ready queue. This method also specifies
        /// whether the synchronization domain for the context (if in a synchronized context) is exited before
        /// the wait and reacquired afterward.
        /// </summary>
        /// <param name="obj">The object on which to wait.</param>
        /// <param name="millisecondsTimeout">
        /// The number of milliseconds to wait before the thread enters the ready queue.
        /// </param>
        /// <param name="exitContext">
        /// True to exit and reacquire the synchronization domain for the context (if in a synchronized context)
        /// before the wait, else false.
        /// </param>
        /// <returns>
        /// True if the lock was reacquired before the specified time elapsed, else false if the lock was reacquired
        /// after the specified time elapsed. The method does not return until the lock is reacquired.
        /// </returns>
        public static bool Wait(object obj, int millisecondsTimeout, bool exitContext)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                var mock = SynchronizedBlock.Mock.Find(obj);
                if (mock is null)
                {
                    throw new SynchronizationLockException();
                }

                // TODO: implement exitContext.
                return mock.Wait(millisecondsTimeout);
            }

            return Monitor.Wait(obj, millisecondsTimeout, exitContext);
        }

        /// <summary>
        /// Releases the lock on an object and blocks the current thread until it reacquires the lock.
        /// If the specified time-out interval elapses, the thread enters the ready queue.
        /// </summary>
        /// <param name="obj">The object on which to wait.</param>
        /// <param name="timeout">
        /// A <see cref="TimeSpan"/> representing the amount of time to wait before the thread enters the ready queue.
        /// </param>
        /// <returns>
        /// True if the lock was reacquired before the specified time elapsed, else false if the lock was reacquired
        /// after the specified time elapsed. The method does not return until the lock is reacquired.
        /// </returns>
        public static bool Wait(object obj, TimeSpan timeout)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                var mock = SynchronizedBlock.Mock.Find(obj);
                if (mock is null)
                {
                    throw new SynchronizationLockException();
                }

                return mock.Wait(timeout);
            }

            return Monitor.Wait(obj, timeout);
        }

        /// <summary>
        /// Releases the lock on an object and blocks the current thread until it reacquires the lock.
        /// If the specified time-out interval elapses, the thread enters the ready queue. Optionally
        /// exits the synchronization domain for the synchronized context before the wait and reacquires
        /// the domain afterward.
        /// </summary>
        /// <param name="obj">The object on which to wait.</param>
        /// <param name="timeout">
        /// A <see cref="TimeSpan"/> representing the amount of time to wait before the thread enters the ready queue.
        /// </param>
        /// <param name="exitContext">
        /// True to exit and reacquire the synchronization domain for the context (if in a synchronized context)
        /// before the wait, else false.
        /// </param>
        /// <returns>
        /// True if the lock was reacquired before the specified time elapsed, else false if the lock was reacquired
        /// after the specified time elapsed. The method does not return until the lock is reacquired.
        /// </returns>
        public static bool Wait(object obj, TimeSpan timeout, bool exitContext)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                var mock = SynchronizedBlock.Mock.Find(obj);
                if (mock is null)
                {
                    throw new SynchronizationLockException();
                }

                // TODO: implement exitContext.
                return mock.Wait(timeout);
            }

            return Monitor.Wait(obj, timeout, exitContext);
        }
    }
}
