// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using Microsoft.Coyote.Runtime.Exploration;

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
            string assembly = Assembly.GetExecutingAssembly().Location;
            Console.WriteLine("Launching " + assembly);
#if NET46 || NET47
            ProcessStartInfo startInfo = new ProcessStartInfo(assembly,
                CreateArgumentsFromConfiguration(id, configuration));
#else
            ProcessStartInfo startInfo = new ProcessStartInfo("dotnet", assembly + " " +
                CreateArgumentsFromConfiguration(id, configuration));
#endif
            startInfo.UseShellExecute = false;

            Process process = new Process();
            process.StartInfo = startInfo;

            return process;
        }

        /// <summary>
        /// Creates arguments from the specified configuration.
        /// </summary>
        internal static string CreateArgumentsFromConfiguration(uint id, Configuration configuration)
        {
            StringBuilder arguments = new StringBuilder();

            arguments.Append($"test {configuration.AssemblyToBeAnalyzed} ");

            if (configuration.EnableDebugging)
            {
                arguments.Append("--debug ");
            }

            if (!string.IsNullOrEmpty(configuration.TestingRuntimeAssembly))
            {
                arguments.Append($"--runtime {configuration.TestingRuntimeAssembly} ");
            }

            if (!string.IsNullOrEmpty(configuration.TestMethodName))
            {
                arguments.Append($"--method {configuration.TestMethodName} ");
            }

            arguments.Append($"--iterations {configuration.SchedulingIterations} ");
            arguments.Append($"--timeout {configuration.Timeout} ");

            if (configuration.UserExplicitlySetMaxFairSchedulingSteps)
            {
                arguments.Append($"--max-steps {configuration.MaxUnfairSchedulingSteps} " +
                    $"{configuration.MaxFairSchedulingSteps} ");
            }
            else
            {
                arguments.Append($"--max-steps {configuration.MaxUnfairSchedulingSteps} ");
            }

            if (configuration.SchedulingStrategy == SchedulingStrategy.PCT ||
                configuration.SchedulingStrategy == SchedulingStrategy.FairPCT)
            {
                arguments.Append($"--sch-{configuration.SchedulingStrategy} ".ToLower() +
                    $"{configuration.PrioritySwitchBound} ");
            }
            else if (configuration.SchedulingStrategy == SchedulingStrategy.ProbabilisticRandom)
            {
                arguments.Append($"--sch-probabilistic {configuration.CoinFlipBound} ");
            }
            else if (configuration.SchedulingStrategy == SchedulingStrategy.Random ||
                configuration.SchedulingStrategy == SchedulingStrategy.Portfolio)
            {
                arguments.Append($"--sch-{configuration.SchedulingStrategy} ".ToLower());
            }

            if (configuration.RandomValueGeneratorSeed.HasValue)
            {
                arguments.Append($"--seed {configuration.RandomValueGeneratorSeed.Value} ");
            }

            if (configuration.PerformFullExploration)
            {
                arguments.Append("--explore ");
            }

            arguments.Append($"--timeout-delay {configuration.TimeoutDelay} ");

            if (configuration.ReportCodeCoverage && configuration.ReportActivityCoverage)
            {
                arguments.Append("--coverage ");
            }
            else if (configuration.ReportCodeCoverage)
            {
                arguments.Append("--coverage code ");
            }
            else if (configuration.ReportActivityCoverage)
            {
                arguments.Append("--coverage activity ");
            }

            if (configuration.IsDgmlGraphEnabled)
            {
                arguments.Append("--graph ");
            }

            if (configuration.IsXmlLogEnabled)
            {
                arguments.Append("--xml-trace ");
            }

            if (!string.IsNullOrEmpty(configuration.CustomActorRuntimeLogType))
            {
                arguments.Append($"--actor-runtime-log {configuration.CustomActorRuntimeLogType} ");
            }

            if (configuration.OutputFilePath.Length > 0)
            {
                arguments.Append($"--outdir {configuration.OutputFilePath} ");
            }

            arguments.Append("--run-as-parallel-testing-task ");
            arguments.Append($"--testing-scheduler-endpoint {configuration.TestingSchedulerEndPoint} ");
            arguments.Append($"--testing-scheduler-ipaddress {configuration.TestingSchedulerIpAddress} ");
            arguments.Append($"--testing-process-id {id} ");

            if (configuration.ParallelDebug)
            {
                arguments.Append($"--parallel-debug ");
            }

            return arguments.ToString();
        }
    }
}
