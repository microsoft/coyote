// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Coyote.IO;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.TestingServices;
using Microsoft.Coyote.Utilities;

namespace Microsoft.Coyote
{
    /// <summary>
    /// The Coyote tester.
    /// </summary>
    internal class Program
    {
        private static Configuration Configuration;

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

            // Parses the command line options to get the Configuration.
            Configuration = new TesterCommandLineOptions().Parse(args);

            if (Configuration.RunAsParallelBugFindingTask)
            {
                // This is being run as the child test process.
                if (Configuration.ParallelDebug)
                {
                    Console.WriteLine("Attach Debugger and press ENTER to continue...");
                    Console.ReadLine();
                }

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
#if NET46
                // Instruments the program under test for code coverage.
                CodeCoverageInstrumentation.Instrument(Configuration);

                // Starts monitoring for code coverage.
                CodeCoverageMonitor.Start(Configuration);
#endif
            }

            Console.WriteLine(". Testing " + Configuration.AssemblyToBeAnalyzed);
            if (!string.IsNullOrEmpty(Configuration.TestMethodName))
            {
                Console.WriteLine("... Method {0}", Configuration.TestMethodName);
            }

            // Creates and runs the testing process scheduler.
            TestingProcessScheduler.Create(Configuration).Run();

            Console.WriteLine(". Done");
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
                        "This can mean you have a code path that is not controlled by the Coyote runtime that threw an unhandled exception. " +
                        "This typically happens when you create an explicit 'Task' instead of 'ControlledTask' or create a 'Task' inside a 'Machine' handler. " +
                        "One known issue that causes this is using 'async void' methods, which is not supported.");
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
#if NET46
            if (Configuration != null && Configuration.ReportCodeCoverage && CodeCoverageMonitor.IsRunning)
            {
                Console.WriteLine(". Shutting down the code coverage monitor, this may take a few seconds...");

                // Stops monitoring for code coverage.
                CodeCoverageMonitor.Stop();
                CodeCoverageInstrumentation.Restore();
            }
#endif
        }
    }
}
