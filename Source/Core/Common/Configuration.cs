// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Microsoft.Coyote.Runtime.Exploration;

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
        /// The output path.
        /// </summary>
        [DataMember]
        public string OutputFilePath;

        /// <summary>
        /// Timeout in seconds.
        /// </summary>
        [DataMember]
        public int Timeout;

        /// <summary>
        /// The current runtime generation.
        /// </summary>
        [DataMember]
        public ulong RuntimeGeneration;

        /// <summary>
        /// The assembly to be analyzed for bugs.
        /// </summary>
        [DataMember]
        public string AssemblyToBeAnalyzed;

        /// <summary>
        /// The assembly that contains the testing runtime.
        /// By default it is empty, which uses the default
        /// testing runtime of Coyote.
        /// </summary>
        [DataMember]
        public string TestingRuntimeAssembly;

        /// <summary>
        /// Test method to be used.
        /// </summary>
        [DataMember]
        public string TestMethodName;

        /// <summary>
        /// Scheduling strategy to use with the Coyote tester.
        /// </summary>
        [DataMember]
        public SchedulingStrategy SchedulingStrategy;

        /// <summary>
        /// Number of scheduling iterations.
        /// </summary>
        [DataMember]
        public int SchedulingIterations;

        /// <summary>
        /// Seed for random scheduling strategies.
        /// </summary>
        [DataMember]
        public int RandomSchedulingSeed;

        /// <summary>
        /// If true, the seed will increment in each
        /// testing iteration.
        /// </summary>
        [DataMember]
        public bool IncrementalSchedulingSeed;

        /// <summary>
        /// If true, the Coyote tester performs a full exploration,
        /// and does not stop when it finds a bug.
        /// </summary>
        [DataMember]
        public bool PerformFullExploration;

        /// <summary>
        /// The maximum scheduling steps to explore
        /// for fair schedulers.
        /// By default there is no bound.
        /// </summary>
        [DataMember]
        public int MaxFairSchedulingSteps;

        /// <summary>
        /// The maximum scheduling steps to explore
        /// for unfair schedulers.
        /// By default there is no bound.
        /// </summary>
        [DataMember]
        public int MaxUnfairSchedulingSteps;

        /// <summary>
        /// The maximum scheduling steps to explore
        /// for both fair and unfair schedulers.
        /// By default there is no bound.
        /// </summary>
        public int MaxSchedulingSteps
        {
            set
            {
                this.MaxUnfairSchedulingSteps = value;
                this.MaxFairSchedulingSteps = value;
            }
        }

        /// <summary>
        /// The user-specified command to perform by the Coyote tool.
        /// </summary>
        [DataMember]
        public string ToolCommand;

        /// <summary>
        /// True if the user has explicitly set the
        /// fair scheduling steps bound.
        /// </summary>
        [DataMember]
        public bool UserExplicitlySetMaxFairSchedulingSteps;

        /// <summary>
        /// Number of parallel bug-finding tasks.
        /// By default it is 1 task.
        /// </summary>
        [DataMember]
        public uint ParallelBugFindingTasks;

        /// <summary>
        /// Put a debug prompt at the beginning of each child TestProcess.
        /// </summary>
        [DataMember]
        public bool ParallelDebug;

        /// <summary>
        /// Specify ip address if you want to use something other than localhost.
        /// </summary>
        [DataMember]
        public string TestingSchedulerIpAddress;

        /// <summary>
        /// Do not automatically launch the TestingProcesses in parallel mode, instead wait for them
        /// to be launched independently.
        /// </summary>
        [DataMember]
        public bool WaitForTestingProcesses;

        /// <summary>
        /// Runs this process as a parallel bug-finding task.
        /// </summary>
        [DataMember]
        public bool RunAsParallelBugFindingTask;

        /// <summary>
        /// The testing scheduler unique endpoint.
        /// </summary>
        [DataMember]
        public string TestingSchedulerEndPoint;

        /// <summary>
        /// The unique testing process id.
        /// </summary>
        [DataMember]
        public uint TestingProcessId;

        /// <summary>
        /// If true, then the Coyote tester will consider an execution
        /// that hits the depth bound as buggy.
        /// </summary>
        [DataMember]
        public bool ConsiderDepthBoundHitAsBug;

        /// <summary>
        /// The priority switch bound. By default it is 2.
        /// Used by priority-based schedulers.
        /// </summary>
        [DataMember]
        public int PrioritySwitchBound;

        /// <summary>
        /// Coin-flip bound. By default it is 2.
        /// </summary>
        [DataMember]
        public int CoinFlipBound;

        /// <summary>
        /// The timeout delay used during testing. By default it is 1.
        /// Increase to the make timeouts less frequent.
        /// </summary>
        [DataMember]
        public uint TimeoutDelay;

        /// <summary>
        /// Safety prefix bound. By default it is 0.
        /// </summary>
        [DataMember]
        public int SafetyPrefixBound;

        /// <summary>
        /// If this option is enabled, liveness checking is enabled during bug-finding.
        /// </summary>
        [DataMember]
        public bool IsLivenessCheckingEnabled;

        /// <summary>
        /// The liveness temperature threshold. If it is 0
        /// then it is disabled.
        /// </summary>
        [DataMember]
        public int LivenessTemperatureThreshold;

        /// <summary>
        /// If this option is enabled, the tester is hashing the program state.
        /// </summary>
        [DataMember]
        public bool IsProgramStateHashingEnabled;

        /// <summary>
        /// If this option is enabled, (safety) monitors are used in the production runtime.
        /// </summary>
        [DataMember]
        public bool IsMonitoringEnabledInInProduction;

        /// <summary>
        /// Attaches the debugger during trace replay.
        /// </summary>
        [DataMember]
        public bool AttachDebugger;

        /// <summary>
        /// The schedule file to be replayed.
        /// </summary>
        public string ScheduleFile;

        /// <summary>
        /// The schedule trace to be replayed.
        /// </summary>
        internal string ScheduleTrace;

        /// <summary>
        /// Enables code coverage reporting of a Coyote program.
        /// </summary>
        [DataMember]
        public bool ReportCodeCoverage;

        /// <summary>
        /// Enables activity coverage reporting of a Coyote program.
        /// </summary>
        [DataMember]
        public bool ReportActivityCoverage;

        /// <summary>
        /// Enables activity coverage debugging.
        /// </summary>
        public bool DebugActivityCoverage;

        /// <summary>
        /// Additional assembly specifications to instrument for code coverage, besides those in the
        /// dependency graph between <see cref="AssemblyToBeAnalyzed"/> and the Microsoft.Coyote DLLs.
        /// Key is filename, value is whether it is a list file (true) or a single file (false).
        /// </summary>
        public Dictionary<string, bool> AdditionalCodeCoverageAssemblies = new Dictionary<string, bool>();

        /// <summary>
        /// If true, then messages are logged.
        /// </summary>
        [DataMember]
        public bool IsVerbose;

        /// <summary>
        /// If specified, requests a DGML graph of the iteration that contains a bug, if a bug is found.
        /// This is different from a coverage activity graph, as it will also show actor instances.
        /// </summary>
        [DataMember]
        public bool IsDgmlGraphEnabled;

        /// <summary>
        /// Produce an XML formatted runtime log file.
        /// </summary>
        [DataMember]
        public bool IsXmlLogEnabled { get; internal set; }

        /// <summary>
        /// Is DGML graph showing all test iterations or just one "bug" iteration.
        /// False means all, and True means only the iteration containing a bug.
        /// </summary>
        [DataMember]
        public bool IsDgmlBugGraph;

        /// <summary>
        /// If specified, requests a custom runtime log to be used instead of the default.
        /// This is the AssemblyQualifiedName of the type to load.
        /// </summary>
        [DataMember]
        public string CustomActorRuntimeLogType;

        /// <summary>
        /// Enables debugging.
        /// </summary>
        [DataMember]
        public bool EnableDebugging;

        /// <summary>
        /// Enables profiling.
        /// </summary>
        [DataMember]
        public bool EnableProfiling;

        /// <summary>
        /// Enables colored console output.
        /// </summary>
        public bool EnableColoredConsoleOutput;

        /// <summary>
        /// If true, then environment exit will be disabled.
        /// </summary>
        internal bool DisableEnvironmentExit;

        /// <summary>
        /// Initializes a new instance of the <see cref="Configuration"/> class.
        /// </summary>
        protected Configuration()
        {
            this.OutputFilePath = string.Empty;

            this.Timeout = 0;
            this.RuntimeGeneration = 0;

            this.AssemblyToBeAnalyzed = string.Empty;
            this.TestingRuntimeAssembly = string.Empty;
            this.TestMethodName = string.Empty;

            this.SchedulingStrategy = SchedulingStrategy.Random;
            this.SchedulingIterations = 1;
            this.RandomSchedulingSeed = DateTime.Now.Millisecond;
            this.IncrementalSchedulingSeed = false;

            this.PerformFullExploration = false;
            this.MaxFairSchedulingSteps = 0;
            this.MaxUnfairSchedulingSteps = 0;
            this.UserExplicitlySetMaxFairSchedulingSteps = false;
            this.ParallelBugFindingTasks = 0;
            this.ParallelDebug = false;
            this.RunAsParallelBugFindingTask = false;
            this.TestingSchedulerEndPoint = "CoyoteTestScheduler.4723bb92-c413-4ecb-8e8a-22eb2ba22234";
            this.TestingSchedulerIpAddress = null;
            this.TestingProcessId = 0;
            this.ConsiderDepthBoundHitAsBug = false;
            this.PrioritySwitchBound = 0;
            this.CoinFlipBound = 0;
            this.TimeoutDelay = 1;
            this.SafetyPrefixBound = 0;

            this.IsLivenessCheckingEnabled = true;
            this.LivenessTemperatureThreshold = 0;
            this.IsProgramStateHashingEnabled = false;
            this.IsMonitoringEnabledInInProduction = false;
            this.AttachDebugger = false;

            this.ScheduleFile = string.Empty;
            this.ScheduleTrace = string.Empty;

            this.ReportCodeCoverage = false;
            this.ReportActivityCoverage = false;
            this.DebugActivityCoverage = false;

            this.IsVerbose = false;
            this.EnableDebugging = false;
            this.EnableProfiling = false;

            this.EnableColoredConsoleOutput = false;
            this.DisableEnvironmentExit = true;
        }

        /// <summary>
        /// Creates a new configuration with default values.
        /// </summary>
        public static Configuration Create()
        {
            return new Configuration();
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
        /// Updates the configuration with verbose output enabled or disabled.
        /// </summary>
        /// <param name="level">The verbosity level.</param>
        [Obsolete("WithVerbosityEnabled(int level) is deprecated; use WithVerbosityEnabled(bool isVerbose) instead.")]
        public Configuration WithVerbosityEnabled(int level)
        {
            this.IsVerbose = level > 0;
            return this;
        }

        /// <summary>
        /// Updates the configuration with the specified scheduling strategy.
        /// </summary>
        /// <param name="strategy">The scheduling strategy.</param>
        public Configuration WithStrategy(SchedulingStrategy strategy)
        {
            this.SchedulingStrategy = strategy;
            return this;
        }

        /// <summary>
        /// Updates the configuration with the specified number of iterations to perform.
        /// </summary>
        /// <param name="iterations">The number of iterations to perform.</param>
        public Configuration WithNumberOfIterations(int iterations)
        {
            this.SchedulingIterations = iterations;
            return this;
        }

        /// <summary>
        /// Updates the configuration with the specified number of scheduling steps
        /// to perform per iteration (for both fair and unfair schedulers).
        /// </summary>
        /// <param name="maxSteps">The scheduling steps to perform per iteration.</param>
        public Configuration WithMaxSteps(int maxSteps)
        {
            this.MaxSchedulingSteps = maxSteps;
            return this;
        }
    }
#pragma warning restore CA1724 // Type names should not match namespaces
}
