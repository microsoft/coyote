// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Actors.Coverage;
using Microsoft.Coyote.IO;
using Microsoft.Coyote.Rewriting;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.Telemetry;

namespace Microsoft.Coyote.SystematicTesting
{
    /// <summary>
    /// Testing engine that can run a controlled concurrency test using
    /// a specified configuration.
    /// </summary>
#if !DEBUG
    [DebuggerStepThrough]
#endif
    public sealed class TestingEngine
    {
        /// <summary>
        /// Url with information about the rewriting process.
        /// </summary>
        private const string LearnAboutRewritingUrl = "https://aka.ms/coyote-rewrite";

        /// <summary>
        /// Url with information about the gathered telemetry.
        /// </summary>
        private const string LearnAboutTelemetryUrl = "https://aka.ms/coyote-telemetry";

        /// <summary>
        /// The project configuration.
        /// </summary>
        private readonly Configuration Configuration;

        /// <summary>
        /// The method to test.
        /// </summary>
        private readonly TestMethodInfo TestMethodInfo;

        /// <summary>
        /// Set of callbacks to invoke at the end
        /// of each iteration.
        /// </summary>
        private readonly ISet<Action<uint>> PerIterationCallbacks;

        /// <summary>
        /// The scheduler used by the runtime during testing.
        /// </summary>
        internal OperationScheduler Scheduler { get; private set; }

        /// <summary>
        /// The profiler.
        /// </summary>
        internal Profiler Profiler { get; private set; }

        /// <summary>
        /// The client used to optionally send anonymized telemetry data.
        /// </summary>
        private static CoyoteTelemetryClient TelemetryClient;

        /// <summary>
        /// The testing task cancellation token source.
        /// </summary>
        private readonly CancellationTokenSource CancellationTokenSource;

        /// <summary>
        /// Data structure containing information
        /// gathered during testing.
        /// </summary>
        public TestReport TestReport { get; set; }

        /// <summary>
        /// The installed logger.
        /// </summary>
        /// <remarks>
        /// See <see href="/coyote/concepts/actors/logging" >Logging</see> for more information.
        /// </remarks>
        private ILogger InstalledLogger;

        /// <summary>
        /// The default logger that is used during testing.
        /// </summary>
        private readonly ILogger DefaultLogger;

        /// <summary>
        /// Get or set the <see cref="ILogger"/> used to log messages during testing.
        /// </summary>
        /// <remarks>
        /// See <see href="/coyote/concepts/actors/logging" >Logging</see> for more information.
        /// </remarks>
        public ILogger Logger
        {
            get
            {
                return this.InstalledLogger;
            }

            set
            {
                var old = this.InstalledLogger;
                if (value is null)
                {
                    this.InstalledLogger = new NullLogger();
                }
                else
                {
                    this.InstalledLogger = value;
                }

                using var v = old;
            }
        }

        /// <summary>
        /// The DGML graph of the execution path explored in the last iteration.
        /// </summary>
        private Graph LastExecutionGraph;

        /// <summary>
        /// Contains a single iteration of XML log output in the case where the IsXmlLogEnabled
        /// configuration is specified.
        /// </summary>
        private StringBuilder XmlLog;

        /// <summary>
        /// The readable trace, if any.
        /// </summary>
        public string ReadableTrace { get; private set; }

        /// <summary>
        /// The reproducable trace, if any.
        /// </summary>
        public string ReproducibleTrace { get; private set; }

        /// <summary>
        /// A guard for printing info.
        /// </summary>
        private int PrintGuard;

        /// <summary>
        /// Creates a new systematic testing engine.
        /// </summary>
        public static TestingEngine Create(Configuration configuration)
        {
            TestMethodInfo testMethodInfo = null;

            try
            {
                testMethodInfo = TestMethodInfo.Create(configuration);
            }
            catch (Exception ex)
            {
                if (configuration.DisableEnvironmentExit)
                {
                    throw;
                }
                else
                {
                    Error.ReportAndExit(ex.Message);
                }
            }

            return new TestingEngine(configuration, testMethodInfo);
        }

        /// <summary>
        /// Creates a new systematic testing engine.
        /// </summary>
        public static TestingEngine Create(Configuration configuration, Action test) =>
            new TestingEngine(configuration, test);

