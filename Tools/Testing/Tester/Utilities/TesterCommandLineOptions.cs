// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Coyote.IO;

namespace Microsoft.Coyote.Utilities
{
    public sealed class TesterCommandLineOptions : BaseCommandLineOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TesterCommandLineOptions"/> class.
        /// </summary>
        public TesterCommandLineOptions(string[] args)
            : base(args)
        {
        }

        /// <summary>
        /// Parses the given option.
        /// </summary>
        protected override void ParseOption(string option)
        {
            if (IsMatch(option, @"^[\/|-]test:") && option.Length > 6)
            {
                this.Configuration.AssemblyToBeAnalyzed = option.Substring(6);
            }
            else if (IsMatch(option, @"^[\/|-]runtime:") && option.Length > 9)
            {
                this.Configuration.TestingRuntimeAssembly = option.Substring(9);
            }
            else if (IsMatch(option, @"^[\/|-]method:") && option.Length > 8)
            {
                this.Configuration.TestMethodName = option.Substring(8);
            }
            else if (IsMatch(option, @"^[\/|-]interactive$"))
            {
                this.Configuration.SchedulingStrategy = SchedulingStrategy.Interactive;
            }
            else if (IsMatch(option, @"^[\/|-]sch:"))
            {
                string scheduler = option.Substring(5);
                if (IsMatch(scheduler, @"^portfolio$"))
                {
                    this.Configuration.SchedulingStrategy = SchedulingStrategy.Portfolio;
                }
                else if (IsMatch(scheduler, @"^random$"))
                {
                    this.Configuration.SchedulingStrategy = SchedulingStrategy.Random;
                }
                else if (IsMatch(scheduler, @"^probabilistic"))
                {
                    int i = 0;
                    if (IsMatch(scheduler, @"^probabilistic$") ||
                        (!int.TryParse(scheduler.Substring(14), out i) && i >= 0))
                    {
                        Error.ReportAndExit("Please give a valid number of coin " +
                            "flip bound '-sch:probabilistic:[bound]', where [bound] >= 0.");
                    }

                    this.Configuration.SchedulingStrategy = SchedulingStrategy.ProbabilisticRandom;
                    this.Configuration.CoinFlipBound = i;
                }
                else if (IsMatch(scheduler, @"^pct"))
                {
                    int i = 0;
                    if (IsMatch(scheduler, @"^pct$") ||
                        (!int.TryParse(scheduler.Substring(4), out i) && i >= 0))
                    {
                        Error.ReportAndExit("Please give a valid number of priority " +
                            "switch bound '-sch:pct:[bound]', where [bound] >= 0.");
                    }

                    this.Configuration.SchedulingStrategy = SchedulingStrategy.PCT;
                    this.Configuration.PrioritySwitchBound = i;
                }
                else if (IsMatch(scheduler, @"^fairpct"))
                {
                    int i = 0;
                    if (IsMatch(scheduler, @"^fairpct$") ||
                        (!int.TryParse(scheduler.Substring("fairpct:".Length), out i) && i >= 0))
                    {
                        Error.ReportAndExit("Please give a valid number of priority " +
                            "switch bound '-sch:fairpct:[bound]', where [bound] >= 0.");
                    }

                    this.Configuration.SchedulingStrategy = SchedulingStrategy.FairPCT;
                    this.Configuration.PrioritySwitchBound = i;
                }
                else if (IsMatch(scheduler, @"^dfs$"))
                {
                    this.Configuration.SchedulingStrategy = SchedulingStrategy.DFS;
                }
                else if (IsMatch(scheduler, @"^iddfs$"))
                {
                    this.Configuration.SchedulingStrategy = SchedulingStrategy.IDDFS;
                }
                else if (IsMatch(scheduler, @"^db"))
                {
                    int i = 0;
                    if (IsMatch(scheduler, @"^db$") ||
                        (!int.TryParse(scheduler.Substring(3), out i) && i >= 0))
                    {
                        Error.ReportAndExit("Please give a valid delay " +
                            "bound '-sch:db:[bound]', where [bound] >= 0.");
                    }

                    this.Configuration.SchedulingStrategy = SchedulingStrategy.DelayBounding;
                    this.Configuration.DelayBound = i;
                }
                else if (IsMatch(scheduler, @"^rdb"))
                {
                    int i = 0;
                    if (IsMatch(scheduler, @"^rdb$") ||
                        (!int.TryParse(scheduler.Substring(4), out i) && i >= 0))
                    {
                        Error.ReportAndExit("Please give a valid delay " +
                            "bound '-sch:rdb:[bound]', where [bound] >= 0.");
                    }

                    this.Configuration.SchedulingStrategy = SchedulingStrategy.RandomDelayBounding;
                    this.Configuration.DelayBound = i;
                }
                else
                {
                    Error.ReportAndExit("Please give a valid scheduling strategy " +
                        "'-sch:[x]', where [x] is 'random', 'pct' or 'dfs' (other " +
                        "experimental strategies also exist, but are not listed here).");
                }
            }
            else if (IsMatch(option, @"^[\/|-]replay:") && option.Length > 8)
            {
                string extension = System.IO.Path.GetExtension(option.Substring(8));
                if (!extension.Equals(".schedule"))
                {
                    Error.ReportAndExit("Please give a valid schedule file " +
                        "'-replay:[x]', where [x] has extension '.schedule'.");
                }

                this.Configuration.ScheduleFile = option.Substring(8);
            }
            else if (IsMatch(option, @"^[\/|-]i:") && option.Length > 3)
            {
                if (!int.TryParse(option.Substring(3), out int i) && i > 0)
                {
                    Error.ReportAndExit("Please give a valid number of " +
                        "iterations '-i:[x]', where [x] > 0.");
                }

                this.Configuration.SchedulingIterations = i;
            }
            else if (IsMatch(option, @"^[\/|-]parallel:") && option.Length > 10)
            {
                if (!uint.TryParse(option.Substring(10), out uint i) || i <= 1)
                {
                    Error.ReportAndExit("Please give a valid number of " +
                        "parallel tasks '-parallel:[x]', where [x] > 1.");
                }

                this.Configuration.ParallelBugFindingTasks = i;
            }
            else if (IsMatch(option, @"^[\/|-]run-as-parallel-testing-task$"))
            {
                this.Configuration.RunAsParallelBugFindingTask = true;
            }
            else if (IsMatch(option, @"^[\/|-]testing-scheduler-endpoint:") && option.Length > 28)
            {
                string endpoint = option.Substring(28);
                if (endpoint.Length != 36)
                {
                    Error.ReportAndExit("Please give a valid testing scheduler endpoint " +
                        "'-testing-scheduler-endpoint:[x]', where [x] is a unique GUID.");
                }

                this.Configuration.TestingSchedulerEndPoint = endpoint;
            }
            else if (IsMatch(option, @"^[\/|-]testing-scheduler-process-id:") && option.Length > 30)
            {
                if (!int.TryParse(option.Substring(30), out int i) && i >= 0)
                {
                    Error.ReportAndExit("Please give a valid testing scheduler " +
                        "process id '-testing-scheduler-process-id:[x]', where [x] >= 0.");
                }

                this.Configuration.TestingSchedulerProcessId = i;
            }
            else if (IsMatch(option, @"^[\/|-]testing-process-id:") && option.Length > 20)
            {
                if (!uint.TryParse(option.Substring(20), out uint i) && i >= 0)
                {
                    Error.ReportAndExit("Please give a valid testing " +
                        "process id '-testing-process-id:[x]', where [x] >= 0.");
                }

                this.Configuration.TestingProcessId = i;
            }
            else if (IsMatch(option, @"^[\/|-]explore$"))
            {
                this.Configuration.PerformFullExploration = true;
            }
            else if (IsMatch(option, @"^[\/|-]coverage$"))
            {
                this.Configuration.ReportCodeCoverage = true;
                this.Configuration.ReportActivityCoverage = true;
            }
            else if (IsMatch(option, @"^[\/|-]coverage:code$"))
            {
                this.Configuration.ReportCodeCoverage = true;
            }
            else if (IsMatch(option, @"^[\/|-]coverage:activity$"))
            {
                this.Configuration.ReportActivityCoverage = true;
            }
            else if (IsMatch(option, @"^[\/|-]coverage:activity-debug$"))
            {
                this.Configuration.ReportActivityCoverage = true;
                this.Configuration.DebugActivityCoverage = true;
            }
            else if (IsMatch(option, @"^[\/|-]instr:"))
            {
                this.Configuration.AdditionalCodeCoverageAssemblies[option.Substring(7)] = false;
            }
            else if (IsMatch(option, @"^[\/|-]instr-list:"))
            {
                this.Configuration.AdditionalCodeCoverageAssemblies[option.Substring(12)] = true;
            }
            else if (IsMatch(option, @"^[\/|-]timeout-delay:") && option.Length > 15)
            {
                if (!uint.TryParse(option.Substring(15), out uint timeoutDelay) && timeoutDelay >= 0)
                {
                    Error.ReportAndExit("Please give a valid timeout delay '-timeout-delay:[x]', where [x] >= 0.");
                }

                this.Configuration.TimeoutDelay = timeoutDelay;
            }
            else if (IsMatch(option, @"^[\/|-]sch-seed:") && option.Length > 10)
            {
                if (!int.TryParse(option.Substring(10), out int seed))
                {
                    Error.ReportAndExit("Please give a valid random scheduling " +
                        "seed '-sch-seed:[x]', where [x] is a signed 32-bit integer.");
                }

                this.Configuration.RandomSchedulingSeed = seed;
            }
            else if (IsMatch(option, @"^[\/|-]max-steps:") && option.Length > 11)
            {
                int i = 0;
                var tokens = option.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length > 3 || tokens.Length <= 1)
                {
                    Error.ReportAndExit("Invalid number of options supplied via '-max-steps'.");
                }

                if (tokens.Length >= 2)
                {
                    if (!int.TryParse(tokens[1], out i) && i >= 0)
                    {
                        Error.ReportAndExit("Please give a valid number of max scheduling " +
                            " steps to explore '-max-steps:[x]', where [x] >= 0.");
                    }
                }

                int j;
                if (tokens.Length == 3)
                {
                    if (!int.TryParse(tokens[2], out j) && j >= 0)
                    {
                        Error.ReportAndExit("Please give a valid number of max scheduling " +
                            " steps to explore '-max-steps:[x]:[y]', where [y] >= 0.");
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
            else if (IsMatch(option, @"^[\/|-]depth-bound-bug$"))
            {
                this.Configuration.ConsiderDepthBoundHitAsBug = true;
            }
            else if (IsMatch(option, @"^[\/|-]prefix:") && option.Length > 8)
            {
                if (!int.TryParse(option.Substring(8), out int i) && i >= 0)
                {
                    Error.ReportAndExit("Please give a valid safety prefix " +
                        "bound '-prefix:[x]', where [x] >= 0.");
                }

                this.Configuration.SafetyPrefixBound = i;
            }
            else if (IsMatch(option, @"^[\/|-]liveness-temperature-threshold:") && option.Length > 32)
            {
                if (!int.TryParse(option.Substring(32), out int i) && i >= 0)
                {
                    Error.ReportAndExit("Please give a valid liveness temperature threshold " +
                        "'-liveness-temperature-threshold:[x]', where [x] >= 0.");
                }

                this.Configuration.LivenessTemperatureThreshold = i;
            }
            else if (IsMatch(option, @"^[\/|-]cycle-detection$"))
            {
                this.Configuration.EnableCycleDetection = true;
            }
            else if (IsMatch(option, @"^[\/|-]custom-state-hashing$"))
            {
                this.Configuration.EnableUserDefinedStateHashing = true;
            }
            else
            {
                base.ParseOption(option);
            }
        }

        /// <summary>
        /// Checks for parsing errors.
        /// </summary>
        protected override void CheckForParsingErrors()
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
                    "'-max-steps:[bound]', where [bound] > 0.");
            }

#if NETCOREAPP2_1
            if (this.Configuration.ParallelBugFindingTasks > 1)
            {
                Error.ReportAndExit("We do not yet support parallel testing when using the .NET Core runtime.");
            }

