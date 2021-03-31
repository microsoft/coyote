// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Coyote.Testing
{
    /// <summary>
    /// Abstract program exploration strategy.
    /// </summary>
    internal abstract class ExplorationStrategy
    {
        /// <summary>
        /// Initializes the next iteration.
        /// </summary>
        /// <param name="iteration">The id of the next iteration.</param>
        /// <returns>True to start the specified iteration, else false to stop exploring.</returns>
        internal abstract bool InitializeNextIteration(uint iteration);

        /// <summary>
        /// Returns the count of explored steps.
        /// </summary>
        internal abstract int GetStepCount();

        /// <summary>
        /// True if the strategy has reached the max exploration steps for the current iteration.
        /// </summary>
        internal abstract bool IsMaxStepsReached();

        /// <summary>
        /// Checks if this is a fair strategy.
        /// </summary>
        internal abstract bool IsFair();

        /// <summary>
        /// Returns a description of the strategy.
        /// </summary>
        internal abstract string GetDescription();
    }
}
