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

        /// <summary>
        /// True if the awaiter has completed, else false.
        /// </summary>
        bool IsDone { get; }

        /// <summary>
        /// Ends asynchronously waiting for the completion of the awaiter.
        /// </summary>
        void WaitCompletion();
    }

    /// <summary>
    /// Represents an awaiter that can be controlled during testing.
    /// </summary>
    internal interface IControllableAwaiter<TResult>
    {
        /// <summary>
        /// True if the awaiter is controlled, else false.
        /// </summary>
        bool IsControlled { get; }

        /// <summary>
        /// True if the awaiter has completed, else false.
        /// </summary>
        bool IsDone { get; }

        /// <summary>
        /// Ends the asynchronous wait for the completion of the awaiter.
        /// </summary>
        TResult WaitCompletion();
    }
}