            if (this.Configuration.ReportCodeCoverage || this.Configuration.ReportActivityCoverage)
            {
                Error.ReportAndExit("We do not yet support coverage reports when using the .NET Core runtime.");
            }
#endif
        }

        /// <summary>
        /// Updates the configuration depending on the user specified options.
        /// </summary>
        protected override void UpdateConfiguration()
        {
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

        /// <summary>
        /// Shows help.
        /// </summary>
        protected override void ShowHelp()
        {
            string help = "\n";

            help += " --------------";
            help += "\n Basic options:";
            help += "\n --------------";
            help += "\n  -?\t\t Show this help menu";
            help += "\n  -test:[x]\t Path to the Coyote program to test";
            help += "\n  -method:[x]\t Suffix of the test method to execute";
            help += "\n  -timeout:[x]\t Timeout in seconds (disabled by default)";
            help += "\n  -v:[x]\t Enable verbose mode (values from '1' to '3')";
            help += "\n  -o:[x]\t Dump output to directory x (absolute path or relative to current directory)";

            help += "\n\n ---------------------------";
            help += "\n Systematic testing options:";
            help += "\n ---------------------------";
            help += "\n  -i:[x]\t\t Number of schedules to explore for bugs";
            help += "\n  -parallel:[x]\t\t Number of parallel testing tasks ('1' by default)";
            help += "\n  -sch:[x]\t\t Choose a systematic testing strategy ('random' by default)";
            help += "\n  -max-steps:[x]\t Max scheduling steps to be explored (disabled by default)";
            help += "\n  -replay:[x]\t Tries to replay the schedule, and then switches to the specified strategy";

            help += "\n\n ---------------------------";
            help += "\n Testing code coverage options:";
            help += "\n ---------------------------";
            help += "\n  -coverage:code\t Generate code coverage statistics (via VS instrumentation)";
            help += "\n  -coverage:activity\t Generate activity (machine, event, etc.) coverage statistics";
            help += "\n  -coverage\t Generate both code and activity coverage statistics";
            help += "\n  -coverage:activity-debug\t Print activity coverage statistics with debug info";
            help += "\n  -instr:[filespec]\t Additional file spec(s) to instrument for -coverage:code; wildcards supported";
            help += "\n  -instr-list:[listfilename]\t File containing the names of additional file(s), one per line,";
            help += "\n         wildcards supported, to instrument for -coverage:code; lines starting with '//' are skipped";

            help += "\n";

            Console.WriteLine(help);
        }
    }
}
