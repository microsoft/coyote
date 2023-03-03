// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Runtime;
using SystemEventResetMode = System.Threading.EventResetMode;
using SystemManualResetEvent = System.Threading.ManualResetEvent;
using SystemWaitHandle = System.Threading.WaitHandle;

namespace Microsoft.Coyote.Rewriting.Types.Threading
{
    /// <summary>
    /// Represents a thread synchronization event that, when signaled, must be reset manually.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static class ManualResetEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ManualResetEvent"/> class, with a value
        /// indicating whether to set the initial state to signaled.
        /// </summary>
        public static SystemManualResetEvent Create(bool initialState)
        {
            var instance = new SystemManualResetEvent(initialState);
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.Interleaving)
            {
                Resource resource = new Resource(runtime, instance, initialState);
                Resource.Add(resource);
            }

            return instance;
        }

        /// <summary>
        /// Resource that is used to control a <see cref="SystemManualResetEvent"/> during testing.
        /// </summary>
        internal class Resource : EventWaitHandle.Resource
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="Resource"/> class.
            /// </summary>
            internal Resource(CoyoteRuntime runtime, SystemWaitHandle handle, bool initialState)
                : base(runtime, handle, initialState, SystemEventResetMode.ManualReset)
            {
            }
        }
    }
}
