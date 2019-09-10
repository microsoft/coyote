// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

namespace Microsoft.Coyote.TestingServices.Scheduling
{
    /// <summary>
    /// Interface of an asynchronous operation that can be controlled during testing.
    /// </summary>
    public interface IAsyncOperation
    {
        /// <summary>
        /// The type of the operation.
        /// </summary>
        AsyncOperationType Type { get; }

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

        /// <summary>
        /// The target of the operation (which can be the source).
        /// </summary>
        AsyncOperationTarget Target { get; }

        /// <summary>
        /// Unique id of the target of the operation.
        /// </summary>
        ulong TargetId { get; }

        /// <summary>
        /// If the operation is of type <see cref="AsyncOperationType.Receive"/>, then this value
        /// gives the step index of the corresponding <see cref="AsyncOperationType.Send"/>.
        /// </summary>
        ulong MatchingSendIndex { get; }
    }
}
