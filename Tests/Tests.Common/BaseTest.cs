// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Coverage;
using Microsoft.Coyote.SystematicTesting;
using Microsoft.Coyote.SystematicTesting.Strategies;
using Microsoft.Coyote.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Tests.Common
{
    public delegate void TestErrorChecker(string error);

    public abstract class BaseTest
    {
        protected readonly ITestOutputHelper TestOutput;

        public BaseTest(ITestOutputHelper output)
        {
            this.TestOutput = output;
        }

        /// <summary>
        /// Override this to run the test under the Coyote TestEngine (default false).
        /// </summary>
        public virtual bool SystematicTest => false;

        protected static Configuration GetConfiguration()
        {
            return Configuration.Create();
        }

        protected static string RemoveNonDeterministicValuesFromReport(string report)
        {
            // Match a GUID or other ids (since they can be nondeterministic).
            report = Regex.Replace(report, @"\'[0-9|a-z|A-Z|-]{36}\'|\'[0-9]+\'|\'<unknown>\'", "''");
            report = Regex.Replace(report, @"\([^)]*\)", "()");
            report = Regex.Replace(report, @"\[[^)]*\]", "[]");
            report = Regex.Replace(report, "[\r\n]+", " ");

            // Match a namespace.
            return RemoveNamespaceReferencesFromReport(report).Trim();
        }

        protected static string SortLines(string text)
        {
            var list = new List<string>(text.Split('\n'));
            list.Sort();
            return string.Join("\n", list);
        }

        protected static string RemoveNamespaceReferencesFromReport(string report)
        {
            report = Regex.Replace(report, @"Microsoft.Coyote.Tests.Common\.", string.Empty);
            return Regex.Replace(report, @"Microsoft\.[^+]*\+", string.Empty);
        }

        protected static string RemoveExcessiveEmptySpaceFromReport(string report)
        {
            return Regex.Replace(report, @"\s+", " ");
        }

        protected static string RemoveStackTraceFromReport(string report,
            string removeUntilContainsText = "Microsoft.Coyote.SystematicTesting.Tests")
        {
            StringBuilder result = new StringBuilder();
            bool strip = false;
            foreach (var line in report.Split('\n'))
            {
                string trimmed = line.Trim('\r');
                string nows = trimmed.Trim();
                if (nows.StartsWith("<StackTrace>"))
                {
                    result.AppendLine("<StackTrace> ");
                    strip = true;
                }
                else if (strip && string.IsNullOrEmpty(nows))
                {
                    strip = false;
                    continue;
                }

                if (!strip)
                {
                    result.AppendLine(trimmed);
                }
                else if (strip && trimmed.Contains(removeUntilContainsText))
                {
                    result.AppendLine(trimmed);
                }
            }

            return result.ToString();
        }

        protected static string RemoveStackTraceFromXmlReport(string report)
        {
            StringBuilder result = new StringBuilder();
            bool strip = false;
            foreach (var line in report.Split('\n'))
            {
                string trimmed = line.Trim('\r');
                string nows = trimmed.Trim();
                if (nows.StartsWith("<AssertionFailure>&lt;StackTrace&gt;"))
                {
                    result.AppendLine("  <AssertionFailure>StackTrace:");
                    strip = true;
                }
                else if (strip && nows.StartsWith("</AssertionFailure>"))
                {
                    result.AppendLine("  </AssertionFailure>");
                    strip = false;
                    continue;
                }

                if (!strip)
                {
                    result.AppendLine(trimmed);
                }
                else if (strip && trimmed.Contains("Microsoft.Coyote.SystematicTesting.Tests"))
                {
                    result.AppendLine(trimmed);
                }
            }

            return result.ToString();
        }

        protected void Test(Action test, Configuration configuration = null)
        {
            if (this.SystematicTest)
            {
                this.InternalTest(test as Delegate, configuration);
            }
            else
            {
                this.Run((r) => test(), configuration);
            }
        }

        protected void Test(Action<IActorRuntime> test, Configuration configuration = null)
        {
            if (this.SystematicTest)
            {
                this.InternalTest(test as Delegate, configuration);
            }
            else
            {
                this.Run(test, configuration);
            }
        }

        protected void Test(Func<Task> test, Configuration configuration = null)
        {
            if (this.SystematicTest)
            {
                this.InternalTest(test as Delegate, configuration);
            }
            else
            {
                this.RunAsync(async (r) => await test(), configuration).Wait();
            }
        }

        protected void Test(Func<IActorRuntime, Task> test, Configuration configuration = null)
        {
            if (this.SystematicTest)
            {
                this.InternalTest(test as Delegate, configuration);
            }
            else
            {
                this.RunAsync(test, configuration).Wait();
            }
        }

        protected string TestCoverage(Action<IActorRuntime> test, Configuration configuration)
        {
            var engine = this.InternalTest(test as Delegate, configuration);
            using (var writer = new StringWriter())
            {
                var activityCoverageReporter = new ActivityCoverageReporter(engine.TestReport.CoverageInfo);
                activityCoverageReporter.WriteCoverageText(writer);
                string result = RemoveNamespaceReferencesFromReport(writer.ToString());
                return result;
            }
        }

        private TestingEngine InternalTest(Delegate test, Configuration configuration)
        {
            configuration = configuration ?? GetConfiguration();

            TextWriter logger;
            if (configuration.IsVerbose)
            {
                logger = new TestOutputLogger(this.TestOutput, true);
            }
            else
            {
                logger = TextWriter.Null;
            }

            TestingEngine engine = null;

            try
            {
                engine = RunTest(test, configuration, logger);
                var numErrors = engine.TestReport.NumOfFoundBugs;
                Assert.True(numErrors == 0, GetBugReport(engine));
            }
            catch (Exception ex)
            {
                Assert.False(true, ex.Message + "\n" + ex.StackTrace);
            }
            finally
            {
                logger.Dispose();
            }

            return engine;
        }

        protected void TestWithError(Action test, Configuration configuration = null, string expectedError = null,
            bool replay = false)
        {
            if (this.SystematicTest)
            {
                this.TestWithErrors(test as Delegate, configuration, (e) => { CheckSingleError(e, expectedError); }, replay);
            }
            else
            {
                this.RunWithErrors((r) => test(), configuration, (e) => { CheckSingleError(e, expectedError); });
            }
        }

        protected void TestWithError(Action<IActorRuntime> test, Configuration configuration = null,
            string expectedError = null, bool replay = false)
        {
            if (this.SystematicTest)
            {
                this.TestWithErrors(test, configuration, (e) => { CheckSingleError(e, expectedError); }, replay);
            }
            else
            {
                this.RunWithErrors(test, configuration, (e) => { CheckSingleError(e, expectedError); });
            }
        }

        protected void TestWithError(Func<Task> test, Configuration configuration = null, string expectedError = null,
            bool replay = false)
        {
            if (this.SystematicTest)
            {
                this.TestWithErrors(test as Delegate, configuration, (e) => { CheckSingleError(e, expectedError); }, replay);
            }
            else
            {
                this.RunWithErrorsAsync(async (r) => await test(), configuration, (e) => { CheckSingleError(e, expectedError); }).Wait();
            }
        }

        protected void TestWithError(Func<IActorRuntime, Task> test, Configuration configuration = null,
            string expectedError = null, bool replay = false)
        {
            if (this.SystematicTest)
            {
                this.TestWithErrors(test as Delegate, configuration, (e) => { CheckSingleError(e, expectedError); }, replay);
            }
            else
            {
                this.RunWithErrorsAsync(test, configuration, (e) => { CheckSingleError(e, expectedError); }).Wait();
            }
        }

        protected void TestWithError(Action test, Configuration configuration = null, string[] expectedErrors = null,
            bool replay = false)
        {
            if (this.SystematicTest)
            {
                this.TestWithErrors(test as Delegate, configuration, (e) => { CheckMultipleErrors(e, expectedErrors); }, replay);
            }
            else
            {
                this.RunWithErrors((r) => test(), configuration, (e) => { CheckMultipleErrors(e, expectedErrors); });
            }
        }

        protected void TestWithError(Action<IActorRuntime> test, Configuration configuration = null,
            string[] expectedErrors = null, bool replay = false)
        {
            if (this.SystematicTest)
            {
                this.TestWithErrors(test, configuration, (e) => { CheckMultipleErrors(e, expectedErrors); }, replay);
            }
            else
            {
                this.RunWithErrors(test, configuration, (e) => { CheckMultipleErrors(e, expectedErrors); });
            }
        }

        protected void TestWithError(Func<Task> test, Configuration configuration = null, string[] expectedErrors = null,
            bool replay = false)
        {
            if (this.SystematicTest)
            {
                this.TestWithErrors(test as Delegate, configuration, (e) => { CheckMultipleErrors(e, expectedErrors); }, replay);
            }
            else
            {
                this.RunWithErrorsAsync(async (r) => await test(), configuration, (e) => { CheckMultipleErrors(e, expectedErrors); }).Wait();
            }
        }

        protected void TestWithError(Func<IActorRuntime, Task> test, Configuration configuration = null,
            string[] expectedErrors = null, bool replay = false)
        {
            if (this.SystematicTest)
            {
                this.TestWithErrors(test as Delegate, configuration, (e) => { CheckMultipleErrors(e, expectedErrors); }, replay);
            }
            else
            {
                this.RunWithErrorsAsync(test, configuration, (e) => { CheckMultipleErrors(e, expectedErrors); }).Wait();
            }
        }

        protected void TestWithError(Action test, TestErrorChecker errorChecker, Configuration configuration = null,
            bool replay = false)
        {
            if (this.SystematicTest)
            {
                this.TestWithErrors(test as Delegate, configuration, errorChecker, replay);
            }
            else
            {
                this.RunWithErrors((r) => test(), configuration, errorChecker);
            }
        }

        protected void TestWithError(Action<IActorRuntime> test, TestErrorChecker errorChecker, Configuration configuration = null,
            bool replay = false)
        {
            if (this.SystematicTest)
            {
                this.TestWithErrors(test, configuration, errorChecker, replay);
            }
            else
            {
                this.RunWithErrors(test, configuration, errorChecker);
            }
        }

        protected void TestWithError(Func<Task> test, TestErrorChecker errorChecker, Configuration configuration = null,
            bool replay = false)
        {
            if (this.SystematicTest)
            {
                this.TestWithErrors(test as Delegate, configuration, errorChecker, replay);
            }
            else
            {
                this.RunWithErrorsAsync(async (r) => await test(), configuration, errorChecker).Wait();
            }
        }

        protected void TestWithError(Func<IActorRuntime, Task> test, TestErrorChecker errorChecker, Configuration configuration = null,
            bool replay = false)
        {
            if (this.SystematicTest)
            {
                this.TestWithErrors(test as Delegate, configuration, errorChecker, replay);
            }
            else
            {
                this.RunWithErrorsAsync(test, configuration, errorChecker).Wait();
            }
        }

        private void TestWithErrors(Delegate test, Configuration configuration, TestErrorChecker errorChecker, bool replay)
        {
            configuration = configuration ?? GetConfiguration();

            TextWriter logger;
            if (configuration.IsVerbose)
            {
                logger = new TestOutputLogger(this.TestOutput, true);
            }
            else
            {
                logger = TextWriter.Null;
            }

            try
            {
                var engine = RunTest(test, configuration, logger);
                CheckErrors(engine, errorChecker);

                if (replay)
                {
                    configuration.WithReplayStrategy(engine.ReproducableTrace);

                    engine = RunTest(test, configuration, logger);

                    string replayError = (engine.Strategy as ReplayStrategy).ErrorText;
                    Assert.True(replayError.Length == 0, replayError);
                    CheckErrors(engine, errorChecker);
                }
            }
            catch (Exception ex)
            {
                Assert.False(true, ex.Message + "\n" + ex.StackTrace);
            }
            finally
            {
                logger.Dispose();
            }
        }

        protected void TestWithException<TException>(Action test, Configuration configuration = null, bool replay = false)
            where TException : Exception
        {
            if (this.SystematicTest)
            {
                this.InternalTestWithException<TException>(test as Delegate, configuration, replay);
            }
            else
            {
            }
        }

        protected void TestWithException<TException>(Action<IActorRuntime> test, Configuration configuration = null,
            bool replay = false)
            where TException : Exception
        {
            this.InternalTestWithException<TException>(test as Delegate, configuration, replay);
        }

        protected void TestWithException<TException>(Func<Task> test, Configuration configuration = null, bool replay = false)
            where TException : Exception
        {
            this.InternalTestWithException<TException>(test as Delegate, configuration, replay);
        }

        protected void TestWithException<TException>(Func<IActorRuntime, Task> test, Configuration configuration = null,
            bool replay = false)
            where TException : Exception
        {
            this.InternalTestWithException<TException>(test as Delegate, configuration, replay);
        }

        private void InternalTestWithException<TException>(Delegate test, Configuration configuration = null, bool replay = false)
            where TException : Exception
        {
            configuration = configuration ?? GetConfiguration();

            Type exceptionType = typeof(TException);
            Assert.True(exceptionType.IsSubclassOf(typeof(Exception)), "Please configure the test correctly. " +
                $"Type '{exceptionType}' is not an exception type.");

            TextWriter logger;
            if (configuration.IsVerbose)
            {
                logger = new TestOutputLogger(this.TestOutput, true);
            }
            else
            {
                logger = TextWriter.Null;
            }

            try
            {
                var engine = RunTest(test, configuration, logger);

                CheckErrors(engine, exceptionType);

                if (replay)
                {
                    configuration.SchedulingStrategy = "replay";
                    configuration.ScheduleTrace = engine.ReproducableTrace;

                    engine = RunTest(test, configuration, logger);

                    string replayError = (engine.Strategy as ReplayStrategy).ErrorText;
                    Assert.True(replayError.Length == 0, replayError);
                    CheckErrors(engine, exceptionType);
                }
            }
            catch (Exception ex)
            {
                Assert.False(true, ex.Message + "\n" + ex.StackTrace);
            }
            finally
            {
                logger.Dispose();
            }
        }

        protected void Run(Action<IActorRuntime> test, Configuration configuration = null)
        {
            configuration = configuration ?? GetConfiguration();

            TextWriter logger;
            if (configuration.IsVerbose)
            {
                logger = new TestOutputLogger(this.TestOutput, true);
            }
            else
            {
                logger = TextWriter.Null;
            }

            try
            {
                var runtime = RuntimeFactory.Create(configuration);
                runtime.SetLogger(logger);
                for (int i = 0; i < configuration.TestingIterations; i++)
                {
                    test(runtime);
                }
            }
            catch (Exception ex)
            {
                Assert.False(true, ex.Message + "\n" + ex.StackTrace);
            }
            finally
            {
                logger.Dispose();
            }
        }

        protected async Task RunAsync(Func<IActorRuntime, Task> test, Configuration configuration = null)
        {
            configuration = configuration ?? GetConfiguration();

            TextWriter logger;
            if (configuration.IsVerbose)
            {
                logger = new TestOutputLogger(this.TestOutput, true);
            }
            else
            {
                logger = TextWriter.Null;
            }

            try
            {
                var runtime = RuntimeFactory.Create(configuration);
                runtime.SetLogger(logger);
                await test(runtime);
            }
            catch (Exception ex)
            {
                Assert.False(true, ex.Message + "\n" + ex.StackTrace);
            }
            finally
            {
                logger.Dispose();
            }
        }

        private void RunWithErrors(Action<IActorRuntime> test, Configuration configuration, TestErrorChecker errorChecker)
        {
            configuration = configuration ?? GetConfiguration();

            TextWriter logger;
            if (configuration.IsVerbose)
            {
                logger = new TestOutputLogger(this.TestOutput, true);
            }
            else
            {
                logger = TextWriter.Null;
            }

            try
            {
                var runtime = RuntimeFactory.Create(configuration);
                runtime.SetLogger(logger);
                for (int i = 0; i < configuration.TestingIterations; i++)
                {
                    test(runtime);
                }
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
                if (ex is AggregateException ae)
                {
                    StringBuilder sb = new StringBuilder();
                    foreach (var e in ae.InnerExceptions)
                    {
                        sb.AppendLine(e.Message);
                    }

                    msg = sb.ToString();
                }

                errorChecker(msg);
            }
            finally
            {
                logger.Dispose();
            }
        }

        private async Task RunWithErrorsAsync(Func<IActorRuntime, Task> test, Configuration configuration, TestErrorChecker errorChecker)
        {
            configuration = configuration ?? GetConfiguration();

            TextWriter logger;
            if (configuration.IsVerbose)
            {
                logger = new TestOutputLogger(this.TestOutput, true);
            }
            else
            {
                logger = TextWriter.Null;
            }

            try
            {
                var runtime = RuntimeFactory.Create(configuration);
                runtime.SetLogger(logger);
                for (int i = 0; i < configuration.TestingIterations; i++)
                {
                    await test(runtime);
                }
            }
            catch (Exception ex)
            {
                errorChecker(GetFirstLine(ex.Message));
            }
            finally
            {
                logger.Dispose();
            }
        }

        protected void RunWithException<TException>(Action<IActorRuntime> test, Configuration configuration = null)
        {
            configuration = configuration ?? GetConfiguration();

            Type exceptionType = typeof(TException);
            Assert.True(exceptionType.IsSubclassOf(typeof(Exception)), "Please configure the test correctly. " +
                $"Type '{exceptionType}' is not an exception type.");

            TextWriter logger;
            if (configuration.IsVerbose)
            {
                logger = new TestOutputLogger(this.TestOutput, true);
            }
            else
            {
                logger = TextWriter.Null;
            }

            try
            {
                var runtime = RuntimeFactory.Create(configuration);
                runtime.SetLogger(logger);
                for (int i = 0; i < configuration.TestingIterations; i++)
                {
                    test(runtime);
                }
            }
            catch (Exception ex)
            {
                Assert.True(ex.GetType() == exceptionType, ex.Message + "\n" + ex.StackTrace);
            }
            finally
            {
                logger.Dispose();
            }
        }

        protected static async Task WaitAsync(Task task, int millisecondsDelay = 5000)
        {
            await Task.WhenAny(task, Task.Delay(millisecondsDelay));
            Assert.True(task.IsCompleted);
        }

        protected static async Task<TResult> GetResultAsync<TResult>(Task<TResult> task, int millisecondsDelay = 5000)
        {
            await Task.WhenAny(task, Task.Delay(millisecondsDelay));
            Assert.True(task.IsCompleted);
            return await task;
        }

        private static TestingEngine RunTest(Delegate test, Configuration configuration, TextWriter logger)
        {
            var engine = new TestingEngine(configuration, test);
            engine.SetLogger(logger);
            engine.Run();
            return engine;
        }

        private static void CheckSingleError(string actual, string expected)
        {
            var a = RemoveNonDeterministicValuesFromReport(actual);
            var b = RemoveNonDeterministicValuesFromReport(expected);
            Assert.Equal(b, a);
        }

        private static void CheckMultipleErrors(string actual, string[] expectedErrors)
        {
            var stripped = RemoveNonDeterministicValuesFromReport(actual);
            try
            {
                Assert.Contains(expectedErrors, (e) => RemoveNonDeterministicValuesFromReport(e) == stripped);
            }
            catch (Exception)
            {
                Debug.WriteLine(actual);
                throw;
            }
        }

        private static void CheckErrors(TestingEngine engine, TestErrorChecker errorChecker)
        {
            Assert.True(engine.TestReport.NumOfFoundBugs > 0, "Expected bugs to be found, but we found none");
            foreach (var bugReport in engine.TestReport.BugReports)
            {
                errorChecker(bugReport);
            }
        }

        private static void CheckErrors(TestingEngine engine, Type exceptionType)
        {
            Assert.Equal(1, engine.TestReport.NumOfFoundBugs);
            Assert.Contains("'" + exceptionType.FullName + "'",
                engine.TestReport.BugReports.First().Split(new[] { '\r', '\n' }).FirstOrDefault());
        }

        protected static string GetBugReport(TestingEngine engine)
        {
            string report = string.Empty;
            foreach (var bug in engine.TestReport.BugReports)
            {
                report += bug + "\n";
            }

            return report;
        }

        private static string GetFirstLine(string msg)
        {
            if (msg.Contains("\n"))
            {
                string[] lines = msg.Split('\n');
                return lines[0].Trim();
            }

            return msg;
        }
    }
}
