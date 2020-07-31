// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Coverage;
using Microsoft.Coyote.SystematicTesting;
using Microsoft.Coyote.SystematicTesting.Strategies;
using Xunit;
using Xunit.Abstractions;
using CoyoteTasks = Microsoft.Coyote.Tasks;

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
        /// Override to true to run a systematic test under the <see cref="TestingEngine"/>.
        /// By default this value is false.
        /// </summary>
        public virtual bool IsSystematicTest => false;

        protected void Test(Action test, Configuration configuration = null)
        {
            if (this.IsSystematicTest)
            {
                this.InternalTest(test, configuration);
            }
            else
            {
                this.Run((r) => test(), configuration);
            }
        }

        protected void Test(Action<IActorRuntime> test, Configuration configuration = null)
        {
            if (this.IsSystematicTest)
            {
                this.InternalTest(test, configuration);
            }
            else
            {
                this.Run(test, configuration);
            }
        }

        protected void Test(Func<Task> test, Configuration configuration = null)
        {
            if (this.IsSystematicTest)
            {
                this.InternalTest(test, configuration);
            }
            else
            {
                this.RunAsync(async (r) => await test(), configuration).Wait();
            }
        }

        protected void Test(Func<IActorRuntime, Task> test, Configuration configuration = null)
        {
            if (this.IsSystematicTest)
            {
                this.InternalTest(test, configuration);
            }
            else
            {
                this.RunAsync(test, configuration).Wait();
            }
        }

        protected string TestCoverage(Action<IActorRuntime> test, Configuration configuration)
        {
            var engine = this.InternalTest(test, configuration);
            using (var writer = new StringWriter())
            {
                var activityCoverageReporter = new ActivityCoverageReporter(engine.TestReport.CoverageInfo);
                activityCoverageReporter.WriteCoverageText(writer);
                string result = writer.ToString().RemoveNamespaceReferences();
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
            if (this.IsSystematicTest)
            {
                this.TestWithErrors(test, configuration, (e) => { CheckSingleError(e, expectedError); }, replay);
            }
            else
            {
                this.RunWithErrors((r) => test(), configuration, (e) => { CheckSingleError(e, expectedError); });
            }
        }

        protected void TestWithError(Action<IActorRuntime> test, Configuration configuration = null,
            string expectedError = null, bool replay = false)
        {
            if (this.IsSystematicTest)
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
            if (this.IsSystematicTest)
            {
                this.TestWithErrors(test, configuration, (e) => { CheckSingleError(e, expectedError); }, replay);
            }
            else
            {
                this.RunWithErrorsAsync(async (r) => await test(), configuration, (e) => { CheckSingleError(e, expectedError); }).Wait();
            }
        }

        protected void TestWithError(Func<IActorRuntime, Task> test, Configuration configuration = null,
            string expectedError = null, bool replay = false)
        {
            if (this.IsSystematicTest)
            {
                this.TestWithErrors(test, configuration, (e) => { CheckSingleError(e, expectedError); }, replay);
            }
            else
            {
                this.RunWithErrorsAsync(test, configuration, (e) => { CheckSingleError(e, expectedError); }).Wait();
            }
        }

        protected void TestWithError(Action test, Configuration configuration = null, string[] expectedErrors = null,
            bool replay = false)
        {
            if (this.IsSystematicTest)
            {
                this.TestWithErrors(test, configuration, (e) => { CheckMultipleErrors(e, expectedErrors); }, replay);
            }
            else
            {
                this.RunWithErrors((r) => test(), configuration, (e) => { CheckMultipleErrors(e, expectedErrors); });
            }
        }

        protected void TestWithError(Action<IActorRuntime> test, Configuration configuration = null,
            string[] expectedErrors = null, bool replay = false)
        {
            if (this.IsSystematicTest)
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
            if (this.IsSystematicTest)
            {
                this.TestWithErrors(test, configuration, (e) => { CheckMultipleErrors(e, expectedErrors); }, replay);
            }
            else
            {
                this.RunWithErrorsAsync(async (r) => await test(), configuration, (e) => { CheckMultipleErrors(e, expectedErrors); }).Wait();
            }
        }

        protected void TestWithError(Func<IActorRuntime, Task> test, Configuration configuration = null,
            string[] expectedErrors = null, bool replay = false)
        {
            if (this.IsSystematicTest)
            {
                this.TestWithErrors(test, configuration, (e) => { CheckMultipleErrors(e, expectedErrors); }, replay);
            }
            else
            {
                this.RunWithErrorsAsync(test, configuration, (e) => { CheckMultipleErrors(e, expectedErrors); }).Wait();
            }
        }

        protected void TestWithError(Action test, TestErrorChecker errorChecker, Configuration configuration = null,
            bool replay = false)
        {
            if (this.IsSystematicTest)
            {
                this.TestWithErrors(test, configuration, errorChecker, replay);
            }
            else
            {
                this.RunWithErrors((r) => test(), configuration, errorChecker);
            }
        }

        protected void TestWithError(Action<IActorRuntime> test, TestErrorChecker errorChecker, Configuration configuration = null,
            bool replay = false)
        {
            if (this.IsSystematicTest)
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
            if (this.IsSystematicTest)
            {
                this.TestWithErrors(test, configuration, errorChecker, replay);
            }
            else
            {
                this.RunWithErrorsAsync(async (r) => await test(), configuration, errorChecker).Wait();
            }
        }

        protected void TestWithError(Func<IActorRuntime, Task> test, TestErrorChecker errorChecker, Configuration configuration = null,
            bool replay = false)
        {
            if (this.IsSystematicTest)
            {
                this.TestWithErrors(test, configuration, errorChecker, replay);
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
            if (this.IsSystematicTest)
            {
                this.InternalTestWithException<TException>(test, configuration, replay);
            }
            else
            {
                this.RunWithException<TException>(test, configuration);
            }
        }

        protected void TestWithException<TException>(Action<IActorRuntime> test, Configuration configuration = null,
            bool replay = false)
            where TException : Exception
        {
            if (this.IsSystematicTest)
            {
                this.InternalTestWithException<TException>(test, configuration, replay);
            }
            else
            {
                this.RunWithException<TException>(test, configuration);
            }
        }

        protected void TestWithException<TException>(Func<Task> test, Configuration configuration = null, bool replay = false)
            where TException : Exception
        {
            if (this.IsSystematicTest)
            {
                this.InternalTestWithException<TException>(test, configuration, replay);
            }
            else
            {
                this.RunWithExceptionAsync<TException>(test, configuration).Wait();
            }
        }

        protected void TestWithException<TException>(Func<IActorRuntime, Task> test, Configuration configuration = null,
            bool replay = false)
            where TException : Exception
        {
            if (this.IsSystematicTest)
            {
                this.InternalTestWithException<TException>(test, configuration, replay);
            }
            else
            {
                this.RunWithExceptionAsync<TException>(test, configuration).Wait();
            }
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

        protected async Task RunAsync(Func<IActorRuntime, Task> test, Configuration configuration = null, bool handleFailures = true)
        {
            configuration = configuration ?? GetConfiguration();

            int iterations = Math.Max(1, configuration.TestingIterations);
            for (int i = 0; i < iterations; i++)
            {
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
                    configuration.IsMonitoringEnabledInInProduction = true;
                    var runtime = RuntimeFactory.Create(configuration);
                    runtime.SetLogger(logger);

                    var errorTask = new TaskCompletionSource<Exception>();
                    if (handleFailures)
                    {
                        runtime.OnFailure += (e) =>
                        {
                            errorTask.SetResult(Unwrap(e));
                        };
                    }

                    await Task.WhenAny(test(runtime), errorTask.Task);
                    if (handleFailures && errorTask.Task.IsCompleted)
                    {
                        Assert.False(true, errorTask.Task.Result.Message);
                    }
                }
                catch (Exception ex)
                {
                    Exception e = Unwrap(ex);
                    Assert.False(true, e.Message + "\n" + e.StackTrace);
                }
                finally
                {
                    logger.Dispose();
                }
            }
        }

        private static Exception Unwrap(Exception ex)
        {
            Exception e = ex;
            if (e is AggregateException ae)
            {
                e = ae.InnerException;
            }
            else if (e is ActionExceptionFilterException fe)
            {
                e = fe.InnerException;
            }

            return e;
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

        protected void RunWithException<TException>(Action test, Configuration configuration = null)
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
                    test();
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

        protected async Task RunWithExceptionAsync<TException>(Func<IActorRuntime, Task> test, Configuration configuration = null)
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
                    await test(runtime);
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

        protected async Task RunWithExceptionAsync<TException>(Func<Task> test, Configuration configuration = null)
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
                    await test();
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

        protected async CoyoteTasks.Task WaitAsync(CoyoteTasks.Task task, int millisecondsDelay = 5000)
        {
            if (Debugger.IsAttached)
            {
                millisecondsDelay = 500000;
            }

            if (this.IsSystematicTest)
            {
                // The TestEngine will throw a Deadlock exception if this task can't possibly complete.
                await task;
            }
            else
            {
                await CoyoteTasks.Task.WhenAny(task, CoyoteTasks.Task.Delay(millisecondsDelay));
            }

            if (task.IsFaulted)
            {
                // unwrap the AggregateException so unit tests can more easily
                // Assert.Throws to match a more specific inner exception.
                throw task.Exception.InnerException;
            }

            Assert.True(task.IsCompleted);
        }

        protected async CoyoteTasks.Task<TResult> GetResultAsync<TResult>(CoyoteTasks.TaskCompletionSource<TResult> tcs, int millisecondsDelay = 5000)
        {
            return await this.GetResultAsync(tcs.Task, millisecondsDelay);
        }

        protected async CoyoteTasks.Task<TResult> GetResultAsync<TResult>(CoyoteTasks.Task<TResult> task, int millisecondsDelay = 5000)
        {
            if (Debugger.IsAttached)
            {
                millisecondsDelay = 500000;
            }

            if (this.IsSystematicTest)
            {
                // The TestEngine will throw a Deadlock exception if this task can't possibly complete.
                await task;
            }
            else
            {
                await CoyoteTasks.Task.WhenAny(task, CoyoteTasks.Task.Delay(millisecondsDelay));
            }

            if (task.IsFaulted)
            {
                // unwrap the AggregateException so unit tests can more easily
                // Assert.Throws to match a more specific inner exception.
                throw task.Exception.InnerException;
            }

            Assert.True(task.IsCompleted, string.Format("Task timed out after '{0}' milliseconds", millisecondsDelay));
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
            var a = actual.RemoveNonDeterministicValues();
            var b = expected.RemoveNonDeterministicValues();
            Assert.Equal(b, a);
        }

        private static void CheckMultipleErrors(string actual, string[] expectedErrors)
        {
            var stripped = actual.RemoveNonDeterministicValues();
            try
            {
                Assert.Contains(expectedErrors, (e) => e.RemoveNonDeterministicValues() == stripped);
            }
            catch (Exception)
            {
                throw new Exception("Actual string was not in the expected list: " + actual);
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
            Assert.Contains(exceptionType.FullName,
                engine.TestReport.BugReports.First().Split(new[] { '\r', '\n' }).FirstOrDefault());
        }

        protected static Configuration GetConfiguration()
        {
            return Configuration.Create().WithTelemetryEnabled(false);
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
