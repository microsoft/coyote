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
        /// An asynchronous operation performs a default context switch.
        /// </summary>
        Default = 0,

        /// <summary>
        /// An asynchronous operation starts executing.
        /// </summary>
        Start,

        /// <summary>
        /// An asynchronous operation creates another asynchronous operation.
        /// </summary>
        Create,

        /// <summary>
        /// An asynchronous operation sends an event.
        /// </summary>
        Send,

        /// <summary>
        /// An asynchronous operation receives an event.
        /// </summary>
        Receive,

        /// <summary>
        /// An asynchronous operation stops executing.
        /// </summary>
        Stop,

        /// <summary>
        /// An asynchronous operation yields.
        /// </summary>
        Yield,

        /// <summary>
        /// An asynchronous operation waits for another asynchronous operation to stop.
        /// </summary>
        Join
    }
}
