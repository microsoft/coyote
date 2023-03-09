// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Runtime;
using SystemAutoResetEvent = System.Threading.AutoResetEvent;
using SystemEventResetMode = System.Threading.EventResetMode;
using SystemWaitHandle = System.Threading.WaitHandle;

namespace Microsoft.Coyote.Rewriting.Types.Threading
{
    /// <summary>
    /// Represents a thread synchronization event that, when signaled, resets automatically
    /// after releasing a single waiting thread.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static class AutoResetEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AutoResetEvent"/> class, with a value
        /// indicating whether to set the initial state to signaled.
        /// </summary>
        public static SystemAutoResetEvent Create(bool initialState)
        {
            var instance = new SystemAutoResetEvent(initialState);
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.Interleaving)
            {
                Resource resource = new Resource(runtime, instance, initialState);
                Resource.Add(resource);
            }

            return instance;
        }

        /// <summary>
        /// Resource that is used to control a <see cref="SystemAutoResetEvent"/> during testing.
        /// </summary>
        internal class Resource : EventWaitHandle.Resource
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="Resource"/> class.
            /// </summary>
            internal Resource(CoyoteRuntime runtime, SystemWaitHandle handle, bool initialState)
                : base(runtime, handle, initialState, SystemEventResetMode.AutoReset)
            {
            }
        }
    }
}
