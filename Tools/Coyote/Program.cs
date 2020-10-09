// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Coyote.IO;
using Microsoft.Coyote.Rewriting;
using Microsoft.Coyote.SystematicTesting;
using Microsoft.Coyote.Telemetry;
using Microsoft.Coyote.Utilities;

namespace Microsoft.Coyote
{
    /// <summary>
    /// Entry point to the Coyote tool.
    /// </summary>
    internal class Program
    {
        private static CoyoteTelemetryClient TelemetryClient;
        private static TextWriter StdOut;
        private static TextWriter StdError;

        private static readonly object ConsoleLock = new object();

        private static void Main(string[] args)
        {
            // Save these so we can force output to happen even if TestingProcess has re-routed it.
            StdOut = Console.Out;
            StdError = Console.Error;
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            Console.CancelKeyPress += OnProcessCanceled;

            // Parses the command line options to get the configuration and rewritingOptions.

            var configuration = Configuration.Create();
            configuration.TelemetryServerPath = typeof(Program).Assembly.Location;
            var rewritingOptions = new RewritingOptions();

            var result = CoyoteTelemetryClient.GetOrCreateMachineId().Result;
            bool firstTime = result.Item2;

            var options = new CommandLineOptions();
            if (!options.Parse(args, configuration, rewritingOptions))
            {
                options.PrintHelp(Console.Out);
                if (!firstTime && configuration.EnableTelemetry)
                {
                    CoyoteTelemetryClient.PrintTelemetryMessage(Console.Out);
                }

                Environment.Exit(1);
            }

            configuration.PlatformVersion = GetPlatformVersion();

            if (!configuration.RunAsParallelBugFindingTask)
            {
                if (firstTime)
                {
                    string version = typeof(Microsoft.Coyote.Runtime.CoyoteRuntime).Assembly.GetName().Version.ToString();
                    Console.WriteLine("Welcome to Microsoft Coyote {0}", version);
                    Console.WriteLine("----------------------------{0}", new string('-', version.Length));
                    if (configuration.EnableTelemetry)
                    {
                        CoyoteTelemetryClient.PrintTelemetryMessage(Console.Out);
                    }

                    TelemetryClient = new CoyoteTelemetryClient(configuration);
                    TelemetryClient.TrackEventAsync("welcome").Wait();
                }

                Console.WriteLine("Microsoft (R) Coyote version {0} for .NET{1}",
                    typeof(CommandLineOptions).Assembly.GetName().Version,
                    GetDotNetVersion());
                Console.WriteLine("Copyright (C) Microsoft Corporation. All rights reserved.");
                Console.WriteLine();
            }

            SetEnvironment(configuration);

            switch (configuration.ToolCommand.ToLower())
            {
                case "test":
                    RunTest(configuration);
                    break;
                case "replay":
                    ReplayTest(configuration);
                    break;
                case "rewrite":
                    RewriteAssemblies(configuration, rewritingOptions);
                    break;
                case "telemetry":
                    RunServer(configuration);
                    break;
            }
        }

        public static void RunServer(Configuration configuration)
        {
            CoyoteTelemetryServer server = new CoyoteTelemetryServer(configuration.IsVerbose);
            server.RunServerAsync().Wait();
        }

        private static void SetEnvironment(Configuration config)
        {
            if (!string.IsNullOrEmpty(config.AdditionalPaths))
            {
                string path = Environment.GetEnvironmentVariable("PATH");
                Environment.SetEnvironmentVariable("PATH", path + Path.PathSeparator + config.AdditionalPaths);
            }
        }

