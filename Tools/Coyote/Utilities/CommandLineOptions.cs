// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net;
using Microsoft.Coyote.IO;
using Microsoft.Coyote.Runtime.Exploration;
using Microsoft.Coyote.TestingServices.Coverage;

namespace Microsoft.Coyote.Utilities
{
    public sealed class CommandLineOptions : BaseCommandLineOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CommandLineOptions"/> class.
        /// </summary>
        public CommandLineOptions()
            : base("Coyote", "The Coyote tool enables you to systematically test a specified Coyote test, generate " +
                  "a reproducible bug-trace if a bug is found, and replay a bug-trace using the VS debugger.")
        {
            var basicOptions = this.Parser.GetOrCreateGroup("Basic", "Basic options");
            var commandArg = basicOptions.AddPositionalArgument("command", "The operation perform (test, replay)");
            commandArg.AllowedValues = new List<string>(new string[] { "test", "replay" });
            basicOptions.AddPositionalArgument("path", "Path to the Coyote program to test");
            basicOptions.AddArgument("method", "m", "Suffix of the test method to execute");

            this.AddCommonOptions();

            var testingGroup = this.Parser.GetOrCreateGroup("testingGroup", "Systematic testing options");
            testingGroup.DependsOn = new CommandLineArgumentDependency() { Name = "command", Value = "test" };
            testingGroup.AddArgument("iterations", "i", "Number of schedules to explore for bugs", typeof(uint));
            testingGroup.AddArgument("max-steps", "ms", @"Max scheduling steps to be explored (disabled by default).
You can provide one or two unsigned integer values", typeof(uint)).IsMultiValue = true;
            testingGroup.AddArgument("parallel", "p", "Number of parallel testing processes (the default '0' runs the test in-process)", typeof(uint));
            testingGroup.AddArgument("sch-random", null, "Choose the random scheduling strategy (this is the default)", typeof(bool));
            testingGroup.AddArgument("sch-pct", null, "Choose the PCT scheduling strategy with given maximum number of priority switch points", typeof(uint));
            testingGroup.AddArgument("sch-fairpct", null, "Choose the fair PCT scheduling strategy with given maximum number of priority switch points", typeof(uint));
            testingGroup.AddArgument("sch-portfolio", null, "Choose the portfolio scheduling strategy", typeof(bool));

            var replayOptions = this.Parser.GetOrCreateGroup("replayOptions", "Replay and debug options");
            replayOptions.DependsOn = new CommandLineArgumentDependency() { Name = "command", Value = "replay" };
            replayOptions.AddPositionalArgument("schedule", "Schedule file to replay");
            replayOptions.AddArgument("break", "b", "Attach debugger and break at bug", typeof(bool));

            var coverageGroup = this.Parser.GetOrCreateGroup("coverageGroup", "Code and activity coverage options");
            var coverageArg = coverageGroup.AddArgument("coverage", "c", @"Generate code coverage statistics (via VS instrumentation) with zero or more values equal to:
 code: Generate code coverage statistics (via VS instrumentation)
 activity: Generate activity (machine, event, etc.) coverage statistics
 activity-debug: Print activity coverage statistics with debug info", typeof(string));
            coverageArg.AllowedValues = new List<string>(new string[] { string.Empty, "code", "activity", "activity-debug" });
            coverageArg.IsMultiValue = true;
            coverageGroup.AddArgument("instrument", "instr", "Additional file spec(s) to instrument for code coverage (wildcards supported)", typeof(string));
            coverageGroup.AddArgument("instrument-list", "instr-list", "File containing the paths to additional file(s) to instrument for code coverage, one per line, wildcards supported, lines starting with '//' are skipped", typeof(string));

            var advancedGroup = this.Parser.GetOrCreateGroup("advancedGroup", "Advanced options");
            advancedGroup.AddArgument("explore", null, "Keep testing until the bound (e.g. iteration or time) is reached", typeof(bool));
            advancedGroup.AddArgument("sch-seed", null, "Specify the random seed for the tester", typeof(int));
            advancedGroup.AddArgument("wait-for-testing-processes", null, "Wait for testing processes to start (default is to launch them)", typeof(bool));
            advancedGroup.AddArgument("testing-scheduler-ipaddress", null, "Specify server ip address and optional port (default: 127.0.0.1:0))", typeof(string));
            advancedGroup.AddArgument("testing-scheduler-endpoint", null, "Specify a name for the server (default: CoyoteTestScheduler)", typeof(string));
            advancedGroup.AddArgument("graph", null, "Output a DGML graph of the iteration that found a bug", typeof(bool));
            advancedGroup.AddArgument("actor-runtime-log", null, "Custom runtime log to use instead of the default", typeof(string));

            // Hidden options (for debugging or experimentation only).
            var hiddenGroup = this.Parser.GetOrCreateGroup("hiddenGroup", "Hidden Options");
            hiddenGroup.IsHidden = true;
            hiddenGroup.AddArgument("timeout-delay", null, "Specifies the default delay on timers created using CreateMachineTimer", typeof(uint));
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
                case "command":
                    this.Configuration.ToolCommand = (string)option.Value;
                    break;
                case "path":
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
                case "schedule":
                    {
                        string filename = (string)option.Value;
                        string extension = System.IO.Path.GetExtension(filename);
                        if (!extension.Equals(".schedule"))
                        {
                            Error.ReportAndExit("Please give a valid schedule file " +
                                "'--replay x', where 'x' has extension '.schedule'.");
                        }

                        this.Configuration.ScheduleFile = filename;
                    }

                    break;
                case "break":
                    this.Configuration.AttachDebugger = true;
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

                        if (!IPAddress.TryParse(ipAddress, out _))
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
                case "graph":
                    this.Configuration.IsDgmlGraphEnabled = true;
                    break;
                case "actor-runtime-log":
                    this.Configuration.CustomActorRuntimeLogType = (string)option.Value;
                    break;
                case "explore":
                    this.Configuration.PerformFullExploration = true;
                    break;
                case "coverage":
                    if (option.Value == null)
                    {
                        this.Configuration.ReportCodeCoverage = true;
                        this.Configuration.ReportActivityCoverage = true;
                    }
                    else
                    {
                        foreach (var item in (string[])option.Value)
                        {
                            switch (item)
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
                        uint[] values = (uint[])option.Value;
                        if (values.Length > 2)
                        {
                            Error.ReportAndExit("Invalid number of options supplied via '--max-steps'.");
                        }

                        uint i = values[0];
                        uint j;
                        if (values.Length == 2)
                        {
                            j = values[1];
                            this.Configuration.UserExplicitlySetMaxFairSchedulingSteps = true;
                        }
                        else
                        {
                            j = 10 * i;
                        }

                        this.Configuration.MaxUnfairSchedulingSteps = (int)i;
                        this.Configuration.MaxFairSchedulingSteps = (int)j;
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
            if (string.IsNullOrEmpty(this.Configuration.AssemblyToBeAnalyzed) &&
                string.Compare(this.Configuration.ToolCommand, "test", StringComparison.OrdinalIgnoreCase) == 0)
            {
                Error.ReportAndExit("Please give a valid path to a Coyote program's dll using 'test x'.");
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
                Error.ReportAndExit("Please provide a scheduling strategy (see --sch* options)");
            }

            if (this.Configuration.MaxFairSchedulingSteps < this.Configuration.MaxUnfairSchedulingSteps)
            {
                Error.ReportAndExit("For the option '-max-steps N[,M]', please make sure that M >= N.");
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
                    "'--max-steps bound', where bound > 0.");
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
