// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

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
        /// The output path.
        /// </summary>
        [DataMember]
        internal string OutputFilePath;

        /// <summary>
        /// Timeout in seconds.
        /// </summary>
        [DataMember]
        internal int Timeout;

        /// <summary>
        /// The current runtime generation.
        /// </summary>
        [DataMember]
        internal ulong RuntimeGeneration;

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
        public int TestingIterations { get; internal set; }

        /// <summary>
        /// Custom seed to be used by the random value generator. By default,
        /// this value is null indicating that no seed has been set.
        /// </summary>
        [DataMember]
        public uint? RandomGeneratorSeed { get; internal set; }

        /// <summary>
        /// If true, the seed will increment in each
        /// testing iteration.
        /// </summary>
        [DataMember]
        internal bool IncrementalSchedulingSeed;

        /// <summary>
        /// If true, the Coyote tester performs a full exploration,
        /// and does not stop when it finds a bug.
        /// </summary>
        [DataMember]
        internal bool PerformFullExploration;

        /// <summary>
        /// The maximum scheduling steps to explore for fair schedulers.
        /// By default this is set to 100,000 steps.
        /// </summary>
        [DataMember]
        public int MaxFairSchedulingSteps { get; internal set; }

        /// <summary>
        /// The maximum scheduling steps to explore for unfair schedulers.
        /// By default this is set to 10,000 steps.
        /// </summary>
        [DataMember]
        public int MaxUnfairSchedulingSteps { get; internal set; }

        /// <summary>
        /// The maximum scheduling steps to explore
        /// for both fair and unfair schedulers.
        /// By default there is no bound.
        /// </summary>
        internal int MaxSchedulingSteps
        {
            set
            {
                this.MaxUnfairSchedulingSteps = this.MaxFairSchedulingSteps = value;
            }
        }

        /// <summary>
        /// True if the user has explicitly set the
        /// fair scheduling steps bound.
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
        public int StrategyBound { get; internal set; }

        /// <summary>
        /// Value that controls the probability of triggering a timeout each time a built-in timer
        /// is scheduled during systematic testing. Decrease the value to increase the frequency of
        /// timeouts (e.g. a value of 1 corresponds to a 50% probability), or increase the value to
        /// decrease the frequency (e.g. a value of 10 corresponds to a 10% probability). By default
        /// this value is 10.
        /// </summary>
        [DataMember]
        public uint TimeoutDelay { get; internal set; }

        /// <summary>
        /// Safety prefix bound. By default it is 0.
        /// </summary>
        [DataMember]
        internal int SafetyPrefixBound;

        /// <summary>
        /// If this option is enabled, liveness checking is enabled during bug-finding.
        /// </summary>
        [DataMember]
        internal bool IsLivenessCheckingEnabled;

        /// <summary>
        /// The liveness temperature threshold. If it is 0
        /// then it is disabled.
        /// </summary>
        [DataMember]
        public int LivenessTemperatureThreshold { get; internal set; }

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
        /// Enables code coverage reporting of a Coyote program.
        /// </summary>
        [DataMember]
        internal bool ReportCodeCoverage;

        /// <summary>
        /// Enables activity coverage reporting of a Coyote program.
        /// </summary>
        [DataMember]
        public bool ReportActivityCoverage { get; internal set; }

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
        public bool IsDgmlGraphEnabled { get; internal set; }

        /// <summary>
        /// Produce an XML formatted runtime log file.
        /// </summary>
        [DataMember]
        public bool IsXmlLogEnabled { get; internal set; }

        /// <summary>
        /// If specified, requests a custom runtime log to be used instead of the default.
        /// This is the AssemblyQualifiedName of the type to load.
        /// </summary>
        [DataMember]
        internal string CustomActorRuntimeLogType;

        /// <summary>
        /// Enables debugging.
        /// </summary>
        [DataMember]
        internal bool EnableDebugging;

        /// <summary>
        /// Number of parallel bug-finding tasks.
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
        /// Runs this process as a parallel bug-finding task.
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
        /// Initializes a new instance of the <see cref="Configuration"/> class.
        /// </summary>
        protected Configuration()
        {
            this.Timeout = this.RuntimeGeneration = this.ParallelBugFindingTasks= this.TestingProcessId = this.StrategyBound = this.SafetyPrefixBound = this.LivenessTemperatureThreshold =  0;
            this.OutputFilePath = this.AssemblyToBeAnalyzed = this.TestMethodName = this.ScheduleFile = this.ScheduleTrace = string.Empty;
            this.SchedulingStrategy = "random";
            this.TestingIterations = 1;
            this.RandomGeneratorSeed = this.TestingSchedulerIpAddress = null;
            this.IncrementalSchedulingSeed = this.PerformFullExploration  = this.UserExplicitlySetMaxFairSchedulingSteps = this.ParallelDebug = this.ConsiderDepthBoundHitAsBug = this.IsProgramStateHashingEnabled =  this.DebugActivityCoverage = this.IsVerbose = this.EnableDebugging = this.EnableColoredConsoleOutput = false;
            this.MaxUnfairSchedulingSteps = 10000;
            this.MaxFairSchedulingSteps = this.MaxUnfairSchedulingSteps * 10;
            this.RunAsParallelBugFindingTask = this.IsMonitoringEnabledInInProduction = this.AttachDebugger = this.ReportCodeCoverage = this.ReportActivityCoverage =  false;
            this.TestingSchedulerEndPoint = "CoyoteTestScheduler.4723bb92-c413-4ecb-8e8a-22eb2ba22234";
            this.TimeoutDelay = 10;  
            this.IsLivenessCheckingEnabled = this.DisableEnvironmentExit = true;
            this.AdditionalCodeCoverageAssemblies = new Dictionary<string, bool>();
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
            this.SchedulingStrategy = "fairpct";
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
        /// Updates the configuration to use the dfs scheduling strategy during systematic testing.
        /// </summary>
        internal Configuration WithDFSStrategy()
        {
            this.SchedulingStrategy = "dfs";
            return this;
        }

        /// <summary>
        /// Updates the configuration with the specified number of iterations to run during systematic testing.
        /// </summary>
        /// <param name="iterations">The number of iterations to run.</param>
        public Configuration WithTestingIterations(uint iterations)
        {
            this.TestingIterations = (int)iterations;
            return this;
        }

        /// <summary>
        /// Updates the configuration with the specified number of scheduling steps to explore per iteration
        /// (for both fair and unfair schedulers) during systematic testing.
        /// </summary>
        /// <param name="maxSteps">The scheduling steps to explore per iteration.</param>
        public Configuration WithMaxSchedulingSteps(uint maxSteps)
        {
            this.MaxSchedulingSteps = (int)maxSteps;
            return this;
        }

        /// <summary>
        /// Updates the configuration with the specified number of fair scheduling steps to explore
        /// per iteration during systematic testing.
        /// </summary>
        /// <param name="maxFairSteps">The scheduling steps to explore per iteration.</param>
        public Configuration WithMaxFairSchedulingSteps(uint maxFairSteps)
        {
            this.MaxFairSchedulingSteps = (int)maxFairSteps;
            return this;
        }

        /// <summary>
        /// Updates the configuration with the specified number of unfair scheduling steps to explore
        /// per iteration during systematic testing.
        /// </summary>
        /// <param name="maxUnfairSteps">The scheduling steps to explore per iteration.</param>
        public Configuration WithMaxUnfairSchedulingSteps(uint maxUnfairSteps)
        {
            this.MaxUnfairSchedulingSteps = (int)maxUnfairSteps;
            return this;
        }

        /// <summary>
        /// Updates the configuration with the specified liveness temperature threshold during systematic testing.
        /// If this value is 0 it disables liveness checking.
        /// </summary>
        /// <param name="threshold">The liveness temperature threshold.</param>
        public Configuration WithLivenessTemperatureThreshold(uint threshold)
        {
            this.LivenessTemperatureThreshold = (int)threshold;
            return this;
        }

        /// <summary>
        /// Updates the <see cref="TimeoutDelay"/> value that controls the probability of triggering
        /// a timeout each time a built-in timer is scheduled during systematic testing. This value
        /// is not a unit of time.
        /// </summary>
        /// <param name="timeoutDelay">The timeout delay during testing.</param>
        public Configuration WithTimeoutDelay(uint timeoutDelay)
        {
            this.TimeoutDelay = timeoutDelay;
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
        /// Updates the configuration with verbose output enabled or disabled.
        /// </summary>
        /// <param name="isVerbose">If true, then messages are logged.</param>
        public Configuration WithVerbosityEnabled(bool isVerbose = true)
        {
            this.IsVerbose = isVerbose;
            return this;
        }

        /// <summary>
        /// Updates the configuration with activity coverage enabled or disabled.
        /// </summary>
        /// <param name="isEnabled">If true, then enables activity coverage.</param>
        public Configuration WithActivityCoverageEnabled(bool isEnabled = true)
        {
            this.ReportActivityCoverage = isEnabled;
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
    }
#pragma warning restore CA1724 // Type names should not match namespaces
}
