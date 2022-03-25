// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;
using Microsoft.Coyote.IO;
using Microsoft.Coyote.Rewriting;

#pragma warning disable CA1801
#pragma warning disable CA1822
#pragma warning disable SA1005
namespace Microsoft.Coyote.Cli
{
    internal sealed class CommandLineParser
    {
        /// <summary>
        /// Url with information about the testing process.
        /// </summary>
        private const string LearnAboutTestUrl = "https://aka.ms/coyote-test";

        /// <summary>
        /// Url with information about the replaying process.
        /// </summary>
        private const string LearnAboutReplayUrl = "https://aka.ms/coyote-replay";

        /// <summary>
        /// Url with information about the rewriting process.
        /// </summary>
        private const string LearnAboutRewritingUrl = "https://aka.ms/coyote-rewrite";

        /// <summary>
        /// The Coyote runtime and testing configuration.
        /// </summary>
        internal Configuration Configuration { get; private set; }

        /// <summary>
        /// The Coyote rewriting options.
        /// </summary>
        internal RewritingOptions RewritingOptions { get; private set; }

        /// <summary>
        /// The test command.
        /// </summary>
        private readonly Command TestCommand;

        /// <summary>
        /// The replay command.
        /// </summary>
        private readonly Command ReplayCommand;

        /// <summary>
        /// The rewrite command.
        /// </summary>
        private readonly Command RewriteCommand;

        /// <summary>
        /// The parse results.
        /// </summary>
        private readonly ParseResult Results;

