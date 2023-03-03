// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Coyote.Runtime;
using SystemThread = System.Threading.Thread;
using SystemTimeout = System.Threading.Timeout;
using SystemWaitHandle = System.Threading.WaitHandle;

namespace Microsoft.Coyote.Rewriting.Types.Threading
{
    /// <summary>
    /// Represents a thread synchronization event.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static class WaitHandle
    {
        /// <summary>
        /// Blocks the current thread until the current handle receives a signal.
        /// </summary>
        public static bool WaitOne(SystemWaitHandle instance) => WaitOne(instance, SystemTimeout.Infinite);

        /// <summary>
        /// Blocks the current thread until the current handle receives a signal, using a time span
        /// to specify the time interval.
        /// </summary>
        public static bool WaitOne(SystemWaitHandle instance, TimeSpan timeout) => WaitOne(instance, timeout, false);

        /// <summary>
        /// Blocks the current thread until the current handle receives a signal, using a 32-bit
        /// signed integer to specify the time interval in milliseconds.
        /// </summary>
        public static bool WaitOne(SystemWaitHandle instance, int millisecondsTimeout) =>
            WaitOne(instance, millisecondsTimeout, false);

        /// <summary>
        /// Blocks the current thread until the current handle receives a signal, using a time span
        /// to specify the time interval and specifying whether to exit the synchronization domain
        /// before the wait.
        /// </summary>
        public static bool WaitOne(SystemWaitHandle instance, TimeSpan timeout, bool exitContext)
        {
            long totalMilliseconds = (long)timeout.TotalMilliseconds;
            if (totalMilliseconds < -1 || totalMilliseconds > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(timeout));
            }

            return WaitOne(instance, (int)totalMilliseconds, exitContext);
        }

