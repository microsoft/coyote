// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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
                return resource.WaitOne(runtime, millisecondsTimeout);
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
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.Interleaving)
            {
                return Resource.WaitAll(runtime, waitHandles, millisecondsTimeout);
            }

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
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.Interleaving)
            {
                return Resource.WaitAny(runtime, waitHandles, millisecondsTimeout);
            }

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
            /// Cache from handles to resources.
            /// </summary>
            private static readonly ConcurrentDictionary<SystemWaitHandle, Resource> Cache =
                new ConcurrentDictionary<SystemWaitHandle, Resource>();

            /// <summary>
            /// Cache from controlled operation ids to resource ids.
            /// </summary>
            private static readonly ConcurrentDictionary<ulong, Guid> SignalCache = new ConcurrentDictionary<ulong, Guid>();

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
            /// Set of operations waiting to be signaled.
            /// </summary>
            protected readonly HashSet<ControlledOperation> PausedOperations;

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
                this.PausedOperations = new HashSet<ControlledOperation>();
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
            internal bool WaitOne(CoyoteRuntime runtime, int millisecondsTimeout)
            {
                using (runtime.EnterSynchronizedSection())
                {
                    this.CheckRuntime(runtime);
                    if (!runtime.TryGetExecutingOperation(out ControlledOperation current))
                    {
                        runtime.NotifyUncontrolledSynchronizationInvocation("WaitHandle.WaitOne");
                    }
                    else if (runtime.Configuration.IsLockAccessRaceCheckingEnabled)
                    {
                        runtime.ScheduleNextOperation(current, SchedulingPointType.Acquire);
                    }

                    if (!this.IsSignaled)
                    {
                        if (millisecondsTimeout is 0)
                        {
                            return false;
                        }

                        runtime.LogWriter.LogDebug(
                            "[coyote::debug] Operation {0} is waiting for '{1}' to get signaled on thread '{2}'.",
                            current.DebugInfo, this.DebugName, SystemThread.CurrentThread.ManagedThreadId);
                        // TODO: consider introducing the notion of a PausedOnResourceOrDelay to model timeouts!
                        current.PauseWithResource(this.ResourceId);
                        this.PausedOperations.Add(current);
                        runtime.ScheduleNextOperation(current, SchedulingPointType.Pause);
                    }

                    return true;
                }
            }

            /// <summary>
            /// Pauses the current operation until it receives a signal from all the specified handles.
            /// </summary>
            internal static bool WaitAll(CoyoteRuntime runtime, SystemWaitHandle[] waitHandles, int millisecondsTimeout)
            {
                using (runtime.EnterSynchronizedSection())
                {
                    if (!runtime.TryGetExecutingOperation(out ControlledOperation current))
                    {
                        runtime.NotifyUncontrolledSynchronizationInvocation("WaitHandle.WaitAll");
                    }
                    else if (runtime.Configuration.IsLockAccessRaceCheckingEnabled)
                    {
                        runtime.ScheduleNextOperation(current, SchedulingPointType.Acquire);
                    }

                    Resource[] resources = GetResources(runtime, waitHandles);
                    if (resources.Any(r => !r.IsSignaled))
                    {
                        if (millisecondsTimeout is 0)
                        {
                            return false;
                        }

                        runtime.LogWriter.LogDebug(
                            "[coyote::debug] Operation {0} is waiting for all 'WaitHandles' to get signaled on thread '{1}'.",
                            current.DebugInfo, SystemThread.CurrentThread.ManagedThreadId);

                        // TODO: consider introducing the notion of a PausedOnResourceOrDelay to model timeouts!
                        var nonSignaled = resources.Where(r => !r.IsSignaled);
                        current.PauseWithResources(nonSignaled.Select(r => r.ResourceId), true);
                        foreach (Resource resource in nonSignaled)
                        {
                            resource.PausedOperations.Add(current);
                        }

                        runtime.ScheduleNextOperation(current, SchedulingPointType.Pause);
                    }

                    return true;
                }
            }

            /// <summary>
            /// Pauses the current operation until it receives a signal from any of the specified handles.
            /// </summary>
            internal static int WaitAny(CoyoteRuntime runtime, SystemWaitHandle[] waitHandles, int millisecondsTimeout)
            {
                using (runtime.EnterSynchronizedSection())
                {
                    if (!runtime.TryGetExecutingOperation(out ControlledOperation current))
                    {
                        runtime.NotifyUncontrolledSynchronizationInvocation("WaitHandle.WaitAny");
                    }
                    else if (runtime.Configuration.IsLockAccessRaceCheckingEnabled)
                    {
                        runtime.ScheduleNextOperation(current, SchedulingPointType.Acquire);
                    }

                    int result = millisecondsTimeout;
                    Resource[] resources = GetResources(runtime, waitHandles);
                    if (!resources.All(r => r.IsSignaled))
                    {
                        if (millisecondsTimeout is 0)
                        {
                            return result;
                        }

                        runtime.LogWriter.LogDebug(
                            "[coyote::debug] Operation {0} is waiting for any 'WaitHandle' to get signaled on thread '{1}'.",
                            current.DebugInfo, SystemThread.CurrentThread.ManagedThreadId);

                        Resource[] nonSignaled = resources.Where(r => !r.IsSignaled).ToArray();
                        try
                        {
                            // TODO: consider introducing the notion of a PausedOnResourceOrDelay to model timeouts!
                            current.PauseWithResources(nonSignaled.Select(r => r.ResourceId), false);
                            foreach (Resource resource in nonSignaled)
                            {
                                resource.PausedOperations.Add(current);
                            }

                            runtime.ScheduleNextOperation(current, SchedulingPointType.Pause);
                        }
                        finally
                        {
                            // Find the index of the signaling resource and clean up the cache.
                            SignalCache.TryGetValue(current.Id, out Guid signalingResource);
                            result = signalingResource == Guid.Empty ?
                                Array.FindIndex(resources, r => r.IsSignaled) :
                                Array.FindIndex(nonSignaled, r => r.ResourceId == signalingResource);
                            SignalCache.TryRemove(current.Id, out _);
                        }
                    }
                    else
                    {
                        result = Array.FindIndex(resources, r => r.IsSignaled);
                    }

                    return result;
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
                foreach (ControlledOperation operation in this.PausedOperations)
                {
                    OperationStatus status = operation.Status;
                    if (operation.Signal(this.ResourceId) && status is OperationStatus.PausedOnAnyResource)
                    {
                        // This signal successfully enabled the operation.
                        SignalCache.TryAdd(operation.Id, this.ResourceId);
                    }
                }

                this.PausedOperations.Clear();
            }

            /// <summary>
            /// Return the resources associated with the specified handles.
            /// </summary>
            /// <remarks>
            /// It is assumed that this method runs in the scope of the runtime <see cref="SynchronizedSection"/>.
            /// </remarks>
            private static Resource[] GetResources(CoyoteRuntime runtime, SystemWaitHandle[] waitHandles)
            {
                var resources = new Resource[waitHandles.Length];
                for (int idx = 0; idx < waitHandles.Length; idx++)
                {
                    if (!TryFind(waitHandles[idx], out Resource resource))
                    {
                        runtime.NotifyAssertionFailure($"Accessing 'WaitHandle' that is not intercepted and controlled " +
                            "during testing, so it can interfere with the ability to reproduce bug traces.");
                    }

                    resource.CheckRuntime(runtime);
                    resources[idx] = resource;
                }

                return resources;
            }

            /// <summary>
            /// Checks that the current runtime is the same runtime that created this resource.
            /// </summary>
            protected void CheckRuntime(CoyoteRuntime runtime)
            {
                if (runtime.Id != this.RuntimeId)
                {
                    runtime.NotifyAssertionFailure($"Accessing '{this.DebugName}' that was created " +
                        $"in a previous test iteration with runtime id '{this.RuntimeId}'.");
                }
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
