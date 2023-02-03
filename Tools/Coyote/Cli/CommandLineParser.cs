// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;
using Microsoft.Coyote.Logging;
using Microsoft.Coyote.Rewriting;
using Microsoft.Coyote.Testing;

namespace Microsoft.Coyote.Cli
{
    internal sealed class CommandLineParser
    {
        /// <summary>
        /// The Coyote runtime and testing configuration.
        /// </summary>
        private readonly Configuration Configuration;

        /// <summary>
        /// The Coyote rewriting options.
        /// </summary>
        private readonly RewritingOptions RewritingOptions;

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
        /// Mao from argument names to arguments.
        /// </summary>
        private readonly Dictionary<string, Argument> Arguments;

        /// <summary>
        /// Mao from option names to options.
        /// </summary>
        private readonly Dictionary<string, Option> Options;

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
            this.Arguments = new Dictionary<string, Argument>();
            this.Options = new Dictionary<string, Option>();

            var allowedVerbosityLevels = new HashSet<string>
            {
                "none",
                "error",
                "warning",
                "info",
                "debug",
                "exhaustive"
            };

            var verbosityOption = new Option<string>(
                aliases: new[] { "-v", "--verbosity" },
                getDefaultValue: () => "error",
                description: "Enable verbosity with an optional verbosity level. " +
                    $"Allowed values are {string.Join(", ", allowedVerbosityLevels)}. " +
                    "Skipping the argument sets the verbosity level to 'info'.")
            {
                ArgumentHelpName = "LEVEL",
                Arity = ArgumentArity.ZeroOrOne
            };

            var consoleLoggingOption = new Option<bool>(
                name: "--console",
                description: "Log all runtime messages to the console unless overridden by a custom ILogger.")
            {
                Arity = ArgumentArity.Zero
            };

            // Add validators.
            verbosityOption.AddValidator(result => ValidateOptionValueIsAllowed(result, allowedVerbosityLevels));

            // Create the commands.
            this.TestCommand = this.CreateTestCommand(this.Configuration);
            this.ReplayCommand = this.CreateReplayCommand();
            this.RewriteCommand = this.CreateRewriteCommand();

            // Create the root command.
            var rootCommand = new RootCommand("The Coyote systematic testing tool.\n\n" +
                $"Learn how to use Coyote at {Documentation.LearnAboutCoyoteUrl}.\nLearn what is new at {Documentation.LearnWhatIsNewUrl}.");
            this.AddGlobalOption(rootCommand, verbosityOption);
            this.TestCommand.AddGlobalOption(consoleLoggingOption);
            this.ReplayCommand.AddGlobalOption(consoleLoggingOption);
            rootCommand.AddCommand(this.TestCommand);
            rootCommand.AddCommand(this.ReplayCommand);
            rootCommand.AddCommand(this.RewriteCommand);
            rootCommand.TreatUnmatchedTokensAsErrors = true;

            var commandLineBuilder = new CommandLineBuilder(rootCommand);
            commandLineBuilder.UseDefaults();
            commandLineBuilder.EnablePosixBundling(false);

            var parser = commandLineBuilder.Build();
            this.Results = parser.Parse(args);
            if (this.Results.Errors.Any() || IsHelpRequested(this.Results))
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
        }

        /// <summary>
        /// Invokes the command selected by the user.
        /// </summary>
        internal ExitCode InvokeCommand()
        {
            PrintDetailedCoyoteVersion();
            return (ExitCode)this.Results.Invoke();
        }

        /// <summary>
        /// Sets the handler to be invoked when the test command is selected by the user.
        /// </summary>
        internal void SetTestCommandHandler(Func<Configuration, ExitCode> testHandler)
        {
            this.TestCommand.SetHandler((InvocationContext context) => context.ExitCode = (int)testHandler(this.Configuration));
        }

        /// <summary>
        /// Sets the handler to be invoked when the replay command is selected by the user.
        /// </summary>
        internal void SetReplayCommandHandler(Func<Configuration, ExitCode> replayHandler)
        {
            this.ReplayCommand.SetHandler((InvocationContext context) => context.ExitCode = (int)replayHandler(this.Configuration));
        }

        /// <summary>
        /// Sets the handler to be invoked when the rewrite command is selected by the user.
        /// </summary>
        internal void SetRewriteCommandHandler(Func<Configuration, RewritingOptions, ExitCode> rewriteHandler)
        {
            this.RewriteCommand.SetHandler((InvocationContext context) => context.ExitCode = (int)rewriteHandler(
                this.Configuration, this.RewritingOptions));
        }

