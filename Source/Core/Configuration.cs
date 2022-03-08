// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Microsoft.Coyote.IO;

namespace Microsoft.Coyote
{
#pragma warning disable CA1724 // Type names should not match namespaces
    /// <summary>
    /// The Coyote project configurations.
    /// </summary>
    [DataContract]
    [Serializable]
    public class Configuration
    {
        /// <summary>
        /// The user-specified command to perform by the Coyote tool.
        /// </summary>
        [DataMember]
        internal string ToolCommand;

        /// <summary>
        /// Something to add to the PATH environment at test time.
        /// </summary>
        internal string AdditionalPaths { get; set; }

        /// <summary>
        /// The output path.
        /// </summary>
        [DataMember]
        internal string OutputFilePath;

        /// <summary>
        /// The assembly to be analyzed for bugs.
        /// </summary>
        [DataMember]
        internal string AssemblyToBeAnalyzed;

        /// <summary>
        /// Test method to be used.
        /// </summary>
        [DataMember]
        internal string TestMethodName;

        /// <summary>
        /// The systematic testing strategy to use.
        /// </summary>
        [DataMember]
        public string SchedulingStrategy { get; internal set; }

        /// <summary>
        /// Number of testing iterations.
        /// </summary>
        [DataMember]
        public uint TestingIterations { get; internal set; }

        /// <summary>
        /// Timeout in seconds after which no more testing iterations will run.
        /// </summary>
        /// <remarks>
        /// Setting this value overrides the <see cref="TestingIterations"/> value.
        /// </remarks>
        [DataMember]
        internal int TestingTimeout;

        /// <summary>
        /// Custom seed to be used by the random value generator. By default,
        /// this value is null indicating that no seed has been set.
        /// </summary>
        [DataMember]
        public uint? RandomGeneratorSeed { get; internal set; }

        /// <summary>
        /// If true, the seed will increment in each testing iteration.
        /// </summary>
        [DataMember]
        internal bool IsSchedulingSeedIncremental;

        /// <summary>
        /// If this option is enabled and uncontrolled concurrency is detected, then the tester
        /// will attempt to partially control the concurrency of the application, instead of
        /// failing with an error.
        /// </summary>
        [DataMember]
        internal bool IsPartiallyControlledConcurrencyEnabled;

        /// <summary>
        /// If this option is enabled, the concurrency fuzzing policy is used during testing.
        /// </summary>
        [DataMember]
        internal bool IsConcurrencyFuzzingEnabled;

        /// <summary>
        /// If this option is enabled and uncontrolled concurrency is detected, then the tester
        /// automatically switches to concurrency fuzzing, instead of failing with an error.
        /// </summary>
        [DataMember]
        internal bool IsConcurrencyFuzzingFallbackEnabled;

        /// <summary>
        /// If this option is enabled, liveness checking is enabled during systematic testing.
        /// </summary>
        [DataMember]
        internal bool IsLivenessCheckingEnabled;

        /// <summary>
        /// If true, the tester runs all iterations up to a bound, even if a bug is found.
        /// </summary>
        [DataMember]
        internal bool RunTestIterationsToCompletion;

        /// <summary>
        /// The maximum scheduling steps to explore for unfair schedulers.
        /// By default this is set to 10,000 steps.
        /// </summary>
        [DataMember]
        public int MaxUnfairSchedulingSteps { get; internal set; }

        /// <summary>
        /// The maximum scheduling steps to explore for fair schedulers.
        /// By default this is set to 100,000 steps.
        /// </summary>
        [DataMember]
        public int MaxFairSchedulingSteps { get; internal set; }

        /// <summary>
        /// True if the user has explicitly set the fair scheduling steps.
        /// </summary>
        [DataMember]
        internal bool UserExplicitlySetMaxFairSchedulingSteps;

        /// <summary>
        /// If true, then the Coyote tester will consider an execution
        /// that hits the depth bound as buggy.
        /// </summary>
        [DataMember]
        internal bool ConsiderDepthBoundHitAsBug;

        /// <summary>
        /// A strategy-specific bound.
        /// </summary>
        [DataMember]
        internal int StrategyBound;

