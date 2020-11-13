// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Monitor = Microsoft.Coyote.Specifications.Monitor;

namespace Microsoft.Coyote.SystematicTesting.Strategies
{
    /// <summary>
    /// Strategy for detecting liveness property violations using the "temperature"
    /// method. It contains a nested <see cref="SchedulingStrategy"/> that is used
    /// for scheduling decisions. Note that liveness property violations are checked
    /// only if the nested strategy is fair.
    /// </summary>
    internal sealed class TemperatureCheckingStrategy : LivenessCheckingStrategy
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TemperatureCheckingStrategy"/> class.
        /// </summary>
        internal TemperatureCheckingStrategy(Configuration configuration, List<Monitor> monitors, SchedulingStrategy strategy)
            : base(configuration, monitors, strategy)
        {
        }

        /// <inheritdoc/>
        internal override bool GetNextOperation(IEnumerable<AsyncOperation> ops, AsyncOperation current,
            bool isYielding, out AsyncOperation next)
        {
            this.CheckLivenessTemperature();
            return this.SchedulingStrategy.GetNextOperation(ops, current, isYielding, out next);
        }

        /// <inheritdoc/>
        internal override bool GetNextBooleanChoice(AsyncOperation current, int maxValue, out bool next)
        {
            this.CheckLivenessTemperature();
            return this.SchedulingStrategy.GetNextBooleanChoice(current, maxValue, out next);
        }

        /// <inheritdoc/>
        internal override bool GetNextIntegerChoice(AsyncOperation current, int maxValue, out int next)
        {
            this.CheckLivenessTemperature();
            return this.SchedulingStrategy.GetNextIntegerChoice(current, maxValue, out next);
        }

        /// <summary>
        /// Checks the liveness temperature of each monitor, and
        /// reports an error if one of the liveness monitors has
        /// passed the temperature threshold.
        /// </summary>
        private void CheckLivenessTemperature()
        {
            if (this.IsFair())
            {
                foreach (var monitor in this.Monitors)
                {
                    monitor.CheckLivenessTemperature();
                }
            }
        }
    }
}