        /// <summary>
        /// Creates the test command.
        /// </summary>
        private Command CreateTestCommand(Configuration configuration)
        {
            var pathArg = new Argument<string>("path", $"Path to the assembly (*.dll, *.exe) to test.")
            {
                HelpName = "PATH"
            };

            var methodOption = new Option<string>(
                aliases: new[] { "-m", "--method" },
                description: "Suffix of the test method to execute.")
            {
                ArgumentHelpName = "METHOD",
                Arity = ArgumentArity.ExactlyOne
            };

            var iterationsOption = new Option<int>(
                aliases: new[] { "-i", "--iterations" },
                getDefaultValue: () => (int)configuration.TestingIterations,
                description: "Number of testing iterations to run.")
            {
                ArgumentHelpName = "ITERATIONS",
                Arity = ArgumentArity.ExactlyOne
            };

            var timeoutOption = new Option<int>(
                aliases: new[] { "-t", "--timeout" },
                getDefaultValue: () => configuration.TestingTimeout,
                description: "Timeout in seconds after which no more testing iterations will run (disabled by default).")
            {
                ArgumentHelpName = "TIMEOUT",
                Arity = ArgumentArity.ExactlyOne
            };

            var allowedStrategies = new HashSet<string>
            {
                "random",
                "probabilistic",
                "prioritization",
                "fair-prioritization",
                "delay-bounding",
                "fair-delay-bounding",
                "q-learning"
            };

            var strategyOption = new Option<string>(
                aliases: new[] { "-s", "--strategy" },
                getDefaultValue: () => configuration.ExplorationStrategy.GetName(),
                description: "Set exploration strategy to use during testing. The exploration strategy controls " +
                    "all scheduling decisions and nondeterministic choices. Note that explicitly setting this " +
                    "value disables the default exploration mode that uses a tuned portfolio of strategies. " +
                    $"Allowed values are {string.Join(", ", allowedStrategies)}.")
            {
                ArgumentHelpName = "STRATEGY",
                Arity = ArgumentArity.ExactlyOne
            };

            var strategyValueOption = new Option<int>(
                aliases: new[] { "-sv", "--strategy-value" },
                description: "Set exploration strategy specific value. Supported strategies (and values): " +
                    "probabilistic (probability of deviating from a scheduled operation), " +
                    "(fair-)prioritization (maximum number of priority changes per iteration), " +
                    "(fair-)delay-bounding (maximum number of delays per iteration).")
            {
                ArgumentHelpName = "VALUE",
                Arity = ArgumentArity.ExactlyOne
            };

            var allowedPortfolioMode = new HashSet<string>
            {
                "fair",
                "unfair"
            };

            var portfolioModeOption = new Option<string>(
                name: "--portfolio-mode",
                getDefaultValue: () => configuration.PortfolioMode.ToString().ToLower(),
                description: "Set the portfolio mode to use during testing. Portfolio mode uses a tuned portfolio " +
                    "of strategies, instead of the default or user-specified strategy. If fair mode is enabled, " +
                    "then the portfolio will upgrade any unfair strategies to fair, by adding a fair execution " +
                    "suffix after the the max fair scheduling steps bound has been reached. " +
                    $"Allowed values are {string.Join(", ", allowedPortfolioMode)}.")
            {
                ArgumentHelpName = "MODE",
                Arity = ArgumentArity.ExactlyOne
            };

            var maxStepsOption = new Option<int>(
                aliases: new[] { "-ms", "--max-steps" },
                description: "Max scheduling steps (i.e. decisions) to be explored during testing. " +
                    "Choosing value 'STEPS' sets 'STEPS' unfair max-steps and 'STEPS*10' fair steps.")
            {
                ArgumentHelpName = "STEPS",
                Arity = ArgumentArity.ExactlyOne
            };

            var maxFairStepsOption = new Option<int>(
                name: "--max-fair-steps",
                getDefaultValue: () => configuration.MaxFairSchedulingSteps,
                description: "Max fair scheduling steps (i.e. decisions) to be explored during testing. " +
                    "Used by exploration strategies that perform fair scheduling.")
            {
                ArgumentHelpName = "STEPS",
                Arity = ArgumentArity.ExactlyOne
            };

            var maxUnfairStepsOption = new Option<int>(
                name: "--max-unfair-steps",
                getDefaultValue: () => configuration.MaxUnfairSchedulingSteps,
                description: "Max unfair scheduling steps (i.e. decisions) to be explored during testing. " +
                    "Used by exploration strategies that perform unfair scheduling.")
            {
                ArgumentHelpName = "STEPS",
                Arity = ArgumentArity.ExactlyOne
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

            var scheduleCoverageOption = new Option<bool>(
                name: "--schedule-coverage",
                description: "Output a '.coverage.schedule.txt' file containing scheduling coverage information during testing.")
            {
                Arity = ArgumentArity.Zero
            };

            var serializeCoverageInfoOption = new Option<bool>(
                name: "--serialize-coverage",
                description: "Output a '.coverage.ser' file that contains the serialized coverage information.")
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

            var reduceExecutionTraceCyclesOption = new Option<bool>(
                name: "--reduce-execution-trace-cycles",
                description: "Enable execution trace cycle detection and reduction heuristics.")
            {
                Arity = ArgumentArity.Zero
            };

            var samplePartialOrdersOption = new Option<bool>(
                name: "--partial-order-sampling",
                description: "Enable partial-order sampling based on 'READ' and 'WRITE' scheduling points.")
            {
                Arity = ArgumentArity.Zero
            };

            var seedOption = new Option<int>(
                name: "--seed",
                description: "Specify the random value generator seed.")
            {
                ArgumentHelpName = "VALUE",
                Arity = ArgumentArity.ExactlyOne
            };

            var livenessTemperatureThresholdOption = new Option<int>(
                name: "--liveness-temperature-threshold",
                getDefaultValue: () => configuration.LivenessTemperatureThreshold,
                description: "Specify the threshold (in number of steps) that triggers a liveness bug.")
            {
                ArgumentHelpName = "THRESHOLD",
                Arity = ArgumentArity.ExactlyOne
            };

            var timeoutDelayOption = new Option<int>(
                name: "--timeout-delay",
                getDefaultValue: () => (int)configuration.TimeoutDelay,
                description: "Specify the frequency of timeouts (not a unit of time).")
            {
                ArgumentHelpName = "DELAY",
                Arity = ArgumentArity.ExactlyOne
            };

            var deadlockTimeoutOption = new Option<int>(
                name: "--deadlock-timeout",
                getDefaultValue: () => (int)configuration.DeadlockTimeout,
                description: "Specify how much time (in ms) to wait before reporting a potential deadlock.")
            {
                ArgumentHelpName = "TIMEOUT",
                Arity = ArgumentArity.ExactlyOne
            };

            var maxFuzzDelayOption = new Option<int>(
                name: "--max-fuzz-delay",
                getDefaultValue: () => (int)configuration.MaxFuzzingDelay,
                description: "Specify the maximum time (in number of busy loops) an operation might " +
                    "get delayed during systematic fuzzing.")
            {
                ArgumentHelpName = "DELAY",
                Arity = ArgumentArity.ExactlyOne
            };

            var uncontrolledConcurrencyResolutionAttemptsOption = new Option<int>(
                name: "--resolve-uncontrolled-concurrency-attempts",
                getDefaultValue: () => (int)configuration.UncontrolledConcurrencyResolutionAttempts,
                description: "Specify how many times to try resolve each instance of uncontrolled concurrency.")
            {
                ArgumentHelpName = "ATTEMPTS",
                Arity = ArgumentArity.ExactlyOne
            };

            var uncontrolledConcurrencyResolutionDelayOption = new Option<int>(
                name: "--resolve-uncontrolled-concurrency-delay",
                getDefaultValue: () => (int)configuration.UncontrolledConcurrencyResolutionDelay,
                description: "Specify how much time (in number of busy loops) to wait between each attempt to " +
                    "resolve each instance of uncontrolled concurrency.")
            {
                ArgumentHelpName = "DELAY",
                Arity = ArgumentArity.ExactlyOne
            };

            var skipExecutionGraphAnalysisOption = new Option<bool>(
                name: "--skip-execution-graph-analysis",
                description: "Disable execution graph analysis during testing.")
            {
                Arity = ArgumentArity.Zero
            };

            var skipPotentialDeadlocksOption = new Option<bool>(
                name: "--skip-potential-deadlocks",
                description: "Only report a deadlock when the runtime can fully determine that it is genuine " +
                    "and not due to partially-controlled concurrency.")
            {
                Arity = ArgumentArity.Zero
            };

            var skipCollectionRacesOption = new Option<bool>(
                name: "--skip-collection-races",
                description: "Disable exploration of race conditions when accessing collections.")
            {
                Arity = ArgumentArity.Zero
            };

            var skipLockRacesOption = new Option<bool>(
                name: "--skip-lock-races",
                description: "Disable exploration of race conditions when accessing lock-based synchronization primitives.")
            {
                Arity = ArgumentArity.Zero
            };

            var skipAtomicRacesOption = new Option<bool>(
                name: "--skip-atomic-races",
                description: "Disable exploration of race conditions when performing atomic operations.")
            {
                Arity = ArgumentArity.Zero
            };

            var noFuzzingFallbackOption = new Option<bool>(
                name: "--no-fuzzing-fallback",
                description: "Disable automatic fallback to systematic fuzzing upon detecting uncontrolled concurrency.")
            {
                Arity = ArgumentArity.Zero
            };

            var allowedPartialControlModes = new HashSet<string>
            {
                "none",
                "concurrency",
                "data"
            };

            var partialControlOption = new Option<string>(
                name: "--partial-control",
                description: "Set the partial controlled mode to use during testing. If set to 'concurrency' then " +
                    "only concurrency can be partially controlled. If set to 'data' then only data non-determinism " +
                    "can be partially controlled. If set to 'none' then partially controlled testing is disabled. " +
                    "By default, both concurrency and data non-determinism can be partially controlled. " +
                    $"Allowed values are {string.Join(", ", allowedPartialControlModes)}.")
            {
                ArgumentHelpName = "MODE",
                Arity = ArgumentArity.ExactlyOne
            };

            var noReproOption = new Option<bool>(
                name: "--no-repro",
                description: "Disable bug trace repro to ignore uncontrolled concurrency errors.")
            {
                Arity = ArgumentArity.Zero
            };

            var logUncontrolledInvocationStackTracesOption = new Option<bool>(
                name: "--log-uncontrolled-invocation-stack-traces",
                description: "Enable logging the stack traces of uncontrolled invocations detected during testing.")
            {
                Arity = ArgumentArity.Zero
            };

            var failOnMaxStepsOption = new Option<bool>(
                name: "--fail-on-max-steps",
                description: "Reaching the specified max-steps is treated as a bug.")
            {
                Arity = ArgumentArity.Zero
            };

            var exploreOption = new Option<bool>(
                name: "--explore",
                description: "Keep testing until the bound (e.g. iteration or time) is reached.")
            {
                Arity = ArgumentArity.Zero,
                IsHidden = true
            };

            var breakOption = new Option<bool>(
                aliases: new[] { "-b", "--break" },
                description: "Attach the debugger and add a breakpoint when an assertion fails.")
            {
                Arity = ArgumentArity.Zero
            };

            var outputDirectoryOption = new Option<string>(
                aliases: new[] { "-o", "--outdir" },
                description: "Output directory for emitting reports. This can be an absolute path or relative to current directory.")
            {
                ArgumentHelpName = "PATH",
                Arity = ArgumentArity.ExactlyOne
            };

            // Add validators.
            pathArg.AddValidator(result => ValidateArgumentValueIsExpectedFile(result, ".dll", ".exe"));
            iterationsOption.AddValidator(result => ValidateOptionValueIsUnsignedInteger(result));
            timeoutOption.AddValidator(result => ValidateOptionValueIsUnsignedInteger(result));
            strategyOption.AddValidator(result => ValidateOptionValueIsAllowed(result, allowedStrategies));
            strategyOption.AddValidator(result => ValidateExclusiveOptionValueIsAvailable(result, portfolioModeOption));
            strategyValueOption.AddValidator(result => ValidatePrerequisiteOptionValueIsAvailable(result, strategyOption));
            portfolioModeOption.AddValidator(result => ValidateOptionValueIsAllowed(result, allowedPortfolioMode));
            maxStepsOption.AddValidator(result => ValidateOptionValueIsUnsignedInteger(result));
            maxStepsOption.AddValidator(result => ValidateExclusiveOptionValueIsAvailable(result, maxFairStepsOption));
            maxStepsOption.AddValidator(result => ValidateExclusiveOptionValueIsAvailable(result, maxUnfairStepsOption));
            maxFairStepsOption.AddValidator(result => ValidateOptionValueIsUnsignedInteger(result));
            maxFairStepsOption.AddValidator(result => ValidateExclusiveOptionValueIsAvailable(result, maxStepsOption));
            maxUnfairStepsOption.AddValidator(result => ValidateOptionValueIsUnsignedInteger(result));
            maxUnfairStepsOption.AddValidator(result => ValidateExclusiveOptionValueIsAvailable(result, maxStepsOption));
            serializeCoverageInfoOption.AddValidator(result => ValidatePrerequisiteOptionValueIsAvailable(result, coverageOption));
            seedOption.AddValidator(result => ValidateOptionValueIsUnsignedInteger(result));
            livenessTemperatureThresholdOption.AddValidator(result => ValidateOptionValueIsUnsignedInteger(result));
            timeoutDelayOption.AddValidator(result => ValidateOptionValueIsUnsignedInteger(result));
            deadlockTimeoutOption.AddValidator(result => ValidateOptionValueIsUnsignedInteger(result));
            maxFuzzDelayOption.AddValidator(result => ValidateOptionValueIsUnsignedInteger(result));
            uncontrolledConcurrencyResolutionAttemptsOption.AddValidator(result => ValidateOptionValueIsUnsignedInteger(result));
            uncontrolledConcurrencyResolutionDelayOption.AddValidator(result => ValidateOptionValueIsUnsignedInteger(result));
            partialControlOption.AddValidator(result => ValidateOptionValueIsAllowed(result, allowedPartialControlModes));

            // Build command.
            var command = new Command("test", "Run tests using the Coyote systematic testing engine.\n" +
                $"Learn more at {Documentation.LearnAboutTestUrl}.");
            this.AddArgument(command, pathArg);
            this.AddOption(command, methodOption);
            this.AddOption(command, iterationsOption);
            this.AddOption(command, timeoutOption);
            this.AddOption(command, strategyOption);
            this.AddOption(command, strategyValueOption);
            this.AddOption(command, portfolioModeOption);
            this.AddOption(command, maxStepsOption);
            this.AddOption(command, maxFairStepsOption);
            this.AddOption(command, maxUnfairStepsOption);
            this.AddOption(command, fuzzOption);
            this.AddOption(command, coverageOption);
            this.AddOption(command, scheduleCoverageOption);
            this.AddOption(command, serializeCoverageInfoOption);
            this.AddOption(command, graphOption);
            this.AddOption(command, xmlLogOption);
            this.AddOption(command, reduceExecutionTraceCyclesOption);
            this.AddOption(command, samplePartialOrdersOption);
            this.AddOption(command, seedOption);
            this.AddOption(command, livenessTemperatureThresholdOption);
            this.AddOption(command, timeoutDelayOption);
            this.AddOption(command, deadlockTimeoutOption);
            this.AddOption(command, maxFuzzDelayOption);
            this.AddOption(command, uncontrolledConcurrencyResolutionAttemptsOption);
            this.AddOption(command, uncontrolledConcurrencyResolutionDelayOption);
            this.AddOption(command, skipExecutionGraphAnalysisOption);
            this.AddOption(command, skipPotentialDeadlocksOption);
            this.AddOption(command, skipCollectionRacesOption);
            this.AddOption(command, skipLockRacesOption);
            this.AddOption(command, skipAtomicRacesOption);
            this.AddOption(command, noFuzzingFallbackOption);
            this.AddOption(command, partialControlOption);
            this.AddOption(command, noReproOption);
            this.AddOption(command, logUncontrolledInvocationStackTracesOption);
            this.AddOption(command, failOnMaxStepsOption);
            this.AddOption(command, exploreOption);
            this.AddOption(command, breakOption);
            this.AddOption(command, outputDirectoryOption);
            command.TreatUnmatchedTokensAsErrors = true;
            return command;
        }

        /// <summary>
        /// Creates the replay command.
        /// </summary>
        private Command CreateReplayCommand()
        {
            var pathArg = new Argument<string>("path", $"Path to the assembly (*.dll, *.exe) to replay.")
            {
                HelpName = "PATH"
            };

            var traceFileArg = new Argument<string>("trace", $"*.trace file containing the execution path to replay.")
            {
                HelpName = "TRACE_FILE"
            };

            var methodOption = new Option<string>(
                aliases: new[] { "-m", "--method" },
                description: "Suffix of the test method to execute.")
            {
                ArgumentHelpName = "METHOD",
                Arity = ArgumentArity.ExactlyOne
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
                ArgumentHelpName = "PATH",
                Arity = ArgumentArity.ExactlyOne
            };

            // Add validators.
            pathArg.AddValidator(result => ValidateArgumentValueIsExpectedFile(result, ".dll", ".exe"));
            traceFileArg.AddValidator(result => ValidateArgumentValueIsExpectedFile(result, ".trace"));

            // Build command.
            var command = new Command("replay", "Replay bugs that Coyote discovered during systematic testing.\n" +
                $"Learn more at {Documentation.LearnAboutReplayUrl}.");
            this.AddArgument(command, pathArg);
            this.AddArgument(command, traceFileArg);
            this.AddOption(command, methodOption);
            this.AddOption(command, breakOption);
            this.AddOption(command, outputDirectoryOption);
            command.TreatUnmatchedTokensAsErrors = true;
            return command;
        }

        /// <summary>
        /// Creates the rewrite command.
        /// </summary>
        private Command CreateRewriteCommand()
        {
            var pathArg = new Argument<string>("path", "Path to the assembly (*.dll, *.exe) to rewrite or to a JSON rewriting configuration file.")
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

            // Add validators.
            pathArg.AddValidator(result => ValidateArgumentValueIsExpectedFile(result, ".dll", ".exe", ".json"));

            // Build command.
            var command = new Command("rewrite", "Rewrite your assemblies to inject logic that allows " +
                "Coyote to take control of the execution during systematic testing.\n" +
                $"Learn more at {Documentation.LearnAboutRewritingUrl}.");
            this.AddArgument(command, pathArg);
            this.AddOption(command, assertDataRacesOption);
            this.AddOption(command, rewriteDependenciesOption);
            this.AddOption(command, rewriteUnitTestsOption);
            this.AddOption(command, rewriteThreadsOption);
            this.AddOption(command, dumpILOption);
            this.AddOption(command, dumpILDiffOption);
            command.TreatUnmatchedTokensAsErrors = true;
            return command;
        }

        /// <summary>
        /// Adds an argument to the specified command.
        /// </summary>
        private void AddArgument(Command command, Argument argument)
        {
            command.AddArgument(argument);
            if (!this.Arguments.ContainsKey(argument.Name))
            {
                this.Arguments.Add(argument.Name, argument);
            }
        }

        /// <summary>
        /// Adds a global option to the specified command.
        /// </summary>
        private void AddGlobalOption(Command command, Option option)
        {
            command.AddGlobalOption(option);
            if (!this.Options.ContainsKey(option.Name))
            {
                this.Options.Add(option.Name, option);
            }
        }

        /// <summary>
        /// Adds an option to the specified command.
        /// </summary>
        private void AddOption(Command command, Option option)
        {
            command.AddOption(option);
            if (!this.Options.ContainsKey(option.Name))
            {
                this.Options.Add(option.Name, option);
            }
        }

        /// <summary>
        /// Validates that the specified argument result is found and has an expected file extension.
        /// </summary>
        private static void ValidateArgumentValueIsExpectedFile(ArgumentResult result, params string[] extensions)
        {
            string fileName = result.GetValueOrDefault<string>();
            string foundExtension = Path.GetExtension(fileName);
            if (!extensions.Any(extension => extension == foundExtension))
            {
                if (extensions.Length is 1)
                {
                    result.ErrorMessage = $"File '{fileName}' does not have the expected '{extensions[0]}' extension.";
                }
                else
                {
                    result.ErrorMessage = $"File '{fileName}' does not have one of the expected extensions: " +
                        $"{string.Join(", ", extensions)}.";
                }
            }
            else if (!File.Exists(fileName))
            {
                result.ErrorMessage = $"File '{fileName}' does not exist.";
            }
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
        /// Validates that the specified prerequisite option is available.
        /// </summary>
        private static void ValidatePrerequisiteOptionValueIsAvailable(OptionResult result, Option prerequisite)
        {
            OptionResult prerequisiteResult = result.FindResultFor(prerequisite);
            if (!result.IsImplicit && (prerequisiteResult is null || prerequisiteResult.IsImplicit))
            {
                result.ErrorMessage = $"Setting option '{result.Option.Name}' requires option '{prerequisite.Name}'.";
            }
        }

        /// <summary>
        /// Validates that the specified exclusive option is available.
        /// </summary>
        private static void ValidateExclusiveOptionValueIsAvailable(OptionResult result, Option exclusive)
        {
            OptionResult exclusiveResult = result.FindResultFor(exclusive);
            if (!result.IsImplicit && exclusiveResult != null && !exclusiveResult.IsImplicit)
            {
                result.ErrorMessage = $"Setting options '{result.Option.Name}' and '{exclusive.Name}' at the same time is not allowed.";
            }
        }

        /// <summary>
        /// Populates the configurations from the specified parse result.
        /// </summary>
        private void UpdateConfigurations(ParseResult result)
        {
            CommandResult commandResult = result.CommandResult;
            Command command = commandResult.Command;
            foreach (var symbolResult in commandResult.Children)
            {
                if (symbolResult is ArgumentResult argument)
                {
                    this.UpdateConfigurationsWithParsedArgument(command, argument);
                }
                else if (symbolResult is OptionResult option)
                {
                    this.UpdateConfigurationsWithParsedOption(option);
                }
            }
        }

        /// <summary>
        /// Updates the configuration with the specified parsed argument.
        /// </summary>
        private void UpdateConfigurationsWithParsedArgument(Command command, ArgumentResult result)
        {
            switch (result.Argument.Name)
            {
                case "path":
                    if (command.Name is "test" || command.Name is "replay")
                    {
                        // In the case of 'coyote test' or 'replay', the path is the assembly to be tested.
                        string path = Path.GetFullPath(result.GetValueOrDefault<string>());
                        this.Configuration.AssemblyToBeAnalyzed = path;
                    }
                    else if (command.Name is "rewrite")
                    {
                        // In the case of 'coyote rewrite', the path is the JSON this.Configuration file
                        // with the binary rewriting options.
                        string filename = result.GetValueOrDefault<string>();
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
                        }
                    }

                    break;
                case "trace":
                    if (command.Name is "replay")
                    {
                        string traceFile = result.GetValueOrDefault<string>();
                        string traceFileContents = File.ReadAllText(traceFile);
                        this.Configuration.WithReproducibleTrace(traceFileContents);
                    }

                    break;
                default:
                    throw new Exception(string.Format("Unhandled parsed argument '{0}'.", result.Argument.Name));
            }
        }

        /// <summary>
        /// Updates the configuration with the specified parsed option.
        /// </summary>
        private void UpdateConfigurationsWithParsedOption(OptionResult result)
        {
            if (!result.IsImplicit)
            {
                switch (result.Option.Name)
                {
                    case "method":
                        this.Configuration.TestMethodName = result.GetValueOrDefault<string>();
                        break;
                    case "iterations":
                        this.Configuration.TestingIterations = (uint)result.GetValueOrDefault<int>();
                        break;
                    case "timeout":
                        this.Configuration.TestingTimeout = result.GetValueOrDefault<int>();
                        break;
                    case "strategy":
                        var strategyBound = result.FindResultFor(this.Options["strategy-value"]);
                        string strategy = result.GetValueOrDefault<string>();
                        switch (strategy)
                        {
                            case "probabilistic":
                                if (strategyBound is null)
                                {
                                    this.Configuration.StrategyBound = 3;
                                }

                                break;
                            case "prioritization":
                            case "fair-prioritization":
                            case "delay-bounding":
                            case "fair-delay-bounding":
                                if (strategyBound is null)
                                {
                                    this.Configuration.StrategyBound = 10;
                                }

                                break;
                            case "q-learning":
                                this.Configuration.IsImplicitProgramStateHashingEnabled = true;
                                break;
                            case "random":
                            default:
                                break;
                        }

                        this.Configuration.ExplorationStrategy = ExplorationStrategyExtensions.FromName(strategy);
                        this.Configuration.PortfolioMode = PortfolioMode.None;
                        break;
                    case "strategy-value":
                        this.Configuration.StrategyBound = result.GetValueOrDefault<int>();
                        break;
                    case "portfolio-mode":
                        switch (result.GetValueOrDefault<string>())
                        {
                            case "unfair":
                                this.Configuration.PortfolioMode = PortfolioMode.Unfair;
                                break;
                            case "fair":
                            default:
                                this.Configuration.PortfolioMode = PortfolioMode.Fair;
                                break;
                        }

                        break;
                    case "max-steps":
                        this.Configuration.WithMaxSchedulingSteps((uint)result.GetValueOrDefault<int>());
                        break;
                    case "max-fair-steps":
                        var maxUnfairSteps = result.FindResultFor(this.Options["max-unfair-steps"]);
                        this.Configuration.WithMaxSchedulingSteps(
                            (uint)(maxUnfairSteps?.GetValueOrDefault<int>() ?? this.Configuration.MaxUnfairSchedulingSteps),
                            (uint)result.GetValueOrDefault<int>());
                        break;
                    case "max-unfair-steps":
                        var maxFairSteps = result.FindResultFor(this.Options["max-fair-steps"]);
                        this.Configuration.WithMaxSchedulingSteps(
                            (uint)result.GetValueOrDefault<int>(),
                            (uint)(maxFairSteps?.GetValueOrDefault<int>() ?? this.Configuration.MaxFairSchedulingSteps));
                        break;
                    case "fuzz":
                    case "no-repro":
                        this.Configuration.IsSystematicFuzzingEnabled = true;
                        break;
                    case "coverage":
                        this.Configuration.IsActivityCoverageReported = true;
                        break;
                    case "schedule-coverage":
                        this.Configuration.IsScheduleCoverageReported = true;
                        break;
                    case "serialize-coverage":
                        this.Configuration.IsCoverageInfoSerialized = true;
                        break;
                    case "graph":
                        this.Configuration.IsTraceVisualizationEnabled = true;
                        break;
                    case "xml-trace":
                        this.Configuration.IsXmlLogEnabled = true;
                        break;
                    case "reduce-execution-trace-cycles":
                        this.Configuration.IsExecutionTraceCycleReductionEnabled = true;
                        break;
                    case "partial-order-sampling":
                        this.Configuration.IsPartialOrderSamplingEnabled = true;
                        break;
                    case "seed":
                        this.Configuration.RandomGeneratorSeed = (uint)result.GetValueOrDefault<int>();
                        break;
                    case "liveness-temperature-threshold":
                        this.Configuration.LivenessTemperatureThreshold = result.GetValueOrDefault<int>();
                        this.Configuration.UserExplicitlySetLivenessTemperatureThreshold = true;
                        break;
                    case "timeout-delay":
                        this.Configuration.TimeoutDelay = (uint)result.GetValueOrDefault<int>();
                        break;
                    case "deadlock-timeout":
                        this.Configuration.DeadlockTimeout = (uint)result.GetValueOrDefault<int>();
                        break;
                    case "max-fuzz-delay":
                        this.Configuration.MaxFuzzingDelay = (uint)result.GetValueOrDefault<int>();
                        break;
                    case "resolve-uncontrolled-concurrency-attempts":
                        this.Configuration.UncontrolledConcurrencyResolutionAttempts = (uint)result.GetValueOrDefault<int>();
                        break;
                    case "resolve-uncontrolled-concurrency-delay":
                        this.Configuration.UncontrolledConcurrencyResolutionDelay = (uint)result.GetValueOrDefault<int>();
                        break;
                    case "skip-execution-graph-analysis":
                        this.Configuration.IsExecutionGraphAnalysisEnabled = false;
                        break;
                    case "skip-potential-deadlocks":
                        this.Configuration.ReportPotentialDeadlocksAsBugs = false;
                        break;
                    case "skip-collection-races":
                        this.Configuration.IsCollectionAccessRaceCheckingEnabled = false;
                        break;
                    case "skip-lock-races":
                        this.Configuration.IsLockAccessRaceCheckingEnabled = false;
                        break;
                    case "skip-atomic-races":
                        this.Configuration.IsAtomicOperationRaceCheckingEnabled = false;
                        break;
                    case "no-fuzzing-fallback":
                        this.Configuration.IsSystematicFuzzingFallbackEnabled = false;
                        break;
                    case "partial-control":
                        string mode = result.GetValueOrDefault<string>();
                        switch (mode)
                        {
                            case "concurrency":
                                this.Configuration.IsPartiallyControlledConcurrencyAllowed = true;
                                this.Configuration.IsPartiallyControlledDataNondeterminismAllowed = false;
                                break;
                            case "data":
                                this.Configuration.IsPartiallyControlledConcurrencyAllowed = false;
                                this.Configuration.IsPartiallyControlledDataNondeterminismAllowed = true;
                                break;
                            case "none":
                            default:
                                this.Configuration.IsPartiallyControlledConcurrencyAllowed = false;
                                this.Configuration.IsPartiallyControlledDataNondeterminismAllowed = false;
                                break;
                        }

                        break;
                    case "log-uncontrolled-invocation-stack-traces":
                        this.Configuration.WithUncontrolledInvocationStackTraceLoggingEnabled();
                        break;
                    case "fail-on-max-steps":
                        this.Configuration.FailOnMaxStepsBound = true;
                        break;
                    case "explore":
                        this.Configuration.RunTestIterationsToCompletion = true;
                        break;
                    case "break":
                        this.Configuration.AttachDebugger = true;
                        break;
                    case "outdir":
                        this.Configuration.OutputFilePath = result.GetValueOrDefault<string>();
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
                    case "verbosity":
                        switch (result.GetValueOrDefault<string>())
                        {
                            case "error":
                                this.Configuration.WithVerbosityEnabled(VerbosityLevel.Error);
                                break;
                            case "warning":
                                this.Configuration.WithVerbosityEnabled(VerbosityLevel.Warning);
                                break;
                            case "debug":
                                this.Configuration.WithVerbosityEnabled(VerbosityLevel.Debug);
                                break;
                            case "exhaustive":
                                this.Configuration.WithVerbosityEnabled(VerbosityLevel.Exhaustive);
                                break;
                            case "info":
                            default:
                                this.Configuration.WithVerbosityEnabled(VerbosityLevel.Info);
                                break;
                        }

                        break;
                    case "console":
                        this.Configuration.WithConsoleLoggingEnabled(true);
                        break;
                    case "help":
                        break;
                    default:
                        throw new Exception(string.Format("Unhandled parsed option '{0}.", result.Option.Name));
                }
            }
        }

        /// <summary>
        /// Returns true if the user is asking for help.
        /// </summary>
        private static bool IsHelpRequested(ParseResult result) => result.CommandResult.Children
            .OfType<OptionResult>()
            .Any(result => result.Option.Name is "help" && !result.IsImplicit);

        /// <summary>
        /// Prints the detailed Coyote version.
        /// </summary>
        private static void PrintDetailedCoyoteVersion()
        {
            Console.WriteLine("Microsoft (R) Coyote version {0} for .NET{1}",
                typeof(CommandLineParser).Assembly.GetName().Version, GetDotNetVersion());
            Console.WriteLine("Copyright (C) Microsoft Corporation. All rights reserved.\n");
        }

        /// <summary>
        /// Returns the current .NET version.
        /// </summary>
        private static string GetDotNetVersion()
        {
            var path = typeof(string).Assembly.Location;
            string result = string.Empty;

            string[] parts = path.Replace("\\", "/").Split('/');
            if (parts.Length > 2)
            {
                var version = parts[parts.Length - 2];
                if (char.IsDigit(version[0]))
                {
                    result += " " + version;
                }
            }

            return result;
        }
    }
}