        /// <summary>
        /// Creates a new systematic testing engine.
        /// </summary>
        public static TestingEngine Create(Configuration configuration, Action<ICoyoteRuntime> test) =>
            new TestingEngine(configuration, test);

        /// <summary>
        /// Creates a new systematic testing engine.
        /// </summary>
        public static TestingEngine Create(Configuration configuration, Action<IActorRuntime> test) =>
            new TestingEngine(configuration, test);

        /// <summary>
        /// Creates a new systematic testing engine.
        /// </summary>
        public static TestingEngine Create(Configuration configuration, Func<Task> test) =>
            new TestingEngine(configuration, test);

        /// <summary>
        /// Creates a new systematic testing engine.
        /// </summary>
        public static TestingEngine Create(Configuration configuration, Func<ICoyoteRuntime, Task> test) =>
            new TestingEngine(configuration, test);

        /// <summary>
        /// Creates a new systematic testing engine.
        /// </summary>
        public static TestingEngine Create(Configuration configuration, Func<IActorRuntime, Task> test) =>
            new TestingEngine(configuration, test);

        /// <summary>
        /// Initializes a new instance of the <see cref="TestingEngine"/> class.
        /// </summary>
        internal TestingEngine(Configuration configuration, Delegate test)
            : this(configuration, TestMethodInfo.Create(test))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestingEngine"/> class.
        /// </summary>
        private TestingEngine(Configuration configuration, TestMethodInfo testMethodInfo)
        {
            this.Configuration = configuration;
            this.TestMethodInfo = testMethodInfo;

            this.DefaultLogger = new ConsoleLogger() { LogLevel = configuration.LogLevel };
            this.Logger = this.DefaultLogger;
            this.Profiler = new Profiler();

            this.PerIterationCallbacks = new HashSet<Action<uint>>();

            this.TestReport = new TestReport(configuration);
            this.ReadableTrace = string.Empty;
            this.ReproducibleTrace = string.Empty;

            this.CancellationTokenSource = new CancellationTokenSource();
            this.PrintGuard = 1;

            if (configuration.IsDebugVerbosityEnabled)
            {
                IO.Debug.IsEnabled = true;
            }

            // Do some sanity checking.
            string error = string.Empty;
            if (configuration.IsSystematicFuzzingEnabled &&
                (configuration.SchedulingStrategy is "replay" || configuration.ScheduleFile.Length > 0))
            {
                error = "Replaying a bug trace is not currently supported in systematic fuzzing.";
            }

            if (configuration.SchedulingStrategy is "portfolio")
            {
                error = "Portfolio testing strategy is only available in parallel testing.";
            }

            if (!string.IsNullOrEmpty(error))
            {
                if (configuration.DisableEnvironmentExit)
                {
                    throw new Exception(error);
                }
                else
                {
                    Error.ReportAndExit(error);
                }
            }

            this.Scheduler = OperationScheduler.Setup(configuration);

            if (TelemetryClient is null)
            {
                TelemetryClient = new CoyoteTelemetryClient(this.Configuration);
            }
        }

        /// <summary>
        /// Runs the testing engine.
        /// </summary>
        public void Run()
        {
            bool isReplaying = this.Scheduler.IsReplayingSchedule;

            try
            {
                TelemetryClient.TrackEventAsync(isReplaying ? "replay" : "test").Wait();

                if (Debugger.IsAttached)
                {
                    TelemetryClient.TrackEventAsync(isReplaying ? "replay-debug" : "test-debug").Wait();
                }

                if (this.Configuration.EnableTelemetry)
                {
                    this.Logger.WriteLine(LogSeverity.Important, $"...... Telemetry is enabled, see {LearnAboutTelemetryUrl}.");
                }

                if (!this.IsTestRewritten())
                {
                    // TODO: eventually will throw an exception; we allow this for now.
                    this.Logger.WriteLine(LogSeverity.Error,
                        $"... Assembly is not rewritten for testing, see {LearnAboutRewritingUrl}.");
                }

                Task task = this.CreateTestingTask(this.TestMethodInfo);
                if (this.Configuration.TestingTimeout > 0)
                {
                    this.CancellationTokenSource.CancelAfter(
                        this.Configuration.TestingTimeout * 1000);
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
                    this.Logger.WriteLine(LogSeverity.Warning, $"... Test timed out.");
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
                    if (this.Configuration.DisableEnvironmentExit)
                    {
                        throw aex.InnerException;
                    }
                    else
                    {
                        Error.ReportAndExit($"{aex.InnerException.Message}");
                    }
                }

                if (this.Configuration.DisableEnvironmentExit)
                {
                    throw aex.InnerException;
                }
                else
                {
                    Error.ReportAndExit("Exception thrown during testing outside the context of an actor, " +
                    "possibly in a test method. Please enable debug verbosity to print more information.");
                }
            }
            catch (Exception ex)
            {
                this.Logger.WriteLine(LogSeverity.Error, $"... Test failed due to an internal error: {ex}");
                this.TestReport.InternalErrors.Add(ex.ToString());
            }
            finally
            {
                this.Profiler.StopMeasuringExecutionTime();
            }

            if (this.TestReport != null && this.TestReport.NumOfFoundBugs > 0)
            {
                TelemetryClient.TrackMetricAsync(isReplaying ? "replay-bugs" : "test-bugs", this.TestReport.NumOfFoundBugs).Wait();
            }

            if (!Debugger.IsAttached)
            {
                TelemetryClient.TrackMetricAsync(isReplaying ? "replay-time" : "test-time", this.Profiler.Results()).Wait();
            }
        }

