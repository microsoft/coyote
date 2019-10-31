// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Microsoft.Coyote.IO;
using Microsoft.Coyote.Runtime.Exploration;
using Microsoft.Coyote.TestingServices.Coverage;
using Microsoft.Coyote.TestingServices.Runtime;
using Microsoft.Coyote.TestingServices.Tracing.Error;
using Microsoft.Coyote.TestingServices.Tracing.Schedule;

namespace Microsoft.Coyote.TestingServices
{
    /// <summary>
    /// Implementation of the bug-finding engine.
    /// </summary>
    [DebuggerStepThrough]
    internal sealed class BugFindingEngine : AbstractTestingEngine
    {
        /// <summary>
        /// The bug trace, if any.
        /// </summary>
        private BugTrace BugTrace;

        /// <summary>
        /// The readable trace, if any.
        /// </summary>
        internal string ReadableTrace { get; private set; }

        /// <summary>
        /// The reproducable trace, if any.
        /// </summary>
        internal string ReproducableTrace { get; private set; }

        /// <summary>
        /// A graph of the machines, states and events.
        /// </summary>
        internal Graph Graph { get; private set; }

        /// <summary>
        /// Creates a new bug-finding engine.
        /// </summary>
        internal static BugFindingEngine Create(Configuration configuration, Delegate testMethod)
        {
            return new BugFindingEngine(configuration, testMethod);
        }

        /// <summary>
        /// Creates a new bug-finding engine.
        /// </summary>
        internal static BugFindingEngine Create(Configuration configuration)
        {
            return new BugFindingEngine(configuration);
        }

