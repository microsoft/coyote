// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
#if NET46 || NET47
using System.Configuration;
#endif
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Coyote.IO;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.Runtime.Exploration;
using Microsoft.Coyote.TestingServices.Coverage;
using Microsoft.Coyote.TestingServices.Runtime;
using Microsoft.Coyote.TestingServices.Scheduling.Strategies;
using Microsoft.Coyote.TestingServices.Tracing;
using Microsoft.Coyote.Utilities;

namespace Microsoft.Coyote.TestingServices
{
    /// <summary>
    /// The Coyote abstract testing engine.
    /// </summary>
    [DebuggerStepThrough]
    internal abstract class AbstractTestingEngine : ITestingEngine
    {
        /// <summary>
        /// Configuration.
        /// </summary>
        internal readonly Configuration Configuration;

        /// <summary>
        /// The method to test.
        /// </summary>
        internal readonly TestMethodInfo TestMethodInfo;

        /// <summary>
        /// Set of callbacks to invoke at the end
        /// of each iteration.
        /// </summary>
        protected readonly ISet<Action<int>> PerIterationCallbacks;

        /// <summary>
        /// The bug-finding scheduling strategy.
        /// </summary>
        protected readonly ISchedulingStrategy Strategy;

        /// <summary>
        /// Random value generator used by the scheduling strategies.
        /// </summary>
        protected readonly IRandomValueGenerator RandomValueGenerator;

        /// <summary>
        /// The error reporter.
        /// </summary>
        protected readonly ErrorReporter ErrorReporter;

        /// <summary>
        /// The installed logger.
        /// </summary>
        /// <remarks>
        /// See <see href="/coyote/learn/advanced/logging" >Logging</see> for more information.
        /// </remarks>
        protected TextWriter Logger;

        /// <summary>
        /// The profiler.
        /// </summary>
        protected readonly Profiler Profiler;

        /// <summary>
        /// The testing task cancellation token source.
        /// </summary>
        protected readonly CancellationTokenSource CancellationTokenSource;

