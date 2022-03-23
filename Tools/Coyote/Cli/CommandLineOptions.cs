// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Coyote.IO;
using Microsoft.Coyote.Rewriting;

namespace Microsoft.Coyote.Cli
{
    internal sealed class CommandLineOptions
    {
        /// <summary>
        /// The command line parser to use.
        /// </summary>
        private readonly CommandLineArgumentParser Parser;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandLineOptions"/> class.
        /// </summary>
        internal CommandLineOptions()
        {
            this.Parser = new CommandLineArgumentParser("Coyote",
                "The Coyote tool enables you to systematically test a specified Coyote test, generate " +
                "a reproducible bug-trace if a bug is found, and replay a bug-trace using the VS debugger.");

            var basicGroup = this.Parser.GetOrCreateGroup("Basic", "Basic options", true);
            var commandArg = basicGroup.AddPositionalArgument("command", "The operation perform (test, replay, rewrite)");
            commandArg.AllowedValues = new List<string>(new string[] { "test", "replay", "rewrite", "telemetry" });
            basicGroup.AddPositionalArgument("path", "Path to the Coyote program to test");
            basicGroup.AddArgument("method", "m", "Suffix of the test method to execute");
            basicGroup.AddArgument("outdir", "o", "Dump output to directory x (absolute path or relative to current directory");
            var verbosityArg = basicGroup.AddArgument("verbosity", "v", "Enable verbose log output during testing providing the level of logging you want to see: quiet, minimal, normal, detailed.  Using -v with no argument defaults to 'detailed'", typeof(string), defaultValue: "detailed");
            verbosityArg.AllowedValues = new List<string>(new string[] { "quiet", "minimal", "normal", "detailed" });
            basicGroup.AddArgument("debug", "d", "Enable debugging", typeof(bool)).IsHidden = true;
            basicGroup.AddArgument("break", "b", "Attaches the debugger and also adds a breakpoint when an assertion fails (disabled during parallel testing)", typeof(bool));
            basicGroup.AddArgument("version", null, "Show tool version", typeof(bool));

            var testingGroup = this.Parser.GetOrCreateGroup("testingGroup", "Testing options");
            testingGroup.DependsOn = new CommandLineArgumentDependency() { Name = "command", Value = "test" };
            testingGroup.AddArgument("iterations", "i", "Number of schedules to explore for bugs", typeof(uint));
            testingGroup.AddArgument("timeout", "t", "Timeout in seconds after which no more testing iterations will run (disabled by default)", typeof(uint));
            testingGroup.AddArgument("max-steps", "ms", @"Max scheduling steps to be explored during testing (by default 10,000 unfair and 100,000 fair steps).
You can provide one or two unsigned integer values", typeof(uint)).IsMultiValue = true;
            testingGroup.AddArgument("fail-on-maxsteps", null, "Consider it a bug if the test hits the specified max-steps", typeof(bool));
            testingGroup.AddArgument("liveness-temperature-threshold", null, "Specify the liveness temperature threshold is the liveness temperature value that triggers a liveness bug", typeof(uint));
            testingGroup.AddArgument("systematic-fuzzing", null, "Use systematic fuzzing instead of controlled testing", typeof(bool));
            testingGroup.AddArgument("sch-random", null, "Choose the random scheduling strategy (this is the default)", typeof(bool));
            testingGroup.AddArgument("sch-probabilistic", "sp", "Choose the probabilistic scheduling strategy with given probability for each scheduling decision where the probability is " +
                "specified as the integer N in the equation 0.5 to the power of N.  So for N=1, the probability is 0.5, for N=2 the probability is 0.25, N=3 you get 0.125, etc.", typeof(uint));
            testingGroup.AddArgument("sch-prioritization", null, "Choose the priority-based scheduling strategy with given maximum number of priority change points", typeof(uint));
            testingGroup.AddArgument("sch-fair-prioritization", null, "Choose the fair priority-based scheduling strategy with given maximum number of priority change points", typeof(uint));
            testingGroup.AddArgument("sch-portfolio", null, "Choose the portfolio scheduling strategy", typeof(bool));
            testingGroup.AddArgument("no-repro", null, "Disable bug trace repro to ignore uncontrolled concurrency errors", typeof(bool));

            var replayOptions = this.Parser.GetOrCreateGroup("replayOptions", "Replay options");
            replayOptions.DependsOn = new CommandLineArgumentDependency() { Name = "command", Value = "replay" };
            replayOptions.AddPositionalArgument("schedule", "Schedule file to replay");

            var rewritingGroup = this.Parser.GetOrCreateGroup("rewritingGroup", "Binary rewriting options");
            rewritingGroup.DependsOn = new CommandLineArgumentDependency() { Name = "command", Value = "rewrite" };
            rewritingGroup.AddArgument("assert-data-races", null, "Add assertions for read/write data races", typeof(bool));
            rewritingGroup.AddArgument("rewrite-dependencies", null, "Rewrite all dependent assemblies that are found in the same location as the given path", typeof(bool));
            rewritingGroup.AddArgument("rewrite-unit-tests", null, "Rewrite unit tests to run in the scope of the Coyote testing engine", typeof(bool));
            rewritingGroup.AddArgument("rewrite-threads", null, "Rewrite low-level threading APIs (experimental)", typeof(bool));
            rewritingGroup.AddArgument("dump-il", null, "Dumps the original and rewritten IL in JSON", typeof(bool));
            rewritingGroup.AddArgument("dump-il-diff", null, "Dumps the IL diff in JSON", typeof(bool));

            var coverageGroup = this.Parser.GetOrCreateGroup("coverageGroup", "Code and activity coverage options");
            coverageGroup.DependsOn = new CommandLineArgumentDependency() { Name = "command", Value = "test" };
            var coverageArg = coverageGroup.AddArgument("coverage", "c", @"Generate code coverage statistics (via VS instrumentation) with zero or more values equal to:
 code: Generate code coverage statistics (via VS instrumentation)
 activity: Generate activity (state machine, event, etc.) coverage statistics", typeof(string));
            coverageArg.AllowedValues = new List<string>(new string[] { string.Empty, "code", "activity" });
            coverageArg.IsMultiValue = true;
            coverageGroup.AddArgument("instrument", "instr", "Additional file spec(s) to instrument for code coverage (wildcards supported)", typeof(string));
            coverageGroup.AddArgument("instrument-list", "instr-list", "File containing the paths to additional file(s) to instrument for code " +
                "coverage, one per line, wildcards supported, lines starting with '//' are skipped", typeof(string));

            var advancedGroup = this.Parser.GetOrCreateGroup("advancedGroup", "Advanced options");
            advancedGroup.DependsOn = new CommandLineArgumentDependency() { Name = "command", Value = "test" };
            advancedGroup.AddArgument("explore", null, "Keep testing until the bound (e.g. iteration or time) is reached", typeof(bool));
            advancedGroup.AddArgument("no-fuzzing-fallback", null, "Disable automatic fallback to systematic fuzzing upon detecting uncontrolled concurrency", typeof(bool));
            advancedGroup.AddArgument("no-partial-control", null, "Disallow partially controlled concurrency during controlled testing", typeof(bool));
            advancedGroup.AddArgument("timeout-delay", null, "Controls the frequency of timeouts by built-in timers (not a unit of time)", typeof(uint));
            advancedGroup.AddArgument("deadlock-timeout", null, "Controls how much time (in ms) to wait before reporting a potential deadlock", typeof(uint));
            advancedGroup.AddArgument("skip-potential-deadlocks", null, "Only report a deadlock when the runtime can fully determine that it is genuine and not due to partially-controlled concurrency", typeof(bool));
            advancedGroup.AddArgument("uncontrolled-concurrency-timeout", null, "Controls how much time (in ms) to try resolve uncontrolled concurrency during testing", typeof(uint));
            advancedGroup.AddArgument("uncontrolled-concurrency-interval", null, "Controls the interval (in ms) between attempts to resolve uncontrolled concurrency during testing", typeof(uint));
            advancedGroup.AddArgument("reduce-shared-state", null, "Enables shared state reduction during testing", typeof(bool));
            advancedGroup.AddArgument("seed", null, "Specify the random value generator seed", typeof(uint));
            advancedGroup.AddArgument("graph", null, "Output a DGML graph that visualizes the failing execution path if a bug is found", typeof(bool));
            advancedGroup.AddArgument("xml-trace", null, "Specify a filename for XML runtime log output to be written to", typeof(bool));
            advancedGroup.AddArgument("actor-runtime-log", null, "Specify an additional custom logger using fully qualified name: 'fullclass,assembly'", typeof(string));

            var experimentalGroup = this.Parser.GetOrCreateGroup("experimentalGroup", "Experimental options");
            experimentalGroup.DependsOn = new CommandLineArgumentDependency() { Name = "command", Value = "test" };
            experimentalGroup.AddArgument("sch-dfs", null, "Choose the depth-first search (DFS) scheduling strategy", typeof(bool));
            experimentalGroup.AddArgument("sch-rl", null, "Choose the reinforcement learning (RL) scheduling strategy", typeof(bool));

            // Hidden options (for debugging or experimentation only).
            var hiddenGroup = this.Parser.GetOrCreateGroup("hiddenGroup", "Hidden Options");
            hiddenGroup.IsHidden = true;
            hiddenGroup.AddArgument("additional-paths", null, null, typeof(string));
        }

        internal void PrintHelp(TextWriter w)
        {
            this.Parser.PrintHelp(w);
        }

        /// <summary>
        /// Parses the command line options and returns a configuration.
        /// </summary>
        /// <param name="args">The command line arguments to parse.</param>
        /// <param name="configuration">The configuration object populated with the parsed command line options.</param>
        /// <param name="rewritingOptions">The rewriting options.</param>
        internal bool Parse(string[] args, Configuration configuration, RewritingOptions rewritingOptions)
        {
            try
            {
                var result = this.Parser.ParseArguments(args);
                if (result != null)
                {
                    foreach (var arg in result)
                    {
                        UpdateConfigurationWithParsedArgument(configuration, rewritingOptions, arg);
                    }

                    SanitizeConfiguration(configuration);
                    return true;
                }
            }
            catch (CommandLineException ex)
            {
                if ((from arg in ex.Result where arg.LongName == "version" select arg).Any())
                {
                    WriteVersion();
                    Environment.Exit((int)ExitCode.Error);
                }
                else
                {
                    this.Parser.PrintHelp(Console.Out);
                    Error.ReportAndExit(ex.Message);
                }
            }
            catch (Exception ex)
            {
                this.Parser.PrintHelp(Console.Out);
                Error.ReportAndExit(ex.Message);
            }

            return false;
        }

        /// <summary>
        /// Updates the configuration with the specified parsed argument.
        /// </summary>
        private static void UpdateConfigurationWithParsedArgument(Configuration configuration,
            RewritingOptions rewritingOptions, CommandLineArgument option)
        {
            switch (option.LongName)
            {
                case "command":
                    configuration.ToolCommand = (string)option.Value;
                    break;
                case "outdir":
                    configuration.OutputFilePath = (string)option.Value;
                    break;
                case "debug":
                    configuration.IsDebugVerbosityEnabled = true;
                    Debug.IsEnabled = true;
                    break;
                case "verbosity":
                    configuration.IsVerbose = true;
                    string verbosity = (string)option.Value;
                    switch (verbosity)
                    {
                        case "quiet":
                            configuration.IsVerbose = false;
                            break;
                        case "detailed":
                            configuration.LogLevel = LogSeverity.Informational;
                            break;
                        case "normal":
                            configuration.LogLevel = LogSeverity.Warning;
                            break;
                        case "minimal":
                            configuration.LogLevel = LogSeverity.Error;
                            break;
                        default:
                            Error.ReportAndExit($"Please give a valid value for 'verbosity' must be one of 'errors', 'warnings', or 'info', but found {verbosity}.");
                            break;
                    }

                    break;
                case "path":
                    if (configuration.ToolCommand is "test" || configuration.ToolCommand is "replay")
                    {
                        // In the case of 'coyote test' or 'replay', the path is the assembly to be tested.
                        configuration.AssemblyToBeAnalyzed = (string)option.Value;
                    }
                    else if (configuration.ToolCommand is "rewrite")
                    {
                        // In the case of 'coyote rewrite', the path is the JSON configuration file
                        // with the binary rewriting options.
                        string filename = (string)option.Value;
                        if (Directory.Exists(filename))
                        {
                            // Then we want to rewrite a whole folder full of assemblies.
                            var assembliesDir = Path.GetFullPath(filename);
                            rewritingOptions.AssembliesDirectory = assembliesDir;
                            rewritingOptions.OutputDirectory = assembliesDir;
                        }
                        else
                        {
                            string extension = Path.GetExtension(filename);
                            if (string.Compare(extension, ".json", StringComparison.OrdinalIgnoreCase) is 0)
                            {
                                // Parse the rewriting options from the JSON file.
                                RewritingOptions.ParseFromJSON(rewritingOptions, filename);
                            }
                            else if (string.Compare(extension, ".dll", StringComparison.OrdinalIgnoreCase) is 0 ||
                                string.Compare(extension, ".exe", StringComparison.OrdinalIgnoreCase) is 0)
                            {
                                configuration.AssemblyToBeAnalyzed = filename;
                                var fullPath = Path.GetFullPath(filename);
                                var assembliesDir = Path.GetDirectoryName(fullPath);
                                rewritingOptions.AssembliesDirectory = assembliesDir;
                                rewritingOptions.OutputDirectory = assembliesDir;
                                rewritingOptions.AssemblyPaths.Add(fullPath);
                            }
                            else
                            {
                                Error.ReportAndExit("Please give a valid .dll or JSON configuration file for binary rewriting.");
                            }
                        }
                    }

                    break;
                case "method":
                    configuration.TestMethodName = (string)option.Value;
                    break;
                case "systematic-fuzzing":
                case "no-repro":
                    configuration.IsSystematicFuzzingEnabled = true;
                    break;
                case "no-partial-control":
                    configuration.IsPartiallyControlledConcurrencyAllowed = false;
                    break;
                case "no-fuzzing-fallback":
                    configuration.IsSystematicFuzzingFallbackEnabled = false;
                    break;
                case "reduce-shared-state":
                    configuration.IsSharedStateReductionEnabled = true;
                    break;
                case "explore":
                    configuration.RunTestIterationsToCompletion = true;
                    break;
                case "seed":
                    configuration.RandomGeneratorSeed = (uint)option.Value;
                    break;
                case "sch-random":
                case "sch-dfs":
                case "sch-portfolio":
                    // configuration.SchedulingStrategy = option.LongName.Substring(4);
                    break;
                case "sch-probabilistic":
                case "sch-prioritization":
                case "sch-fair-prioritization":
                    configuration.SchedulingStrategy = option.LongName.Substring(4);
                    configuration.StrategyBound = (int)(uint)option.Value;
                    break;
                case "sch-rl":
                    configuration.SchedulingStrategy = option.LongName.Substring(4);
                    configuration.IsProgramStateHashingEnabled = true;
                    break;
                case "schedule":
                    {
                        string filename = (string)option.Value;
                        string extension = Path.GetExtension(filename);
                        if (!extension.Equals(".schedule"))
                        {
                            Error.ReportAndExit("Please give a valid schedule file " +
                                "with extension '.schedule'.");
                        }

                        configuration.ScheduleFile = filename;
                    }

                    break;
                case "version":
                    WriteVersion();
                    Environment.Exit((int)ExitCode.Error);
                    break;
                case "break":
                    configuration.AttachDebugger = true;
                    break;
                case "iterations":
                    configuration.TestingIterations = (uint)option.Value;
                    break;
                case "timeout":
                    configuration.TestingTimeout = (int)(uint)option.Value;
                    break;
                case "additional-paths":
                    configuration.AdditionalPaths = (string)option.Value;
                    break;
                case "graph":
                    configuration.IsDgmlGraphEnabled = true;
                    break;
                case "xml-trace":
                    configuration.IsXmlLogEnabled = true;
                    break;
                case "actor-runtime-log":
                    configuration.CustomActorRuntimeLogType = (string)option.Value;
                    break;
                case "coverage":
                    if (option.Value is null)
                    {
                        configuration.ReportCodeCoverage = true;
                        configuration.IsActivityCoverageReported = true;
                    }
                    else
                    {
                        foreach (var item in (string[])option.Value)
                        {
                            switch (item)
                            {
                                case "code":
                                    configuration.ReportCodeCoverage = true;
                                    break;
                                case "activity":
                                    configuration.IsActivityCoverageReported = true;
                                    break;
                                default:
                                    break;
                            }
                        }
                    }

                    break;
                case "instrument":
                    configuration.AdditionalCodeCoverageAssemblies[(string)option.Value] = false;
                    break;
                case "instrument-list":
                    configuration.AdditionalCodeCoverageAssemblies[(string)option.Value] = true;
                    break;
                case "assert-data-races":
                    rewritingOptions.IsDataRaceCheckingEnabled = true;
                    break;
                case "rewrite-dependencies":
                    rewritingOptions.IsRewritingDependencies = true;
                    break;
                case "rewrite-unit-tests":
                    rewritingOptions.IsRewritingUnitTests = true;
                    break;
                case "rewrite-threads":
                    rewritingOptions.IsRewritingThreads = true;
                    break;
                case "dump-il":
                    rewritingOptions.IsLoggingAssemblyContents = true;
                    break;
                case "dump-il-diff":
                    rewritingOptions.IsDiffingAssemblyContents = true;
                    break;
                case "timeout-delay":
                    configuration.TimeoutDelay = (uint)option.Value;
                    break;
                case "deadlock-timeout":
                    configuration.DeadlockTimeout = (uint)option.Value;
                    break;
                case "skip-potential-deadlocks":
                    configuration.ReportPotentialDeadlocksAsBugs = false;
                    break;
                case "uncontrolled-concurrency-timeout":
                    configuration.UncontrolledConcurrencyResolutionTimeout = (uint)option.Value;
                    break;
                case "uncontrolled-concurrency-interval":
                    configuration.UncontrolledConcurrencyResolutionInterval = (uint)option.Value;
                    break;
                case "max-steps":
                    {
                        uint[] values = (uint[])option.Value;
                        if (values.Length > 2)
                        {
                            Error.ReportAndExit("Invalid number of options supplied via '--max-steps'.");
                        }

                        try
                        {
                            uint i = values[0];
                            uint j;
                            if (values.Length is 2)
                            {
                                j = values[1];
                                configuration.WithMaxSchedulingSteps(i, j);
                            }
                            else
                            {
                                configuration.WithMaxSchedulingSteps(i);
                            }
                        }
                        catch (ArgumentException)
                        {
                            Error.ReportAndExit("For the option '--max-steps N[,M]', please make sure that M >= N.");
                        }
                    }

                    break;
                case "fail-on-maxsteps":
                    configuration.ConsiderDepthBoundHitAsBug = true;
                    break;
                case "liveness-temperature-threshold":
                    configuration.LivenessTemperatureThreshold = (int)(uint)option.Value;
                    configuration.UserExplicitlySetLivenessTemperatureThreshold = true;
                    break;
                default:
                    throw new Exception(string.Format("Unhandled parsed argument: '{0}'", option.LongName));
            }
        }

        private static void WriteVersion()
        {
            Console.WriteLine("Version: {0}", typeof(CommandLineOptions).Assembly.GetName().Version);
        }

        /// <summary>
        /// Sanitizes the configuration.
        /// </summary>
        private static void SanitizeConfiguration(Configuration configuration)
        {
            if (string.IsNullOrEmpty(configuration.AssemblyToBeAnalyzed) &&
                string.Compare(configuration.ToolCommand, "test", StringComparison.OrdinalIgnoreCase) is 0)
            {
                Error.ReportAndExit("Please give a valid path to a Coyote program's dll using 'test x'.");
            }

            if (configuration.SchedulingStrategy != "portfolio" &&
                configuration.SchedulingStrategy != "random" &&
                configuration.SchedulingStrategy != "prioritization" &&
                configuration.SchedulingStrategy != "fair-prioritization" &&
                configuration.SchedulingStrategy != "probabilistic" &&
                configuration.SchedulingStrategy != "rl" &&
                configuration.SchedulingStrategy != "dfs")
            {
                Error.ReportAndExit("Please provide a scheduling strategy (see --sch* options)");
            }
        }
    }
}
