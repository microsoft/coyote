// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Coyote.Actors
{
    /// <summary>
    /// The execution status of an actor.
    /// </summary>
    public enum ActorExecutionStatus
    {
        /// <summary>
        /// No status is available.
        /// </summary>
        /// <remarks>
        /// An actor has no status if it was not created by the current
        /// runtime, or it has halted and the runtime disposed it.
        /// </remarks>
        None = 0,

        /// <summary>
        /// The actor is active.
        /// </summary>
        Active,

        /// <summary>
        /// The actor is halting.
        /// </summary>
        Halting,

        /// <summary>
        /// The actor is halted.
        /// </summary>
        Halted
    }
}