        /// <summary>
        /// Value that controls the probability of triggering a timeout each time an operation gets delayed
        /// or a built-in timer gets scheduled during systematic testing. Decrease the value to increase the
        /// frequency of timeouts (e.g. a value of 1 corresponds to a 50% probability), or increase the value
        /// to decrease the frequency (e.g. a value of 10 corresponds to a 10% probability).
        /// </summary>
        [DataMember]
        public uint TimeoutDelay { get; internal set; }

        /// <summary>
        /// Value that controls how much time the deadlock monitor should wait during concurrency testing
        /// before reporting a potential deadlock. This value is in milliseconds.
        /// </summary>
        [DataMember]
        public uint DeadlockTimeout { get; internal set; }

        /// <summary>
        /// Value that controls how much time the runtime should wait for uncontrolled concurrency
        /// to resolve before continuing exploration. This value is in milliseconds.
        /// </summary>
        [DataMember]
        public uint UncontrolledConcurrencyTimeout { get; internal set; }

        /// <summary>
        /// Safety prefix bound. By default it is 0.
        /// </summary>
        [DataMember]
        internal int SafetyPrefixBound;

        /// <summary>
        /// The liveness temperature threshold. If it is 0 then it is disabled. By default
        /// this value is assigned to <see cref="MaxFairSchedulingSteps"/> / 2.
        /// </summary>
        [DataMember]
        public int LivenessTemperatureThreshold { get; internal set; }

        /// <summary>
        /// True if the user has explicitly set the liveness temperature threshold.
        /// </summary>
        [DataMember]
        internal bool UserExplicitlySetLivenessTemperatureThreshold;

        /// <summary>
        /// If this option is enabled, the tester is hashing the program state.
        /// </summary>
        [DataMember]
        internal bool IsProgramStateHashingEnabled;

        /// <summary>
        /// If this option is enabled, (safety) monitors are used in the production runtime.
        /// </summary>
        [DataMember]
        internal bool IsMonitoringEnabledInInProduction;

        /// <summary>
        /// Attaches the debugger during trace replay.
        /// </summary>
        [DataMember]
        internal bool AttachDebugger;

        /// <summary>
        /// The schedule file to be replayed.
        /// </summary>
        internal string ScheduleFile;

        /// <summary>
        /// The schedule trace to be replayed.
        /// </summary>
        internal string ScheduleTrace;

        /// <summary>
        /// If true, then messages are logged.
        /// </summary>
        [DataMember]
        public bool IsVerbose { get; internal set; }

        /// <summary>
        /// If true, then debug verbosity is enabled.
        /// </summary>
        [DataMember]
        internal bool IsDebugVerbosityEnabled;

        /// <summary>
        /// The level of detail to provide in verbose logging.
        /// </summary>
        [DataMember]
        public LogSeverity LogLevel { get; internal set; }

        /// <summary>
        /// Enables code coverage reporting of a Coyote program.
        /// </summary>
        [DataMember]
        internal bool ReportCodeCoverage;

        /// <summary>
        /// Enables activity coverage reporting of a Coyote program.
        /// </summary>
        [DataMember]
        internal bool IsActivityCoverageReported;

        /// <summary>
        /// Enables activity coverage debugging.
        /// </summary>
        internal bool DebugActivityCoverage;

        /// <summary>
        /// Is DGML graph showing all test iterations or just one "bug" iteration.
        /// False means all, and True means only the iteration containing a bug.
        /// </summary>
        [DataMember]
        internal bool IsDgmlBugGraph;

        /// <summary>
        /// If specified, requests a DGML graph of the iteration that contains a bug, if a bug is found.
        /// This is different from a coverage activity graph, as it will also show actor instances.
        /// </summary>
        [DataMember]
        internal bool IsDgmlGraphEnabled;

        /// <summary>
        /// Produce an XML formatted runtime log file.
        /// </summary>
        [DataMember]
        internal bool IsXmlLogEnabled;

        /// <summary>
        /// If specified, requests a custom runtime log to be used instead of the default.
        /// This is the AssemblyQualifiedName of the type to load.
        /// </summary>
        [DataMember]
        internal string CustomActorRuntimeLogType;

        /// <summary>
        /// Number of parallel systematic testing tasks.
        /// By default it is 1 task.
        /// </summary>
        [DataMember]
        internal uint ParallelBugFindingTasks;

        /// <summary>
        /// Put a debug prompt at the beginning of each child TestProcess.
        /// </summary>
        [DataMember]
        internal bool ParallelDebug;

