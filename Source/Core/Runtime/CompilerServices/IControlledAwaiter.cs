// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Coyote.Runtime.CompilerServices
{
    /// <summary>
    /// Represents an awaiter that can be controlled during testing.
    /// </summary>
    internal interface IControlledAwaiter
    {
        /// <summary>
        /// Returns true if the task being awaited is controlled, else false.
        /// </summary>
        bool IsTaskControlled();
    }
}
