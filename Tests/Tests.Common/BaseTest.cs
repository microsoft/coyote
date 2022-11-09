// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Actors.Coverage;
using Microsoft.Coyote.Logging;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.SystematicTesting;
using Xunit;
using Xunit.Abstractions;
using ActorRuntimeFactory = Microsoft.Coyote.Actors.RuntimeFactory;

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
        /// Override to change the test scheduling policy used by the <see cref="TestingEngine"/>.
        /// By default this value is <see cref="SchedulingPolicy.None"/>.
        /// </summary>
        private protected virtual SchedulingPolicy SchedulingPolicy => SchedulingPolicy.None;

        protected void Test(Action test, Configuration configuration = null)
        {
            if (this.SchedulingPolicy is SchedulingPolicy.None)
            {
                this.RunTest((r) => test(), configuration);
            }
            else
            {
                this.RunSystematicTest(test, configuration, null, null);
            }
        }

        protected void Test(Action<IActorRuntime> test, Configuration configuration = null)
        {
            if (this.SchedulingPolicy is SchedulingPolicy.None)
            {
                this.RunTest(test, configuration);
            }
            else
            {
                this.RunSystematicTest(test, configuration, null, null);
            }
        }

        protected void Test(Func<Task> test, Configuration configuration = null)
        {
            if (this.SchedulingPolicy is SchedulingPolicy.None)
            {
                this.RunAsync(async (r) => await test(), configuration).Wait();
            }
            else
            {
                this.RunSystematicTest(test, configuration, null, null);
            }
        }

        protected void Test(Func<IActorRuntime, Task> test, Configuration configuration = null)
        {
            if (this.SchedulingPolicy is SchedulingPolicy.None)
            {
                this.RunAsync(test, configuration).Wait();
            }
            else
            {
                this.RunSystematicTest(test, configuration, null, null);
            }
        }

        protected TestReport RunSystematicTest(Action test, Configuration configuration = null) =>
            this.RunSystematicTest(test as Delegate, configuration, null, null);

        protected TestReport RunSystematicTest(Func<Task> test, Configuration configuration = null) =>
            this.RunSystematicTest(test as Delegate, configuration, null, null);

        protected TestReport RunSystematicTest(Action test, Configuration configuration = null,
            Action<uint> startIterationCallBack = null, Action<uint> endIterationCallBack = null) =>
            this.RunSystematicTest(test as Delegate, configuration, startIterationCallBack, endIterationCallBack);

        protected TestReport RunSystematicTest(Func<Task> test, Configuration configuration = null,
            Action<uint> startIterationCallBack = null, Action<uint> endIterationCallBack = null) =>
            this.RunSystematicTest(test as Delegate, configuration, startIterationCallBack, endIterationCallBack);

        private TestReport RunSystematicTest(Delegate test, Configuration configuration,
            Action<uint> startIterationCallBack, Action<uint> endIterationCallBack)
        {
            configuration ??= this.GetConfiguration();

            using var logger = new TestOutputLogger(this.TestOutput);
            try
            {
                using TestingEngine engine = RunTestingEngine(test, configuration,
                    startIterationCallBack, endIterationCallBack, logger);
                if (!configuration.RunTestIterationsToCompletion)
                {
                    var numErrors = engine.TestReport.NumOfFoundBugs;
                    Assert.True(numErrors is 0, GetBugReport(engine));
                }

                return engine.TestReport;
            }
            catch (Exception ex)
            {
                Assert.False(true, ex.Message + "\n" + ex.StackTrace);
            }

            return null;
        }

        protected string TestCoverage(Action<IActorRuntime> test, Configuration configuration)
        {
            TestReport report = this.RunSystematicTest(test, configuration, null, null);
            using var writer = new StringWriter();
            var activityCoverageReporter = new ActorActivityCoverageReporter(report.CoverageInfo);
            activityCoverageReporter.WriteCoverageText(writer);
            string result = writer.ToString().RemoveNamespaceReferences();
            return result;
        }

        protected void TestWithError(Action test, Configuration configuration = null, string expectedError = null,
            bool replay = false)
        {
            if (this.SchedulingPolicy is SchedulingPolicy.None)
            {
                this.RunWithErrors((r) => test(), configuration, (e) => { CheckSingleError(e, expectedError); });
            }
            else
            {
                this.RunSystematicTestWithErrors(test, configuration, (e) => { CheckSingleError(e, expectedError); }, replay);
            }
        }

        protected void TestWithError(Action<IActorRuntime> test, Configuration configuration = null,
            string expectedError = null, bool replay = false)
        {
            if (this.SchedulingPolicy is SchedulingPolicy.None)
            {
                this.RunWithErrors(test, configuration, (e) => { CheckSingleError(e, expectedError); });
            }
            else
            {
                this.RunSystematicTestWithErrors(test, configuration, (e) => { CheckSingleError(e, expectedError); }, replay);
            }
        }

        protected void TestWithError(Func<Task> test, Configuration configuration = null, string expectedError = null,
            bool replay = false)
        {
            if (this.SchedulingPolicy is SchedulingPolicy.None)
            {
                this.RunWithErrorsAsync(async (r) => await test(), configuration, (e) => { CheckSingleError(e, expectedError); }).Wait();
            }
            else
            {
                this.RunSystematicTestWithErrors(test, configuration, (e) => { CheckSingleError(e, expectedError); }, replay);
            }
        }

        protected void TestWithError(Func<IActorRuntime, Task> test, Configuration configuration = null,
            string expectedError = null, bool replay = false)
        {
            if (this.SchedulingPolicy is SchedulingPolicy.None)
            {
                this.RunWithErrorsAsync(test, configuration, (e) => { CheckSingleError(e, expectedError); }).Wait();
            }
            else
            {
                this.RunSystematicTestWithErrors(test, configuration, (e) => { CheckSingleError(e, expectedError); }, replay);
            }
        }

        protected void TestWithError(Action test, Configuration configuration = null, string[] expectedErrors = null,
            bool replay = false)
        {
            if (this.SchedulingPolicy is SchedulingPolicy.None)
            {
                this.RunWithErrors((r) => test(), configuration, (e) => { CheckMultipleErrors(e, expectedErrors); });
            }
            else
            {
                this.RunSystematicTestWithErrors(test, configuration, (e) => { CheckMultipleErrors(e, expectedErrors); }, replay);
            }
        }

        protected void TestWithError(Action<IActorRuntime> test, Configuration configuration = null,
            string[] expectedErrors = null, bool replay = false)
        {
            if (this.SchedulingPolicy is SchedulingPolicy.None)
            {
                this.RunWithErrors(test, configuration, (e) => { CheckMultipleErrors(e, expectedErrors); });
            }
            else
            {
                this.RunSystematicTestWithErrors(test, configuration, (e) => { CheckMultipleErrors(e, expectedErrors); }, replay);
            }
        }

        protected void TestWithError(Func<Task> test, Configuration configuration = null, string[] expectedErrors = null,
            bool replay = false)
        {
            if (this.SchedulingPolicy is SchedulingPolicy.None)
            {
                this.RunWithErrorsAsync(async (r) => await test(), configuration, (e) => { CheckMultipleErrors(e, expectedErrors); }).Wait();
            }
            else
            {
                this.RunSystematicTestWithErrors(test, configuration, (e) => { CheckMultipleErrors(e, expectedErrors); }, replay);
            }
        }

        protected void TestWithError(Func<IActorRuntime, Task> test, Configuration configuration = null,
            string[] expectedErrors = null, bool replay = false)
        {
            if (this.SchedulingPolicy is SchedulingPolicy.None)
            {
                this.RunWithErrorsAsync(test, configuration, (e) => { CheckMultipleErrors(e, expectedErrors); }).Wait();
            }
            else
            {
                this.RunSystematicTestWithErrors(test, configuration, (e) => { CheckMultipleErrors(e, expectedErrors); }, replay);
            }
        }

        protected void TestWithError(Action test, TestErrorChecker errorChecker, Configuration configuration = null,
            bool replay = false)
        {
            if (this.SchedulingPolicy is SchedulingPolicy.None)
            {
                this.RunWithErrors((r) => test(), configuration, errorChecker);
            }
            else
            {
                this.RunSystematicTestWithErrors(test, configuration, errorChecker, replay);
            }
        }

        protected void TestWithError(Action<IActorRuntime> test, TestErrorChecker errorChecker, Configuration configuration = null,
            bool replay = false)
        {
            if (this.SchedulingPolicy is SchedulingPolicy.None)
            {
                this.RunWithErrors(test, configuration, errorChecker);
            }
            else
            {
                this.RunSystematicTestWithErrors(test, configuration, errorChecker, replay);
            }
        }

        protected void TestWithError(Func<Task> test, TestErrorChecker errorChecker, Configuration configuration = null,
            bool replay = false)
        {
            if (this.SchedulingPolicy is SchedulingPolicy.None)
            {
                this.RunWithErrorsAsync(async (r) => await test(), configuration, errorChecker).Wait();
            }
            else
            {
                this.RunSystematicTestWithErrors(test, configuration, errorChecker, replay);
            }
        }

        protected void TestWithError(Func<IActorRuntime, Task> test, TestErrorChecker errorChecker, Configuration configuration = null,
            bool replay = false)
        {
            if (this.SchedulingPolicy is SchedulingPolicy.None)
            {
                this.RunWithErrorsAsync(test, configuration, errorChecker).Wait();
            }
            else
            {
                this.RunSystematicTestWithErrors(test, configuration, errorChecker, replay);
            }
        }

        private void RunSystematicTestWithErrors(Delegate test, Configuration configuration, TestErrorChecker errorChecker, bool replay)
        {
            configuration ??= this.GetConfiguration();
            if (this.SchedulingPolicy is SchedulingPolicy.Fuzzing)
            {
                // Increase iterations during fuzzing as some bugs might be harder to be found.
                configuration = configuration.WithTestingIterations(configuration.TestingIterations * 50);
            }

            using var logger = new TestOutputLogger(this.TestOutput);
            try
            {
                using TestingEngine engine = RunTestingEngine(test, configuration, null, null, logger);
                CheckErrors(engine, errorChecker);

                if (replay && this.SchedulingPolicy is SchedulingPolicy.Interleaving)
                {
                    configuration.WithReproducibleTrace(engine.ReproducibleTrace);
                    using TestingEngine replayEngine = RunTestingEngine(test, configuration, null, null, logger);
                    if (engine.TestReport.NumOfFoundBugs is 0)
                    {
                        this.TestOutput.WriteLine(engine.ReproducibleTrace);
                    }

                    string replayError = replayEngine.Scheduler.GetLastError();
                    Assert.True(replayError.Length is 0, replayError);
                    CheckErrors(replayEngine, errorChecker);
                }
            }
            catch (Exception ex)
            {
                Assert.False(true, ex.Message + "\n" + ex.StackTrace);
            }
        }

        protected void TestWithException<TException>(Action test, Configuration configuration = null, bool replay = false)
            where TException : Exception
        {
            if (this.SchedulingPolicy is SchedulingPolicy.None)
            {
                this.RunTestWithException<TException>(test, configuration);
            }
            else
            {
                this.RunSystematicTestWithException<TException>(test, configuration, replay);
            }
        }

        protected void TestWithException<TException>(Action<IActorRuntime> test, Configuration configuration = null,
            bool replay = false)
            where TException : Exception
        {
            if (this.SchedulingPolicy is SchedulingPolicy.None)
            {
                this.RunTestWithException<TException>(test, configuration);
            }
            else
            {
                this.RunSystematicTestWithException<TException>(test, configuration, replay);
            }
        }

        protected void TestWithException<TException>(Func<Task> test, Configuration configuration = null, bool replay = false)
            where TException : Exception
        {
            if (this.SchedulingPolicy is SchedulingPolicy.None)
            {
                this.RunTestWithExceptionAsync<TException>(test, configuration).Wait();
            }
            else
            {
                this.RunSystematicTestWithException<TException>(test, configuration, replay);
            }
        }

        protected void TestWithException<TException>(Func<IActorRuntime, Task> test, Configuration configuration = null,
            bool replay = false)
            where TException : Exception
        {
            if (this.SchedulingPolicy is SchedulingPolicy.None)
            {
                this.RunTestWithExceptionAsync<TException>(test, configuration).Wait();
            }
            else
            {
                this.RunSystematicTestWithException<TException>(test, configuration, replay);
            }
        }

        private void RunSystematicTestWithException<TException>(Delegate test, Configuration configuration = null, bool replay = false)
            where TException : Exception
        {
            configuration ??= this.GetConfiguration();
            if (this.SchedulingPolicy is SchedulingPolicy.Fuzzing)
            {
                // Increase iterations during fuzzing as some bugs might be harder to be found.
                configuration = configuration.WithTestingIterations(configuration.TestingIterations * 50);
            }

            Type exceptionType = typeof(TException);
            Assert.True(exceptionType.IsSubclassOf(typeof(Exception)), "Please configure the test correctly. " +
                $"Type '{exceptionType}' is not an exception type.");

            using var logger = new TestOutputLogger(this.TestOutput);
            try
            {
                using TestingEngine engine = RunTestingEngine(test, configuration, null, null, logger);
                CheckErrors(engine, exceptionType);

                if (replay && this.SchedulingPolicy is SchedulingPolicy.Interleaving)
                {
                    configuration.WithReproducibleTrace(engine.ReproducibleTrace);
                    using TestingEngine replayEngine = RunTestingEngine(test, configuration, null, null, logger);
                    if (engine.TestReport.NumOfFoundBugs is 0)
                    {
                        this.TestOutput.WriteLine(engine.ReproducibleTrace);
                    }

                    string replayError = replayEngine.Scheduler.GetLastError();
                    Assert.True(replayError.Length is 0, replayError);
                    CheckErrors(replayEngine, exceptionType);
                }
            }
            catch (Exception ex)
            {
                Assert.False(true, ex.Message + "\n" + ex.StackTrace);
            }
        }

        protected void RunTest(Action<IActorRuntime> test, Configuration configuration = null)
        {
            configuration ??= this.GetConfiguration();
            configuration.WithActorQuiescenceCheckingEnabledOutsideTesting();
            configuration.WithMonitoringEnabledOutsideTesting();

            using var logger = new TestOutputLogger(this.TestOutput);
            try
            {
                var runtime = ActorRuntimeFactory.Create(configuration);
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
        }

        protected async Task RunAsync(Func<IActorRuntime, Task> test, Configuration configuration = null, bool handleFailures = true)
        {
            configuration ??= this.GetConfiguration();
            configuration.WithActorQuiescenceCheckingEnabledOutsideTesting();
            configuration.WithMonitoringEnabledOutsideTesting();

            uint iterations = Math.Max(1, configuration.TestingIterations);
            for (int i = 0; i < iterations; i++)
            {
                using var logger = new TestOutputLogger(this.TestOutput);
                try
                {
                    var runtime = ActorRuntimeFactory.Create(configuration);
                    if (!configuration.IsConsoleLoggingEnabled)
                    {
                        runtime.Logger = logger;
                    }

                    var errorTask = new TaskCompletionSource<Exception>();
                    if (handleFailures)
                    {
                        runtime.OnFailure += (e) =>
                        {
                            errorTask.TrySetResult(Unwrap(e));
                        };
                    }

                    // TODO: but is this actually letting the test complete in the case
                    // of actors which run completely asynchronously?
                    await await Task.WhenAny(test(runtime), errorTask.Task);
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
            configuration ??= this.GetConfiguration();
            configuration.WithActorQuiescenceCheckingEnabledOutsideTesting();
            configuration.WithMonitoringEnabledOutsideTesting();

            string errorMessage = string.Empty;
            using var logger = new TestOutputLogger(this.TestOutput);
            try
            {
                var runtime = ActorRuntimeFactory.Create(configuration);
                var errorTask = new TaskCompletionSource<Exception>();
                runtime.OnFailure += (e) =>
                {
                    errorTask.TrySetResult(e);
                };

                runtime.Logger = logger;
                for (int i = 0; i < configuration.TestingIterations; i++)
                {
                    test(runtime);
                    if (configuration.TestingIterations is 1)
                    {
                        Assert.True(errorTask.Task.Wait(GetErrorWaitingTimeout()), "Timeout waiting for error");
                        errorMessage = ExtractErrorMessage(errorTask.Task.Result);
                    }
                }
            }
            catch (Exception ex)
            {
                errorMessage = ExtractErrorMessage(ex);
            }

            if (string.IsNullOrEmpty(errorMessage))
            {
                Assert.True(false, string.Format("Error not found after all {0} test iterations", configuration.TestingIterations));
            }

            errorChecker(errorMessage);
        }

        private async Task RunWithErrorsAsync(Func<IActorRuntime, Task> test, Configuration configuration, TestErrorChecker errorChecker)
        {
            configuration ??= this.GetConfiguration();
            configuration.WithActorQuiescenceCheckingEnabledOutsideTesting();
            configuration.WithMonitoringEnabledOutsideTesting();

            string errorMessage = string.Empty;
            using var logger = new TestOutputLogger(this.TestOutput);
            try
            {
                var runtime = ActorRuntimeFactory.Create(configuration);
                var errorCompletion = new TaskCompletionSource<Exception>();
                runtime.OnFailure += (e) =>
                {
                    errorCompletion.TrySetResult(e);
                };

                runtime.Logger = logger;
                for (int i = 0; i < configuration.TestingIterations; i++)
                {
                    await test(runtime);
                    if (configuration.TestingIterations is 1)
                    {
                        Assert.True(errorCompletion.Task.Wait(GetErrorWaitingTimeout()), "Timeout waiting for error");
                        errorMessage = ExtractErrorMessage(errorCompletion.Task.Result);
                    }
                }
            }
            catch (Exception ex)
            {
                errorMessage = ExtractErrorMessage(ex);
            }

            if (string.IsNullOrEmpty(errorMessage))
            {
                Assert.True(false, string.Format("Error not found after all {0} test iterations", configuration.TestingIterations));
            }

            errorChecker(errorMessage);
        }

        protected void RunTestWithException<TException>(Action<IActorRuntime> test, Configuration configuration = null)
        {
            configuration ??= this.GetConfiguration();
            configuration.WithActorQuiescenceCheckingEnabledOutsideTesting();
            configuration.WithMonitoringEnabledOutsideTesting();

            Exception actualException = null;
            Type exceptionType = typeof(TException);
            Assert.True(exceptionType.IsSubclassOf(typeof(Exception)), "Please configure the test correctly. " +
                $"Type '{exceptionType}' is not an exception type.");

            using var logger = new TestOutputLogger(this.TestOutput);
            try
            {
                var runtime = ActorRuntimeFactory.Create(configuration);
                var errorCompletion = new TaskCompletionSource<Exception>();
                runtime.OnFailure += (e) =>
                {
                    errorCompletion.TrySetResult(e);
                };
                runtime.Logger = logger;
                for (int i = 0; i < configuration.TestingIterations; i++)
                {
                    test(runtime);
                    if (configuration.TestingIterations is 1)
                    {
                        Assert.True(errorCompletion.Task.Wait(GetErrorWaitingTimeout()), "Timeout waiting for error");
                        actualException = errorCompletion.Task.Result;
                    }
                }
            }
            catch (Exception ex)
            {
                actualException = ex;
            }

            if (actualException is null)
            {
                Assert.True(false, string.Format("Error not found after all {0} test iterations", configuration.TestingIterations));
            }

            Assert.True(actualException.GetType() == exceptionType, actualException.Message + "\n" + actualException.StackTrace);
        }

        protected void RunTestWithException<TException>(Action test, Configuration configuration = null)
        {
            configuration ??= this.GetConfiguration();
            configuration.WithActorQuiescenceCheckingEnabledOutsideTesting();
            configuration.WithMonitoringEnabledOutsideTesting();

            Exception actualException = null;
            Type exceptionType = typeof(TException);
            Assert.True(exceptionType.IsSubclassOf(typeof(Exception)), "Please configure the test correctly. " +
                $"Type '{exceptionType}' is not an exception type.");

            using var logger = new TestOutputLogger(this.TestOutput);
            try
            {
                var runtime = ActorRuntimeFactory.Create(configuration);
                var errorCompletion = new TaskCompletionSource<Exception>();
                runtime.OnFailure += (e) =>
                {
                    errorCompletion.TrySetResult(e);
                };
                runtime.Logger = logger;
                for (int i = 0; i < configuration.TestingIterations; i++)
                {
                    test();
                    if (configuration.TestingIterations is 1)
                    {
                        Assert.True(errorCompletion.Task.Wait(GetErrorWaitingTimeout()), "Timeout waiting for error");
                        actualException = errorCompletion.Task.Result;
                    }
                }
            }
            catch (Exception ex)
            {
                actualException = ex;
            }

            if (actualException is null)
            {
                Assert.True(false, string.Format("Error not found after all {0} test iterations", configuration.TestingIterations));
            }

            Assert.True(actualException.GetType() == exceptionType, actualException.Message + "\n" + actualException.StackTrace);
        }

        protected async Task RunTestWithExceptionAsync<TException>(Func<IActorRuntime, Task> test, Configuration configuration = null)
        {
            configuration ??= this.GetConfiguration();
            configuration.WithActorQuiescenceCheckingEnabledOutsideTesting();
            configuration.WithMonitoringEnabledOutsideTesting();

            Exception actualException = null;
            Type exceptionType = typeof(TException);
            Assert.True(exceptionType.IsSubclassOf(typeof(Exception)), "Please configure the test correctly. " +
                $"Type '{exceptionType}' is not an exception type.");

            using var logger = new TestOutputLogger(this.TestOutput);
            try
            {
                var runtime = ActorRuntimeFactory.Create(configuration);
                var errorCompletion = new TaskCompletionSource<Exception>();
                runtime.OnFailure += (e) =>
                {
                    errorCompletion.TrySetResult(e);
                };

                runtime.Logger = logger;
                for (int i = 0; i < configuration.TestingIterations; i++)
                {
                    await test(runtime);

                    if (configuration.TestingIterations is 1)
                    {
                        Assert.True(errorCompletion.Task.Wait(GetErrorWaitingTimeout()), "Timeout waiting for error");
                        actualException = errorCompletion.Task.Result;
                    }
                }
            }
            catch (Exception ex)
            {
                actualException = ex;
            }

            if (actualException is null)
            {
                Assert.True(false, string.Format("Error not found after all {0} test iterations", configuration.TestingIterations));
            }

            Assert.True(actualException.GetType() == exceptionType, actualException.Message + "\n" + actualException.StackTrace);
        }

        protected async Task RunTestWithExceptionAsync<TException>(Func<Task> test, Configuration configuration = null)
        {
            configuration ??= this.GetConfiguration();
            configuration.WithActorQuiescenceCheckingEnabledOutsideTesting();
            configuration.WithMonitoringEnabledOutsideTesting();

            Exception actualException = null;
            Type exceptionType = typeof(TException);
            Assert.True(exceptionType.IsSubclassOf(typeof(Exception)), "Please configure the test correctly. " +
                $"Type '{exceptionType}' is not an exception type.");

            using var logger = new TestOutputLogger(this.TestOutput);
            try
            {
                var runtime = ActorRuntimeFactory.Create(configuration);
                var errorCompletion = new TaskCompletionSource<Exception>();
                runtime.OnFailure += (e) =>
                {
                    errorCompletion.TrySetResult(e);
                };

                runtime.Logger = logger;
                for (int i = 0; i < configuration.TestingIterations; i++)
                {
                    await test();

                    if (configuration.TestingIterations is 1)
                    {
                        Assert.True(errorCompletion.Task.Wait(GetErrorWaitingTimeout()), "Timeout waiting for error");
                        actualException = errorCompletion.Task.Result;
                    }
                }
            }
            catch (Exception ex)
            {
                actualException = ex;
            }

            if (actualException is null)
            {
                Assert.True(false, string.Format("Error not found after all {0} test iterations", configuration.TestingIterations));
            }

            Assert.True(actualException.GetType() == exceptionType, actualException.Message + "\n" + actualException.StackTrace);
        }

        private static TestingEngine RunTestingEngine(Delegate test, Configuration configuration,
            Action<uint> startIterationCallBack, Action<uint> endIterationCallBack, TestOutputLogger logger)
        {
            var logWriter = new LogWriter(configuration);
            var engine = new TestingEngine(configuration, test, logWriter);
            if (!configuration.IsConsoleLoggingEnabled)
            {
                engine.SetLogger(logger);
            }

            if (startIterationCallBack != null)
            {
                engine.RegisterStartIterationCallBack(startIterationCallBack);
            }

            if (endIterationCallBack != null)
            {
                engine.RegisterEndIterationCallBack(endIterationCallBack);
            }

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
            Assert.IsType(exceptionType, engine.TestReport.ThrownException);
        }

        protected static void ThrowException<T>()
            where T : Exception, new() =>
            throw new T();

        protected virtual Configuration GetConfiguration() => Configuration.Create()
            .WithVerbosityEnabled(VerbosityLevel.Debug)
            .WithTelemetryEnabled(false)
            .WithPartiallyControlledConcurrencyAllowed(false)
            .WithSystematicFuzzingFallbackEnabled(false);

        protected static string GetBugReport(TestingEngine engine)
        {
            string report = string.Empty;
            foreach (var bug in engine.TestReport.BugReports)
            {
                report += bug + "\n";
            }

            return report;
        }

        protected static TimeSpan GetErrorWaitingTimeout(int timeout = 5000) => Debugger.IsAttached ?
            Timeout.InfiniteTimeSpan : TimeSpan.FromMilliseconds(timeout);
    }
}
