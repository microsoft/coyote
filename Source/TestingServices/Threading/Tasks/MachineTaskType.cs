// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.Coyote.Threading.Tasks;

namespace Microsoft.Coyote.TestingServices.Threading.Tasks
{
    /// <summary>
    /// Specifies the type of a <see cref="ControlledTask"/>.
    /// </summary>
    internal enum MachineTaskType
    {
        /// <summary>
        /// Specifies that the task was explicitly created.
        /// </summary>
        ExplicitTask = 0,

        /// <summary>
        /// Specifies that the task was created by a completion source.
        /// </summary>
        CompletionSourceTask
    }
}