        /// <summary>
        /// Runs the test specified in the configuration.
        /// </summary>
        private static void RunTest(Configuration configuration)
        {
            if (configuration.RunAsParallelBugFindingTask)
            {
                // This is being run as the child test process.
                if (configuration.ParallelDebug)
                {
                    Console.WriteLine("Attach the debugger and press ENTER to continue...");
                    Console.ReadLine();
                }

                // Load the configuration of the assembly to be tested.
                LoadAssemblyConfiguration(configuration.AssemblyToBeAnalyzed);

                TestingProcess testingProcess = TestingProcess.Create(configuration);
                testingProcess.Run();
                return;
            }

            if (configuration.ReportCodeCoverage || configuration.ReportActivityCoverage)
            {
                // This has to be here because both forms of coverage require it.
                CodeCoverageInstrumentation.SetOutputDirectory(configuration, makeHistory: true);
            }

            if (configuration.ReportCodeCoverage)
            {
                // Instruments the program under test for code coverage.
                CodeCoverageInstrumentation.Instrument(configuration);

                // Starts monitoring for code coverage.
                CodeCoverageMonitor.Start(configuration);
            }

            Console.WriteLine(". Testing " + configuration.AssemblyToBeAnalyzed);
            if (!string.IsNullOrEmpty(configuration.TestMethodName))
            {
                Console.WriteLine("... Method {0}", configuration.TestMethodName);
            }

            // Creates and runs the testing process scheduler.
            TestingProcessScheduler.Create(configuration).Run();
        }

        /// <summary>
        /// Replays an execution that is specified in the configuration.
        /// </summary>
        private static void ReplayTest(Configuration configuration)
        {
            // Set some replay specific options.
            configuration.SchedulingStrategy = "replay";
            configuration.EnableColoredConsoleOutput = true;
            configuration.DisableEnvironmentExit = false;

            // Load the configuration of the assembly to be replayed.
            LoadAssemblyConfiguration(configuration.AssemblyToBeAnalyzed);

            Console.WriteLine($". Replaying {configuration.ScheduleFile}");
            TestingEngine engine = TestingEngine.Create(configuration);
            engine.Run();
            Console.WriteLine(engine.GetReport());
        }

        /// <summary>
        /// Rewrites the assemblies specified in the configuration.
        /// </summary>
        private static void RewriteAssemblies(Configuration configuration, RewritingOptions options)
        {
            try
            {
                string assemblyDir = null;
                var fileList = new HashSet<string>();
                if (!string.IsNullOrEmpty(configuration.AssemblyToBeAnalyzed))
                {
                    var fullPath = Path.GetFullPath(configuration.AssemblyToBeAnalyzed);
                    Console.WriteLine($". Rewriting {fullPath}");
                    assemblyDir = Path.GetDirectoryName(fullPath);
                    fileList.Add(fullPath);
                }
                else if (Directory.Exists(configuration.RewritingOptionsPath))
                {
                    assemblyDir = Path.GetFullPath(configuration.RewritingOptionsPath);
                    Console.WriteLine($". Rewriting the assemblies specified in {assemblyDir}");
                }

                RewritingOptions config = options;

                if (!string.IsNullOrEmpty(assemblyDir))
                {
                    // Create a new RewritingOptions object from command line args only.
                    config.AssembliesDirectory = assemblyDir;
                    config.OutputDirectory = assemblyDir;
                    config.AssemblyPaths = fileList;
                }
                else
                {
                    // Load options from JSON file.
                    config = RewritingOptions.ParseFromJSON(configuration.RewritingOptionsPath);
                    Console.WriteLine($". Rewriting the assemblies specified in {config}");
                    config.PlatformVersion = configuration.PlatformVersion;

                    // allow command line options to override the json file.
                    if (!string.IsNullOrEmpty(options.StrongNameKeyFile))
                    {
                        config.StrongNameKeyFile = options.StrongNameKeyFile;
                    }

                    if (options.IsRewritingDependencies)
                    {
                        config.IsRewritingDependencies = options.IsRewritingDependencies;
                    }

                    if (options.IsRewritingThreads)
                    {
                        config.IsRewritingThreads = options.IsRewritingThreads;
                    }

                    if (options.IsRewritingUnitTests)
                    {
                        config.IsRewritingUnitTests = options.IsRewritingUnitTests;
                    }
                }

                RewritingEngine.Run(configuration, config);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.StackTrace);
                Error.ReportAndExit(ex.Message);
            }
        }

