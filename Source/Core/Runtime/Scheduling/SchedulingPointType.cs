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
        /// A controlled operation starts executing.
        /// </summary>
        Start,

        /// <summary>
        /// A controlled operation was created.
        /// </summary>
        Create,

        /// <summary>
        /// A controlled operation sends an event.
        /// </summary>
        Send,

        /// <summary>
        /// A controlled operation receives an event.
        /// </summary>
        Receive,

        /// <summary>
        /// A controlled operation halts executing.
        /// </summary>
        Halt,

        /// <summary>
        /// A controlled operation stops executing.
        /// </summary>
        Stop,

        /// <summary>
        /// A controlled operation yields its execution.
        /// </summary>
        Yield,

        /// <summary>
        /// A controlled operation acquires a synchronized resource.
        /// </summary>
        Acquire,

        /// <summary>
        /// A controlled operation releases a synchronized resource.
        /// </summary>
        Release,

        /// <summary>
        /// A controlled operation waits for another operation to stop.
        /// </summary>
        Join,

        /// <summary>
        /// A controlled operation injects a failure.
        /// </summary>
        InjectFailure
    }
}
