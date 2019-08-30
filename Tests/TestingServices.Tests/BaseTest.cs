// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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

        protected ITestingEngine Test(Action<ICoyoteRuntime> test, Configuration configuration = null)
        {
            configuration = configuration ?? GetConfiguration();
            BugFindingEngine engine = BugFindingEngine.Create(configuration, test);
            return this.Test(engine);
        }

        protected ITestingEngine Test(Func<ICoyoteRuntime, Task> test, Configuration configuration = null)
        {
            configuration = configuration ?? GetConfiguration();
            BugFindingEngine engine = BugFindingEngine.Create(configuration, test);
            return this.Test(engine);
        }

        private ITestingEngine Test(BugFindingEngine engine)
        {
            var logger = new Common.TestOutputLogger(this.TestOutput);

            try
            {
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

        protected void TestWithError(Action<ICoyoteRuntime> test, Configuration configuration = null,
            string expectedError = null, bool replay = false)
        {
            configuration = configuration ?? GetConfiguration();
            this.TestWithError(test, configuration, new string[] { expectedError }, replay);
        }

        protected void TestWithError(Func<ICoyoteRuntime, Task> test, Configuration configuration = null,
            string expectedError = null, bool replay = false)
        {
            configuration = configuration ?? GetConfiguration();
            this.TestWithError(test, configuration, new string[] { expectedError }, replay);
        }

        protected void TestWithError(Action<ICoyoteRuntime> test, Configuration configuration = null,
            string[] expectedErrors = null, bool replay = false)
        {
            configuration = configuration ?? GetConfiguration();

            var logger = new Common.TestOutputLogger(this.TestOutput);

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

        protected void TestWithError(Func<ICoyoteRuntime, Task> test, Configuration configuration = null,
            IEnumerable<string> expectedErrors = null, bool replay = false)
        {
            configuration = configuration ?? GetConfiguration();

            var logger = new Common.TestOutputLogger(this.TestOutput);

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

        protected void TestWithException<TException>(Action<ICoyoteRuntime> test, Configuration configuration = null,
            bool replay = false)
            where TException : Exception
        {
            configuration = configuration ?? GetConfiguration();

            Type exceptionType = typeof(TException);
            Assert.True(exceptionType.IsSubclassOf(typeof(Exception)), "Please configure the test correctly. " +
                $"Type '{exceptionType}' is not an exception type.");

            var logger = new Common.TestOutputLogger(this.TestOutput);

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

        protected void TestWithException<TException>(Func<ICoyoteRuntime, Task> test, Configuration configuration = null,
            bool replay = false)
            where TException : Exception
        {
            configuration = configuration ?? GetConfiguration();

            Type exceptionType = typeof(TException);
            Assert.True(exceptionType.IsSubclassOf(typeof(Exception)), "Please configure the test correctly. " +
                $"Type '{exceptionType}' is not an exception type.");

            var logger = new Common.TestOutputLogger(this.TestOutput);

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
            Assert.Equal(1, engine.TestReport.NumOfFoundBugs);
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
