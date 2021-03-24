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
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Actors.Coverage;
using Microsoft.Coyote.IO;
using Microsoft.Coyote.Rewriting;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.Telemetry;
using Microsoft.Coyote.Testing;
using CoyoteTasks = Microsoft.Coyote.Tasks;

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
        /// Url with information about the gathered telemetry.
        /// </summary>
        private const string LearnAboutTelemetryUrl = "http://aka.ms/coyote-telemetry";

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
        /// The context of the scheduler during controlled testing.
        /// </summary>
        internal readonly SchedulingContext SchedulingContext;

        /// <summary>
        /// The profiler.
        /// </summary>
        private readonly Profiler Profiler;

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
        /// A graph of the actors, state machines and events of a single test iteration.
        /// </summary>
        private Graph Graph;

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
        /// Creates a new systematic testing engine.
        /// </summary>
        public static TestingEngine Create(Configuration configuration, Func<CoyoteTasks.Task> test) =>
            new TestingEngine(configuration, test);

        /// <summary>
        /// Creates a new systematic testing engine.
        /// </summary>
        public static TestingEngine Create(Configuration configuration, Func<ICoyoteRuntime, CoyoteTasks.Task> test) =>
            new TestingEngine(configuration, test);

        /// <summary>
        /// Creates a new systematic testing engine.
        /// </summary>
        public static TestingEngine Create(Configuration configuration, Func<IActorRuntime, CoyoteTasks.Task> test) =>
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

            if (configuration.SchedulingStrategy is "portfolio")
            {
                var msg = "Portfolio testing strategy is only " +
                    "available in parallel testing.";
                if (configuration.DisableEnvironmentExit)
                {
                    throw new Exception(msg);
                }
                else
                {
                    Error.ReportAndExit(msg);
                }
            }

            this.SchedulingContext = SchedulingContext.Setup(configuration, this.Logger);

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
            bool isReplaying = this.SchedulingContext.IsReplayingSchedule;

            try
            {
                TelemetryClient.TrackEventAsync(isReplaying ? "replay" : "test").Wait();

                if (Debugger.IsAttached)
                {
                    TelemetryClient.TrackEventAsync(isReplaying ? "replay-debug" : "test-debug").Wait();
                }

                Task task = this.CreateTestingTask();
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
                    this.Logger.WriteLine(LogSeverity.Warning, $"... Task {this.Configuration.TestingProcessId} timed out.");
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
                    "possibly in a test method. Please use /debug /v:2 to print more information.");
                }
            }
            catch (Exception ex)
            {
                this.Logger.WriteLine(LogSeverity.Error, $"... Task {this.Configuration.TestingProcessId} failed due to an internal error: {ex}");
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
        /// Creates a new testing task.
        /// </summary>
        private Task CreateTestingTask()
        {
            string options = string.Empty;
            if (this.Configuration.SchedulingStrategy is "random" ||
                this.Configuration.SchedulingStrategy is "pct" ||
                this.Configuration.SchedulingStrategy is "fairpct" ||
                this.Configuration.SchedulingStrategy is "probabilistic")
            {
                options = $" (seed:{this.SchedulingContext.ValueGenerator.Seed})";
            }

            this.Logger.WriteLine(LogSeverity.Important, $"... Task {this.Configuration.TestingProcessId} is " +
                $"using '{this.Configuration.SchedulingStrategy}' strategy{options}.");

            if (this.Configuration.EnableTelemetry)
            {
                this.Logger.WriteLine(LogSeverity.Important, $"... Telemetry is enabled, see {LearnAboutTelemetryUrl}.");
            }

            return new Task(() =>
            {
                if (this.Configuration.AttachDebugger)
                {
                    Debugger.Launch();
                }

                try
                {
                    // Invokes the user-specified initialization method.
                    this.TestMethodInfo.InitializeAllIterations();

                    uint iteration = 0;
                    while (iteration < this.Configuration.TestingIterations || this.Configuration.TestingTimeout > 0)
                    {
                        if (this.CancellationTokenSource.IsCancellationRequested)
                        {
                            break;
                        }

                        // Runs the next iteration.
                        bool runNext = this.RunNextIteration(iteration);
                        if ((!this.Configuration.PerformFullExploration && this.TestReport.NumOfFoundBugs > 0) ||
                            this.SchedulingContext.IsReplayingSchedule || !runNext)
                        {
                            break;
                        }

                        if (this.SchedulingContext.ValueGenerator != null && this.Configuration.IncrementalSchedulingSeed)
                        {
                            // Increments the seed in the random number generator (if one is used), to
                            // capture the seed used by the scheduling strategy in the next iteration.
                            this.SchedulingContext.ValueGenerator.Seed += 1;
                        }

                        iteration++;
                    }

                    // Invokes the user-specified test disposal method.
                    this.TestMethodInfo.DisposeAllIterations();
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
        /// Runs the next testing iteration.
        /// </summary>
        private bool RunNextIteration(uint iteration)
        {
            if (!this.SchedulingContext.InitializeNextIteration(iteration))
            {
                // The next iteration cannot run, so stop exploring.
                return false;
            }

            if (!this.SchedulingContext.IsReplayingSchedule && this.ShouldPrintIteration(iteration + 1))
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
                runtime = new CoyoteRuntime(this.Configuration, this.SchedulingContext);

                // If verbosity is turned off, then intercept the program log, and also redirect
                // the standard output and error streams to a nul logger.
                if (!this.Configuration.IsVerbose)
                {
                    runtimeLogger = new InMemoryLogger();
                    if (this.Logger != this.DefaultLogger)
                    {
                        runtimeLogger.UserLogger = this.Logger;
                    }

                    runtime.Logger = runtimeLogger;

                    var writer = TextWriter.Null;
                    Console.SetOut(writer);
                    Console.SetError(writer);
                }
                else if (this.Logger != this.DefaultLogger)
                {
                    runtime.Logger = this.Logger;
                }

                this.InitializeCustomActorLogging(runtime.DefaultActorExecutionContext);

                // Runs the test and waits for it to terminate.
                runtime.RunTest(this.TestMethodInfo.Method, this.TestMethodInfo.Name);
                runtime.WaitAsync().Wait();

                // Invokes the user-specified iteration disposal method.
                this.TestMethodInfo.DisposeCurrentIteration();

                // Invoke the per iteration callbacks, if any.
                foreach (var callback in this.PerIterationCallbacks)
                {
                    callback(iteration);
                }

                if (runtime.IsBugFound)
                {
                    this.Logger.WriteLine(LogSeverity.Error, runtime.BugReport);
                }
                else
                {
                    // Checks for liveness errors. Only checked if no safety errors have been found.
                    runtime.CheckLivenessErrors();
                }

                runtime.LogWriter.LogCompletion();

                this.GatherTestingStatistics(runtime);

                if (!this.SchedulingContext.IsReplayingSchedule && this.TestReport.NumOfFoundBugs > 0)
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

                    this.ReproducibleTrace = runtime.ScheduleTrace.Serialize(
                        this.Configuration, this.SchedulingContext.IsScheduleFair);
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

                if (!this.SchedulingContext.IsReplayingSchedule && this.Configuration.PerformFullExploration && runtime.IsBugFound)
                {
                    this.Logger.WriteLine(LogSeverity.Important, $"..... Iteration #{iteration + 1} " +
                        $"triggered bug #{this.TestReport.NumOfFoundBugs} " +
                        $"[task-{this.Configuration.TestingProcessId}]");
                }

                // Cleans up the runtime before the next iteration starts.
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
            if (this.SchedulingContext.IsReplayingSchedule)
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
        /// Tries to emit the testing traces, if any.
        /// </summary>
        public IEnumerable<string> TryEmitTraces(string directory, string file)
        {
            int index = 0;
            // Find the next available file index.
            Regex match = new Regex("^(.*)_([0-9]+)_([0-9]+)");
            foreach (var path in Directory.GetFiles(directory))
            {
                string name = Path.GetFileName(path);
                if (name.StartsWith(file))
                {
                    var result = match.Match(name);
                    if (result.Success)
                    {
                        string value = result.Groups[3].Value;
                        if (int.TryParse(value, out int i))
                        {
                            index = Math.Max(index, i + 1);
                        }
                    }
                }
            }

            if (!this.Configuration.PerformFullExploration)
            {
                // Emits the human readable trace, if it exists.
                if (!string.IsNullOrEmpty(this.ReadableTrace))
                {
                    string readableTracePath = Path.Combine(directory, file + "_" + index + ".txt");
                    this.Logger.WriteLine(LogSeverity.Important, $"..... Writing {readableTracePath}");
                    File.WriteAllText(readableTracePath, this.ReadableTrace);
                    yield return readableTracePath;
                }
            }

            if (this.Configuration.IsXmlLogEnabled)
            {
                string xmlPath = Path.Combine(directory, file + "_" + index + ".trace.xml");
                this.Logger.WriteLine(LogSeverity.Important, $"..... Writing {xmlPath}");
                File.WriteAllText(xmlPath, this.XmlLog.ToString());
                yield return xmlPath;
            }

            if (this.Graph != null)
            {
                string graphPath = Path.Combine(directory, file + "_" + index + ".dgml");
                this.Graph.SaveDgml(graphPath, true);
                this.Logger.WriteLine(LogSeverity.Important, $"..... Writing {graphPath}");
                yield return graphPath;
            }

            if (!this.Configuration.PerformFullExploration)
            {
                // Emits the reproducable trace, if it exists.
                if (!string.IsNullOrEmpty(this.ReproducibleTrace))
                {
                    string reproTracePath = Path.Combine(directory, file + "_" + index + ".schedule");
                    this.Logger.WriteLine(LogSeverity.Important, $"..... Writing {reproTracePath}");
                    File.WriteAllText(reproTracePath, this.ReproducibleTrace);
                    yield return reproTracePath;
                }
            }

            this.Logger.WriteLine(LogSeverity.Important, $"... Elapsed {this.Profiler.Results()} sec.");
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
        /// <see cref="Configuration.IsDgmlGraphEnabled"/>, and <see cref="Configuration.ReportActivityCoverage"/> by setting up the
        /// LogWriters on the given <see cref="IActorRuntime"/> object.
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
            runtime.GetSchedulingStatisticsAndResults(out bool isBugFound, out string bugReport, out int steps,
                out bool isMaxStepsReached, out bool isScheduleFair, out Exception unhandledException);

            TestReport report = TestReport.CreateTestReportFromStats(this.Configuration, isBugFound, bugReport,
                steps, isMaxStepsReached, isScheduleFair, unhandledException);
            if (this.Configuration.ReportActivityCoverage)
            {
                report.CoverageInfo.CoverageGraph = this.Graph;
            }

            var coverageInfo = runtime.DefaultActorExecutionContext.BuildCoverageInfo();
            report.CoverageInfo.Merge(coverageInfo);
            this.TestReport.Merge(report);

            // Also save the graph snapshot of the last iteration, if there is one.
            this.Graph = coverageInfo.CoverageGraph;
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
        /// Checks if the test executed by the testing engine has been rewritten.
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
