// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Coyote;
using Microsoft.Coyote.SystematicTesting;

namespace ImageGallery.Tests
{
    public static class Program
    {
        /// <summary>
        /// Uses the Coyote testing engine in replay mode to reproduce a buggy trace.
        /// </summary>
        /// <remarks>
        /// Learn more in our documentation: https://microsoft.github.io/coyote/how-to/unit-testing
        /// </remarks>
        public static void Main(string[] args)
        {
            // NOTE: This is just a hack to make it easy to invoke `coyote replay` for the
            // ASP.NET MSTest for the purposes of this sample.

            if (args.Length != 2)
            {
                Console.WriteLine($"Error: expecting arguments. Run as follows:");
                Console.WriteLine($"TraceReplayer.exe [NAME_OF_TEST] [PATH_TO_TRACE_FILE]");
                Environment.Exit(1);
            }

            Func<Task> test = GetTest(args[0]);
            string trace = GetTrace(args[1]);

            Debugger.Launch();

            Console.WriteLine($"Starting test...");
            var configuration = Configuration.Create().
                WithReproducibleTrace(trace).
                WithVerbosityEnabled();
            var testingEngine = TestingEngine.Create(configuration, test);
            testingEngine.Run();
            Console.WriteLine($"Done testing. Found {testingEngine.TestReport.NumOfFoundBugs} bugs.");
        }

        private static Func<Task> GetTest(string testName)
        {
            var tests = new UnitTests();
            Func<Task> test = null;
            if (testName is "TestConcurrentAccountRequests")
            {
                test = tests.TestConcurrentAccountRequestsAsync;
            }
            else if (testName is "TestConcurrentAccountAndImageRequests")
            {
                test = tests.TestConcurrentAccountAndImageRequestsAsync;
            }
            else
            {
                Console.WriteLine($"Error: uknown test name.");
                Environment.Exit(1);
            }

            return test;
        }

        private static string GetTrace(string traceFile)
        {
            if (!File.Exists(traceFile))
            {
                Console.WriteLine($"Error: trace file not found.");
                Environment.Exit(1);
            }

            return File.ReadAllText(traceFile);
        }
    }
}
