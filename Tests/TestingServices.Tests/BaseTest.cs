// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Coyote.IO;
using Microsoft.Coyote.Threading.Tasks;
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

        protected ITestingEngine Test(Action test, Configuration configuration = null) =>
            this.Test(test as Delegate, configuration);

        protected ITestingEngine Test(Action<IMachineRuntime> test, Configuration configuration = null) =>
            this.Test(test as Delegate, configuration);

        protected ITestingEngine Test(Func<ControlledTask> test, Configuration configuration = null) =>
            this.Test(test as Delegate, configuration);

        protected ITestingEngine Test(Func<IMachineRuntime, ControlledTask> test, Configuration configuration = null) =>
            this.Test(test as Delegate, configuration);

        private ITestingEngine Test(Delegate test, Configuration configuration)
        {
            configuration = configuration ?? GetConfiguration();

            ILogger logger;
            if (configuration.IsVerbose)
            {
                logger = new Common.TestOutputLogger(this.TestOutput, true);
            }
            else
            {
                logger = new NulLogger();
            }

            BugFindingEngine engine = null;

            try
            {
                engine = BugFindingEngine.Create(configuration, test);
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

            return engine;
        }

        protected void TestWithError(Action test, Configuration configuration = null, string expectedError = null,
            bool replay = false)
        {
            this.TestWithError(test as Delegate, configuration, new string[] { expectedError }, replay);
        }

        protected void TestWithError(Action<IMachineRuntime> test, Configuration configuration = null,
            string expectedError = null, bool replay = false)
        {
            this.TestWithError(test as Delegate, configuration, new string[] { expectedError }, replay);
        }

        protected void TestWithError(Func<ControlledTask> test, Configuration configuration = null, string expectedError = null,
            bool replay = false)
        {
            this.TestWithError(test as Delegate, configuration, new string[] { expectedError }, replay);
        }

        protected void TestWithError(Func<IMachineRuntime, ControlledTask> test, Configuration configuration = null,
            string expectedError = null, bool replay = false)
        {
            this.TestWithError(test as Delegate, configuration, new string[] { expectedError }, replay);
        }

        protected void TestWithError(Action test, Configuration configuration = null, string[] expectedErrors = null,
            bool replay = false)
        {
            this.TestWithError(test as Delegate, configuration, expectedErrors, replay);
        }

        protected void TestWithError(Action<IMachineRuntime> test, Configuration configuration = null,
            string[] expectedErrors = null, bool replay = false)
        {
            this.TestWithError(test as Delegate, configuration, expectedErrors, replay);
        }

        protected void TestWithError(Func<ControlledTask> test, Configuration configuration = null, string[] expectedErrors = null,
            bool replay = false)
        {
            this.TestWithError(test as Delegate, configuration, expectedErrors, replay);
        }

        protected void TestWithError(Func<IMachineRuntime, ControlledTask> test, Configuration configuration = null,
            string[] expectedErrors = null, bool replay = false)
        {
            this.TestWithError(test as Delegate, configuration, expectedErrors, replay);
        }

        private void TestWithError(Delegate test, Configuration configuration, string[] expectedErrors, bool replay)
        {
            configuration = configuration ?? GetConfiguration();

            ILogger logger;
            if (configuration.IsVerbose)
            {
                logger = new Common.TestOutputLogger(this.TestOutput, true);
            }
            else
            {
                logger = new NulLogger();
            }

            try
            {
                var bfEngine = BugFindingEngine.Create(configuration, test);
                bfEngine.SetLogger(logger);
                bfEngine.Run();

                CheckErrors(bfEngine, expectedErrors);

                if (replay && !configuration.EnableCycleDetection)
                {
                    var rEngine = ReplayEngine.Create(configuration, test, bfEngine.ReproducableTrace);
                    rEngine.SetLogger(logger);
                    rEngine.Run();

                    Assert.True(rEngine.InternalError.Length == 0, rEngine.InternalError);
                    CheckErrors(rEngine, expectedErrors);
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

        protected void TestWithException<TException>(Action<IMachineRuntime> test, Configuration configuration = null,
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

        protected void TestWithException<TException>(Func<IMachineRuntime, ControlledTask> test, Configuration configuration = null,
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

            ILogger logger;
            if (configuration.IsVerbose)
            {
                logger = new Common.TestOutputLogger(this.TestOutput, true);
            }
            else
            {
                logger = new NulLogger();
            }

            try
            {
                var bfEngine = BugFindingEngine.Create(configuration, test);
                bfEngine.SetLogger(logger);
                bfEngine.Run();

                CheckErrors(bfEngine, exceptionType);

                if (replay && !configuration.EnableCycleDetection)
                {
                    var rEngine = ReplayEngine.Create(configuration, test, bfEngine.ReproducableTrace);
                    rEngine.SetLogger(logger);
                    rEngine.Run();

                    Assert.True(rEngine.InternalError.Length == 0, rEngine.InternalError);
                    CheckErrors(rEngine, exceptionType);
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

        private static void CheckErrors(ITestingEngine engine, IEnumerable<string> expectedErrors)
        {
            if (!expectedErrors.Contains(string.Empty))
            {
                Assert.True(engine.TestReport.NumOfFoundBugs > 0);
            }

            foreach (var bugReport in engine.TestReport.BugReports)
            {
                var actual = RemoveNonDeterministicValuesFromReport(bugReport);
                Assert.Contains(actual, expectedErrors);
            }
        }

        private static void CheckErrors(ITestingEngine engine, Type exceptionType)
        {
            Assert.Equal(1, engine.TestReport.NumOfFoundBugs);
            Assert.Contains("'" + exceptionType.FullName + "'",
                engine.TestReport.BugReports.First().Split(new[] { '\r', '\n' }).FirstOrDefault());
        }

        protected static Configuration GetConfiguration()
        {
            return Configuration.Create();
        }

        protected static string GetBugReport(ITestingEngine engine)
        {
            string report = string.Empty;
            foreach (var bug in engine.TestReport.BugReports)
            {
                report += bug + "\n";
            }

            return report;
        }

        protected static string RemoveNonDeterministicValuesFromReport(string report)
        {
            string result;

            // Match a GUID or other ids (since they can be nondeterministic).
            result = Regex.Replace(report, @"\'[0-9|a-z|A-Z|-]{36}\'|\'[0-9]+\'", "''");
            result = Regex.Replace(result, @"\([^)]*\)", "()");
            result = Regex.Replace(result, @"\[[^)]*\]", "[]");

            // Match a namespace.
            result = RemoveNamespaceReferencesFromReport(result);
            return result;
        }

        protected static string RemoveNamespaceReferencesFromReport(string report)
        {
            return Regex.Replace(report, @"Microsoft\.[^+]*\+", string.Empty);
        }

        protected static string RemoveExcessiveEmptySpaceFromReport(string report)
        {
            return Regex.Replace(report, @"\s+", " ");
        }
    }
}
