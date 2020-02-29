// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Utilities;

namespace Microsoft.Coyote.TestingServices
{
    /// <summary>
    /// The Coyote testing portfolio.
    /// </summary>
    internal static class TestingPortfolio
    {
        /// <summary>
        /// Configures the testing strategy for the current
        /// testing process.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        internal static void ConfigureStrategyForCurrentProcess(Configuration configuration)
        {
            // Random, PCT[1], ProbabilisticRandom[1], PCT[5], ProbabilisticRandom[2], PCT[10], etc.
            if (configuration.TestingProcessId == 0)
            {
                configuration.SchedulingStrategy = SchedulingStrategy.Random;
            }
            else if (configuration.TestingProcessId % 2 == 0)
            {
                configuration.SchedulingStrategy = SchedulingStrategy.ProbabilisticRandom;
                configuration.CoinFlipBound = (int)(configuration.TestingProcessId / 2);
            }
            else if (configuration.TestingProcessId == 1)
            {
                configuration.SchedulingStrategy = SchedulingStrategy.FairPCT;
                configuration.PrioritySwitchBound = 1;
            }
            else
            {
                configuration.SchedulingStrategy = SchedulingStrategy.FairPCT;
                configuration.PrioritySwitchBound = 5 * (int)((configuration.TestingProcessId + 1) / 2);
            }
        }
    }
}
