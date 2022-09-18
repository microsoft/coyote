// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Coyote;
using Microsoft.Coyote.SystematicTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ImageGallery.Tests
{
    [TestClass]
    public class SystematicTests
    {
        [TestMethod]
        public void TestConcurrentAccountRequests()
        {
            var tests = new UnitTests();
            RunSystematicTest(tests.TestConcurrentAccountRequestsAsync, "TestConcurrentAccountRequests"); 
        }

        [TestMethod]
        public void TestConcurrentAccountAndImageRequests()
        {
            var tests = new UnitTests();
            RunSystematicTest(tests.TestConcurrentAccountAndImageRequestsAsync, "TestConcurrentAccountAndImageRequests");
        }

        /// <summary>
        /// Invoke the Coyote systematic testing engine to run the specified test multiple iterations,
        /// each iteration exploring potentially different interleavings using some underlying program
        /// exploration strategy (by default a uniform probabilistic strategy).
        /// </summary>
        /// <remarks>
        /// Learn more in our documentation: https://microsoft.github.io/coyote/how-to/unit-testing
        /// </remarks>
        private static void RunSystematicTest(Func<Task> test, string testName)
        {
            Console.WriteLine($"Starting systematic test...");
            var configuration = Configuration.Create().
                WithTestingIterations(1000). // Change this to tweak the number of iterations.
                WithVerbosityEnabled();
            var testingEngine = TestingEngine.Create(configuration, test);
            testingEngine.Run();
            Console.WriteLine($"Done testing. Found {testingEngine.TestReport.NumOfFoundBugs} bugs.");
            if (testingEngine.TestReport.NumOfFoundBugs > 0)
            {
                var error = testingEngine.TestReport.BugReports.First();
                var traceFile = WriteReproducibleTrace(testingEngine.ReproducibleTrace, testName);

                Assert.Fail("Found bug: {0}\n   Replay trace using Coyote by running:\n     TraceReplayer.exe {1} {2}",
                    error, testName, traceFile);
            }
        }

        private static string WriteReproducibleTrace(string trace, string testName)
        {
            string assemblyPath = Assembly.GetAssembly(typeof(SystematicTests)).Location;
            string directory = Path.GetDirectoryName(assemblyPath);
            string traceFile = Path.Combine(directory, $"{testName}.trace");
            File.WriteAllText(traceFile, trace);
            return traceFile;
        }
    }
}