        /// <summary>
        /// Creates a new testing task for the specified test method.
        /// </summary>
        private Task CreateTestingTask(TestMethodInfo methodInfo)
        {
            return new Task(() =>
            {
                this.Logger.WriteLine(LogSeverity.Important, "... Setting up the{0} test:",
                    string.IsNullOrEmpty(methodInfo.Name) ? string.Empty : $" '{methodInfo.Name}'");
                this.Logger.WriteLine(LogSeverity.Important,
                    $"...... Using the {this.Scheduler.GetDescription()} exploration strategy.");
                if (this.Configuration.AttachDebugger)
                {
                    this.Logger.WriteLine(LogSeverity.Important,
                        $"...... Launching and attaching the debugger.");
                    Debugger.Launch();
                }

                try
                {
                    // Invokes the user-specified initialization method.
                    methodInfo.InitializeAllIterations();

                    this.Logger.WriteLine(LogSeverity.Important, "... Running test iterations:");

                    uint iteration = 0;
                    while (iteration < this.Configuration.TestingIterations || this.Configuration.TestingTimeout > 0)
                    {
                        if (this.CancellationTokenSource.IsCancellationRequested)
                        {
                            break;
                        }

                        // Runs the next iteration.
                        bool runNext = this.RunNextIteration(methodInfo, iteration);
                        if ((!this.Configuration.RunTestIterationsToCompletion && this.TestReport.NumOfFoundBugs > 0) ||
                            this.Scheduler.IsReplayingSchedule || !runNext)
                        {
                            break;
                        }

                        if (this.Scheduler.ValueGenerator != null && this.Configuration.IsSchedulingSeedIncremental)
                        {
                            // Increments the seed in the random number generator (if one is used), to
                            // capture the seed used by the scheduling strategy in the next iteration.
                            this.Scheduler.ValueGenerator.Seed += 1;
                        }

                        iteration++;
                    }

                    // Invokes the user-specified test disposal method.
                    methodInfo.DisposeAllIterations();
                }
                catch (Exception ex)
                {
                    Exception innerException = ex;
                    while (innerException is TargetInvocationException)
                    {
                        innerException = innerException.InnerException;
                    }

                    if (innerException is AggregateException)
                    {
                        innerException = innerException.InnerException;
                    }

                    if (!(innerException is TaskCanceledException))
                    {
                        ExceptionDispatchInfo.Capture(innerException).Throw();
                    }
                }
            }, this.CancellationTokenSource.Token);
        }

