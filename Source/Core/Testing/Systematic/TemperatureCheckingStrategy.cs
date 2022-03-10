// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.Specifications;

namespace Microsoft.Coyote.Testing.Systematic
{
    /// <summary>
    /// Strategy for detecting liveness property violations using the "temperature"
    /// method. It contains a nested <see cref="SystematicStrategy"/> that is used
    /// for scheduling decisions. Note that liveness property violations are checked
    /// only if the nested strategy is fair.
    /// </summary>
    internal sealed class TemperatureCheckingStrategy : LivenessCheckingStrategy
    {
        /// <summary>
        /// Responsible for checking specifications.
        /// </summary>
        private SpecificationEngine SpecificationEngine;

        /// <summary>
        /// Initializes a new instance of the <see cref="TemperatureCheckingStrategy"/> class.
        /// </summary>
        internal TemperatureCheckingStrategy(Configuration configuration, IRandomValueGenerator generator,
            SystematicStrategy strategy)
            : base(configuration, generator, strategy)
        {
        }

        /// <summary>
        /// Sets the specification engine.
        /// </summary>
        internal void SetSpecificationEngine(SpecificationEngine specificationEngine)
        {
            this.SpecificationEngine = specificationEngine;
        }

        /// <inheritdoc/>
        internal override bool GetNextOperation(IEnumerable<ControlledOperation> ops, ControlledOperation current,
            bool isYielding, out ControlledOperation next)
        {
            if (this.IsFair)
            {
                this.SpecificationEngine?.CheckLivenessThresholdExceeded();
            }

            return this.SchedulingStrategy.GetNextOperation(ops, current, isYielding, out next);
        }

        /// <inheritdoc/>
        internal override bool GetNextBooleanChoice(ControlledOperation current, int maxValue, out bool next)
        {
            if (this.IsFair)
            {
                this.SpecificationEngine?.CheckLivenessThresholdExceeded();
            }

            return this.SchedulingStrategy.GetNextBooleanChoice(current, maxValue, out next);
        }

        /// <inheritdoc/>
        internal override bool GetNextIntegerChoice(ControlledOperation current, int maxValue, out int next)
        {
            if (this.IsFair)
            {
                this.SpecificationEngine?.CheckLivenessThresholdExceeded();
            }

            return this.SchedulingStrategy.GetNextIntegerChoice(current, maxValue, out next);
        }
    }
}
