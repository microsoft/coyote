// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Coyote.TestingServices.Scheduling
{
    /// <summary>
    /// Interface of an asynchronous operation that can be controlled during testing.
    /// </summary>
    public interface IAsyncOperation
    {
        /// <summary>
        /// Unique id of the source of the operation.
        /// </summary>
        ulong SourceId { get; }

        /// <summary>
        /// Unique name of the source of the operation.
        /// </summary>
        string SourceName { get; }

        /// <summary>
        /// The status of the operation. An operation can be scheduled only
        /// if it is <see cref="AsyncOperationStatus.Enabled"/>.
        /// </summary>
        AsyncOperationStatus Status { get; set; }
    }
}