        /// <summary>
        /// Runs the next testing iteration for the specified test method.
        /// </summary>
        private bool RunNextIteration(TestMethodInfo methodInfo, uint iteration)
        {
            if (!this.Scheduler.InitializeNextIteration(iteration))
            {
                // The next iteration cannot run, so stop exploring.
                return false;
            }

            if (!this.Scheduler.IsReplayingSchedule && this.ShouldPrintIteration(iteration + 1))
            {
                this.Logger.WriteLine(LogSeverity.Important, $"..... Iteration #{iteration + 1}");

                // Flush when logging to console.
                if (this.Logger is ConsoleLogger)
                {
                    Console.Out.Flush();
                }
            }

            // Runtime used to serialize and test the program in this iteration.
            CoyoteRuntime runtime = null;

            // Logger used to intercept the program output if no custom logger
            // is installed and if verbosity is turned off.
            InMemoryLogger runtimeLogger = null;

            // Gets a handle to the standard output and error streams.
            var stdOut = Console.Out;
            var stdErr = Console.Error;

            try
            {
                // Creates a new instance of the controlled runtime.
                runtime = new CoyoteRuntime(this.Configuration, this.Scheduler);

                // If verbosity is turned off, then intercept the program log, and also redirect
                // the standard output and error streams to the runtime logger.
                if (!this.Configuration.IsVerbose)
                {
                    runtimeLogger = new InMemoryLogger();
                    if (this.Logger != this.DefaultLogger)
                    {
                        runtimeLogger.UserLogger = this.Logger;
                    }

                    runtime.Logger = runtimeLogger;

                    Console.SetOut(runtimeLogger.TextWriter);
                    Console.SetError(runtimeLogger.TextWriter);
                }
                else if (this.Logger != this.DefaultLogger)
                {
                    runtime.Logger = this.Logger;
                }

                this.InitializeCustomActorLogging(runtime.DefaultActorExecutionContext);

                // Runs the test and waits for it to terminate.
                Task task = runtime.RunTestAsync(methodInfo.Method, methodInfo.Name);
                task.Wait();

                // Invokes the user-specified iteration disposal method.
                methodInfo.DisposeCurrentIteration();

                // Invoke the per iteration callbacks, if any.
                foreach (var callback in this.PerIterationCallbacks)
                {
                    callback(iteration);
                }

                runtime.LogWriter.LogCompletion();

                this.GatherTestingStatistics(runtime);

                if (!this.Scheduler.IsReplayingSchedule && this.TestReport.NumOfFoundBugs > 0)
                {
                    if (runtimeLogger != null)
                    {
                        this.ReadableTrace = string.Empty;
                        if (this.Configuration.EnableTelemetry)
                        {
                            this.ReadableTrace += $"<TelemetryLog> Telemetry is enabled, see {LearnAboutTelemetryUrl}.\n";
                        }

                        this.ReadableTrace += runtimeLogger.ToString();
                        this.ReadableTrace += this.TestReport.GetText(this.Configuration, "<StrategyLog>");
                    }

                    if (runtime.SchedulingPolicy is SchedulingPolicy.Interleaving)
                    {
                        this.ReproducibleTrace = this.Scheduler.Trace.Serialize(
                            this.Configuration, this.Scheduler.IsScheduleFair);
                    }
                }
            }
            finally
            {
                if (!this.Configuration.IsVerbose)
                {
                    // Restores the standard output and error streams.
                    Console.SetOut(stdOut);
                    Console.SetError(stdErr);
                }

                if (this.Configuration.IsSystematicFuzzingFallbackEnabled &&
                    runtime.SchedulingPolicy is SchedulingPolicy.Interleaving &&
                    (runtime.ExecutionStatus is ExecutionStatus.ConcurrencyUncontrolled ||
                    runtime.ExecutionStatus is ExecutionStatus.Deadlocked))
                {
                    // Detected uncontrolled concurrency or deadlock, so switch to systematic fuzzing.
                    this.Scheduler = OperationScheduler.Setup(this.Configuration, SchedulingPolicy.Fuzzing,
                        this.Scheduler.ValueGenerator);
                    this.Logger.WriteLine(LogSeverity.Important, $"..... Iteration #{iteration + 1} " +
                        $"switching to fuzzing due to uncontrolled concurrency");
                }
                else if (runtime.IsBugFound)
                {
                    if (!this.Scheduler.IsReplayingSchedule)
                    {
                        this.Logger.WriteLine(LogSeverity.Important, $"..... Iteration #{iteration + 1} " +
                            $"found bug #{this.TestReport.NumOfFoundBugs}");
                    }

                    this.Logger.WriteLine(LogSeverity.Error, runtime.BugReport);
                }

                // Cleans up the runtime before the next iteration starts.
                runtimeLogger?.Close();
                runtimeLogger?.Dispose();
                runtime?.Dispose();
            }

            return true;
        }

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
        public string GetReport()
        {
            if (this.Scheduler.IsReplayingSchedule)
            {
                StringBuilder report = new StringBuilder();
                report.AppendFormat("... Reproduced {0} bug{1}{2}.", this.TestReport.NumOfFoundBugs,
                    this.TestReport.NumOfFoundBugs is 1 ? string.Empty : "s",
                    this.Configuration.AttachDebugger ? string.Empty : " (use --break to attach the debugger)");
                report.AppendLine();
                report.Append($"... Elapsed {this.Profiler.Results()} sec.");
                return report.ToString();
            }

            return this.TestReport.GetText(this.Configuration, "...");
        }

