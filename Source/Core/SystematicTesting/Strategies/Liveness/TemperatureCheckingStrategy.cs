// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.Specifications;

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
        internal TemperatureCheckingStrategy(Configuration configuration, SpecificationEngine specificationEngine,
            SchedulingStrategy strategy)
            : base(configuration, specificationEngine, strategy)
        {
        }

        /// <inheritdoc/>
        internal override bool GetNextOperation(IEnumerable<AsyncOperation> ops, AsyncOperation current,
            bool isYielding, out AsyncOperation next)
        {
            if (this.IsFair())
            {
                this.SpecificationEngine.CheckLivenessThresholdExceeded();
            }

            return this.SchedulingStrategy.GetNextOperation(ops, current, isYielding, out next);
        }

        /// <inheritdoc/>
        internal override bool GetNextBooleanChoice(AsyncOperation current, int maxValue, out bool next)
        {
            if (this.IsFair())
            {
                this.SpecificationEngine.CheckLivenessThresholdExceeded();
            }

            return this.SchedulingStrategy.GetNextBooleanChoice(current, maxValue, out next);
        }

        /// <inheritdoc/>
        internal override bool GetNextIntegerChoice(AsyncOperation current, int maxValue, out int next)
        {
            if (this.IsFair())
            {
                this.SpecificationEngine.CheckLivenessThresholdExceeded();
            }

            return this.SchedulingStrategy.GetNextIntegerChoice(current, maxValue, out next);
        }
    }
}