        /// <summary>
        /// Specify ip address if you want to use something other than localhost.
        /// </summary>
        [DataMember]
        internal string TestingSchedulerIpAddress;

        /// <summary>
        /// Do not automatically launch the TestingProcesses in parallel mode, instead wait for them
        /// to be launched independently.
        /// </summary>
        [DataMember]
        internal bool WaitForTestingProcesses;

        /// <summary>
        /// Runs this process as a parallel systematic testing task.
        /// </summary>
        [DataMember]
        internal bool RunAsParallelBugFindingTask;

        /// <summary>
        /// The testing scheduler unique endpoint.
        /// </summary>
        [DataMember]
        internal string TestingSchedulerEndPoint;

        /// <summary>
        /// The unique testing process id.
        /// </summary>
        [DataMember]
        internal uint TestingProcessId;

        /// <summary>
        /// Additional assembly specifications to instrument for code coverage, besides those in the
        /// dependency graph between <see cref="AssemblyToBeAnalyzed"/> and the Microsoft.Coyote DLLs.
        /// Key is filename, value is whether it is a list file (true) or a single file (false).
        /// </summary>
        internal Dictionary<string, bool> AdditionalCodeCoverageAssemblies;

        /// <summary>
        /// Enables colored console output.
        /// </summary>
        internal bool EnableColoredConsoleOutput;

        /// <summary>
        /// If true, then environment exit will be disabled.
        /// </summary>
        internal bool DisableEnvironmentExit;

        /// <summary>
        /// Enable Coyote sending Telemetry to Azure which is used to help improve the tool (default true).
        /// </summary>
        internal bool EnableTelemetry;

        /// <summary>
        /// Optional location of app that can run as a telemetry server.
        /// </summary>
        internal string TelemetryServerPath;

        /// <summary>
        /// Initializes a new instance of the <see cref="Configuration"/> class.
        /// </summary>
        protected Configuration()
        {
            this.OutputFilePath = string.Empty;

            this.AssemblyToBeAnalyzed = string.Empty;
            this.TestMethodName = string.Empty;

            this.SchedulingStrategy = "random";
            this.TestingIterations = 1;
            this.TestingTimeout = 0;
            this.RandomGeneratorSeed = null;
            this.IsSchedulingSeedIncremental = false;
            this.IsPartiallyControlledConcurrencyEnabled = true;
            this.IsConcurrencyFuzzingEnabled = false;
            this.IsConcurrencyFuzzingFallbackEnabled = true;
            this.IsLivenessCheckingEnabled = true;
            this.RunTestIterationsToCompletion = false;
            this.MaxUnfairSchedulingSteps = 10000;
            this.MaxFairSchedulingSteps = 100000; // 10 times the unfair steps.
            this.UserExplicitlySetMaxFairSchedulingSteps = false;
            this.ParallelBugFindingTasks = 0;
            this.ParallelDebug = false;
            this.RunAsParallelBugFindingTask = false;
            this.TestingSchedulerEndPoint = "CoyoteTestScheduler.4723bb92-c413-4ecb-8e8a-22eb2ba22234";
            this.TestingSchedulerIpAddress = null;
            this.TestingProcessId = 0;
            this.ConsiderDepthBoundHitAsBug = false;
            this.StrategyBound = 0;
            this.TimeoutDelay = 10;
            this.DeadlockTimeout = 2500;
            this.UncontrolledConcurrencyTimeout = 1;
            this.SafetyPrefixBound = 0;
            this.LivenessTemperatureThreshold = 50000;
            this.UserExplicitlySetLivenessTemperatureThreshold = false;
            this.IsProgramStateHashingEnabled = false;
            this.IsMonitoringEnabledInInProduction = false;
            this.AttachDebugger = false;

            this.ScheduleFile = string.Empty;
            this.ScheduleTrace = string.Empty;

            this.ReportCodeCoverage = false;
            this.IsActivityCoverageReported = false;
            this.DebugActivityCoverage = false;

            this.IsVerbose = false;
            this.IsDebugVerbosityEnabled = false;
            this.LogLevel = LogSeverity.Informational;

            this.AdditionalCodeCoverageAssemblies = new Dictionary<string, bool>();

            this.EnableColoredConsoleOutput = false;
            this.DisableEnvironmentExit = true;
            this.EnableTelemetry = true;

            string optout = Environment.GetEnvironmentVariable("COYOTE_CLI_TELEMETRY_OPTOUT");
            if (optout is "1" || optout is "true")
            {
                this.EnableTelemetry = false;
            }
        }

