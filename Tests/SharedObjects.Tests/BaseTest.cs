// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.TestingServices;
using Xunit;
using Xunit.Abstractions;
using Common = Microsoft.Coyote.Tests.Common;

namespace Microsoft.Coyote.SharedObjects.Tests
{
    public abstract class BaseTest : Common.BaseTest
    {
        public BaseTest(ITestOutputHelper output)
            : base(output)
        {
        }

        protected void AssertSucceeded(Action<IActorRuntime> test)
        {
            var configuration = GetConfiguration();
            this.AssertSucceeded(configuration, test);
        }

        protected void AssertSucceeded(Configuration configuration, Action<IActorRuntime> test)
        {
            var logger = new Common.TestOutputLogger(this.TestOutput);

            try
            {
                var engine = BugFindingEngine.Create(configuration, test);
                engine.SetLogger(logger);
                engine.Run();

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
        }

        protected void AssertFailed(Action<IActorRuntime> test, int numExpectedErrors)
        {
            var configuration = GetConfiguration();
            this.AssertFailed(configuration, test, numExpectedErrors);
        }

        protected void AssertFailed(Action<IActorRuntime> test, string expectedOutput)
        {
            var configuration = GetConfiguration();
            this.AssertFailed(configuration, test, 1, new HashSet<string> { expectedOutput });
        }

        protected void AssertFailed(Action<IActorRuntime> test, int numExpectedErrors, ISet<string> expectedOutputs)
        {
            var configuration = GetConfiguration();
            this.AssertFailed(configuration, test, numExpectedErrors, expectedOutputs);
        }

        protected void AssertFailed(Configuration configuration, Action<IActorRuntime> test, int numExpectedErrors)
        {
            this.AssertFailed(configuration, test, numExpectedErrors, new HashSet<string>());
        }

        protected void AssertFailed(Configuration configuration, Action<IActorRuntime> test, string expectedOutput)
        {
            this.AssertFailed(configuration, test, 1, new HashSet<string> { expectedOutput });
        }

        protected void AssertFailed(Configuration configuration, Action<IActorRuntime> test, int numExpectedErrors, ISet<string> expectedOutputs)
        {
            var logger = new Common.TestOutputLogger(this.TestOutput);

            try
            {
                var bfEngine = BugFindingEngine.Create(configuration, test);
                bfEngine.SetLogger(logger);
                bfEngine.Run();

                CheckErrors(bfEngine, numExpectedErrors, expectedOutputs);

                var rEngine = ReplayEngine.Create(configuration, test, bfEngine.ReproducableTrace);
                rEngine.SetLogger(logger);
                rEngine.Run();

                Assert.True(rEngine.InternalError.Length == 0, rEngine.InternalError);
                CheckErrors(rEngine, numExpectedErrors, expectedOutputs);
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

        private static void CheckErrors(ITestingEngine engine, int numExpectedErrors, ISet<string> expectedOutputs)
        {
            var numErrors = engine.TestReport.NumOfFoundBugs;
            Assert.Equal(numExpectedErrors, numErrors);

            if (expectedOutputs.Count > 0)
            {
                var bugReports = new HashSet<string>();
                foreach (var bugReport in engine.TestReport.BugReports)
                {
                    var actual = RemoveNonDeterministicValuesFromReport(bugReport);
                    bugReports.Add(actual);
                }

                foreach (var expected in expectedOutputs)
                {
                    Assert.Contains(expected, bugReports);
                }
            }
        }

        protected void AssertFailedWithException(Action<IActorRuntime> test, Type exceptionType)
        {
            var configuration = GetConfiguration();
            this.AssertFailedWithException(configuration, test, exceptionType);
        }

        protected void AssertFailedWithException(Configuration configuration, Action<IActorRuntime> test, Type exceptionType)
        {
            Assert.True(exceptionType.IsSubclassOf(typeof(Exception)), "Please configure the test correctly. " +
                $"Type '{exceptionType}' is not an exception type.");

            var logger = new Common.TestOutputLogger(this.TestOutput);

            try
            {
                var bfEngine = BugFindingEngine.Create(configuration, test);
                bfEngine.SetLogger(logger);
                bfEngine.Run();

                CheckErrors(bfEngine, exceptionType);

                var rEngine = ReplayEngine.Create(configuration, test, bfEngine.ReproducableTrace);
                rEngine.SetLogger(logger);
                rEngine.Run();

                Assert.True(rEngine.InternalError.Length == 0, rEngine.InternalError);
                CheckErrors(rEngine, exceptionType);
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

        private static void CheckErrors(ITestingEngine engine, Type exceptionType)
        {
            var numErrors = engine.TestReport.NumOfFoundBugs;
            Assert.Equal(1, numErrors);

            var exception = RemoveNonDeterministicValuesFromReport(engine.TestReport.BugReports.First()).
                Split(new[] { '\r', '\n' }).FirstOrDefault();
            Assert.Contains("'" + exceptionType.ToString() + "'", exception);
        }

        protected static Configuration GetConfiguration()
        {
            return Configuration.Create();
        }

        private static string GetBugReport(ITestingEngine engine)
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
