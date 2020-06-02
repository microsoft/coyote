// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using Microsoft.Coyote.IO;
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
        private static Configuration Configuration;
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

            // Parses the command line options to get the configuration.

            Configuration = Configuration.Create();
            Configuration.TelemetryServerPath = typeof(Program).Assembly.Location;

#if NETSTANDARD2_1
            Configuration.DotnetFrameworkVersion = "netstandard2.1";
#elif NETSTANDARD2_0
            Configuration.DotnetFrameworkVersion = "netstandard2.0";
#elif NETSTANDARD
            Configuration.DotnetFrameworkVersion = "netstandard";
#elif NETCOREAPP3_1
            Configuration.DotnetFrameworkVersion = "netcoreapp3.1";
#elif NETCOREAPP
            Configuration.DotnetFrameworkVersion = "netcoreapp";
#elif NET48
            Configuration.DotnetFrameworkVersion = "net48";
#elif NET47
            Configuration.DotnetFrameworkVersion = "net47";
#elif NETFRAMEWORK
            Configuration.DotnetFrameworkVersion = "net";
#endif

            var result = CoyoteTelemetryClient.GetOrCreateMachineId().Result;
            bool firstTime = result.Item2;

            var options = new CommandLineOptions();
            if (!options.Parse(args, Configuration))
            {
                options.PrintHelp(Console.Out);
                if (!firstTime && Configuration.EnableTelemetry)
                {
                    CoyoteTelemetryClient.PrintTelemetryMessage(Console.Out);
                }

                Environment.Exit(1);
            }

            if (!Configuration.RunAsParallelBugFindingTask)
            {
                if (firstTime)
                {
                    string version = typeof(Microsoft.Coyote.Runtime.CoyoteRuntime).Assembly.GetName().Version.ToString();
                    Console.WriteLine("Welcome to Microsoft Coyote {0}", version);
                    Console.WriteLine("----------------------------{0}", new string('-', version.Length));
                    if (Configuration.EnableTelemetry)
                    {
                        CoyoteTelemetryClient.PrintTelemetryMessage(Console.Out);
                    }

                    TelemetryClient = new CoyoteTelemetryClient(Configuration);
                    TelemetryClient.TrackEventAsync("welcome").Wait();
                }

                Console.WriteLine("Microsoft (R) Coyote version {0} for .NET{1}",
                    typeof(CommandLineOptions).Assembly.GetName().Version,
                    GetDotNetVersion());
                Console.WriteLine("Copyright (C) Microsoft Corporation. All rights reserved.");
                Console.WriteLine();
            }

            SetEnvironment(Configuration);

            switch (Configuration.ToolCommand.ToLower())
            {
                case "test":
                    RunTest();
                    break;
                case "replay":
                    ReplayTest();
                    break;
                case "telemetry":
                    RunServer();
                    break;
            }
        }

        public static void RunServer()
        {
            CoyoteTelemetryServer server = new CoyoteTelemetryServer(Configuration.IsVerbose);
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

        private static void RunTest()
        {
            if (Configuration.RunAsParallelBugFindingTask)
            {
                // This is being run as the child test process.
                if (Configuration.ParallelDebug)
                {
                    Console.WriteLine("Attach the debugger and press ENTER to continue...");
                    Console.ReadLine();
                }

                // Load the configuration of the assembly to be tested.
                LoadAssemblyConfiguration(Configuration.AssemblyToBeAnalyzed);

                TestingProcess testingProcess = TestingProcess.Create(Configuration);
                testingProcess.Run();
                return;
            }

            if (Configuration.ReportCodeCoverage || Configuration.ReportActivityCoverage)
            {
                // This has to be here because both forms of coverage require it.
                CodeCoverageInstrumentation.SetOutputDirectory(Configuration, makeHistory: true);
            }

            if (Configuration.ReportCodeCoverage)
            {
                // Instruments the program under test for code coverage.
                CodeCoverageInstrumentation.Instrument(Configuration);

                // Starts monitoring for code coverage.
                CodeCoverageMonitor.Start(Configuration);
            }

            Console.WriteLine(". Testing " + Configuration.AssemblyToBeAnalyzed);
            if (!string.IsNullOrEmpty(Configuration.TestMethodName))
            {
                Console.WriteLine("... Method {0}", Configuration.TestMethodName);
            }

            // Creates and runs the testing process scheduler.
            TestingProcessScheduler.Create(Configuration).Run();
        }

        private static void ReplayTest()
        {
            // Set some replay specific options.
            Configuration.SchedulingStrategy = "replay";
            Configuration.EnableColoredConsoleOutput = true;
            Configuration.DisableEnvironmentExit = false;

            // Load the configuration of the assembly to be replayed.
            LoadAssemblyConfiguration(Configuration.AssemblyToBeAnalyzed);

            Console.WriteLine($". Replaying {Configuration.ScheduleFile}");
            TestingEngine engine = TestingEngine.Create(Configuration);
            engine.Run();
            Console.WriteLine(engine.GetReport());
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
            if (Configuration != null && Configuration.ReportCodeCoverage && CodeCoverageMonitor.IsRunning)
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
    }
}
