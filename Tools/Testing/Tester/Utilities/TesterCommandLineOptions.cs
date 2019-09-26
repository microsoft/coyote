// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Net;
using Microsoft.Coyote.IO;
using Microsoft.Coyote.Tooling.Utilities;

namespace Microsoft.Coyote.Utilities
{
    public sealed class TesterCommandLineOptions : BaseCommandLineOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TesterCommandLineOptions"/> class.
        /// </summary>
        public TesterCommandLineOptions()
            : base("CoyoteTester", "Tests a given Coyote program and generates a reproducible bug trace if it finds a bug.")
        {
            var basicOptions = this.Parser.GetOrCreateGroup("Basic", "Basic options");
            basicOptions.AddArgument("test", "t", "Path to the Coyote program to test", required: true);
            basicOptions.AddArgument("method", "m", "Suffix of the test method to execute");

            this.AddCommonOptions();

            var testingGroup = this.Parser.GetOrCreateGroup("group1", "Systematic testing options");
            testingGroup.AddArgument("iterations", "i", "Number of schedules to explore for bugs", typeof(uint));

            testingGroup.AddArgument("sch-random", "sr", "Choose the random scheduling strategy (this is the default)", typeof(bool));
            testingGroup.AddArgument("sch-pct", null, "Choose the PCT scheduling strategy with given maximum number of priority switch points", typeof(uint));
            testingGroup.AddArgument("sch-fairpct", null, "Choose the fair PCT scheduling strategy with given maximum number of priority switch points", typeof(uint));
            testingGroup.AddArgument("sch-portfolio", null, "Choose the portfolio scheduling strategy", typeof(bool));

            testingGroup.AddArgument("sch-seed", null, "Specify the random seed for the scheduler", typeof(int));

            testingGroup.AddArgument("max-steps", "ms", "Max scheduling steps to be explored (disabled by default)", typeof(string));
            testingGroup.AddArgument("replay", "r", "Tries to replay the schedule, and then switches to the specified strategy", typeof(string));

            var parallelGroup = this.Parser.GetOrCreateGroup("group2", "Parallel testing options");
            parallelGroup.AddArgument("parallel", "p", "Run a test server with a number of parallel testing child processes ('0' by default runs the test in-proc)", typeof(uint));
            parallelGroup.AddArgument("wait-for-testing-processes", null, "Wait for testing processes to start (default is to launch them)", typeof(bool));
            parallelGroup.AddArgument("testing-scheduler-ipaddress", null, "Specify server ip address and optional port (default: 127.0.0.1:0))", typeof(string));
            parallelGroup.AddArgument("testing-scheduler-endpoint", null, "Specify a name for the server (default: CoyoteTestScheduler)", typeof(string));

            var coverageGroup = this.Parser.GetOrCreateGroup("group3", "Testing code coverage options");
            coverageGroup.AddArgument("coverage", "c", @"Generate code coverage statistics (via VS instrumentation) where x is:
code: Generate code coverage statistics (via VS instrumentation)
activity: Generate activity (machine, event, etc.) coverage statistics
activity-debug: Print activity coverage statistics with debug info", typeof(string)).AllowedValues = new List<string>(new string[] { string.Empty, "code", "activity", "activity-debug" });
            coverageGroup.AddArgument("instrument", "instr", "Additional file spec(s) to instrument for code coverage (wildcards supported)", typeof(string));
            coverageGroup.AddArgument("instrument-list", "instr-list", "File containing the names of additional file(s) to instrument for code coverage, one per line, wildcards supported, lines starting with '//' are skipped", typeof(string));

            // hidden options (for debugging only).
            var hiddenGroup = this.Parser.GetOrCreateGroup("group4", "Hidden Options");
            hiddenGroup.IsHidden = true;
            hiddenGroup.AddArgument("timeout-delay", null, "Specifies the default delay on timers created using CreateMachineTimer", typeof(uint));
            hiddenGroup.AddArgument("explore", "e", "Keep testing until we find a bug or reach max-steps", typeof(bool));
            hiddenGroup.AddArgument("interactive", null, "Test using the interactive test strategy", typeof(bool));
            hiddenGroup.AddArgument("runtime", null, "The path to the testing runtime to use");
            hiddenGroup.AddArgument("run-as-parallel-testing-task", null, null, typeof(bool));
            hiddenGroup.AddArgument("testing-process-id", null, "The id of the controlling TestingProcessScheduler", typeof(uint));
            hiddenGroup.AddArgument("depth-bound-bug", null, "Consider depth bound hit as a bug", typeof(bool));
            hiddenGroup.AddArgument("prefix", null, "Safety prefix bound", typeof(int));
            hiddenGroup.AddArgument("liveness-temperature-threshold", null, "Liveness temperature threshold", typeof(int));
            hiddenGroup.AddArgument("cycle-detection", null, "Enable cycle detection", typeof(bool));
            hiddenGroup.AddArgument("custom-state-hashing", null, "Enable custom state hashing", typeof(bool));
            hiddenGroup.AddArgument("sch-probabilistic", "sp", "Choose the probabilistic scheduling strategy with given number of coin flips on each for each new schedule.", typeof(uint));
            hiddenGroup.AddArgument("sch-dfs", null, "Choose the DFS scheduling strategy", typeof(bool));
            hiddenGroup.AddArgument("sch-iddfs", null, "Choose the IDDFS scheduling strategy", typeof(bool));
            hiddenGroup.AddArgument("sch-db", null, "Choose the delay bound scheduling strategy with given maximum number of delays", typeof(uint));
            hiddenGroup.AddArgument("sch-rdb", null, "Choose the random delay bound scheduling strategy with given maximum number of delays", typeof(uint));
            hiddenGroup.AddArgument("parallel-debug", "pd", "Used with --parallel to put up a debugger prompt on each child process", typeof(bool));
        }

        /// <summary>
        /// Handle the parsed command line options.
        /// </summary>
        protected override void HandledParsedArgument(CommandLineArgument option)
        {
            switch (option.LongName)
            {
                case "test":
                    this.Configuration.AssemblyToBeAnalyzed = (string)option.Value;
                    break;
                case "runtime":
                    this.Configuration.TestingRuntimeAssembly = (string)option.Value;
                    break;
                case "method":
                    this.Configuration.TestMethodName = (string)option.Value;
                    break;
                case "interactive":
                    this.Configuration.SchedulingStrategy = SchedulingStrategy.Interactive;
                    break;
                case "sch-random":
                    this.Configuration.SchedulingStrategy = SchedulingStrategy.Random;
                    break;
                case "sch-portfolio":
                    this.Configuration.SchedulingStrategy = SchedulingStrategy.Portfolio;
                    break;
                case "sch-probabilistic":
                    this.Configuration.SchedulingStrategy = SchedulingStrategy.ProbabilisticRandom;
                    this.Configuration.CoinFlipBound = (int)(uint)option.Value;
                    break;
                case "sch-pct":
                    this.Configuration.SchedulingStrategy = SchedulingStrategy.PCT;
                    this.Configuration.PrioritySwitchBound = (int)(uint)option.Value;
                    break;
                case "sch-fairpct":
                    this.Configuration.SchedulingStrategy = SchedulingStrategy.FairPCT;
                    this.Configuration.PrioritySwitchBound = (int)(uint)option.Value;
                    break;
                case "sch-dfs":
                    this.Configuration.SchedulingStrategy = SchedulingStrategy.DFS;
                    break;
                case "sch-iddfs":
                    this.Configuration.SchedulingStrategy = SchedulingStrategy.IDDFS;
                    break;
                case "sch-db":
                    this.Configuration.SchedulingStrategy = SchedulingStrategy.DelayBounding;
                    this.Configuration.DelayBound = (int)(uint)option.Value;
                    break;
                case "sch-rdb":
                    this.Configuration.SchedulingStrategy = SchedulingStrategy.RandomDelayBounding;
                    this.Configuration.DelayBound = (int)(uint)option.Value;
                    break;
                case "replay":
                    {
                        string filename = (string)option.Value;
                        string extension = System.IO.Path.GetExtension(filename);
                        if (!extension.Equals(".schedule"))
                        {
                            Error.ReportAndExit("Please give a valid schedule file " +
                                "'-replay:[x]', where [x] has extension '.schedule'.");
                        }

                        this.Configuration.ScheduleFile = filename;
                    }

                    break;
                case "iterations":
                    this.Configuration.SchedulingIterations = (int)(uint)option.Value;
                    break;
                case "parallel":
                    this.Configuration.ParallelBugFindingTasks = (uint)option.Value;
                    break;
                case "parallel-debug":
                    this.Configuration.ParallelDebug = true;
                    break;
                case "wait-for-testing-processes":
                    this.Configuration.WaitForTestingProcesses = true;
                    break;
                case "testing-scheduler-ipaddress":
                    {
                        var ipAddress = (string)option.Value;
                        int port = 0;
                        if (ipAddress.Contains(":"))
                        {
                            string[] parts = ipAddress.Split(':');
                            if (parts.Length != 2 || !int.TryParse(parts[1], out port))
                            {
                                Error.ReportAndExit("Please give a valid port number for --testing-scheduler-ipaddress option");
                            }

                            ipAddress = parts[0];
                        }

                        if (!IPAddress.TryParse(ipAddress, out IPAddress addr))
                        {
                            Error.ReportAndExit("Please give a valid ip address for --testing-scheduler-ipaddress option");
                        }

                        this.Configuration.TestingSchedulerIpAddress = ipAddress + ":" + port;
                    }

                    break;
                case "run-as-parallel-testing-task":
                    this.Configuration.RunAsParallelBugFindingTask = true;
                    break;
                case "testing-scheduler-endpoint":
                    this.Configuration.TestingSchedulerEndPoint = (string)option.Value;
                    break;
                case "testing-process-id":
                    this.Configuration.TestingProcessId = (uint)option.Value;
                    break;
                case "explore":
                    this.Configuration.PerformFullExploration = true;
                    break;
                case "coverage":
                    if (string.IsNullOrEmpty((string)option.Value))
                    {
                        this.Configuration.ReportCodeCoverage = true;
                        this.Configuration.ReportActivityCoverage = true;
                    }
                    else
                    {
                        switch ((string)option.Value)
                        {
                            case "code":
                                this.Configuration.ReportCodeCoverage = true;
                                break;
                            case "activity":
                                this.Configuration.ReportActivityCoverage = true;
                                break;
                            case "activity-debug":
                                this.Configuration.ReportActivityCoverage = true;
                                this.Configuration.DebugActivityCoverage = true;
                                break;
                            default:
                                break;
                        }
                    }

                    break;
                case "instrument":
                case "instrument-list":
                    this.Configuration.AdditionalCodeCoverageAssemblies[(string)option.Value] = false;
                    break;
                case "timeout-delay":
                    this.Configuration.TimeoutDelay = (uint)option.Value;
                    break;
                case "sch-seed":
                    this.Configuration.RandomSchedulingSeed = (int)option.Value;
                    break;
                case "max-steps":
                    {
                        int i = 0;
                        string value = (string)option.Value;
                        var tokens = value.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                        if (tokens.Length > 2 || tokens.Length <= 0)
                        {
                            Error.ReportAndExit("Invalid number of options supplied via '--max-steps'.");
                        }

                        if (tokens.Length >= 1)
                        {
                            if (!int.TryParse(tokens[0], out i) && i >= 0)
                            {
                                Error.ReportAndExit("Please give a valid number of max scheduling " +
                                    " steps to explore '--max-steps:[x]', where [x] >= 0.");
                            }
                        }

                        int j;
                        if (tokens.Length == 2)
                        {
                            if (!int.TryParse(tokens[1], out j) && j >= 0)
                            {
                                Error.ReportAndExit("Please give a valid number of max scheduling " +
                                    " steps to explore '--max-steps:[x]:[y]', where [y] >= 0.");
                            }

                            this.Configuration.UserExplicitlySetMaxFairSchedulingSteps = true;
                        }
                        else
                        {
                            j = 10 * i;
                        }

                        this.Configuration.MaxUnfairSchedulingSteps = i;
                        this.Configuration.MaxFairSchedulingSteps = j;
                    }

                    break;
                case "depth-bound-bug":
                    this.Configuration.ConsiderDepthBoundHitAsBug = true;
                    break;
                case "prefix":
                    this.Configuration.SafetyPrefixBound = (int)option.Value;
                    break;
                case "liveness-temperature-threshold":
                    this.Configuration.LivenessTemperatureThreshold = (int)option.Value;
                    break;
                case "cycle-detection":
                    this.Configuration.EnableCycleDetection = true;
                    break;
                case "custom-state-hashing":
                    this.Configuration.EnableUserDefinedStateHashing = true;
                    break;
                default:
                    base.HandledParsedArgument(option);
                    break;
            }
        }

        /// <summary>
        /// Updates the configuration depending on the user specified options.
        /// </summary>
        protected override void UpdateConfiguration()
        {
            if (string.IsNullOrEmpty(this.Configuration.AssemblyToBeAnalyzed))
            {
                Error.ReportAndExit("Please give a valid path to a Coyote program's dll using '-test:[x]'.");
            }

            if (this.Configuration.SchedulingStrategy != SchedulingStrategy.Interactive &&
                this.Configuration.SchedulingStrategy != SchedulingStrategy.Portfolio &&
                this.Configuration.SchedulingStrategy != SchedulingStrategy.Random &&
                this.Configuration.SchedulingStrategy != SchedulingStrategy.ProbabilisticRandom &&
                this.Configuration.SchedulingStrategy != SchedulingStrategy.PCT &&
                this.Configuration.SchedulingStrategy != SchedulingStrategy.FairPCT &&
                this.Configuration.SchedulingStrategy != SchedulingStrategy.DFS &&
                this.Configuration.SchedulingStrategy != SchedulingStrategy.IDDFS &&
                this.Configuration.SchedulingStrategy != SchedulingStrategy.DelayBounding &&
                this.Configuration.SchedulingStrategy != SchedulingStrategy.RandomDelayBounding)
            {
                Error.ReportAndExit("Please give a valid scheduling strategy " +
                        "'-sch:[x]', where [x] is 'random' or 'pct' (other experimental " +
                        "strategies also exist, but are not listed here).");
            }

            if (this.Configuration.MaxFairSchedulingSteps < this.Configuration.MaxUnfairSchedulingSteps)
            {
                Error.ReportAndExit("For the option '-max-steps:[N]:[M]', please make sure that [M] >= [N].");
            }

            if (this.Configuration.SafetyPrefixBound > 0 &&
                this.Configuration.SafetyPrefixBound >= this.Configuration.MaxUnfairSchedulingSteps)
            {
                Error.ReportAndExit("Please give a safety prefix bound that is less than the " +
                    "max scheduling steps bound.");
            }

            if (this.Configuration.SchedulingStrategy.Equals("iddfs") &&
                this.Configuration.MaxUnfairSchedulingSteps == 0)
            {
                Error.ReportAndExit("The Iterative Deepening DFS scheduler ('iddfs') " +
                    "must have a max scheduling steps bound, which can be given using " +
                    "'-max-steps:bound', where bound > 0.");
            }

#if NETCOREAPP2_1
            if (this.Configuration.ReportCodeCoverage || this.Configuration.ReportActivityCoverage)
            {
                Error.ReportAndExit("We do not yet support coverage reports when using the .NET Core runtime.");
            }
#endif
            if (this.Configuration.LivenessTemperatureThreshold == 0)
            {
                if (this.Configuration.EnableCycleDetection)
                {
                    this.Configuration.LivenessTemperatureThreshold = 100;
                }
                else if (this.Configuration.MaxFairSchedulingSteps > 0)
                {
                    this.Configuration.LivenessTemperatureThreshold =
                        this.Configuration.MaxFairSchedulingSteps / 2;
                }
            }

            if (this.Configuration.RandomSchedulingSeed is null)
            {
                this.Configuration.RandomSchedulingSeed = DateTime.Now.Millisecond;
            }
        }
    }
}