        /// <summary>
        /// Creates a new configuration with default values.
        /// </summary>
        public static Configuration Create()
        {
            return new Configuration();
        }

        /// <summary>
        /// Updates the configuration to use the random scheduling strategy during systematic testing.
        /// </summary>
        public Configuration WithRandomStrategy()
        {
            this.SchedulingStrategy = "random";
            return this;
        }

        /// <summary>
        /// Updates the configuration to use the probabilistic scheduling strategy during systematic testing.
        /// You can specify a value controlling the probability of each scheduling decision. This value is
        /// specified as the integer N in the equation 0.5 to the power of N. So for N=1, the probability is
        /// 0.5, for N=2 the probability is 0.25, N=3 you get 0.125, etc. By default, this value is 3.
        /// </summary>
        /// <param name="probabilityLevel">The probability level.</param>
        public Configuration WithProbabilisticStrategy(uint probabilityLevel = 3)
        {
            this.SchedulingStrategy = "probabilistic";
            this.StrategyBound = (int)probabilityLevel;
            return this;
        }

        /// <summary>
        /// Updates the configuration to use the PCT scheduling strategy during systematic testing.
        /// You can specify the number of priority switch points, which by default are 10.
        /// </summary>
        /// <param name="isFair">If true, use the fair version of PCT.</param>
        /// <param name="numPrioritySwitchPoints">The nunmber of priority switch points.</param>
        public Configuration WithPCTStrategy(bool isFair, uint numPrioritySwitchPoints = 10)
        {
            this.SchedulingStrategy = isFair ? "fairpct" : "pct";
            this.StrategyBound = (int)numPrioritySwitchPoints;
            return this;
        }

        /// <summary>
        /// Updates the configuration to use the reinforcement learning (RL) scheduling strategy
        /// during systematic testing.
        /// </summary>
        public Configuration WithRLStrategy()
        {
            this.SchedulingStrategy = "rl";
            this.IsProgramStateHashingEnabled = true;
            return this;
        }

        /// <summary>
        /// Updates the configuration to use the dfs scheduling strategy during systematic testing.
        /// </summary>
        internal Configuration WithDFSStrategy()
        {
            this.SchedulingStrategy = "dfs";
            return this;
        }

        /// <summary>
        /// Updates the configuration to use the replay scheduling strategy during systematic testing.
        /// This strategy replays the specified schedule trace to reproduce the same execution.
        /// </summary>
        /// <param name="scheduleTrace">The schedule trace to be replayed.</param>
        public Configuration WithReplayStrategy(string scheduleTrace)
        {
            this.SchedulingStrategy = "replay";
            this.ScheduleTrace = scheduleTrace;
            return this;
        }

        /// <summary>
        /// Updates the configuration with the specified number of iterations to run during systematic testing.
        /// </summary>
        /// <param name="iterations">The number of iterations to run.</param>
        public Configuration WithTestingIterations(uint iterations)
        {
            this.TestingIterations = iterations;
            return this;
        }

        /// <summary>
        /// Updates the configuration with the specified systematic testing timeout in seconds.
        /// </summary>
        /// <param name="timeout">The timeout value in seconds.</param>
        /// <remarks>
        /// Setting this value overrides the <see cref="TestingIterations"/> value.
        /// </remarks>
        public Configuration WithTestingTimeout(int timeout)
        {
            this.TestingTimeout = timeout;
            return this;
        }

        /// <summary>
        /// Updates the configuration with partial controlled concurrency enabled or disabled.
        /// </summary>
        /// <param name="isEnabled">If true, then partial controlled concurrency is enabled.</param>
        public Configuration WithPartiallyControlledConcurrencyEnabled(bool isEnabled = true)
        {
            this.IsPartiallyControlledConcurrencyEnabled = isEnabled;
            return this;
        }

        /// <summary>
        /// Updates the configuration with concurrency fuzzing enabled or disabled.
        /// </summary>
        /// <param name="isEnabled">If true, then concurrency fuzzing is enabled.</param>
        public Configuration WithConcurrencyFuzzingEnabled(bool isEnabled = true)
        {
            this.IsConcurrencyFuzzingEnabled = isEnabled;
            return this;
        }

