// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.Runtime.Exploration;
using Microsoft.Coyote.Tasks;
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
        internal Configuration Configuration;

        /// <summary>
        /// The Coyote assembly to analyze.
        /// </summary>
        internal Assembly Assembly;

        /// <summary>
        /// The assembly that provides the runtime to use during testing.
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
        /// The method to test.
        /// </summary>
        internal Delegate TestMethod;

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
        /// <remarks>
        /// See <see href="/coyote/learn/advanced/logging" >Logging</see> for more information.
        /// </remarks>
        protected TextWriter Logger;

        /// <summary>
        /// The bug-finding scheduling strategy.
        /// </summary>
        protected ISchedulingStrategy Strategy;

        /// <summary>
        /// Random value generator used by the scheduling strategies.
        /// </summary>
        protected IRandomValueGenerator RandomValueGenerator;

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
        protected AbstractTestingEngine(Configuration configuration, Delegate testMethod)
        {
            this.Initialize(configuration);
            this.TestMethod = testMethod;
        }

        /// <summary>
        /// Initialized the testing engine.
        /// </summary>
        private void Initialize(Configuration configuration)
        {
            this.Configuration = configuration;
            this.Logger = new ConsoleLogger();
            this.ErrorReporter = new ErrorReporter(configuration, this.Logger);
            this.Profiler = new Profiler();

            this.PerIterationCallbacks = new HashSet<Action<int>>();

            // Initializes scheduling strategy specific components.
            this.RandomValueGenerator = new RandomValueGenerator(configuration);

            this.TestReport = new TestReport(configuration);
            this.CancellationTokenSource = new CancellationTokenSource();
            this.PrintGuard = 1;

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
        public virtual IEnumerable<string> TryEmitTraces(string directory, string file)
        {
            // No-op, must be implemented in subclass.
            throw new NotImplementedException();
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

            MethodInfo testMethod = filteredTestMethods[0];
            ParameterInfo[] testParams = testMethod.GetParameters();

            bool hasExpectedReturnType = (testMethod.ReturnType == typeof(void) &&
                testMethod.GetCustomAttribute(typeof(AsyncStateMachineAttribute)) == null) ||
                (testMethod.ReturnType == typeof(ControlledTask) &&
                testMethod.GetCustomAttribute(typeof(AsyncStateMachineAttribute)) != null);
            bool hasExpectedParameters = !testMethod.ContainsGenericParameters &&
                (testParams.Length is 0 ||
                (testParams.Length is 1 && testParams[0].ParameterType == typeof(IActorRuntime)));

            if (testMethod.IsAbstract || testMethod.IsVirtual || testMethod.IsConstructor ||
                !testMethod.IsPublic || !testMethod.IsStatic ||
                !hasExpectedReturnType || !hasExpectedParameters)
            {
                Error.ReportAndExit("Incorrect test method declaration. Please " +
                    "use one of the following supported declarations:\n\n" +
                    $"  [{typeof(TestAttribute).FullName}]\n" +
                    $"  public static void {testMethod.Name}() {{ ... }}\n\n" +
                    $"  [{typeof(TestAttribute).FullName}]\n" +
                    $"  public static void {testMethod.Name}(IActorRuntime runtime) {{ ... await ... }}\n\n" +
                    $"  [{typeof(TestAttribute).FullName}]\n" +
                    $"  public static async ControlledTask {testMethod.Name}() {{ ... }}\n\n" +
                    $"  [{typeof(TestAttribute).FullName}]\n" +
                    $"  public static async ControlledTask {testMethod.Name}(IActorRuntime runtime) {{ ... await ... }}");
            }

            if (testMethod.ReturnType == typeof(void) && testParams.Length == 1)
            {
                this.TestMethod = Delegate.CreateDelegate(typeof(Action<IActorRuntime>), testMethod);
            }
            else if (testMethod.ReturnType == typeof(void))
            {
                this.TestMethod = Delegate.CreateDelegate(typeof(Action), testMethod);
            }
            else if (testParams.Length == 1)
            {
                this.TestMethod = Delegate.CreateDelegate(typeof(Func<IActorRuntime, ControlledTask>), testMethod);
            }
            else
            {
                this.TestMethod = Delegate.CreateDelegate(typeof(Func<ControlledTask>), testMethod);
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
