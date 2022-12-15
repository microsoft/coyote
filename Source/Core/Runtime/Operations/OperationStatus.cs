// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Coyote.Runtime
{
    /// <summary>
    /// The status of a controlled operation.
    /// </summary>
    internal enum OperationStatus
    {
        /// <summary>
        /// The operation does not have a status yet.
        /// </summary>
        None = 0,

        /// <summary>
        /// The operation is enabled.
        /// </summary>
        Enabled,

        /// <summary>
        /// The operation is paused until a dependency is resolved.
        /// </summary>
        Paused,

        /// <summary>
        /// The operation is paused until a delay completes.
        /// </summary>
        PausedOnDelay,

        /// <summary>
        /// The operation is paused until it acquires a resource.
        /// </summary>
        PausedOnResource,

        /// <summary>
        /// The operation is paused until receives an event.
        /// </summary>
        PausedOnReceive,

        /// <summary>
        /// The operation is suppressed until it is resumed.
        /// </summary>
        Suppressed,

        /// <summary>
        /// The operation is completed.
        /// </summary>
        Completed
    }
}