        /// <summary>
        /// Blocks the current thread until the current handle receives a signal, using a 32-bit
        /// signed integer to specify the time interval and specifying whether to exit the
        /// synchronization domain before the wait.
        /// </summary>
        public static bool WaitOne(SystemWaitHandle instance, int millisecondsTimeout, bool exitContext)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.Interleaving &&
                Resource.TryFind(instance, out Resource resource))
            {
                return resource.WaitOne(millisecondsTimeout);
            }

            return instance.WaitOne(millisecondsTimeout, exitContext);
        }

        /// <summary>
        /// Waits for all the elements in the specified array to receive a signal.
        /// </summary>
        public static bool WaitAll(SystemWaitHandle[] waitHandles) => WaitAll(waitHandles, SystemTimeout.Infinite);

        /// <summary>
        /// Waits for all the elements in the specified array to receive a signal, using
        /// a time span value to specify the time interval.
        /// </summary>
        public static bool WaitAll(SystemWaitHandle[] waitHandles, TimeSpan timeout) =>
            WaitAll(waitHandles, timeout, false);

        /// <summary>
        /// Waits for all the elements in the specified array to receive a signal, using
        /// a 32-bit integer value to specify the time interval.
        /// </summary>
        public static bool WaitAll(SystemWaitHandle[] waitHandles, int millisecondsTimeout) =>
            WaitAll(waitHandles, millisecondsTimeout, false);

        /// <summary>
        /// Waits for all the elements in the specified array to receive a signal, using
        /// a time span value to specify the time interval and specifying whether to
        /// exit the synchronization domain before the wait.
        /// </summary>
        public static bool WaitAll(SystemWaitHandle[] waitHandles, TimeSpan timeout, bool exitContext)
        {
            long totalMilliseconds = (long)timeout.TotalMilliseconds;
            if (totalMilliseconds < -1 || totalMilliseconds > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(timeout));
            }

            return WaitAll(waitHandles, (int)totalMilliseconds, exitContext);
        }

        /// <summary>
        /// Waits for all the elements in the specified array to receive a signal, using
        /// a 32-bit integer value to specify the time interval and specifying whether to
        /// exit the synchronization domain before the wait.
        /// </summary>
        public static bool WaitAll(SystemWaitHandle[] waitHandles, int millisecondsTimeout, bool exitContext)
        {
            // var runtime = CoyoteRuntime.Current;
            // if (runtime.SchedulingPolicy is SchedulingPolicy.Interleaving)
            // {
            //     return new Wrapper(runtime, initialState);
            // }

            return SystemWaitHandle.WaitAll(waitHandles, millisecondsTimeout, exitContext);
        }

        /// <summary>
        /// Waits for any of the elements in the specified array to receive a signal.
        /// </summary>
        public static int WaitAny(SystemWaitHandle[] waitHandles) => WaitAny(waitHandles, SystemTimeout.Infinite);

        /// <summary>
        /// Waits for any of the elements in the specified array to receive a signal, using
        /// a time span value to specify the time interval.
        /// </summary>
        public static int WaitAny(SystemWaitHandle[] waitHandles, TimeSpan timeout) =>
            WaitAny(waitHandles, timeout, false);

        /// <summary>
        /// Waits for any of the elements in the specified array to receive a signal, using
        /// a 32-bit integer value to specify the time interval.
        /// </summary>
        public static int WaitAny(SystemWaitHandle[] waitHandles, int millisecondsTimeout) =>
            WaitAny(waitHandles, millisecondsTimeout, false);

        /// <summary>
        /// Waits for any of the elements in the specified array to receive a signal, using
        /// a time span value to specify the time interval and specifying whether to
        /// exit the synchronization domain before the wait.
        /// </summary>
        public static int WaitAny(SystemWaitHandle[] waitHandles, TimeSpan timeout, bool exitContext)
        {
            long totalMilliseconds = (long)timeout.TotalMilliseconds;
            if (totalMilliseconds < -1 || totalMilliseconds > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(timeout));
            }

            return WaitAny(waitHandles, (int)totalMilliseconds, exitContext);
        }

        /// <summary>
        /// Waits for any of the elements in the specified array to receive a signal, using
        /// a 32-bit integer value to specify the time interval and specifying whether to
        /// exit the synchronization domain before the wait.
        /// </summary>
        public static int WaitAny(SystemWaitHandle[] waitHandles, int millisecondsTimeout, bool exitContext)
        {
            // var runtime = CoyoteRuntime.Current;
            // if (runtime.SchedulingPolicy is SchedulingPolicy.Interleaving)
            // {
            //     return new Wrapper(runtime, initialState);
            // }

            return SystemWaitHandle.WaitAny(waitHandles, millisecondsTimeout, exitContext);
        }

        /// <summary>
        /// Releases all resources held by the current <see cref="SystemWaitHandle"/>.
        /// </summary>
        public static void Close(SystemWaitHandle instance)
        {
            Resource.Remove(instance);
            instance.Close();
        }

        /// <summary>
        /// Releases all resources held by the current <see cref="SystemWaitHandle"/>.
        /// </summary>
        public static void Dispose(SystemWaitHandle instance)
        {
            Resource.Remove(instance);
            instance.Dispose();
        }

        /// <summary>
        /// Resource that is used to control a <see cref="SystemWaitHandle"/> during testing.
        /// </summary>
        internal abstract class Resource : IDisposable
        {
            /// <summary>
            /// Cache from synchronized objects to synchronized block instances.
            /// </summary>
            private static readonly ConcurrentDictionary<SystemWaitHandle, Resource> Cache =
                new ConcurrentDictionary<SystemWaitHandle, Resource>();

            /// <summary>
            /// The id of the <see cref="CoyoteRuntime"/> that created this handle.
            /// </summary>
            protected readonly Guid RuntimeId;

            /// <summary>
            /// The resource id of this handle.
            /// </summary>
            protected readonly Guid ResourceId;

            /// <summary>
            /// The wait handle that is being controlled.
            /// </summary>
            protected readonly SystemWaitHandle Handle;

            /// <summary>
            /// True if the handle is signaled, else false.
            /// </summary>
            protected bool IsSignaled;

            /// <summary>
            /// Queue of operations waiting to be released.
            /// </summary>
            protected readonly Queue<ControlledOperation> PausedOperations;

            /// <summary>
            /// The debug name of this handle.
            /// </summary>
            protected readonly string DebugName;

            /// <summary>
            /// Initializes a new instance of the <see cref="Resource"/> class.
            /// </summary>
            internal Resource(CoyoteRuntime runtime, SystemWaitHandle handle, bool isSignaled)
            {
                this.RuntimeId = runtime.Id;
                this.ResourceId = Guid.NewGuid();
                this.Handle = handle;
                this.IsSignaled = isSignaled;
                this.PausedOperations = new Queue<ControlledOperation>();
                this.DebugName = $"{handle.GetType().Name}({this.ResourceId})";
            }

            /// <summary>
            /// Adds the specified resource to the cache.
            /// </summary>
            internal static void Add(Resource handle) => Cache.GetOrAdd(handle.Handle, key => handle);

            /// <summary>
            /// Removes the resource associated with the specified wait handle. from the cache.
            /// </summary>
            internal static void Remove(SystemWaitHandle handle) => Cache.TryRemove(handle, out _);

            /// <summary>
            /// Finds the resource associated with the specified wait handle.
            /// </summary>
            internal static bool TryFind(SystemWaitHandle handle, out Resource resource) =>
                Cache.TryGetValue(handle, out resource);

            /// <summary>
            /// Pauses the current operation until it receives a signal.
            /// </summary>
            internal bool WaitOne(int millisecondsTimeout)
            {
                CoyoteRuntime runtime = this.GetRuntime();
                using (runtime.EnterSynchronizedSection())
                {
                    if (!runtime.TryGetExecutingOperation(out ControlledOperation current))
                    {
                        runtime.NotifyUncontrolledSynchronizationInvocation("WaitHandle.WaitOne");
                    }
                    else if (runtime.Configuration.IsLockAccessRaceCheckingEnabled)
                    {
                        runtime.ScheduleNextOperation(current, SchedulingPointType.Acquire);
                    }

                    // TODO: check what is returned if the handle is signaled already.
                    if (millisecondsTimeout is 0 || this.IsSignaled)
                    {
                        return false;
                    }

                    runtime.LogWriter.LogDebug(
                        "[coyote::debug] Operation {0} is waiting for '{1}' to get signaled on thread '{2}'.",
                        current.DebugInfo, this.DebugName, SystemThread.CurrentThread.ManagedThreadId);
                    // TODO: consider introducing the notion of a PausedOnResourceOrDelay to model timeouts!
                    current.Status = OperationStatus.PausedOnResource;
                    this.PausedOperations.Enqueue(current);
                    runtime.ScheduleNextOperation(current, SchedulingPointType.Pause);
                    return true;
                }
            }

            /// <summary>
            /// Sends a signal to any waiting operations.
            /// </summary>
            /// <remarks>
            /// It is assumed that this method runs in the scope of the runtime <see cref="SynchronizedSection"/>.
            /// </remarks>
            protected void Signal()
            {
                while (this.PausedOperations.Count > 0)
                {
                    // Release the next operation awaiting a signal.
                    ControlledOperation operation = this.PausedOperations.Dequeue();
                    operation.Status = OperationStatus.Enabled;
                }
            }

            /// <summary>
            /// Returns the current runtime, asserting that it is the same runtime that created this resource.
            /// </summary>
            protected CoyoteRuntime GetRuntime()
            {
                var runtime = CoyoteRuntime.Current;
                if (runtime.Id != this.RuntimeId)
                {
                    runtime.NotifyAssertionFailure($"Accessing '{this.DebugName}' that was created " +
                        $"in a previous test iteration with runtime id '{this.RuntimeId}'.");
                }

                return runtime;
            }

            /// <summary>
            /// Releases resources used by the resource.
            /// </summary>
            protected void Dispose(bool disposing)
            {
                if (disposing)
                {
                    Resource.Remove(this.Handle);
                }
            }

            /// <summary>
            /// Releases resources used by the resource.
            /// </summary>
            public void Dispose()
            {
                this.Dispose(true);
                GC.SuppressFinalize(this);
            }
        }
    }
}
