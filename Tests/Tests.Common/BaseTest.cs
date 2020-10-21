// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Actors.Coverage;
using Microsoft.Coyote.IO;
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
            ILogger logger = this.GetLogger(configuration);

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
            ILogger logger = this.GetLogger(configuration);

            try
            {
                var engine = RunTest(test, configuration, logger);
                CheckErrors(engine, errorChecker);

                if (replay)
                {
                    configuration.WithReplayStrategy(engine.ReproducibleTrace);

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

            ILogger logger = this.GetLogger(configuration);

            try
            {
                var engine = RunTest(test, configuration, logger);

                CheckErrors(engine, exceptionType);

                if (replay)
                {
                    configuration.SchedulingStrategy = "replay";
                    configuration.ScheduleTrace = engine.ReproducibleTrace;

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

            ILogger logger = this.GetLogger(configuration);

            try
            {
                configuration.IsMonitoringEnabledInInProduction = true;
                var runtime = RuntimeFactory.Create(configuration);
                runtime.Logger = logger;
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

            uint iterations = Math.Max(1, configuration.TestingIterations);
            for (int i = 0; i < iterations; i++)
            {
                ILogger logger = this.GetLogger(configuration);

                try
                {
                    configuration.IsMonitoringEnabledInInProduction = true;
                    var runtime = RuntimeFactory.Create(configuration);
                    runtime.Logger = logger;

                    var errorTask = new TaskCompletionSource<Exception>();
                    if (handleFailures)
                    {
                        runtime.OnFailure += (e) =>
                        {
                            errorTask.TrySetResult(Unwrap(e));
                        };
                    }

                    // BUGBUG: but is this actually letting the test complete in the case
                    // of actors which run completely asynchronously?
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

        private static string ExtractErrorMessage(Exception ex)
        {
            if (ex is ActionExceptionFilterException actionException)
            {
                ex = actionException.InnerException;
            }

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

            return msg;
        }

        private void RunWithErrors(Action<IActorRuntime> test, Configuration configuration, TestErrorChecker errorChecker)
        {
            configuration = configuration ?? GetConfiguration();

            string errorMessage = string.Empty;
            ILogger logger = this.GetLogger(configuration);

            try
            {
                configuration.IsMonitoringEnabledInInProduction = true;
                var runtime = RuntimeFactory.Create(configuration);
                var errorTask = new TaskCompletionSource<Exception>();
                runtime.OnFailure += (e) =>
                {
                    errorTask.TrySetResult(e);
                };

                runtime.Logger = logger;
                for (int i = 0; i < configuration.TestingIterations; i++)
                {
                    test(runtime);
                    if (configuration.TestingIterations == 1)
                    {
                        Assert.True(errorTask.Task.Wait(GetExceptionTimeout()), "Timeout waiting for error");
                        errorMessage = ExtractErrorMessage(errorTask.Task.Result);
                    }
                }
            }
            catch (Exception ex)
            {
                errorMessage = ExtractErrorMessage(ex);
            }
            finally
            {
                logger.Dispose();
            }

            if (string.IsNullOrEmpty(errorMessage))
            {
                Assert.True(false, string.Format("Error not found after all {0} test iterations", configuration.TestingIterations));
            }

            errorChecker(errorMessage);
        }

        private async Task RunWithErrorsAsync(Func<IActorRuntime, Task> test, Configuration configuration, TestErrorChecker errorChecker)
        {
            configuration = configuration ?? GetConfiguration();

            string errorMessage = string.Empty;
            ILogger logger = this.GetLogger(configuration);

            try
            {
                configuration.IsMonitoringEnabledInInProduction = true;
                var runtime = RuntimeFactory.Create(configuration);
                var errorCompletion = new TaskCompletionSource<Exception>();
                runtime.OnFailure += (e) =>
                {
                    errorCompletion.TrySetResult(e);
                };

                runtime.Logger = logger;
                for (int i = 0; i < configuration.TestingIterations; i++)
                {
                    await test(runtime);
                    if (configuration.TestingIterations == 1)
                    {
                        Assert.True(errorCompletion.Task.Wait(GetExceptionTimeout()), "Timeout waiting for error");
                        errorMessage = ExtractErrorMessage(errorCompletion.Task.Result);
                    }
                }
            }
            catch (Exception ex)
            {
                errorMessage = ExtractErrorMessage(ex);
            }
            finally
            {
                logger.Dispose();
            }

            if (string.IsNullOrEmpty(errorMessage))
            {
                Assert.True(false, string.Format("Error not found after all {0} test iterations", configuration.TestingIterations));
            }

            errorChecker(errorMessage);
        }

        protected void RunWithException<TException>(Action<IActorRuntime> test, Configuration configuration = null)
        {
            configuration = configuration ?? GetConfiguration();

            Exception actualException = null;
            Type exceptionType = typeof(TException);
            Assert.True(exceptionType.IsSubclassOf(typeof(Exception)), "Please configure the test correctly. " +
                $"Type '{exceptionType}' is not an exception type.");

            ILogger logger = this.GetLogger(configuration);

            try
            {
                configuration.IsMonitoringEnabledInInProduction = true;
                var runtime = RuntimeFactory.Create(configuration);
                var errorCompletion = new TaskCompletionSource<Exception>();
                runtime.OnFailure += (e) =>
                {
                    errorCompletion.TrySetResult(e);
                };
                runtime.Logger = logger;
                for (int i = 0; i < configuration.TestingIterations; i++)
                {
                    test(runtime);
                    if (configuration.TestingIterations == 1)
                    {
                        Assert.True(errorCompletion.Task.Wait(GetExceptionTimeout()), "Timeout waiting for error");
                        actualException = errorCompletion.Task.Result;
                    }
                }
            }
            catch (Exception ex)
            {
                actualException = ex;
            }
            finally
            {
                logger.Dispose();
            }

            if (actualException == null)
            {
                Assert.True(false, string.Format("Error not found after all {0} test iterations", configuration.TestingIterations));
            }

            Assert.True(actualException.GetType() == exceptionType, actualException.Message + "\n" + actualException.StackTrace);
        }

        protected void RunWithException<TException>(Action test, Configuration configuration = null)
        {
            configuration = configuration ?? GetConfiguration();

            Exception actualException = null;
            Type exceptionType = typeof(TException);
            Assert.True(exceptionType.IsSubclassOf(typeof(Exception)), "Please configure the test correctly. " +
                $"Type '{exceptionType}' is not an exception type.");

            ILogger logger = this.GetLogger(configuration);

            try
            {
                configuration.IsMonitoringEnabledInInProduction = true;
                var runtime = RuntimeFactory.Create(configuration);
                var errorCompletion = new TaskCompletionSource<Exception>();
                runtime.OnFailure += (e) =>
                {
                    errorCompletion.TrySetResult(e);
                };
                runtime.Logger = logger;
                for (int i = 0; i < configuration.TestingIterations; i++)
                {
                    test();
                    if (configuration.TestingIterations == 1)
                    {
                        Assert.True(errorCompletion.Task.Wait(GetExceptionTimeout()), "Timeout waiting for error");
                        actualException = errorCompletion.Task.Result;
                    }
                }
            }
            catch (Exception ex)
            {
                actualException = ex;
            }
            finally
            {
                logger.Dispose();
            }

            if (actualException == null)
            {
                Assert.True(false, string.Format("Error not found after all {0} test iterations", configuration.TestingIterations));
            }

            Assert.True(actualException.GetType() == exceptionType, actualException.Message + "\n" + actualException.StackTrace);
        }

        protected async Task RunWithExceptionAsync<TException>(Func<IActorRuntime, Task> test, Configuration configuration = null)
        {
            configuration = configuration ?? GetConfiguration();

            Exception actualException = null;
            Type exceptionType = typeof(TException);
            Assert.True(exceptionType.IsSubclassOf(typeof(Exception)), "Please configure the test correctly. " +
                $"Type '{exceptionType}' is not an exception type.");

            ILogger logger = this.GetLogger(configuration);

            try
            {
                configuration.IsMonitoringEnabledInInProduction = true;
                var runtime = RuntimeFactory.Create(configuration);
                var errorCompletion = new TaskCompletionSource<Exception>();
                runtime.OnFailure += (e) =>
                {
                    errorCompletion.TrySetResult(e);
                };

                runtime.Logger = logger;
                for (int i = 0; i < configuration.TestingIterations; i++)
                {
                    await test(runtime);

                    if (configuration.TestingIterations == 1)
                    {
                        Assert.True(errorCompletion.Task.Wait(GetExceptionTimeout()), "Timeout waiting for error");
                        actualException = errorCompletion.Task.Result;
                    }
                }
            }
            catch (Exception ex)
            {
                actualException = ex;
            }
            finally
            {
                logger.Dispose();
            }

            if (actualException == null)
            {
                Assert.True(false, string.Format("Error not found after all {0} test iterations", configuration.TestingIterations));
            }

            Assert.True(actualException.GetType() == exceptionType, actualException.Message + "\n" + actualException.StackTrace);
        }

        protected async Task RunWithExceptionAsync<TException>(Func<Task> test, Configuration configuration = null)
        {
            configuration = configuration ?? GetConfiguration();

            Exception actualException = null;
            Type exceptionType = typeof(TException);
            Assert.True(exceptionType.IsSubclassOf(typeof(Exception)), "Please configure the test correctly. " +
                $"Type '{exceptionType}' is not an exception type.");

            ILogger logger = this.GetLogger(configuration);

            try
            {
                configuration.IsMonitoringEnabledInInProduction = true;
                var runtime = RuntimeFactory.Create(configuration);
                var errorCompletion = new TaskCompletionSource<Exception>();
                runtime.OnFailure += (e) =>
                {
                    errorCompletion.TrySetResult(e);
                };

                runtime.Logger = logger;
                for (int i = 0; i < configuration.TestingIterations; i++)
                {
                    await test();

                    if (configuration.TestingIterations == 1)
                    {
                        Assert.True(errorCompletion.Task.Wait(GetExceptionTimeout()), "Timeout waiting for error");
                        actualException = errorCompletion.Task.Result;
                    }
                }
            }
            catch (Exception ex)
            {
                actualException = ex;
            }
            finally
            {
                logger.Dispose();
            }

            if (actualException == null)
            {
                Assert.True(false, string.Format("Error not found after all {0} test iterations", configuration.TestingIterations));
            }

            Assert.True(actualException.GetType() == exceptionType, actualException.Message + "\n" + actualException.StackTrace);
        }

        private ILogger GetLogger(Configuration configuration)
        {
            ILogger logger;
            if (configuration.IsVerbose)
            {
                logger = new TestOutputLogger(this.TestOutput, true);
            }
            else
            {
                logger = new NullLogger();
            }

            return logger;
        }

        private static int GetExceptionTimeout(int millisecondsDelay = 5000)
        {
            if (Debugger.IsAttached)
            {
                millisecondsDelay = 500000;
            }

            return millisecondsDelay;
        }

        protected async CoyoteTasks.Task WaitAsync(CoyoteTasks.Task task, int millisecondsDelay = 5000)
        {
            millisecondsDelay = GetExceptionTimeout(millisecondsDelay);

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
            millisecondsDelay = GetExceptionTimeout(millisecondsDelay);

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

        private static TestingEngine RunTest(Delegate test, Configuration configuration, ILogger logger)
        {
            var engine = new TestingEngine(configuration, test);
            engine.Logger = logger;
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