        /// <summary>
        /// Updates the configuration with concurrency fuzzing fallback enabled or disabled.
        /// </summary>
        /// <param name="isEnabled">If true, then concurrency fuzzing fallback is enabled.</param>
        public Configuration WithConcurrencyFuzzingFallbackEnabled(bool isEnabled = true)
        {
            this.IsConcurrencyFuzzingFallbackEnabled = isEnabled;
            return this;
        }

        /// <summary>
        /// Updates the configuration with the ability to reproduce bug traces enabled or disabled.
        /// Disabling reproducibility allows skipping errors due to uncontrolled concurrency, for
        /// example when the program is only partially rewritten, or there is external concurrency
        /// that is not mocked, or when the program uses an API that is not yet supported.
        /// </summary>
        /// <param name="isDisabled">If true, then reproducing bug traces is disabled.</param>
        public Configuration WithNoBugTraceRepro(bool isDisabled = true)
        {
            this.IsConcurrencyFuzzingEnabled = isDisabled;
            return this;
        }

        /// <summary>
        /// Updates the configuration with the specified number of maximum scheduling steps to explore per
        /// iteration during systematic testing. The <see cref="MaxUnfairSchedulingSteps"/> is assigned the
        /// <paramref name="maxSteps"/> value, whereas the <see cref="MaxFairSchedulingSteps"/> is assigned
        /// a value using the default heuristic, which is 10 * <paramref name="maxSteps"/>.
        /// </summary>
        /// <param name="maxSteps">The maximum scheduling steps to explore per iteration.</param>
        public Configuration WithMaxSchedulingSteps(uint maxSteps)
        {
            this.MaxUnfairSchedulingSteps = (int)maxSteps;
            this.MaxFairSchedulingSteps = 10 * (int)maxSteps;
            return this;
        }

        /// <summary>
        /// Updates the configuration with the specified number of maximum unfair and fair scheduling
        /// steps to explore per iteration during systematic testing. It is recommended to use
        /// <see cref="WithMaxSchedulingSteps(uint)"/> instead of this overloaded method.
        /// </summary>
        /// <param name="maxUnfairSteps">The unfair scheduling steps to explore per iteration.</param>
        /// <param name="maxFairSteps">The fair scheduling steps to explore per iteration.</param>
        public Configuration WithMaxSchedulingSteps(uint maxUnfairSteps, uint maxFairSteps)
        {
            if (maxFairSteps < maxUnfairSteps)
            {
                throw new ArgumentException("The max fair steps cannot not be less than the max unfair steps.", nameof(maxFairSteps));
            }

            this.MaxUnfairSchedulingSteps = (int)maxUnfairSteps;
            this.MaxFairSchedulingSteps = (int)maxFairSteps;
            this.UserExplicitlySetMaxFairSchedulingSteps = true;
            return this;
        }

        /// <summary>
        /// Updates the configuration with the specified liveness temperature threshold during
        /// systematic testing. If this value is 0 it disables liveness checking. It is not
        /// recommended to explicitly set this value, instead use the default value which is
        /// assigned to <see cref="MaxFairSchedulingSteps"/> / 2.
        /// </summary>
        /// <param name="threshold">The liveness temperature threshold.</param>
        public Configuration WithLivenessTemperatureThreshold(uint threshold)
        {
            this.LivenessTemperatureThreshold = (int)threshold;
            this.UserExplicitlySetLivenessTemperatureThreshold = true;
            return this;
        }

        /// <summary>
        /// Updates the value that controls the probability of triggering a timeout each time an
        /// operation gets delayed or a built-in timer gets scheduled during systematic testing.
        /// </summary>
        /// <param name="delay">The timeout delay during testing, which by default is 10.</param>
        /// <remarks>
        /// Increase the value to decrease the probability. This value is not a unit of time.
        /// </remarks>
        public Configuration WithTimeoutDelay(uint delay)
        {
            this.TimeoutDelay = delay;
            return this;
        }

        /// <summary>
        /// Updates the value that controls how much time the deadlock monitor should
        /// wait during concurrency testing before reporting a potential deadlock.
        /// </summary>
        /// <param name="timeout">The timeout value in milliseconds, which by default is 2500.</param>
        /// <remarks>
        /// Increase the value to give more time to the test to resolve a potential deadlock.
        /// </remarks>
        public Configuration WithDeadlockTimeout(uint timeout)
        {
            this.DeadlockTimeout = timeout;
            return this;
        }

