// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Coyote.Cli;
using Microsoft.Coyote.IO;
using Microsoft.Coyote.Rewriting;
using Microsoft.Coyote.SystematicTesting;
using Microsoft.Coyote.Telemetry;

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

        private static int Main(string[] args)
        {
            // Save these so we can force output to happen even if they have been re-routed.
            StdOut = Console.Out;
            StdError = Console.Error;

            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            Console.CancelKeyPress += OnProcessCanceled;

            // Parses the command line options to get the configuration and rewriting options.
            var configuration = Configuration.Create();
            configuration.TelemetryServerPath = typeof(Program).Assembly.Location;
            var rewritingOptions = RewritingOptions.Create();

            bool isFirstTime = CoyoteTelemetryClient.GetOrCreateMachineId().Result.Item2;
            var options = new CommandLineOptions();
            if (!options.Parse(args, configuration, rewritingOptions))
            {
                options.PrintHelp(Console.Out);
                if (!isFirstTime && configuration.EnableTelemetry)
                {
                    CoyoteTelemetryClient.PrintTelemetryMessage(Console.Out);
                }

                return (int)ExitCode.Error;
            }

            if (isFirstTime)
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

            ExitCode exitCode = ExitCode.Success;
            switch (configuration.ToolCommand.ToLower())
            {
                case "test":
                    exitCode = RunTest(configuration);
                    break;
                case "replay":
                    exitCode = ReplayTest(configuration);
                    break;
                case "rewrite":
                    exitCode = RewriteAssemblies(configuration, rewritingOptions);
                    break;
                case "telemetry":
                    RunServer(configuration);
                    break;
            }

            return (int)exitCode;
        }

        /// <summary>
        /// Runs the test specified in the configuration.
        /// </summary>
        private static ExitCode RunTest(Configuration configuration)
        {
            Console.WriteLine($". Testing '{configuration.AssemblyToBeAnalyzed}'");
            TestingEngine engine = TestingEngine.Create(configuration);
            engine.Run();

            string directory = OutputFileManager.CreateOutputDirectory(configuration);
            string fileName = OutputFileManager.GetResolvedFileName(configuration.AssemblyToBeAnalyzed, directory);

            // Emit the test reports.
            Console.WriteLine($"... Emitting trace-related reports:");
            if (engine.TryEmitReports(directory, fileName, out IEnumerable<string> reportPaths))
            {
                foreach (var path in reportPaths)
                {
                    Console.WriteLine($"..... Writing {path}");
                }
            }
            else
            {
                Console.WriteLine($"..... No test reports available.");
            }

            // Emit the coverage reports.
            Console.WriteLine($"... Emitting coverage reports:");
            if (engine.TryEmitCoverageReports(directory, fileName, out reportPaths))
            {
                foreach (var path in reportPaths)
                {
                    Console.WriteLine($"..... Writing {path}");
                }
            }
            else
            {
                Console.WriteLine($"..... No coverage reports available.");
            }

            Console.WriteLine(engine.TestReport.GetText(configuration, "..."));
            Console.WriteLine($"... Elapsed {engine.Profiler.Results()} sec.");
            return GetExitCodeFromTestReport(engine.TestReport);
        }

        /// <summary>
        /// Replays an execution that is specified in the configuration.
        /// </summary>
        private static ExitCode ReplayTest(Configuration configuration)
        {
            // Set some replay specific options.
            configuration.SchedulingStrategy = "replay";
            configuration.DisableEnvironmentExit = false;

            // Load the configuration of the assembly to be replayed.
            LoadAssemblyConfiguration(configuration.AssemblyToBeAnalyzed);

            Console.WriteLine($". Replaying '{configuration.ScheduleFile}'");
            TestingEngine engine = TestingEngine.Create(configuration);
            engine.Run();

            // Emit the report.
            Console.WriteLine(engine.GetReport());
            return GetExitCodeFromTestReport(engine.TestReport);
        }

        /// <summary>
        /// Rewrites the assemblies specified in the configuration.
        /// </summary>
        private static ExitCode RewriteAssemblies(Configuration configuration, RewritingOptions options)
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
                Console.WriteLine($"... Elapsed {profiler.Results()} sec.");
            }
            catch (Exception ex)
            {
                if (ex is AggregateException aex)
                {
                    ex = aex.Flatten().InnerException;
                }

                Error.Report(configuration.IsDebugVerbosityEnabled ? ex.ToString() : ex.Message);
                return ExitCode.Error;
            }

            return ExitCode.Success;
        }

        private static void RunServer(Configuration configuration)
        {
            CoyoteTelemetryServer server = new CoyoteTelemetryServer(configuration.IsVerbose);
            server.RunServerAsync().Wait();
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

        private static void SetEnvironment(Configuration configuration)
        {
            if (!string.IsNullOrEmpty(configuration.AdditionalPaths))
            {
                string path = Environment.GetEnvironmentVariable("PATH");
                Environment.SetEnvironmentVariable("PATH", path + Path.PathSeparator + configuration.AdditionalPaths);
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
            Environment.Exit((int)ExitCode.InternalError);
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

        private static ExitCode GetExitCodeFromTestReport(TestReport report) =>
            report.InternalErrors.Count > 0 ? ExitCode.InternalError :
            report.NumOfFoundBugs > 0 ? ExitCode.BugFound :
            ExitCode.Success;

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