        /// <summary>
        /// Creates a new bug-finding engine.
        /// </summary>
        internal static BugFindingEngine Create(Configuration configuration, Assembly assembly)
        {
            return new BugFindingEngine(configuration, assembly);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BugFindingEngine"/> class.
        /// </summary>
        private BugFindingEngine(Configuration configuration)
            : base(configuration)
        {
            this.Initialize();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BugFindingEngine"/> class.
        /// </summary>
        private BugFindingEngine(Configuration configuration, Assembly assembly)
            : base(configuration, assembly)
        {
            this.Initialize();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BugFindingEngine"/> class.
        /// </summary>
        private BugFindingEngine(Configuration configuration, Delegate testMethod)
            : base(configuration, testMethod)
        {
            this.Initialize();
        }

        /// <summary>
        /// Initializes the bug-finding engine.
        /// </summary>
        private void Initialize()
        {
            this.ReadableTrace = string.Empty;
            this.ReproducableTrace = string.Empty;
        }

        /// <summary>
        /// Creates a new testing task.
        /// </summary>
        protected override Task CreateTestingTask()
        {
            string options = string.Empty;
            if (this.Configuration.SchedulingStrategy == SchedulingStrategy.Random ||
                this.Configuration.SchedulingStrategy == SchedulingStrategy.ProbabilisticRandom ||
                this.Configuration.SchedulingStrategy == SchedulingStrategy.PCT ||
                this.Configuration.SchedulingStrategy == SchedulingStrategy.FairPCT ||
                this.Configuration.SchedulingStrategy == SchedulingStrategy.RandomDelayBounding)
            {
                options = $" (seed:{this.Configuration.RandomSchedulingSeed})";
            }

            this.Logger.WriteLine($"... Task {this.Configuration.TestingProcessId} is " +
                $"using '{this.Configuration.SchedulingStrategy}' strategy{options}.");

            return new Task(() =>
            {
                try
                {
                    if (this.TestInitMethod != null)
                    {
                        // Initializes the test state.
                        this.TestInitMethod.Invoke(null, Array.Empty<object>());
                    }

                    int maxIterations = this.Configuration.SchedulingIterations;
                    for (int i = 0; i < maxIterations; i++)
                    {
                        if (this.CancellationTokenSource.IsCancellationRequested)
                        {
                            break;
                        }

                        // Runs a new testing iteration.
                        this.RunNextIteration(i);

                        if (!this.Configuration.PerformFullExploration && this.TestReport.NumOfFoundBugs > 0)
                        {
                            break;
                        }

                        if (!this.Strategy.PrepareForNextIteration())
                        {
                            break;
                        }

                        if (this.RandomNumberGenerator != null && this.Configuration.IncrementalSchedulingSeed)
                        {
                            // Increments the seed in the random number generator (if one is used), to
                            // capture the seed used by the scheduling strategy in the next iteration.
                            this.RandomNumberGenerator.Seed += 1;
                        }

                        // Increases iterations if there is a specified timeout
                        // and the default iteration given.
                        if (this.Configuration.SchedulingIterations == 1 &&
                            this.Configuration.Timeout > 0)
                        {
                            maxIterations++;
                        }
                    }

                    if (this.TestDisposeMethod != null)
                    {
                        // Disposes the test state.
                        this.TestDisposeMethod.Invoke(null, Array.Empty<object>());
                    }
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
        private void RunNextIteration(int iteration)
        {
            if (this.ShouldPrintIteration(iteration + 1))
            {
                this.Logger.WriteLine($"..... Iteration #{iteration + 1}");

                // Flush when logging to console.
                if (this.Logger is ConsoleLogger)
                {
                    Console.Out.Flush();
                }
            }

            // Runtime used to serialize and test the program in this iteration.
            SystematicTestingRuntime runtime = null;

            // Logger used to intercept the program output if no custom logger
            // is installed and if verbosity is turned off.
            InMemoryLogger runtimeLogger = null;

            // Gets a handle to the standard output and error streams.
            var stdOut = Console.Out;
            var stdErr = Console.Error;

            try
            {
                // Creates a new instance of the bug-finding runtime.
                if (this.TestRuntimeFactoryMethod != null)
                {
                    runtime = (SystematicTestingRuntime)this.TestRuntimeFactoryMethod.Invoke(
                        null,
                        new object[] { this.Configuration, this.Strategy });
                }
                else
                {
                    runtime = new SystematicTestingRuntime(this.Configuration, this.Strategy);
                }

                // If verbosity is turned off, then intercept the program log, and also redirect
                // the standard output and error streams to a nul logger.
                if (!this.Configuration.IsVerbose)
                {
                    runtimeLogger = new InMemoryLogger();
                    runtime.SetLogger(runtimeLogger);

                    var writer = new LogWriter(new NulLogger());
                    Console.SetOut(writer);
                    Console.SetError(writer);
                }

                this.InitializeCustomLogging(runtime);

                // Runs the test and waits for it to terminate.
                runtime.RunTest(this.TestMethod, this.TestName);
                runtime.WaitAsync().Wait();

                // Invokes user-provided cleanup for this iteration.
                if (this.TestIterationDisposeMethod != null)
                {
                    // Disposes the test state.
                    this.TestIterationDisposeMethod.Invoke(null, null);
                }

                // Invoke the per iteration callbacks, if any.
                foreach (var callback in this.PerIterationCallbacks)
                {
                    callback(iteration);
                }

                // Checks that no monitor is in a hot state at termination. Only
                // checked if no safety property violations have been found.
                if (!runtime.Scheduler.BugFound)
                {
                    runtime.CheckNoMonitorInHotStateAtTermination();
                }

                if (runtime.Scheduler.BugFound)
                {
                    this.ErrorReporter.WriteErrorLine(runtime.Scheduler.BugReport);
                }

                this.GatherIterationStatistics(runtime);

                if (this.TestReport.NumOfFoundBugs > 0)
                {
                    if (runtimeLogger != null)
                    {
                        this.ReadableTrace = runtimeLogger.ToString();
                        this.ReadableTrace += this.TestReport.GetText(this.Configuration, "<StrategyLog>");
                    }

                    this.BugTrace = runtime.BugTrace;
                    this.ConstructReproducableTrace(runtime);
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

                if (this.Configuration.PerformFullExploration && runtime.Scheduler.BugFound)
                {
                    this.Logger.WriteLine($"..... Iteration #{iteration + 1} " +
                        $"triggered bug #{this.TestReport.NumOfFoundBugs} " +
                        $"[task-{this.Configuration.TestingProcessId}]");
                }

                // Cleans up the runtime before the next iteration starts.
                runtimeLogger?.Dispose();
                runtime?.Dispose();
            }
        }

        /// <summary>
        /// Returns a report with the testing results.
        /// </summary>
        public override string GetReport()
        {
            return this.TestReport.GetText(this.Configuration, "...");
        }

        /// <summary>
        /// Tries to emit the testing traces, if any.
        /// </summary>
        public override IEnumerable<string> TryEmitTraces(string directory, string file)
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

            // Emits the human readable trace, if it exists.
            if (!string.IsNullOrEmpty(this.ReadableTrace))
            {
                string readableTracePath = directory + file + "_" + index + ".txt";

                this.Logger.WriteLine($"..... Writing {readableTracePath}");
                File.WriteAllText(readableTracePath, this.ReadableTrace);
                yield return readableTracePath;
            }

            if (this.Graph != null)
            {
                string graphPath = directory + file + "_" + index + ".dgml";
                this.Graph.SaveDgml(graphPath);
                this.Logger.WriteLine($"..... Writing {graphPath}");
                yield return graphPath;
            }

            // Emits the bug trace, if it exists.
            if (this.BugTrace != null)
            {
                string bugTracePath = directory + file + "_" + index + ".pstrace";

                using (FileStream stream = File.Open(bugTracePath, FileMode.Create))
                {
                    DataContractSerializer serializer = new DataContractSerializer(typeof(BugTrace));
                    this.Logger.WriteLine($"..... Writing {bugTracePath}");
                    serializer.WriteObject(stream, this.BugTrace);
                }

                yield return bugTracePath;
            }

            // Emits the reproducable trace, if it exists.
            if (!string.IsNullOrEmpty(this.ReproducableTrace))
            {
                string reproTracePath = directory + file + "_" + index + ".schedule";

                this.Logger.WriteLine($"..... Writing {reproTracePath}");
                File.WriteAllText(reproTracePath, this.ReproducableTrace);
                yield return reproTracePath;
            }

            this.Logger.WriteLine($"... Elapsed {this.Profiler.Results()} sec.");
        }

        /// <summary>
        /// Gathers the exploration strategy statistics for the latest testing iteration.
        /// </summary>
        private void GatherIterationStatistics(SystematicTestingRuntime runtime)
        {
            TestReport report = runtime.Scheduler.GetReport();
            report.CoverageInfo.Merge(runtime.CoverageInfo);
            this.TestReport.Merge(report);

            // Save the graph snapshot if there is one.
            var graphLog = FindGraphLog(runtime);
            if (graphLog != null)
            {
                this.Graph = graphLog.SnapshotGraph();
                // Store it here so it is sent back to server in the distributed test scenario.
                this.TestReport.CoverageInfo.CoverageGraph = this.Graph;
            }
        }

        /// <summary>
        /// Look for a GraphStateMachineLog in the chain of log writers.
        /// </summary>
        /// <param name="runtime">The runtime to search.</param>
        /// <returns>A GraphStateMachineLog if found, or null.</returns>
        private static GraphMachineRuntimeLog FindGraphLog(SystematicTestingRuntime runtime)
        {
            IActorRuntimeLog start = runtime.LogWriter;
            while (start != null)
            {
                GraphMachineRuntimeLog graphLogger = runtime.LogWriter as GraphMachineRuntimeLog;
                if (graphLogger != null)
                {
                    return graphLogger;
                }

                start = start.Next;
            }

            return null;
        }

        /// <summary>
        /// Constructs a reproducable trace.
        /// </summary>
        private void ConstructReproducableTrace(SystematicTestingRuntime runtime)
        {
            StringBuilder stringBuilder = new StringBuilder();

            if (this.Strategy.IsFair())
            {
                stringBuilder.Append("--fair-scheduling").Append(Environment.NewLine);
            }

            if (this.Configuration.EnableCycleDetection)
            {
                stringBuilder.Append("--cycle-detection").Append(Environment.NewLine);
                stringBuilder.Append("--liveness-temperature-threshold:" +
                    this.Configuration.LivenessTemperatureThreshold).
                    Append(Environment.NewLine);
            }
            else
            {
                stringBuilder.Append("--liveness-temperature-threshold:" +
                    this.Configuration.LivenessTemperatureThreshold).
                    Append(Environment.NewLine);
            }

            if (!string.IsNullOrEmpty(this.Configuration.TestMethodName))
            {
                stringBuilder.Append("--test-method:" +
                    this.Configuration.TestMethodName).
                    Append(Environment.NewLine);
            }

            for (int idx = 0; idx < runtime.Scheduler.ScheduleTrace.Count; idx++)
            {
                ScheduleStep step = runtime.Scheduler.ScheduleTrace[idx];
                if (step.Type == ScheduleStepType.SchedulingChoice)
                {
                    stringBuilder.Append($"({step.ScheduledOperationId})");
                }
                else if (step.BooleanChoice != null)
                {
                    stringBuilder.Append(step.BooleanChoice.Value);
                }
                else
                {
                    stringBuilder.Append(step.IntegerChoice.Value);
                }

                if (idx < runtime.Scheduler.ScheduleTrace.Count - 1)
                {
                    stringBuilder.Append(Environment.NewLine);
                }
            }

            this.ReproducableTrace = stringBuilder.ToString();
        }

        /// <summary>
        /// Returns true if the engine should print the current iteration.
        /// </summary>
        private bool ShouldPrintIteration(int iteration)
        {
            if (iteration > this.PrintGuard * 10)
            {
                var count = iteration.ToString().Length - 1;
                var guard = "1" + (count > 0 ? string.Concat(Enumerable.Repeat("0", count)) : string.Empty);
                this.PrintGuard = int.Parse(guard);
            }

            return iteration % this.PrintGuard == 0;
        }
    }
}
