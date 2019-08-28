// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Diagnostics;
using System.Reflection;
using System.Text;

using Microsoft.Coyote.Utilities;

namespace Microsoft.Coyote.TestingServices
{
    /// <summary>
    /// The Coyote testing process factory.
    /// </summary>
    internal static class TestingProcessFactory
    {
        /// <summary>
        /// Creates a new testing process.
        /// </summary>
        public static Process Create(uint id, Configuration configuration)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo(
                Assembly.GetExecutingAssembly().Location,
                CreateArgumentsFromConfiguration(id, configuration));
            startInfo.UseShellExecute = false;

            Process process = new Process();
            process.StartInfo = startInfo;

            return process;
        }

        /// <summary>
        /// Creates arguments from the specified configuration.
        /// </summary>
        private static string CreateArgumentsFromConfiguration(uint id, Configuration configuration)
        {
            StringBuilder arguments = new StringBuilder();

            if (configuration.EnableDebugging)
            {
                arguments.Append($"/debug ");
            }

            arguments.Append($"/test:{configuration.AssemblyToBeAnalyzed} ");

            if (!string.IsNullOrEmpty(configuration.TestingRuntimeAssembly))
            {
                arguments.Append($"/runtime:{configuration.TestingRuntimeAssembly} ");
            }

            if (!string.IsNullOrEmpty(configuration.TestMethodName))
            {
                arguments.Append($"/method:{configuration.TestMethodName} ");
            }

            arguments.Append($"/i:{configuration.SchedulingIterations} ");
            arguments.Append($"/timeout:{configuration.Timeout} ");

            if (configuration.UserExplicitlySetMaxFairSchedulingSteps)
            {
                arguments.Append($"/max-steps:{configuration.MaxUnfairSchedulingSteps}:" +
                    $"{configuration.MaxFairSchedulingSteps} ");
            }
            else
            {
                arguments.Append($"/max-steps:{configuration.MaxUnfairSchedulingSteps} ");
            }

            if (configuration.SchedulingStrategy == SchedulingStrategy.PCT ||
                configuration.SchedulingStrategy == SchedulingStrategy.FairPCT)
            {
                arguments.Append($"/sch:{configuration.SchedulingStrategy}:" +
                    $"{configuration.PrioritySwitchBound} ");
            }
            else if (configuration.SchedulingStrategy == SchedulingStrategy.ProbabilisticRandom)
            {
                arguments.Append($"/sch:probabilistic:{configuration.CoinFlipBound} ");
            }
            else if (configuration.SchedulingStrategy == SchedulingStrategy.Random ||
                configuration.SchedulingStrategy == SchedulingStrategy.Portfolio)
            {
                arguments.Append($"/sch:{configuration.SchedulingStrategy} ");
            }

            if (configuration.RandomSchedulingSeed != null)
            {
                arguments.Append($"/sch-seed:{configuration.RandomSchedulingSeed} ");
            }

            if (configuration.PerformFullExploration)
            {
                arguments.Append($"/explore ");
            }

            arguments.Append($"/timeout-delay:{configuration.TimeoutDelay} ");

            if (configuration.ReportCodeCoverage && configuration.ReportActivityCoverage)
            {
                arguments.Append($"/coverage ");
            }
            else if (configuration.ReportCodeCoverage)
            {
                arguments.Append($"/coverage:code ");
            }
            else if (configuration.ReportActivityCoverage)
            {
                arguments.Append($"/coverage:activity ");
            }

            if (configuration.EnableCycleDetection)
            {
                arguments.Append($"/cycle-detection ");
            }

            if (configuration.OutputFilePath.Length > 0)
            {
                arguments.Append($"/o:{configuration.OutputFilePath} ");
            }

            arguments.Append($"/run-as-parallel-testing-task ");
            arguments.Append($"/testing-scheduler-endpoint:{configuration.TestingSchedulerEndPoint} ");
            arguments.Append($"/testing-scheduler-process-id:{Process.GetCurrentProcess().Id} ");
            arguments.Append($"/testing-process-id:{id}");

            return arguments.ToString();
        }
    }
}