        /// <summary>
        /// Throws either an <see cref="AssertionFailureException"/>, if a bug was found,
        /// or an unhandled <see cref="Exception"/>, if one was thrown.
        /// </summary>
        public void ThrowIfBugFound()
        {
            if (this.TestReport.NumOfFoundBugs > 0)
            {
                if (this.TestReport.ThrownException != null)
                {
                    ExceptionDispatchInfo.Capture(this.TestReport.ThrownException).Throw();
                }

                throw new AssertionFailureException(this.TestReport.BugReports.FirstOrDefault());
            }
        }

        /// <summary>
        /// Tries to emit the available reports to the specified directory with the given file name,
        /// and returns the paths of all emitted reports.
        /// </summary>
        public bool TryEmitReports(string directory, string fileName, out IEnumerable<string> reportPaths)
        {
            var paths = new List<string>();
            if (!this.Configuration.RunTestIterationsToCompletion)
            {
                // Emits the human readable trace, if it exists.
                if (!string.IsNullOrEmpty(this.ReadableTrace))
                {
                    string readableTracePath = Path.Combine(directory, fileName + ".txt");
                    File.WriteAllText(readableTracePath, this.ReadableTrace);
                    paths.Add(readableTracePath);
                }
            }

            if (!this.Configuration.RunTestIterationsToCompletion)
            {
                if (this.Configuration.IsXmlLogEnabled)
                {
                    string xmlPath = Path.Combine(directory, fileName + ".trace.xml");
                    File.WriteAllText(xmlPath, this.XmlLog.ToString());
                    paths.Add(xmlPath);
                }

                if (this.LastExecutionGraph != null && this.TestReport.NumOfFoundBugs > 0)
                {
                    string graphPath = Path.Combine(directory, fileName + ".trace.dgml");
                    this.LastExecutionGraph.SaveDgml(graphPath, true);
                    paths.Add(graphPath);
                }

                // Emits the reproducible trace, if it exists.
                if (!string.IsNullOrEmpty(this.ReproducibleTrace))
                {
                    string reproTracePath = Path.Combine(directory, fileName + ".schedule");
                    File.WriteAllText(reproTracePath, this.ReproducibleTrace);
                    paths.Add(reproTracePath);
                }
            }

            // Emits the uncontrolled invocations report, if it exists.
            if (this.TestReport.UncontrolledInvocations.Count > 0)
            {
                string reportPath = Path.Combine(directory, fileName + ".uncontrolled.json");
                File.WriteAllText(reportPath, UncontrolledInvocationsReport.ToJSON(this.TestReport.UncontrolledInvocations));
                paths.Add(reportPath);
            }

            reportPaths = paths;
            return paths.Count > 0;
        }

        /// <summary>
        /// Tries to emit the available coverage reports to the specified directory with the given file name,
        /// and returns the paths of all emitted coverage reports.
        /// </summary>
        public bool TryEmitCoverageReports(string directory, string fileName, out IEnumerable<string> reportPaths)
        {
            var paths = new List<string>();
            if (this.Configuration.IsActivityCoverageReported)
            {
                var codeCoverageReporter = new ActivityCoverageReporter(this.TestReport.CoverageInfo);

                string graphFilePath = Path.Combine(directory, fileName + ".coverage.dgml");
                codeCoverageReporter.EmitVisualizationGraph(graphFilePath);
                paths.Add(graphFilePath);

                string coverageFilePath = Path.Combine(directory, fileName + ".coverage.txt");
                codeCoverageReporter.EmitCoverageReport(coverageFilePath);
                paths.Add(coverageFilePath);

                string serFilePath = Path.Combine(directory, fileName + ".sci");
                this.TestReport.CoverageInfo.Save(serFilePath);
                paths.Add(serFilePath);
            }

            reportPaths = paths;
            return paths.Count > 0;
        }

