// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
#if NET46
using System.Configuration;
#endif
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Coyote.IO;
using Microsoft.Coyote.TestingServices.Runtime;
using Microsoft.Coyote.TestingServices.Scheduling;
using Microsoft.Coyote.TestingServices.Scheduling.Strategies;
using Microsoft.Coyote.TestingServices.Tracing.Schedule;
using Microsoft.Coyote.Utilities;

namespace Microsoft.Coyote.TestingServices
{
    /// <summary>
    /// The Coyote abstract testing engine.
    /// </summary>
    internal abstract class AbstractTestingEngine : ITestingEngine
    {
        /// <summary>
        /// Configuration.
        /// </summary>
        internal Configuration Configuration;

        /// <summary>
        /// The Coyote assembly to analyze.
        /// </summary>
        internal Assembly Assembly;

        /// <summary>
        /// The assembly that provides the Coyote runtime to use during testing.
        /// If its null, the engine uses the default Coyote testing runtime.
        /// </summary>
        internal Assembly RuntimeAssembly;

        /// <summary>
        /// The Coyote test runtime factory method.
        /// </summary>
        internal MethodInfo TestRuntimeFactoryMethod;

        /// <summary>
        /// The Coyote test initialization method.
        /// </summary>
        internal MethodInfo TestInitMethod;

        /// <summary>
        /// The Coyote test dispose method.
        /// </summary>
        internal MethodInfo TestDisposeMethod;

        /// <summary>
        /// The Coyote test dispose method per iteration.
        /// </summary>
        internal MethodInfo TestIterationDisposeMethod;

        /// <summary>
        /// The action to test.
        /// </summary>
        internal Action<ICoyoteRuntime> TestAction;

        /// <summary>
        /// The function to test.
        /// </summary>
        internal Func<ICoyoteRuntime, Task> TestFunction;

        /// <summary>
        /// The name of the test.
        /// </summary>
        internal string TestName;

        /// <summary>
        /// Set of callbacks to invoke at the end
        /// of each iteration.
        /// </summary>
        protected ISet<Action<int>> PerIterationCallbacks;

        /// <summary>
        /// The installed logger.
        /// </summary>
        protected IO.ILogger Logger;

        /// <summary>
        /// The bug-finding scheduling strategy.
        /// </summary>
        protected ISchedulingStrategy Strategy;

        /// <summary>
        /// Random number generator used by the scheduling strategies.
        /// </summary>
        protected IRandomNumberGenerator RandomNumberGenerator;

        /// <summary>
        /// The error reporter.
        /// </summary>
        protected ErrorReporter ErrorReporter;

        /// <summary>
        /// The profiler.
        /// </summary>
        protected Profiler Profiler;

        /// <summary>
        /// The testing task cancellation token source.
        /// </summary>
        protected CancellationTokenSource CancellationTokenSource;

        /// <summary>
        /// A guard for printing info.
        /// </summary>
        protected int PrintGuard;

        /// <summary>
        /// Data structure containing information
        /// gathered during testing.
        /// </summary>
        public TestReport TestReport { get; set; }

        /// <summary>
        /// Runs the Coyote testing engine.
        /// </summary>
        public ITestingEngine Run()
        {
            try
            {
                Task task = this.CreateTestingTask();

                if (this.Configuration.AttachDebugger)
                {
                    System.Diagnostics.Debugger.Launch();
                }

                if (this.Configuration.Timeout > 0)
                {
                    this.CancellationTokenSource.CancelAfter(
                        this.Configuration.Timeout * 1000);
                }

                this.Profiler.StartMeasuringExecutionTime();

                task.Start();
                task.Wait(this.CancellationTokenSource.Token);
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
                    Debug.WriteLine(ex.Message);
                    Debug.WriteLine(ex.StackTrace);
                    return true;
                });

                if (aex.InnerException is FileNotFoundException)
                {
                    Error.ReportAndExit($"{aex.InnerException.Message}");
                }

                Error.ReportAndExit("Exception thrown during testing outside the context of a " +
                    "machine, possibly in a test method. Please use " +
                    "/debug /v:2 to print more information.");
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
        /// Stops the Coyote testing engine.
        /// </summary>
        public void Stop()
        {
            this.CancellationTokenSource.Cancel();
        }

        /// <summary>
        /// Tries to emit the testing traces, if any.
        /// </summary>
        public virtual void TryEmitTraces(string directory, string file)
        {
            // No-op, must be implemented in subclass.
        }

