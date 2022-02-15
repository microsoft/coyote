// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Coyote.Runtime
{
    /// <summary>
    /// The type of a scheduling point.
    /// </summary>
    internal enum SchedulingPointType
    {
        /// <summary>
        /// The default scheduling point type.
        /// </summary>
        Default = 0,

        /// <summary>
        /// A controlled operation started executing.
        /// </summary>
        Start,

        /// <summary>
        /// A controlled operation was created.
        /// </summary>
        Create,

        /// <summary>
        /// A controlled operation scheduled its continuation.
        /// </summary>
        ContinueWith,

        /// <summary>
        /// A controlled operation is blocked until one or more dependencies are satisfied.
        /// </summary>
        Wait,

        /// <summary>
        /// A controlled operation yielded its execution.
        /// </summary>
        Yield,

        /// <summary>
        /// A controlled operation acquired a synchronized resource.
        /// </summary>
        Acquire,

        /// <summary>
        /// A controlled operation released a synchronized resource.
        /// </summary>
        Release,

        /// <summary>
        /// A controlled operation sent an event.
        /// </summary>
        Send,

        /// <summary>
        /// A controlled operation received an event.
        /// </summary>
        Receive,

        /// <summary>
        /// A controlled operation halted executing.
        /// </summary>
        Halt,

        /// <summary>
        /// A controlled operation completed its execution.
        /// </summary>
        Complete,

        /// <summary>
        /// A controlled operation injected a failure.
        /// </summary>
        InjectFailure
    }
}