        /// <summary>
        /// Data structure containing information
        /// gathered during testing.
        /// </summary>
        public TestReport TestReport { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AbstractTestingEngine"/> class.
        /// </summary>
        protected AbstractTestingEngine(Configuration configuration, TestMethodInfo testMethodInfo)
        {
            this.Configuration = configuration;
            this.TestMethodInfo = testMethodInfo;

            this.Logger = new ConsoleLogger();
            this.ErrorReporter = new ErrorReporter(configuration, this.Logger);
            this.Profiler = new Profiler();

            this.PerIterationCallbacks = new HashSet<Action<int>>();

            // Initializes scheduling strategy specific components.
            this.RandomValueGenerator = new RandomValueGenerator(configuration);

            this.TestReport = new TestReport(configuration);
            this.CancellationTokenSource = new CancellationTokenSource();

            if (configuration.SchedulingStrategy == SchedulingStrategy.Interactive)
            {
                configuration.SchedulingIterations = 1;
                configuration.PerformFullExploration = false;
                configuration.IsVerbose = true;
                this.Strategy = new InteractiveStrategy(configuration, this.Logger);
            }
            else if (configuration.SchedulingStrategy == SchedulingStrategy.Replay)
            {
                var scheduleDump = this.GetScheduleForReplay(out bool isFair);
                ScheduleTrace schedule = new ScheduleTrace(scheduleDump);
                this.Strategy = new ReplayStrategy(configuration, schedule, isFair);
            }
            else if (configuration.SchedulingStrategy == SchedulingStrategy.Random)
            {
                this.Strategy = new RandomStrategy(configuration.MaxFairSchedulingSteps, this.RandomValueGenerator);
            }
            else if (configuration.SchedulingStrategy == SchedulingStrategy.PCT)
            {
                this.Strategy = new PCTStrategy(configuration.MaxUnfairSchedulingSteps, configuration.PrioritySwitchBound,
                    this.RandomValueGenerator);
            }
            else if (configuration.SchedulingStrategy == SchedulingStrategy.FairPCT)
            {
                var prefixLength = configuration.SafetyPrefixBound == 0 ?
                    configuration.MaxUnfairSchedulingSteps : configuration.SafetyPrefixBound;
                var prefixStrategy = new PCTStrategy(prefixLength, configuration.PrioritySwitchBound, this.RandomValueGenerator);
                var suffixStrategy = new RandomStrategy(configuration.MaxFairSchedulingSteps, this.RandomValueGenerator);
                this.Strategy = new ComboStrategy(prefixStrategy, suffixStrategy);
            }
            else if (configuration.SchedulingStrategy == SchedulingStrategy.ProbabilisticRandom)
            {
                this.Strategy = new ProbabilisticRandomStrategy(
                    configuration.MaxFairSchedulingSteps,
                    configuration.CoinFlipBound,
                    this.RandomValueGenerator);
            }
            else if (configuration.SchedulingStrategy == SchedulingStrategy.DFS)
            {
                this.Strategy = new DFSStrategy(configuration.MaxUnfairSchedulingSteps);
            }
            else if (configuration.SchedulingStrategy == SchedulingStrategy.Portfolio)
            {
                Error.ReportAndExit("Portfolio testing strategy is only " +
                    "available in parallel testing.");
            }

            if (configuration.SchedulingStrategy != SchedulingStrategy.Replay &&
                configuration.ScheduleFile.Length > 0)
            {
                var scheduleDump = this.GetScheduleForReplay(out bool isFair);
                ScheduleTrace schedule = new ScheduleTrace(scheduleDump);
                this.Strategy = new ReplayStrategy(configuration, schedule, isFair, this.Strategy);
            }
        }

        /// <summary>
        /// Take care of handling the <see cref="Configuration"/> settings for <see cref="Configuration.CustomActorRuntimeLogType"/>,
        /// <see cref="Configuration.IsDgmlGraphEnabled"/>, and <see cref="Configuration.ReportActivityCoverage"/> by setting up the
        /// LogWriters on the given <see cref="SystematicTestingRuntime"/> object.
        /// </summary>
        protected void InitializeCustomLogging(SystematicTestingRuntime runtime)
        {
            if (!string.IsNullOrEmpty(this.Configuration.CustomActorRuntimeLogType))
            {
                var log = this.Activate<IActorRuntimeLog>(this.Configuration.CustomActorRuntimeLogType);
                if (log != null)
                {
                    runtime.RegisterLog(log);
                }
            }

            if (this.Configuration.IsDgmlGraphEnabled || this.Configuration.ReportActivityCoverage)
            {
                // Registers an activity coverage graph builder.
                runtime.RegisterLog(new ActorRuntimeLogGraphBuilder(false)
                {
                    CollapseMachineInstances = this.Configuration.ReportActivityCoverage
                });
            }

            if (this.Configuration.ReportActivityCoverage)
            {
                // Need this additional logger to get the event coverage report correct
                runtime.RegisterLog(new ActorRuntimeLogEventCoverage());
            }
        }

        private T Activate<T>(string assemblyQualifiedName)
            where T : class
        {
            // Parses the result of Type.AssemblyQualifiedName.
            // e.g.: ConsoleApp1.Program, ConsoleApp1, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
            try
            {
                string[] parts = assemblyQualifiedName.Split(',');
                if (parts.Length > 1)
                {
                    string typeName = parts[0];
                    string assemblyName = parts[1];
                    Assembly a = null;
                    if (File.Exists(assemblyName))
                    {
                        a = Assembly.LoadFrom(assemblyName);
                    }
                    else
                    {
                        a = Assembly.Load(assemblyName);
                    }

                    if (a != null)
                    {
                        object o = a.CreateInstance(typeName);
                        return o as T;
                    }
                }
            }
            catch (Exception ex)
            {
                this.Logger.WriteLine(ex.Message);
            }

            return null;
        }

        /// <summary>
        /// Runs the testing engine.
        /// </summary>
        public ITestingEngine Run()
        {
            try
            {
                Task task = this.CreateTestingTask();
                if (this.Configuration.Timeout > 0)
                {
                    this.CancellationTokenSource.CancelAfter(
                        this.Configuration.Timeout * 1000);
                }

                this.Profiler.StartMeasuringExecutionTime();
                if (!this.CancellationTokenSource.IsCancellationRequested)
                {
                    task.Start();
                    task.Wait(this.CancellationTokenSource.Token);
                }
            }
            catch (OperationCanceledException)
            {
                if (this.CancellationTokenSource.IsCancellationRequested)
                {
                    this.Logger.WriteLine($"... Task {this.Configuration.TestingProcessId} timed out.");
                }
            }
            catch (AggregateException aex)
            {
                aex.Handle((ex) =>
                {
                    IO.Debug.WriteLine(ex.Message);
                    IO.Debug.WriteLine(ex.StackTrace);
                    return true;
                });

                if (aex.InnerException is FileNotFoundException)
                {
                    Error.ReportAndExit($"{aex.InnerException.Message}");
                }

                Error.ReportAndExit("Exception thrown during testing outside the context of an actor, " +
                    "possibly in a test method. Please use /debug /v:2 to print more information.");
            }
            catch (Exception ex)
            {
                this.Logger.WriteLine($"... Task {this.Configuration.TestingProcessId} failed due to an internal error: {ex}");
                this.TestReport.InternalErrors.Add(ex.ToString());
            }
            finally
            {
                this.Profiler.StopMeasuringExecutionTime();
            }

            return this;
        }

        /// <summary>
        /// Creates a new testing task.
        /// </summary>
        protected abstract Task CreateTestingTask();

        /// <summary>
        /// Stops the testing engine.
        /// </summary>
        public void Stop()
        {
            this.CancellationTokenSource.Cancel();
        }

        /// <summary>
        /// Returns a report with the testing results.
        /// </summary>
        public abstract string GetReport();

        /// <summary>
        /// Tries to emit the testing traces, if any.
        /// </summary>
        public virtual IEnumerable<string> TryEmitTraces(string directory, string file) => Array.Empty<string>();

        /// <summary>
        /// Registers a callback to invoke at the end of each iteration. The callback takes as
        /// a parameter an integer representing the current iteration.
        /// </summary>
        public void RegisterPerIterationCallBack(Action<int> callback)
        {
            this.PerIterationCallbacks.Add(callback);
        }

        /// <summary>
        /// Returns the schedule to replay.
        /// </summary>
        private string[] GetScheduleForReplay(out bool isFair)
        {
            string[] scheduleDump;
            if (this.Configuration.ScheduleTrace.Length > 0)
            {
                scheduleDump = this.Configuration.ScheduleTrace.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
            }
            else
            {
                scheduleDump = File.ReadAllLines(this.Configuration.ScheduleFile);
            }

            isFair = false;
            foreach (var line in scheduleDump)
            {
                if (!line.StartsWith("--"))
                {
                    break;
                }

                if (line.Equals("--fair-scheduling"))
                {
                    isFair = true;
                }
                else if (line.StartsWith("--liveness-temperature-threshold:"))
                {
                    this.Configuration.LivenessTemperatureThreshold =
                        int.Parse(line.Substring("--liveness-temperature-threshold:".Length));
                }
                else if (line.StartsWith("--test-method:"))
                {
                    this.Configuration.TestMethodName =
                        line.Substring("--test-method:".Length);
                }
            }

            return scheduleDump;
        }

        /// <summary>
        /// Loads and returns the specified assembly.
        /// </summary>
        protected static Assembly LoadAssembly(string assemblyFile)
        {
            Assembly assembly = null;

            try
            {
                assembly = Assembly.LoadFrom(assemblyFile);
            }
            catch (FileNotFoundException ex)
            {
                Error.ReportAndExit(ex.Message);
            }

#if NET46 || NET47
            // Load config file and absorb its settings.
            try
            {
                var configFile = ConfigurationManager.OpenExeConfiguration(assemblyFile);

                var settings = configFile.AppSettings.Settings;
                foreach (var key in settings.AllKeys)
                {
                    if (ConfigurationManager.AppSettings.Get(key) is null)
                    {
                        ConfigurationManager.AppSettings.Set(key, settings[key].Value);
                    }
                    else
                    {
                        ConfigurationManager.AppSettings.Add(key, settings[key].Value);
                    }
                }
            }
            catch (ConfigurationErrorsException ex)
            {
                Error.Report(ex.Message);
            }
#endif

            return assembly;
        }

        /// <summary>
        /// Installs the specified <see cref="TextWriter"/>.
        /// </summary>
        public void SetLogger(TextWriter logger)
        {
            this.Logger.Dispose();

            if (logger is null)
            {
                this.Logger = TextWriter.Null;
            }
            else
            {
                this.Logger = logger;
            }

            this.ErrorReporter.Logger = logger;
        }
    }
}