        /// <summary>
        /// Registers a callback to invoke at the end of each iteration. The callback takes as
        /// a parameter an integer representing the current iteration.
        /// </summary>
        public void RegisterPerIterationCallBack(Action<int> callback)
        {
            this.PerIterationCallbacks.Add(callback);
        }

        /// <summary>
        /// Returns a report with the testing results.
        /// </summary>
        public abstract string Report();

        /// <summary>
        /// Initializes a new instance of the <see cref="AbstractTestingEngine"/> class.
        /// </summary>
        protected AbstractTestingEngine(Configuration configuration)
        {
            this.Initialize(configuration);

            try
            {
                this.Assembly = Assembly.LoadFrom(configuration.AssemblyToBeAnalyzed);
            }
            catch (FileNotFoundException ex)
            {
                Error.ReportAndExit(ex.Message);
            }

#if NET46
            // Load config file and absorb its settings.
            try
            {
                var configFile = ConfigurationManager.OpenExeConfiguration(configuration.AssemblyToBeAnalyzed);

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

            if (!string.IsNullOrEmpty(configuration.TestingRuntimeAssembly))
            {
                try
                {
                    this.RuntimeAssembly = Assembly.LoadFrom(configuration.TestingRuntimeAssembly);
                }
                catch (FileNotFoundException ex)
                {
                    Error.ReportAndExit(ex.Message);
                }

                this.FindRuntimeFactoryMethod();
            }

            this.FindEntryPoint();
            this.TestInitMethod = this.FindTestMethod(typeof(TestInitAttribute));
            this.TestDisposeMethod = this.FindTestMethod(typeof(TestDisposeAttribute));
            this.TestIterationDisposeMethod = this.FindTestMethod(typeof(TestIterationDisposeAttribute));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AbstractTestingEngine"/> class.
        /// </summary>
        protected AbstractTestingEngine(Configuration configuration, Assembly assembly)
        {
            this.Initialize(configuration);
            this.Assembly = assembly;
            this.FindEntryPoint();
            this.TestInitMethod = this.FindTestMethod(typeof(TestInitAttribute));
            this.TestDisposeMethod = this.FindTestMethod(typeof(TestDisposeAttribute));
            this.TestIterationDisposeMethod = this.FindTestMethod(typeof(TestIterationDisposeAttribute));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AbstractTestingEngine"/> class.
        /// </summary>
        protected AbstractTestingEngine(Configuration configuration, Action<ICoyoteRuntime> action)
        {
            this.Initialize(configuration);
            this.TestAction = action;
        }

        protected AbstractTestingEngine(Configuration configuration, Func<ICoyoteRuntime, Task> function)
        {
            this.Initialize(configuration);
            this.TestFunction = function;
        }

        /// <summary>
        /// Initialized the testing engine.
        /// </summary>
        private void Initialize(Configuration configuration)
        {
            this.Configuration = configuration;
            this.Logger = new ConsoleLogger(true);
            this.ErrorReporter = new ErrorReporter(this.Configuration, this.Logger);
            this.Profiler = new Profiler();

            this.PerIterationCallbacks = new HashSet<Action<int>>();

            // Initializes scheduling strategy specific components.
            this.SetRandomNumberGenerator();

            this.TestReport = new TestReport(this.Configuration);
            this.CancellationTokenSource = new CancellationTokenSource();
            this.PrintGuard = 1;

            if (this.Configuration.SchedulingStrategy == SchedulingStrategy.Interactive)
            {
                this.Strategy = new InteractiveStrategy(this.Configuration, this.Logger);
                this.Configuration.SchedulingIterations = 1;
                this.Configuration.PerformFullExploration = false;
                this.Configuration.IsVerbose = true;
            }
            else if (this.Configuration.SchedulingStrategy == SchedulingStrategy.Replay)
            {
                var scheduleDump = this.GetScheduleForReplay(out bool isFair);
                ScheduleTrace schedule = new ScheduleTrace(scheduleDump);
                this.Strategy = new ReplayStrategy(this.Configuration, schedule, isFair);
            }
            else if (this.Configuration.SchedulingStrategy == SchedulingStrategy.Random)
            {
                this.Strategy = new RandomStrategy(this.Configuration.MaxFairSchedulingSteps, this.RandomNumberGenerator);
            }
            else if (this.Configuration.SchedulingStrategy == SchedulingStrategy.ProbabilisticRandom)
            {
                this.Strategy = new ProbabilisticRandomStrategy(
                    this.Configuration.MaxFairSchedulingSteps,
                    this.Configuration.CoinFlipBound,
                    this.RandomNumberGenerator);
            }
            else if (this.Configuration.SchedulingStrategy == SchedulingStrategy.PCT)
            {
                this.Strategy = new PCTStrategy(this.Configuration.MaxUnfairSchedulingSteps, this.Configuration.PrioritySwitchBound,
                    this.RandomNumberGenerator);
            }
            else if (this.Configuration.SchedulingStrategy == SchedulingStrategy.FairPCT)
            {
                var prefixLength = this.Configuration.SafetyPrefixBound == 0 ?
                    this.Configuration.MaxUnfairSchedulingSteps : this.Configuration.SafetyPrefixBound;
                var prefixStrategy = new PCTStrategy(prefixLength, this.Configuration.PrioritySwitchBound, this.RandomNumberGenerator);
                var suffixStrategy = new RandomStrategy(this.Configuration.MaxFairSchedulingSteps, this.RandomNumberGenerator);
                this.Strategy = new ComboStrategy(prefixStrategy, suffixStrategy);
            }
            else if (this.Configuration.SchedulingStrategy == SchedulingStrategy.DFS)
            {
                this.Strategy = new DFSStrategy(this.Configuration.MaxUnfairSchedulingSteps);
                this.Configuration.PerformFullExploration = false;
            }
            else if (this.Configuration.SchedulingStrategy == SchedulingStrategy.IDDFS)
            {
                this.Strategy = new IterativeDeepeningDFSStrategy(this.Configuration.MaxUnfairSchedulingSteps);
                this.Configuration.PerformFullExploration = false;
            }
            else if (this.Configuration.SchedulingStrategy == SchedulingStrategy.DPOR)
            {
                this.Strategy = new DPORStrategy(null, -1, this.Configuration.MaxUnfairSchedulingSteps);
            }
            else if (this.Configuration.SchedulingStrategy == SchedulingStrategy.RDPOR)
            {
                this.Strategy = new DPORStrategy(this.RandomNumberGenerator, -1, this.Configuration.MaxFairSchedulingSteps);
            }
            else if (this.Configuration.SchedulingStrategy == SchedulingStrategy.DelayBounding)
            {
                this.Strategy = new ExhaustiveDelayBoundingStrategy(this.Configuration.MaxUnfairSchedulingSteps,
                    this.Configuration.DelayBound, this.RandomNumberGenerator);
            }
            else if (this.Configuration.SchedulingStrategy == SchedulingStrategy.RandomDelayBounding)
            {
                this.Strategy = new RandomDelayBoundingStrategy(this.Configuration.MaxUnfairSchedulingSteps,
                    this.Configuration.DelayBound, this.RandomNumberGenerator);
            }
            else if (this.Configuration.SchedulingStrategy == SchedulingStrategy.Portfolio)
            {
                Error.ReportAndExit("Portfolio testing strategy is only " +
                    "available in parallel testing.");
            }

            if (this.Configuration.SchedulingStrategy != SchedulingStrategy.Replay &&
                this.Configuration.ScheduleFile.Length > 0)
            {
                var scheduleDump = this.GetScheduleForReplay(out bool isFair);
                ScheduleTrace schedule = new ScheduleTrace(scheduleDump);
                this.Strategy = new ReplayStrategy(this.Configuration, schedule, isFair, this.Strategy);
            }
        }

        /// <summary>
        /// Finds the testing runtime factory method, if one is provided.
        /// </summary>
        private void FindRuntimeFactoryMethod()
        {
            BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.DeclaredOnly | BindingFlags.InvokeMethod;
            List<MethodInfo> runtimeFactoryMethods = this.FindTestMethodsWithAttribute(typeof(TestRuntimeCreateAttribute), flags, this.RuntimeAssembly);
            if (runtimeFactoryMethods.Count == 0)
            {
                Error.ReportAndExit($"Failed to find a testing runtime factory method in the '{this.RuntimeAssembly.FullName}' assembly.");
            }
            else if (runtimeFactoryMethods.Count > 1)
            {
                Error.ReportAndExit("Only one testing runtime factory method can be declared with " +
                    $"the attribute '{typeof(TestRuntimeCreateAttribute).FullName}'. " +
                    $"'{runtimeFactoryMethods.Count}' factory methods were found instead.");
            }

            if (runtimeFactoryMethods[0].ReturnType != typeof(SystematicTestingRuntime) ||
                runtimeFactoryMethods[0].ContainsGenericParameters ||
                runtimeFactoryMethods[0].IsAbstract || runtimeFactoryMethods[0].IsVirtual ||
                runtimeFactoryMethods[0].IsConstructor ||
                runtimeFactoryMethods[0].IsPublic || !runtimeFactoryMethods[0].IsStatic ||
                runtimeFactoryMethods[0].GetParameters().Length != 2 ||
                runtimeFactoryMethods[0].GetParameters()[0].ParameterType != typeof(Configuration) ||
                runtimeFactoryMethods[0].GetParameters()[1].ParameterType != typeof(ISchedulingStrategy))
            {
                Error.ReportAndExit("Incorrect test runtime factory method declaration. Please " +
                    "declare the method as follows:\n" +
                    $"  [{typeof(TestRuntimeCreateAttribute).FullName}] internal static SystematicTestingRuntime " +
                    $"{runtimeFactoryMethods[0].Name}(Configuration configuration, ISchedulingStrategy strategy) {{ ... }}");
            }

            this.TestRuntimeFactoryMethod = runtimeFactoryMethods[0];
        }

        /// <summary>
        /// Finds the entry point to the Coyote program.
        /// </summary>
        private void FindEntryPoint()
        {
            BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.InvokeMethod;
            List<MethodInfo> testMethods = this.FindTestMethodsWithAttribute(typeof(TestAttribute), flags, this.Assembly);

            // Filter by test method name
            var filteredTestMethods = testMethods
                .FindAll(mi => string.Format("{0}.{1}", mi.DeclaringType.FullName, mi.Name)
                .EndsWith(this.Configuration.TestMethodName));

            if (filteredTestMethods.Count == 0)
            {
                if (testMethods.Count > 0)
                {
                    var msg = "Cannot detect a Coyote test method with name " + this.Configuration.TestMethodName +
                        ". Possible options are: " + Environment.NewLine;
                    foreach (var mi in testMethods)
                    {
                        msg += string.Format("{0}.{1}{2}", mi.DeclaringType.FullName, mi.Name, Environment.NewLine);
                    }

                    Error.ReportAndExit(msg);
                }
                else
                {
                    Error.ReportAndExit("Cannot detect a Coyote test method. Use the " +
                        $"attribute '[{typeof(TestAttribute).FullName}]' to declare a test method.");
                }
            }
            else if (filteredTestMethods.Count > 1)
            {
                var msg = "Only one test method to the Coyote program can " +
                    $"be declared with the attribute '{typeof(TestAttribute).FullName}'. " +
                    $"'{testMethods.Count}' test methods were found instead. Provide " +
                    $"/method flag to qualify the test method name you wish to use. " +
                    "Possible options are: " + Environment.NewLine;

                foreach (var mi in testMethods)
                {
                    msg += string.Format("{0}.{1}{2}", mi.DeclaringType.FullName, mi.Name, Environment.NewLine);
                }

                Error.ReportAndExit(msg);
            }

            var testMethod = filteredTestMethods[0];

            bool hasExpectedReturnType = (testMethod.ReturnType == typeof(void) &&
                testMethod.GetCustomAttribute(typeof(AsyncStateMachineAttribute)) == null) ||
                (testMethod.ReturnType == typeof(Task) &&
                testMethod.GetCustomAttribute(typeof(AsyncStateMachineAttribute)) != null);
            bool hasExpectedParameters = !testMethod.ContainsGenericParameters &&
                testMethod.GetParameters().Length is 1 &&
                testMethod.GetParameters()[0].ParameterType == typeof(ICoyoteRuntime);

            if (testMethod.IsAbstract || testMethod.IsVirtual || testMethod.IsConstructor ||
                !testMethod.IsPublic || !testMethod.IsStatic ||
                !hasExpectedReturnType || !hasExpectedParameters)
            {
                Error.ReportAndExit("Incorrect test method declaration. Please " +
                    "declare the test method as follows:\n" +
                    $"  [{typeof(TestAttribute).FullName}] public static void " +
                    $"{testMethod.Name}(ICoyoteRuntime runtime) {{ ... }}\n" +
                    "or:\n" +
                    $"  [{typeof(TestAttribute).FullName}] public static async Task " +
                    $"{testMethod.Name}(ICoyoteRuntime runtime) {{ ... }}");
            }

            if (testMethod.ReturnType == typeof(void))
            {
                this.TestAction = (Action<ICoyoteRuntime>)Delegate.CreateDelegate(typeof(Action<ICoyoteRuntime>), testMethod);
            }
            else
            {
                this.TestFunction = (Func<ICoyoteRuntime, Task>)Delegate.CreateDelegate(typeof(Func<ICoyoteRuntime, Task>), testMethod);
            }

            this.TestName = $"{testMethod.DeclaringType}.{testMethod.Name}";
        }

        /// <summary>
        /// Finds the test method with the specified attribute.
        /// Returns null if no such method is found.
        /// </summary>
        private MethodInfo FindTestMethod(Type attribute)
        {
            BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.InvokeMethod;
            List<MethodInfo> testMethods = this.FindTestMethodsWithAttribute(attribute, flags, this.Assembly);

            if (testMethods.Count == 0)
            {
                return null;
            }
            else if (testMethods.Count > 1)
            {
                Error.ReportAndExit("Only one test method to the Coyote program can " +
                    $"be declared with the attribute '{attribute.FullName}'. " +
                    $"'{testMethods.Count}' test methods were found instead.");
            }

            if (testMethods[0].ReturnType != typeof(void) ||
                testMethods[0].ContainsGenericParameters ||
                testMethods[0].IsAbstract || testMethods[0].IsVirtual ||
                testMethods[0].IsConstructor ||
                !testMethods[0].IsPublic || !testMethods[0].IsStatic ||
                testMethods[0].GetParameters().Length != 0)
            {
                Error.ReportAndExit("Incorrect test method declaration. Please " +
                    "declare the test method as follows:\n" +
                    $"  [{attribute.FullName}] public static void " +
                    $"{testMethods[0].Name}() {{ ... }}");
            }

            return testMethods[0];
        }

        /// <summary>
        /// Finds the test methods with the specified attribute in the given assembly.
        /// Returns an empty list if no such methods are found.
        /// </summary>
        private List<MethodInfo> FindTestMethodsWithAttribute(Type attribute, BindingFlags bindingFlags, Assembly assembly)
        {
            List<MethodInfo> testMethods = null;

            try
            {
                testMethods = assembly.GetTypes().SelectMany(t => t.GetMethods(bindingFlags)).
                    Where(m => m.GetCustomAttributes(attribute, false).Length > 0).ToList();
            }
            catch (ReflectionTypeLoadException ex)
            {
                foreach (var le in ex.LoaderExceptions)
                {
                    this.ErrorReporter.WriteErrorLine(le.Message);
                }

                Error.ReportAndExit($"Failed to load assembly '{assembly.FullName}'");
            }
            catch (Exception ex)
            {
                this.ErrorReporter.WriteErrorLine(ex.Message);
                Error.ReportAndExit($"Failed to load assembly '{assembly.FullName}'");
            }

            return testMethods;
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
                else if (line.Equals("--cycle-detection"))
                {
                    this.Configuration.EnableCycleDetection = true;
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
        /// Returns (and creates if it does not exist) the output directory.
        /// </summary>
        protected string GetOutputDirectory()
        {
            string directoryPath = Path.GetDirectoryName(this.Assembly.Location) +
                Path.DirectorySeparatorChar + "Output" + Path.DirectorySeparatorChar;
            Directory.CreateDirectory(directoryPath);
            return directoryPath;
        }

        /// <summary>
        /// Installs the specified <see cref="IO.ILogger"/>.
        /// </summary>
        public void SetLogger(IO.ILogger logger)
        {
            if (logger is null)
            {
                throw new InvalidOperationException("Cannot install a null logger.");
            }

            this.Logger.Dispose();
            this.Logger = logger;
            this.ErrorReporter.Logger = logger;
        }

        /// <summary>
        /// Sets the random number generator to be used by the scheduling strategy.
        /// </summary>
        private void SetRandomNumberGenerator()
        {
            int seed = this.Configuration.RandomSchedulingSeed ?? DateTime.Now.Millisecond;
            this.RandomNumberGenerator = new DefaultRandomNumberGenerator(seed);
        }
    }
}
