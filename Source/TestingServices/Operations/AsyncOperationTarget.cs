// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

namespace Microsoft.Coyote.TestingServices.Scheduling
{
    /// <summary>
    /// The target of an asynchronous operation.
    /// </summary>
    public enum AsyncOperationTarget
    {
        /// <summary>
        /// The target of the operation is a task. For example, 'Create', 'Start'
        /// and 'Stop' are operations that act upon a task.
        /// </summary>
        Task = 0,

        /// <summary>
        /// The target of the operation is an inbox. For example, 'Send'
        /// and 'Receive' are operations that act upon an inbox.
        /// </summary>
        Inbox
    }
}
