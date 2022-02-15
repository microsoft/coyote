// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Coyote.Runtime.CompilerServices
{
    /// <summary>
    /// Represents an awaiter that can be controlled during testing.
    /// </summary>
    internal interface IControllableAwaiter
    {
        /// <summary>
        /// True if the awaiter is controlled, else false.
        /// </summary>
        bool IsControlled { get; }
    }
}