        /// <summary>
        /// True if parsing was successful, else false.
        /// </summary>
        internal bool IsSuccessful { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandLineParser"/> class.
        /// </summary>
        internal CommandLineParser(string[] args)
        {
            this.Configuration = Configuration.Create();
            this.RewritingOptions = RewritingOptions.Create();

            var allowedVerbosityLevels = new HashSet<string>
            {
                "quiet",
                "minimal",
                "normal",
                "detailed"
            };

            var verbosityOption = new Option<string>(
                aliases: new[] { "-v", "--verbosity" },
                getDefaultValue: () => "quiet",
                description: "Enable verbosity with an optional verbosity level. " +
                    $"Allowed values are {string.Join(", ", allowedVerbosityLevels)}. " +
                    "Skipping the argument sets the verbosity level to 'detailed'.")
            {
                ArgumentHelpName = "LEVEL",
                Arity = ArgumentArity.ZeroOrOne
            };

            var debugOption = new Option<bool>(aliases: new[] { "-d", "--debug" })
            {
                Arity = ArgumentArity.Zero
            };

            // Add validators.
            verbosityOption.AddValidator(result => ValidateOptionValueIsAllowed(result, allowedVerbosityLevels));

            // Create the commands.
            this.TestCommand = CreateTestCommand(this.Configuration);
            this.ReplayCommand = CreateReplayCommand(this.Configuration);
            this.RewriteCommand = CreateRewriteCommand(this.Configuration);

            // Create the root command.
            var rootCommand = new RootCommand("The Coyote systematic testing tool.");
            rootCommand.AddGlobalOption(verbosityOption);
            rootCommand.AddGlobalOption(debugOption);
            rootCommand.AddCommand(this.TestCommand);
            rootCommand.AddCommand(this.ReplayCommand);
            rootCommand.AddCommand(this.RewriteCommand);
            rootCommand.TreatUnmatchedTokensAsErrors = true;

            var commandLineBuilder = new CommandLineBuilder(rootCommand);
            commandLineBuilder.UseDefaults();

            var parser = commandLineBuilder.Build();
            this.Results = parser.Parse(args);
            if (this.Results.Errors.Any())
            {
                // There are parsing errors, so invoke the result to print the errors and help message.
                this.Results.Invoke();
                this.IsSuccessful = false;
            }
            else
            {
                // There were no errors, so use the parsed results to update the default configurations.
                this.UpdateConfigurations(this.Results);
                this.IsSuccessful = true;
            }

            //var commandLineBuilder = new CommandLineBuilder(rootCommand);
            //commandLineBuilder.UseDefaults();
            //commandLineBuilder.AddMiddleware(async (context, next) =>
            //{
            //    Console.WriteLine($"Command: {context.ParseResult.CommandResult.Command.Name}");
            //    var commandResult = context.ParseResult.CommandResult;
            //    var command = commandResult.Command;
            //    foreach (var result in commandResult.Children)
            //    {
            //        Argument arg = commandResult.Command.Arguments.FirstOrDefault(a => a.Name == result.Symbol.Name);
            //        Option option = commandResult.Command.Options.FirstOrDefault(o => o.Name == result.Symbol.Name);
            //        if (arg != null)
            //        {
            //            Console.WriteLine(arg.Name);
            //            Console.WriteLine(commandResult.GetValueForArgument(arg));
            //        }
            //        else if (option != null)
            //        {
            //            Console.WriteLine(option.Name);
            //            Console.WriteLine(commandResult.GetValueForOption(option));
            //        }
            //    }

            //    await next(context);
            //});

            //return commandLineBuilder.Build();
        }

        /// <summary>
        /// Installs a handler to run when the 'test' command is specified.
        /// </summary>
        internal void InstallTestHandler(Action handler) =>
            this.TestCommand.SetHandler(handler);

        /// <summary>
        /// Installs a handler to run when the 'replay' command is specified.
        /// </summary>
        internal void InstallReplayHandler(Action handler) =>
            this.ReplayCommand.SetHandler(handler);

        /// <summary>
        /// Installs a handler to run when the 'rewrite' command is specified.
        /// </summary>
        internal void InstallRewriteHandler(Action handler) =>
            this.RewriteCommand.SetHandler(handler);

        /// <summary>
        /// Invoke the user-specified command handler.
        /// </summary>
        internal ExitCode InvokeCommand() => (ExitCode)this.Results.Invoke();

        /// <summary>
        /// Creates the test command.
        /// </summary>
        private static Command CreateTestCommand(Configuration configuration)
        {
            var pathArg = new Argument("path", $"Path to the assembly to test.")
            {
                HelpName = "PATH"
            };

            var methodOption = new Option<string>(
                aliases: new[] { "-m", "--method" },
                description: "Suffix of the test method to execute.")
            {
                ArgumentHelpName = "METHOD"
            };

            var iterationsOption = new Option<int>(
                aliases: new[] { "-i", "--iterations" },
                getDefaultValue: () => (int)configuration.TestingIterations,
                description: "Number of testing iterations to run.")
            {
                ArgumentHelpName = "ITERATIONS"
            };

            var timeoutOption = new Option<int>(
                aliases: new[] { "-t", "--timeout" },
                getDefaultValue: () => configuration.TestingTimeout,
                description: "Timeout in seconds after which no more testing iterations will run (disabled by default).")
            {
                ArgumentHelpName = "TIMEOUT"
            };

            var allowedStrategies = new HashSet<string>
            {
                "random",
                "prioritization",
                "fair-prioritization",
                "probabilistic",
                "rl",
                "portfolio"
            };

            var strategyOption = new Option<string>(
                aliases: new[] { "-str", "--strategy" },
                getDefaultValue: () => configuration.SchedulingStrategy,
                description: "Set exploration strategy to use during testing. The exploration strategy " +
                    "controls all scheduling decisions and nondeterministic choices. " +
                    $"Allowed values are {string.Join(", ", allowedStrategies)}.")
            {
                ArgumentHelpName = "STRATEGY"
            };

            var strategyValueOption = new Option<int>(
                aliases: new[] { "-sv", "--strategy-value" },
                description: "Set exploration strategy specific value. Supported strategies (and values): " +
                    "(fair-)prioritization (maximum number of priority change points per iteration), " +
                    "probabilistic (probability of deviating from a scheduled operation).")
            {
                ArgumentHelpName = "VALUE"
            };

            var maxStepsOption = new Option<int>(
                aliases: new[] { "-ms", "--max-steps" },
                description: "Max scheduling steps (i.e. decisions) to be explored during testing. " +
                    "Choosing value 'STEPS' sets 'STEPS' unfair max-steps and 'STEPS*10' fair steps.")
            {
                ArgumentHelpName = "STEPS"
            };

            var maxFairStepsOption = new Option<int>(
                name: "--max-fair-steps",
                getDefaultValue: () => configuration.MaxFairSchedulingSteps,
                description: "Max fair scheduling steps (i.e. decisions) to be explored during testing. " +
                    "Used by exploration strategies that perform fair scheduling.")
            {
                ArgumentHelpName = "STEPS"
            };

            var maxUnfairStepsOption = new Option<int>(
                name: "--max-unfair-steps",
                getDefaultValue: () => configuration.MaxUnfairSchedulingSteps,
                description: "Max unfair scheduling steps (i.e. decisions) to be explored during testing. " +
                    "Used by exploration strategies that perform unfair scheduling.")
            {
                ArgumentHelpName = "STEPS"
            };

            var fuzzOption = new Option<bool>(
                name: "--fuzz",
                description: "Use systematic fuzzing instead of controlled testing.")
            {
                Arity = ArgumentArity.Zero
            };

            var coverageOption = new Option<bool>(
                aliases: new[] { "-c", "--coverage" },
                description: "Generate coverage reports if supported for the programming model used by the test.")
            {
                Arity = ArgumentArity.Zero
            };

            var graphOption = new Option<bool>(
                name: "--graph",
                description: "Output a DGML graph that visualizes the failing execution path if a bug is found.")
            {
                Arity = ArgumentArity.Zero
            };

            var xmlLogOption = new Option<bool>(
                name: "--xml-trace",
                description: "Output an XML formatted runtime log file.")
            {
                Arity = ArgumentArity.Zero
            };

            var reduceSharedStateOption = new Option<bool>(
                name: "--reduce-shared-state",
                description: "Enables shared state reduction based on 'READ' and 'WRITE' scheduling points.")
            {
                Arity = ArgumentArity.Zero
            };

            var seedOption = new Option<int>(
                name: "--seed",
                description: "Specify the random value generator seed.")
            {
                ArgumentHelpName = "VALUE"
            };

            var livenessTemperatureThresholdOption = new Option<int>(
                name: "--liveness-temperature-threshold",
                getDefaultValue: () => configuration.LivenessTemperatureThreshold,
                description: "Specify the threshold (in number of steps) that triggers a liveness bug.")
            {
                ArgumentHelpName = "THRESHOLD"
            };

            var timeoutDelayOption = new Option<int>(
                name: "--timeout-delay",
                getDefaultValue: () => (int)configuration.TimeoutDelay,
                description: "Controls the frequency of timeouts by built-in timers (not a unit of time).")
            {
                ArgumentHelpName = "DELAY"
            };

            var deadlockTimeoutOption = new Option<int>(
                name: "--deadlock-timeout",
                getDefaultValue: () => (int)configuration.DeadlockTimeout,
                description: "Controls how much time (in ms) to wait before reporting a potential deadlock.")
            {
                ArgumentHelpName = "TIMEOUT"
            };

            var uncontrolledConcurrencyTimeoutOption = new Option<int>(
                name: "--uncontrolled-concurrency-timeout",
                getDefaultValue: () => (int)configuration.UncontrolledConcurrencyResolutionTimeout,
                description: "Controls how much time (in ms) to try resolve uncontrolled concurrency.")
            {
                ArgumentHelpName = "TIMEOUT"
            };

            var uncontrolledConcurrencyIntervalOption = new Option<int>(
                name: "--uncontrolled-concurrency-interval",
                getDefaultValue: () => (int)configuration.UncontrolledConcurrencyResolutionInterval,
                description: "Controls the interval (in ms) between attempts to resolve uncontrolled concurrency.")
            {
                ArgumentHelpName = "INTERVAL"
            };

            var skipPotentialDeadlocksOption = new Option<bool>(
                name: "--skip-potential-deadlocks",
                description: "Only report a deadlock when the runtime can fully determine that it is genuine " +
                    "and not due to partially-controlled concurrency.")
            {
                Arity = ArgumentArity.Zero
            };

            var failOnMaxStepsOption = new Option<bool>(
                name: "--fail-on-maxsteps",
                description: "Reaching the specified max-steps is considered a bug.")
            {
                Arity = ArgumentArity.Zero
            };

            var noFuzzingFallbackOption = new Option<bool>(
                name: "--no-fuzzing-fallback",
                description: "Disable automatic fallback to systematic fuzzing upon detecting uncontrolled concurrency.")
            {
                Arity = ArgumentArity.Zero
            };

            var noPartialControlOption = new Option<bool>(
                name: "--no-partial-control",
                description: "Disallow partially controlled concurrency during controlled testing.")
            {
                Arity = ArgumentArity.Zero
            };

            var noReproOption = new Option<bool>(
                name: "--no-repro",
                description: "Disable bug trace repro to ignore uncontrolled concurrency errors.")
            {
                Arity = ArgumentArity.Zero
            };

            var exploreOption = new Option<bool>(
                name: "explore",
                description: "Keep testing until the bound (e.g. iteration or time) is reached.")
            {
                Arity = ArgumentArity.Zero,
                IsHidden = true
            };

            var breakOption = new Option<bool>(
                aliases: new[] { "-b", "--break" },
                description: "Attaches the debugger and adds a breakpoint when an assertion fails.")
            {
                Arity = ArgumentArity.Zero
            };

            var outputDirectoryOption = new Option<string>(
                aliases: new[] { "-o", "--outdir" },
                description: "Output directory for emitting reports. This can be an absolute path or relative to current directory.")
            {
                ArgumentHelpName = "PATH"
            };

            // Add validators.
            iterationsOption.AddValidator(result => ValidateOptionValueIsUnsignedInteger(result));
            timeoutOption.AddValidator(result => ValidateOptionValueIsUnsignedInteger(result));
            strategyOption.AddValidator(result => ValidateOptionValueIsAllowed(result, allowedStrategies));
            maxStepsOption.AddValidator(result => ValidateOptionValueIsUnsignedInteger(result));
            maxFairStepsOption.AddValidator(result => ValidateOptionValueIsUnsignedInteger(result));
            maxUnfairStepsOption.AddValidator(result => ValidateOptionValueIsUnsignedInteger(result));
            seedOption.AddValidator(result => ValidateOptionValueIsUnsignedInteger(result));
            livenessTemperatureThresholdOption.AddValidator(result => ValidateOptionValueIsUnsignedInteger(result));
            timeoutDelayOption.AddValidator(result => ValidateOptionValueIsUnsignedInteger(result));
            deadlockTimeoutOption.AddValidator(result => ValidateOptionValueIsUnsignedInteger(result));
            uncontrolledConcurrencyTimeoutOption.AddValidator(result => ValidateOptionValueIsUnsignedInteger(result));
            uncontrolledConcurrencyIntervalOption.AddValidator(result => ValidateOptionValueIsUnsignedInteger(result));

            // Build command.
            var command = new Command("test", "Run tests using the Coyote systematic testing engine. " +
                $"Learn more at {LearnAboutTestUrl}.");
            command.AddArgument(pathArg);
            command.AddOption(methodOption);
            command.AddOption(iterationsOption);
            command.AddOption(timeoutOption);
            command.AddOption(strategyOption);
            command.AddOption(strategyValueOption);
            command.AddOption(maxStepsOption);
            command.AddOption(maxFairStepsOption);
            command.AddOption(maxUnfairStepsOption);
            command.AddOption(fuzzOption);
            command.AddOption(coverageOption);
            command.AddOption(graphOption);
            command.AddOption(xmlLogOption);
            command.AddOption(reduceSharedStateOption);
            command.AddOption(seedOption);
            command.AddOption(livenessTemperatureThresholdOption);
            command.AddOption(timeoutDelayOption);
            command.AddOption(deadlockTimeoutOption);
            command.AddOption(uncontrolledConcurrencyTimeoutOption);
            command.AddOption(uncontrolledConcurrencyIntervalOption);
            command.AddOption(skipPotentialDeadlocksOption);
            command.AddOption(failOnMaxStepsOption);
            command.AddOption(noFuzzingFallbackOption);
            command.AddOption(noPartialControlOption);
            command.AddOption(noReproOption);
            command.AddOption(exploreOption);
            command.AddOption(breakOption);
            command.AddOption(outputDirectoryOption);
            command.TreatUnmatchedTokensAsErrors = true;
            return command;
        }

        /// <summary>
        /// Creates the replay command.
        /// </summary>
        private static Command CreateReplayCommand(Configuration configuration)
        {
            var pathArg = new Argument("path", $"Path to the assembly to replay.")
            {
                HelpName = "PATH"
            };

            var scheduleFileArg = new Argument("schedule", $"*.schedule file containing the execution to replay.")
            {
                HelpName = "SCHEDULE_FILE"
            };

            var methodOption = new Option<string>(
                aliases: new[] { "-m", "--method" },
                description: "Suffix of the test method to execute.")
            {
                ArgumentHelpName = "METHOD"
            };

            var breakOption = new Option<bool>(
                aliases: new[] { "-b", "--break" },
                description: "Attaches the debugger and adds a breakpoint when an assertion fails.")
            {
                Arity = ArgumentArity.Zero
            };

            var outputDirectoryOption = new Option<string>(
                aliases: new[] { "-o", "--outdir" },
                description: "Output directory for emitting reports. This can be an absolute path or relative to current directory.")
            {
                ArgumentHelpName = "PATH"
            };

            // Build command.
            var command = new Command("replay", "Replay bugs that Coyote discovered during systematic testing. " +
                $"Learn more at {LearnAboutReplayUrl}.");
            command.AddArgument(pathArg);
            command.AddArgument(scheduleFileArg);
            command.AddOption(methodOption);
            command.AddOption(breakOption);
            command.AddOption(outputDirectoryOption);
            command.TreatUnmatchedTokensAsErrors = true;
            return command;
        }

        /// <summary>
        /// Creates the rewrite command.
        /// </summary>
        private static Command CreateRewriteCommand(Configuration configuration)
        {
            var pathArg = new Argument("path", "Path to the assembly (or a JSON configuration file) to rewrite.")
            {
                HelpName = "PATH"
            };

            var assertDataRacesOption = new Option<bool>(
                name: "--assert-data-races",
                getDefaultValue: () => false,
                description: "Add assertions for read/write data races.")
            {
                Arity = ArgumentArity.Zero,
                IsHidden = true
            };

            var rewriteDependenciesOption = new Option<bool>(
                name: "--rewrite-dependencies",
                getDefaultValue: () => false,
                description: "Rewrite all dependent assemblies that are found in the same location as the given path.")
            {
                Arity = ArgumentArity.Zero,
                IsHidden = true
            };

            var rewriteUnitTestsOption = new Option<bool>(
                name: "--rewrite-unit-tests",
                getDefaultValue: () => false,
                description: "Rewrite unit tests to automatically inject the Coyote testing engine.")
            {
                Arity = ArgumentArity.Zero,
                IsHidden = true
            };

            var rewriteThreadsOption = new Option<bool>(
                name: "--rewrite-threads",
                getDefaultValue: () => false,
                description: "Rewrite low-level threading APIs.")
            {
                Arity = ArgumentArity.Zero,
                IsHidden = true
            };

            var dumpILOption = new Option<bool>(
                name: "--dump-il",
                getDefaultValue: () => false,
                description: "Dumps the original and rewritten IL in JSON for debugging purposes.")
            {
                Arity = ArgumentArity.Zero
            };

            var dumpILDiffOption = new Option<bool>(
                name: "--dump-il-diff",
                getDefaultValue: () => false,
                description: "Dumps the IL diff in JSON for debugging purposes.")
            {
                Arity = ArgumentArity.Zero
            };

            // Build command.
            var command = new Command("rewrite", "Rewrite your assemblies to inject logic that allows " +
                "Coyote to take control of the schedule during systematic testing. " +
                $"Learn more at {LearnAboutRewritingUrl}.");
            command.AddArgument(pathArg);
            command.AddOption(assertDataRacesOption);
            command.AddOption(rewriteDependenciesOption);
            command.AddOption(rewriteUnitTestsOption);
            command.AddOption(rewriteThreadsOption);
            command.AddOption(dumpILOption);
            command.AddOption(dumpILDiffOption);
            command.TreatUnmatchedTokensAsErrors = true;
            return command;
        }

        /// <summary>
        /// Validates that the specified option result is an unsigned integer.
        /// </summary>
        private static void ValidateOptionValueIsUnsignedInteger(OptionResult result)
        {
            if (result.Tokens.Select(token => token.Value).Where(v => !uint.TryParse(v, out _)).Any())
            {
                result.ErrorMessage = $"Please give a positive integer to option '{result.Option.Name}'.";
            }
        }

        /// <summary>
        /// Validates that the specified option result has an allowed value.
        /// </summary>
        private static void ValidateOptionValueIsAllowed(OptionResult result, IEnumerable<string> allowedValues)
        {
            if (result.Tokens.Select(token => token.Value).Where(v => !allowedValues.Contains(v)).Any())
            {
                result.ErrorMessage = $"Please give an allowed value to option '{result.Option.Name}': " +
                    $"{string.Join(", ", allowedValues)}.";
            }
        }

        /// <summary>
        /// Populates the configurations from the specified parse result.
        /// </summary>
        private void UpdateConfigurations(ParseResult result)
        {
            var commandResult = result.CommandResult;
            Command command = commandResult.Command;
            foreach (var symbolResult in commandResult.Children)
            {
                Argument arg = command.Arguments.FirstOrDefault(a => a.Name == symbolResult.Symbol.Name);
                Option option = command.Options.FirstOrDefault(o => o.Name == symbolResult.Symbol.Name);
                if (arg != null)
                {
                    if (arg.Name == "path")
                    {
                        this.Configuration.AssemblyToBeAnalyzed = (string)commandResult.GetValueForArgument(arg);
                    }
                }
                else if (option != null)
                {
                    this.UpdateConfigurationsWithParsedOption(command, option, commandResult.GetValueForOption(option));
                }
            }
        }

        /// <summary>
        /// Updates the configuration with the specified parsed option and value.
        /// </summary>
        private void UpdateConfigurationsWithParsedOption(Command command, Option option, object value)
        {
            Console.WriteLine($"Option '{option.Name}': {value}");
            switch (option.Name)
            {
                case "path":
                    if (command.Name is "test" || command.Name is "replay")
                    {
                        // In the case of 'coyote test' or 'replay', the path is the assembly to be tested.
                        this.Configuration.AssemblyToBeAnalyzed = (string)value;
                    }
                    else if (command.Name is "rewrite")
                    {
                        // In the case of 'coyote rewrite', the path is the JSON this.Configuration file
                        // with the binary rewriting options.
                        string filename = (string)value;
                        if (Directory.Exists(filename))
                        {
                            // Then we want to rewrite a whole folder full of assemblies.
                            var assembliesDir = Path.GetFullPath(filename);
                            this.RewritingOptions.AssembliesDirectory = assembliesDir;
                            this.RewritingOptions.OutputDirectory = assembliesDir;
                        }
                        else
                        {
                            string extension = Path.GetExtension(filename);
                            if (string.Compare(extension, ".json", StringComparison.OrdinalIgnoreCase) is 0)
                            {
                                // Parse the rewriting options from the JSON file.
                                RewritingOptions.ParseFromJSON(this.RewritingOptions, filename);
                            }
                            else if (string.Compare(extension, ".dll", StringComparison.OrdinalIgnoreCase) is 0 ||
                                string.Compare(extension, ".exe", StringComparison.OrdinalIgnoreCase) is 0)
                            {
                                this.Configuration.AssemblyToBeAnalyzed = filename;
                                var fullPath = Path.GetFullPath(filename);
                                var assembliesDir = Path.GetDirectoryName(fullPath);
                                this.RewritingOptions.AssembliesDirectory = assembliesDir;
                                this.RewritingOptions.OutputDirectory = assembliesDir;
                                this.RewritingOptions.AssemblyPaths.Add(fullPath);
                            }
                            else
                            {
                                Error.ReportAndExit("Please give a valid .dll or JSON this.Configuration file for binary rewriting.");
                            }
                        }
                    }

                    break;
                case "method":
                    this.Configuration.TestMethodName = (string)value;
                    break;
                case "iterations":
                    this.Configuration.TestingIterations = (uint)(int)value;
                    break;
                case "timeout":
                    this.Configuration.TestingTimeout = (int)value;
                    break;
                case "strategy":
                    this.Configuration.SchedulingStrategy = (string)value;
                    break;
                case "strategy-value":
                    this.Configuration.StrategyBound = (int)value;
                    break;
                case "max-steps":
                    this.Configuration.WithMaxSchedulingSteps((uint)(int)value);
                    break;
                case "max-fair-steps":
                    this.Configuration.WithMaxSchedulingSteps((uint)this.Configuration.MaxUnfairSchedulingSteps, (uint)(int)value);
                    break;
                case "max-unfair-steps":
                    this.Configuration.WithMaxSchedulingSteps((uint)(int)value, (uint)this.Configuration.MaxFairSchedulingSteps);
                    break;
                case "fuzz":
                case "no-repro":
                    this.Configuration.IsSystematicFuzzingEnabled = true;
                    break;
                case "coverage":
                    this.Configuration.ReportCodeCoverage = true;
                    this.Configuration.IsActivityCoverageReported = true;
                    break;
                case "graph":
                    this.Configuration.IsTraceVisualizationEnabled = true;
                    break;
                case "xml-trace":
                    this.Configuration.IsXmlLogEnabled = true;
                    break;
                case "reduce-shared-state":
                    this.Configuration.IsSharedStateReductionEnabled = true;
                    break;
                case "seed":
                    this.Configuration.RandomGeneratorSeed = (uint)(int)value;
                    break;
                case "liveness-temperature-threshold":
                    this.Configuration.LivenessTemperatureThreshold = (int)value;
                    this.Configuration.UserExplicitlySetLivenessTemperatureThreshold = true;
                    break;
                case "timeout-delay":
                    this.Configuration.TimeoutDelay = (uint)(int)value;
                    break;
                case "deadlock-timeout":
                    this.Configuration.DeadlockTimeout = (uint)(int)value;
                    break;
                case "uncontrolled-concurrency-timeout":
                    this.Configuration.UncontrolledConcurrencyResolutionTimeout = (uint)(int)value;
                    break;
                case "uncontrolled-concurrency-interval":
                    this.Configuration.UncontrolledConcurrencyResolutionInterval = (uint)(int)value;
                    break;
                case "skip-potential-deadlocks":
                    this.Configuration.ReportPotentialDeadlocksAsBugs = false;
                    break;
                case "fail-on-maxsteps":
                    this.Configuration.ConsiderDepthBoundHitAsBug = true;
                    break;
                case "no-fuzzing-fallback":
                    this.Configuration.IsSystematicFuzzingFallbackEnabled = false;
                    break;
                case "no-partial-control":
                    this.Configuration.IsPartiallyControlledConcurrencyAllowed = false;
                    break;
                case "explore":
                    this.Configuration.RunTestIterationsToCompletion = true;
                    break;
                case "break":
                    this.Configuration.AttachDebugger = true;
                    break;
                case "outdir":
                    this.Configuration.OutputFilePath = (string)value;
                    break;
                case "verbosity":
                    this.Configuration.IsVerbose = true;
                    string verbosity = (string)value;
                    switch (verbosity)
                    {
                        case "quiet":
                            this.Configuration.IsVerbose = false;
                            break;
                        case "detailed":
                            this.Configuration.LogLevel = LogSeverity.Informational;
                            break;
                        case "normal":
                            this.Configuration.LogLevel = LogSeverity.Warning;
                            break;
                        case "minimal":
                            this.Configuration.LogLevel = LogSeverity.Error;
                            break;
                        default:
                            Error.ReportAndExit($"Please give a valid value for 'verbosity' must be one of 'errors', 'warnings', or 'info', but found {verbosity}.");
                            break;
                    }

                    break;
                case "debug":
                    this.Configuration.IsDebugVerbosityEnabled = true;
                    Debug.IsEnabled = true;
                    break;
                case "assert-data-races":
                    this.RewritingOptions.IsDataRaceCheckingEnabled = true;
                    break;
                case "rewrite-dependencies":
                    this.RewritingOptions.IsRewritingDependencies = true;
                    break;
                case "rewrite-unit-tests":
                    this.RewritingOptions.IsRewritingUnitTests = true;
                    break;
                case "rewrite-threads":
                    this.RewritingOptions.IsRewritingThreads = true;
                    break;
                case "dump-il":
                    this.RewritingOptions.IsLoggingAssemblyContents = true;
                    break;
                case "dump-il-diff":
                    this.RewritingOptions.IsDiffingAssemblyContents = true;
                    break;
                //case "schedule":
                //    {
                //        string filename = (string)value;
                //        string extension = Path.GetExtension(filename);
                //        if (!extension.Equals(".schedule"))
                //        {
                //            Error.ReportAndExit("Please give a valid schedule file " +
                //                "with extension '.schedule'.");
                //        }

                //        this.Configuration.ScheduleFile = filename;
                //    }

                //    break;
                default:
                    throw new Exception(string.Format("Unhandled parsed argument: '{0}'", option));
            }
        }
    }
}