        /// <summary>
        /// Loads the configuration of the specified assembly.
        /// </summary>
        private static void LoadAssemblyConfiguration(string assemblyFile)
        {
            // Load config file and absorb its settings.
            try
            {
                var configFile = System.Configuration.ConfigurationManager.OpenExeConfiguration(assemblyFile);
                var settings = configFile.AppSettings.Settings;
                foreach (var key in settings.AllKeys)
                {
                    if (System.Configuration.ConfigurationManager.AppSettings.Get(key) is null)
                    {
                        System.Configuration.ConfigurationManager.AppSettings.Set(key, settings[key].Value);
                    }
                    else
                    {
                        System.Configuration.ConfigurationManager.AppSettings.Add(key, settings[key].Value);
                    }
                }
            }
            catch (System.Configuration.ConfigurationErrorsException ex)
            {
                Error.Report(ex.Message);
            }
        }

        /// <summary>
        /// Callback invoked when the current process terminates.
        /// </summary>
        private static void OnProcessExit(object sender, EventArgs e) => Shutdown();

        /// <summary>
        /// Callback invoked when the current process is canceled.
        /// </summary>
        private static void OnProcessCanceled(object sender, EventArgs e)
        {
            if (!TestingProcessScheduler.IsProcessCanceled)
            {
                TestingProcessScheduler.IsProcessCanceled = true;
                Shutdown();
            }
        }

        /// <summary>
        /// Callback invoked when an unhandled exception occurs.
        /// </summary>
        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            ReportUnhandledException((Exception)args.ExceptionObject);
            Environment.Exit(1);
        }

        private static void ReportUnhandledException(Exception ex)
        {
            Console.SetOut(StdOut);
            Console.SetError(StdError);

            PrintException(ex);
            for (var inner = ex.InnerException; inner != null; inner = inner.InnerException)
            {
                PrintException(inner);
            }
        }

        private static void PrintException(Exception ex)
        {
            lock (ConsoleLock)
            {
                if (ex is ExecutionCanceledException)
                {
                    Error.Report("[CoyoteTester] unhandled exception: {0}: {1}", ex.GetType().ToString(),
                        "This can mean you have a code path that is not controlled by the runtime that threw an unhandled exception. " +
                        "This typically happens when you create a 'System.Threading.Tasks.Task' instead of 'Microsoft.Coyote.Tasks.Task' " +
                        "or create a 'Task' inside a 'StateMachine' handler. One known issue that causes this is using 'async void' " +
                        "methods, which is not supported.");
                    StdOut.WriteLine(ex.StackTrace);
                }
                else
                {
                    Error.Report("[CoyoteTester] unhandled exception: {0}: {1}", ex.GetType().ToString(), ex.Message);
                    StdOut.WriteLine(ex.StackTrace);
                }
            }
        }

        /// <summary>
        /// Shutdowns any active monitors.
        /// </summary>
        private static void Shutdown()
        {
            if (CodeCoverageMonitor.IsRunning)
            {
                Console.WriteLine(". Shutting down the code coverage monitor, this may take a few seconds...");

                // Stops monitoring for code coverage.
                CodeCoverageMonitor.Stop();
            }

            using (TelemetryClient)
            {
            }
        }

        private static string GetDotNetVersion()
        {
            var path = typeof(string).Assembly.Location;
            string result = string.Empty;

            string[] parts = path.Replace("\\", "/").Split('/');
            if (path.Contains("Microsoft.NETCore"))
            {
                result += " Core";
            }

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

        /// <summary>
        /// Returns the .NET platform version this assembly was compiled for.
        /// </summary>
        private static string GetPlatformVersion()
        {
#if NET5_0
            return "net5.0";
#elif NET48
            return "net48";
#elif NET47
            return "net47";
#elif NETSTANDARD2_1
            return "netstandard2.1";
#elif NETSTANDARD2_0
            return "netstandard2.0";
#elif NETSTANDARD
            return "netstandard";
#elif NETCOREAPP3_1
            return "netcoreapp3.1";
#elif NETCOREAPP
            return "netcoreapp";
#elif NETFRAMEWORK
            return "net";
#endif
        }
    }
}
