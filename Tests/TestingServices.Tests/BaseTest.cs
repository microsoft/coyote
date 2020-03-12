// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.Runtime.Exploration;
using Microsoft.Coyote.Runtime.Exploration.Strategies;
using Microsoft.Coyote.Tasks;
using Xunit;
using Xunit.Abstractions;
using Common = Microsoft.Coyote.Tests.Common;

namespace Microsoft.Coyote.TestingServices.Tests
{
    public abstract class BaseTest : Common.BaseTest
    {
        public BaseTest(ITestOutputHelper output)
            : base(output)
        {
        }

        protected TestingEngine Test(Action test, Configuration configuration = null) =>
            this.Test(test as Delegate, configuration);

        protected TestingEngine Test(Action<IActorRuntime> test, Configuration configuration = null) =>
            this.Test(test as Delegate, configuration);

        protected TestingEngine Test(Func<ControlledTask> test, Configuration configuration = null) =>
            this.Test(test as Delegate, configuration);

        protected TestingEngine Test(Func<IActorRuntime, ControlledTask> test, Configuration configuration = null) =>
            this.Test(test as Delegate, configuration);

        private TestingEngine Test(Delegate test, Configuration configuration)
        {
            configuration = configuration ?? GetConfiguration();

            TextWriter logger;
            if (configuration.IsVerbose)
            {
                logger = new Common.TestOutputLogger(this.TestOutput, true);
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
            this.TestWithError(test as Delegate, configuration, new string[] { expectedError }, replay);
        }

        protected void TestWithError(Action<IActorRuntime> test, Configuration configuration = null,
            string expectedError = null, bool replay = false)
        {
            this.TestWithError(test as Delegate, configuration, new string[] { expectedError }, replay);
        }

        protected void TestWithError(Func<ControlledTask> test, Configuration configuration = null, string expectedError = null,
            bool replay = false)
        {
            this.TestWithError(test as Delegate, configuration, new string[] { expectedError }, replay);
        }

        protected void TestWithError(Func<IActorRuntime, ControlledTask> test, Configuration configuration = null,
            string expectedError = null, bool replay = false)
        {
            this.TestWithError(test as Delegate, configuration, new string[] { expectedError }, replay);
        }

        protected void TestWithError(Action test, Configuration configuration = null, string[] expectedErrors = null,
            bool replay = false)
        {
            this.TestWithError(test as Delegate, configuration, expectedErrors, replay);
        }

        protected void TestWithError(Action<IActorRuntime> test, Configuration configuration = null,
            string[] expectedErrors = null, bool replay = false)
        {
            this.TestWithError(test as Delegate, configuration, expectedErrors, replay);
        }

        protected void TestWithError(Func<ControlledTask> test, Configuration configuration = null, string[] expectedErrors = null,
            bool replay = false)
        {
            this.TestWithError(test as Delegate, configuration, expectedErrors, replay);
        }

        protected void TestWithError(Func<IActorRuntime, ControlledTask> test, Configuration configuration = null,
            string[] expectedErrors = null, bool replay = false)
        {
            this.TestWithError(test as Delegate, configuration, expectedErrors, replay);
        }

        private void TestWithError(Delegate test, Configuration configuration, string[] expectedErrors, bool replay)
        {
            configuration = configuration ?? GetConfiguration();

            TextWriter logger;
            if (configuration.IsVerbose)
            {
                logger = new Common.TestOutputLogger(this.TestOutput, true);
            }
            else
            {
                logger = TextWriter.Null;
            }

            try
            {
                var engine = RunTest(test, configuration, logger);
                CheckErrors(engine, expectedErrors);

                if (replay)
                {
                    configuration.SchedulingStrategy = SchedulingStrategy.Replay;
                    configuration.ScheduleTrace = engine.ReproducableTrace;

                    engine = RunTest(test, configuration, logger);

                    string replayError = (engine.Strategy as ReplayStrategy).ErrorText;
                    Assert.True(replayError.Length == 0, replayError);
                    CheckErrors(engine, expectedErrors);
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
            this.TestWithException<TException>(test as Delegate, configuration, replay);
        }

        protected void TestWithException<TException>(Action<IActorRuntime> test, Configuration configuration = null,
            bool replay = false)
            where TException : Exception
        {
            this.TestWithException<TException>(test as Delegate, configuration, replay);
        }

        protected void TestWithException<TException>(Func<ControlledTask> test, Configuration configuration = null, bool replay = false)
            where TException : Exception
        {
            this.TestWithException<TException>(test as Delegate, configuration, replay);
        }

        protected void TestWithException<TException>(Func<IActorRuntime, ControlledTask> test, Configuration configuration = null,
            bool replay = false)
            where TException : Exception
        {
            this.TestWithException<TException>(test as Delegate, configuration, replay);
        }

        private void TestWithException<TException>(Delegate test, Configuration configuration, bool replay)
            where TException : Exception
        {
            configuration = configuration ?? GetConfiguration();

            Type exceptionType = typeof(TException);
            Assert.True(exceptionType.IsSubclassOf(typeof(Exception)), "Please configure the test correctly. " +
                $"Type '{exceptionType}' is not an exception type.");

            TextWriter logger;
            if (configuration.IsVerbose)
            {
                logger = new Common.TestOutputLogger(this.TestOutput, true);
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
                    configuration.SchedulingStrategy = SchedulingStrategy.Replay;
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

        private static TestingEngine RunTest(Delegate test, Configuration configuration, TextWriter logger)
        {
            var engine = new TestingEngine(configuration, test);
            engine.SetLogger(logger);
            engine.Run();
            return engine;
        }

        private static void CheckErrors(TestingEngine engine, IEnumerable<string> expectedErrors)
        {
            Assert.True(engine.TestReport.NumOfFoundBugs > 0);
            foreach (var bugReport in engine.TestReport.BugReports)
            {
                var actual = RemoveNonDeterministicValuesFromReport(bugReport);
                Assert.Contains(actual, expectedErrors);
            }
        }

        private static void CheckErrors(TestingEngine engine, Type exceptionType)
        {
            Assert.Equal(1, engine.TestReport.NumOfFoundBugs);
            Assert.Contains("'" + exceptionType.FullName + "'",
                engine.TestReport.BugReports.First().Split(new[] { '\r', '\n' }).FirstOrDefault());
        }

        protected static Configuration GetConfiguration()
        {
            return Configuration.Create();
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
    }
}
