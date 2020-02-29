// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace Microsoft.Coyote.TestingServices.Scheduling.Strategies
{
    /// <summary>
    /// Interface of a machine scheduling strategy.
    /// </summary>
    public interface ISchedulingStrategy
    {
        /// <summary>
        /// Forces the next asynchronous operation to be scheduled.
        /// </summary>
        /// <param name="next">The next operation to schedule.</param>
        /// <param name="ops">List of operations that can be scheduled.</param>
        /// <param name="current">The currently scheduled operation.</param>
        /// <returns>True if there is a next choice, else false.</returns>
        bool GetNext(out IAsyncOperation next, List<IAsyncOperation> ops, IAsyncOperation current);

        /// <summary>
        /// Returns the next boolean choice.
        /// </summary>
        /// <param name="maxValue">The max value.</param>
        /// <param name="next">The next boolean choice.</param>
        /// <returns>True if there is a next choice, else false.</returns>
        bool GetNextBooleanChoice(int maxValue, out bool next);

        /// <summary>
        /// Returns the next integer choice.
        /// </summary>
        /// <param name="maxValue">The max value.</param>
        /// <param name="next">The next integer choice.</param>
        /// <returns>True if there is a next choice, else false.</returns>
        bool GetNextIntegerChoice(int maxValue, out int next);

        /// <summary>
        /// Forces the next asynchronous operation to be scheduled.
        /// </summary>
        /// <param name="next">The next operation to schedule.</param>
        /// <param name="ops">List of operations that can be scheduled.</param>
        /// <param name="current">The currently scheduled operation.</param>
        void ForceNext(IAsyncOperation next, List<IAsyncOperation> ops, IAsyncOperation current);

        /// <summary>
        /// Forces the next boolean choice.
        /// </summary>
        /// <param name="maxValue">The max value.</param>
        /// <param name="next">The next boolean choice.</param>
        void ForceNextBooleanChoice(int maxValue, bool next);

        /// <summary>
        /// Forces the next integer choice.
        /// </summary>
        /// <param name="maxValue">The max value.</param>
        /// <param name="next">The next integer choice.</param>
        void ForceNextIntegerChoice(int maxValue, int next);

        /// <summary>
        /// Prepares for the next scheduling iteration. This is invoked
        /// at the end of a scheduling iteration. It must return false
        /// if the scheduling strategy should stop exploring.
        /// </summary>
        /// <returns>True to start the next iteration.</returns>
        bool PrepareForNextIteration();

        /// <summary>
        /// Resets the scheduling strategy. This is typically invoked by
        /// parent strategies to reset child strategies.
        /// </summary>
        void Reset();

        /// <summary>
        /// Returns the scheduled steps.
        /// </summary>
        int GetScheduledSteps();

        /// <summary>
        /// True if the scheduling strategy has reached the max
        /// scheduling steps for the given scheduling iteration.
        /// </summary>
        bool HasReachedMaxSchedulingSteps();

        /// <summary>
        /// Checks if this is a fair scheduling strategy.
        /// </summary>
        bool IsFair();

        /// <summary>
        /// Returns a textual description of the scheduling strategy.
        /// </summary>
        string GetDescription();
    }
}