        /// <summary>
        /// Registers a callback to invoke at the end of each iteration. The callback takes as
        /// a parameter an integer representing the current iteration.
        /// </summary>
        public void RegisterPerIterationCallBack(Action<uint> callback)
        {
            this.PerIterationCallbacks.Add(callback);
        }

        /// <summary>
        /// Take care of handling the <see cref="Configuration"/> settings for <see cref="Configuration.CustomActorRuntimeLogType"/>,
        /// <see cref="Configuration.IsTraceVisualizationEnabled"/>, and <see cref="Configuration.IsActivityCoverageReported"/> by
        /// setting up the LogWriters on the given <see cref="IActorRuntime"/> object.
        /// </summary>
        private void InitializeCustomActorLogging(IActorRuntime runtime)
        {
            if (!string.IsNullOrEmpty(this.Configuration.CustomActorRuntimeLogType))
            {
                var log = this.Activate<IActorRuntimeLog>(this.Configuration.CustomActorRuntimeLogType);
                if (log != null)
                {
                    runtime.RegisterLog(log);
                }
            }

            if (this.Configuration.IsTraceVisualizationEnabled)
            {
                // Registers an activity coverage graph builder.
                runtime.RegisterLog(new ActorRuntimeLogGraphBuilder(false, false));
            }

            if (this.Configuration.IsActivityCoverageReported)
            {
                // Registers an activity coverage graph builder that collapses instances.
                runtime.RegisterLog(new ActorRuntimeLogGraphBuilder(false, true));
            }

            if (this.Configuration.IsActivityCoverageReported)
            {
                // Need this additional logger to get the event coverage report correct
                runtime.RegisterLog(new ActorRuntimeLogEventCoverage());
            }

            if (this.Configuration.IsXmlLogEnabled)
            {
                this.XmlLog = new StringBuilder();
                runtime.RegisterLog(new ActorRuntimeLogXmlFormatter(XmlWriter.Create(this.XmlLog,
                    new XmlWriterSettings() { Indent = true, IndentChars = "  ", OmitXmlDeclaration = true })));
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
                this.Logger.WriteLine(LogSeverity.Error, ex.Message);
            }

            return null;
        }

        /// <summary>
        /// Gathers the exploration strategy statistics from the specified runtimne.
        /// </summary>
        private void GatherTestingStatistics(CoyoteRuntime runtime)
        {
            TestReport report = new TestReport(this.Configuration);
            runtime.PopulateTestReport(report);

            var coverageInfo = runtime.DefaultActorExecutionContext.BuildCoverageInfo();
            report.CoverageInfo.Merge(coverageInfo);
            this.TestReport.Merge(report);

            // Save the DGML graph of the execution path explored in the last iteration.
            this.LastExecutionGraph = runtime.DefaultActorExecutionContext.GetExecutionGraph();
        }

        /// <summary>
        /// Returns true if the engine should print the current iteration.
        /// </summary>
        private bool ShouldPrintIteration(uint iteration)
        {
            if (iteration > this.PrintGuard * 10)
            {
                var count = iteration.ToString().Length - 1;
                var guard = "1" + (count > 0 ? string.Concat(Enumerable.Repeat("0", count)) : string.Empty);
                this.PrintGuard = int.Parse(guard);
            }

            return iteration % this.PrintGuard is 0;
        }

        /// <summary>
        /// Checks if the test executed by the testing engine has been rewritten with the current version.
        /// </summary>
        /// <returns>True if the test has been rewritten, else false.</returns>
        public bool IsTestRewritten() => RewritingEngine.IsAssemblyRewritten(this.TestMethodInfo.Assembly);

        /// <summary>
        /// Installs the specified TextWriter for logging.
        /// </summary>
        /// <remarks>
        /// This writer will be wrapped in an object that implements the <see cref="ILogger"/> interface which
        /// will have a minor performance overhead, so it is better to set the <see cref="Logger"/> property instead.
        /// </remarks>
        /// <param name="writer">The writer to use for logging.</param>
        /// <returns>The previously installed logger.</returns>
        [Obsolete("Use the new ILogger version of SetLogger")]
        public TextWriter SetLogger(TextWriter writer)
        {
            ILogger oldLogger = this.Logger;
            if (oldLogger == this.DefaultLogger)
            {
                oldLogger = null;
            }

            this.Logger = new TextWriterLogger(writer);

            if (oldLogger != null)
            {
                return oldLogger.TextWriter;
            }

            return null;
        }
    }
}