        /// <summary>
        /// Updates the value that controls how much time the runtime should wait for
        /// uncontrolled concurrency to resolve before continuing exploration.
        /// </summary>
        /// <param name="timeout">The timeout value in milliseconds, which by default is 1.</param>
        /// <remarks>
        /// Increase the value to give more time to try resolve uncontrolled concurrency.
        /// </remarks>
        public Configuration WithUncontrolledConcurrencyTimeout(uint timeout)
        {
            this.UncontrolledConcurrencyTimeout = timeout;
            return this;
        }

        /// <summary>
        /// Updates the seed used by the random value generator during systematic testing.
        /// </summary>
        /// <param name="seed">The seed used by the random value generator.</param>
        public Configuration WithRandomGeneratorSeed(uint seed)
        {
            this.RandomGeneratorSeed = seed;
            return this;
        }

        /// <summary>
        /// Updates the configuration with incremental seed generation enabled or disabled.
        /// </summary>
        /// <param name="isIncremental">If true, then incremental seed generation is used.</param>
        public Configuration WithIncrementalSeedGenerationEnabled(bool isIncremental = true)
        {
            this.IsSchedulingSeedIncremental = isIncremental;
            return this;
        }

        /// <summary>
        /// Updates the configuration so that the tester continues running test iterations
        /// up to a bound, even if a bug is already found.
        /// </summary>
        /// <param name="runToCompletion">
        /// If true, the tester runs all iterations up to a bound, even if a bug is found.
        /// </param>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public Configuration WithTestIterationsRunToCompletion(bool runToCompletion = true)
        {
            this.RunTestIterationsToCompletion = runToCompletion;
            return this;
        }

        /// <summary>
        /// Updates the configuration with verbose output enabled or disabled.
        /// </summary>
        /// <param name="isVerbose">If true, then messages are logged.</param>
        /// <param name="logLevel">The level of detail to provide in verbose logging.</param>
        public Configuration WithVerbosityEnabled(bool isVerbose = true, LogSeverity logLevel = LogSeverity.Informational)
        {
            this.IsVerbose = isVerbose;
            this.LogLevel = logLevel;
            return this;
        }

        /// <summary>
        /// Updates the configuration with debug logging enabled or disabled.
        /// </summary>
        /// <param name="isDebugLoggingEnabled">If true, then debug messages are logged.</param>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public Configuration WithDebugLoggingEnabled(bool isDebugLoggingEnabled = true)
        {
            this.IsDebugVerbosityEnabled = isDebugLoggingEnabled;
            return this;
        }

        /// <summary>
        /// Updates the configuration to enable or disable reporting activity coverage.
        /// </summary>
        /// <param name="isEnabled">If true, then enables activity coverage.</param>
        public Configuration WithActivityCoverageReported(bool isEnabled = true)
        {
            this.IsActivityCoverageReported = isEnabled;
            return this;
        }

        /// <summary>
        /// Updates the configuration with DGML graph generation enabled or disabled.
        /// </summary>
        /// <param name="isEnabled">If true, then enables DGML graph generation.</param>
        public Configuration WithDgmlGraphEnabled(bool isEnabled = true)
        {
            this.IsDgmlGraphEnabled = isEnabled;
            return this;
        }

        /// <summary>
        /// Updates the configuration with XML log generation enabled or disabled.
        /// </summary>
        /// <param name="isEnabled">If true, then enables XML log generation.</param>
        public Configuration WithXmlLogEnabled(bool isEnabled = true)
        {
            this.IsXmlLogEnabled = isEnabled;
            return this;
        }

        /// <summary>
        /// Updates the configuration with telemetry enabled or disabled.
        /// </summary>
        public Configuration WithTelemetryEnabled(bool isEnabled = true)
        {
            this.EnableTelemetry = isEnabled;
            return this;
        }

        /// <summary>
        /// Enable running of Monitor objects in production.
        /// </summary>
        internal Configuration WithProductionMonitorEnabled(bool isEnabled = true)
        {
            this.IsMonitoringEnabledInInProduction = isEnabled;
            return this;
        }
    }
#pragma warning restore CA1724 // Type names should not match namespaces
}
