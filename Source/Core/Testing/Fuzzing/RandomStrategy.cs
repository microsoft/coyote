// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Coyote.Testing.Fuzzing
{
    /// <summary>
    /// Random strategy. (Delay prob - 0.05; random delay range - [0, 100]; upper bound: 5000 per task).
    /// </summary>
    internal class RandomStrategy : FuzzingStrategy
    {
        /// <summary>
        /// Random value generator.
        /// </summary>
        protected IRandomValueGenerator RandomValueGenerator;

        /// <summary>
        /// The maximum number of steps to explore.
        /// </summary>
        protected readonly int MaxSteps;

        /// <summary>
        /// The number of exploration steps.
        /// </summary>
        protected int StepCount;

        /// <summary>
        /// Dictionary to keep a track of delay per thread.
        /// </summary>
        private readonly Dictionary<int, int> PerTaskTotalDelay;

        /// <summary>
        /// Initializes a new instance of the <see cref="RandomStrategy"/> class.
        /// </summary>
        internal RandomStrategy(int maxDelays, IRandomValueGenerator random)
        {
            this.RandomValueGenerator = random;
            this.MaxSteps = maxDelays;
            this.PerTaskTotalDelay = new Dictionary<int, int>();
        }

        /// <inheritdoc/>
        internal override bool InitializeNextIteration(uint iteration)
        {
            this.StepCount = 0;
            this.PerTaskTotalDelay.Clear();
            return true;
        }

        /// <inheritdoc/>
        internal override bool GetNextDelay(int maxValue, out int next)
        {
            int? currentTaskId = Task.CurrentId;
            if (currentTaskId == null)
            {
                next = 0;
                return true;
            }

            this.StepCount++;

            int retval = 0;
            // 0.05 probability of 1-100ms delay
            if (this.RandomValueGenerator.NextDouble() < 0.05)
            {
                retval = this.RandomValueGenerator.Next(100);
            }

            // Make sure that the max delay per Task is less than 5s.
            if (this.PerTaskTotalDelay.TryGetValue((int)currentTaskId, out int delay))
            {
                // Max delay per thread.
                if (delay > 5000)
                {
                    retval = 0;
                }

                // Update the total delay per thread.
                this.PerTaskTotalDelay.Remove((int)currentTaskId);
                this.PerTaskTotalDelay.Add((int)currentTaskId, delay + retval);
            }
            else
            {
                this.PerTaskTotalDelay.Add((int)currentTaskId, retval);
            }

            next = retval;
            return true;
        }

        /// <inheritdoc/>
        internal override int GetStepCount() => this.StepCount;

        /// <inheritdoc/>
        internal override bool IsMaxStepsReached()
        {
            if (this.MaxSteps is 0)
            {
                return false;
            }

            return this.StepCount >= this.MaxSteps;
        }

        /// <inheritdoc/>
        internal override bool IsFair() => true;

        /// <inheritdoc/>
        internal override string GetDescription() => $"Random[seed '{this.RandomValueGenerator.Seed}']";
    }
}
