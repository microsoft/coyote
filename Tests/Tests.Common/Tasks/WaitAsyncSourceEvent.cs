// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Coyote.Tests.Common.Tasks
{
    /// <summary>
    /// The event that completes the source task of a wait.
    /// </summary>
    public enum WaitAsyncSourceEvent
    {
        /// <summary>
        /// The source task completes successfully with a result.
        /// </summary>
        Result,

        /// <summary>
        /// The source task faults.
        /// </summary>
        Fault,

        /// <summary>
        /// The source task is canceled with its own token.
        /// </summary>
        Cancellation
    }
}
