// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

namespace Microsoft.Coyote.TestingServices.Scheduling
{
    /// <summary>
    /// The type of an asynchronous operation.
    /// </summary>
    public enum AsyncOperationType
    {
        /// <summary>
        /// An operation performs a default context switch.
        /// </summary>
        Default = 0,

        /// <summary>
        /// An operation starts executing.
        /// </summary>
        Start,

        /// <summary>
        /// An operation creates another operation.
        /// </summary>
        Create,

        /// <summary>
        /// An operation sends an event.
        /// </summary>
        Send,

        /// <summary>
        /// An operation receives an event.
        /// </summary>
        Receive,

        /// <summary>
        /// An operation stops executing.
        /// </summary>
        Stop,

        /// <summary>
        /// An operation yields.
        /// </summary>
        Yield,

        /// <summary>
        /// An operation waits for another operation to stop.
        /// </summary>
        Join,

        /// <summary>
        /// An operation acquires a synchronized resource.
        /// </summary>
        Acquire,

        /// <summary>
        /// An operation releases a synchronized resource.
        /// </summary>
        Release
    }
}
