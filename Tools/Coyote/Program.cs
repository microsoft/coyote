// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
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

            // Parses the command line options to get the configuration and rewriting options.
            var configuration = Configuration.Create();
            configuration.TelemetryServerPath = typeof(Program).Assembly.Location;
            var rewritingOptions = RewritingOptions.Create();

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

            if (firstTime)
            {
                string version = typeof(Runtime.CoyoteRuntime).Assembly.GetName().Version.ToString();
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
            Console.WriteLine("Copyright (C) Microsoft Corporation. All rights reserved.\n");

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
            if (configuration.IsActivityCoverageReported)
            {
                // This has to be here because both forms of coverage require it.
                CodeCoverageInstrumentation.SetOutputDirectory(configuration, makeHistory: true);
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
                if (options.AssemblyPaths.Count is 1)
                {
                    Console.WriteLine($". Rewriting {options.AssemblyPaths.First()}");
                }
                else
                {
                    Console.WriteLine($". Rewriting the assemblies specified in {options.AssembliesDirectory}");
                }

                var profiler = new Profiler();
                RewritingEngine.Run(options, configuration, profiler);
                Console.WriteLine($". Done rewriting in {profiler.Results()} sec");
            }
            catch (Exception ex)
            {
                if (ex is AggregateException aex)
                {
                    ex = aex.Flatten().InnerException;
                }

                Error.ReportAndExit(configuration.IsDebugVerbosityEnabled ? ex.ToString() : ex.Message);
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
        private static void OnProcessExit(object sender, EventArgs e) => TelemetryClient?.Dispose();

        /// <summary>
        /// Callback invoked when the current process is canceled.
        /// </summary>
        private static void OnProcessCanceled(object sender, EventArgs e) => TelemetryClient?.Dispose();

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
                Error.Report($"[CoyoteTester] unhandled exception: {ex}");
                StdOut.WriteLine(ex.StackTrace);
            }
        }

        private static string GetDotNetVersion()
        {
            var path = typeof(string).Assembly.Location;
            string result = string.Empty;

            string[] parts = path.Replace("\\", "/").Split('/');
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
