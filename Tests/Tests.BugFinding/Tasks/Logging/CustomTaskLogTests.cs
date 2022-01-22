// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Coyote.IO;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.Specifications;
using Microsoft.Coyote.SystematicTesting;
using Microsoft.Coyote.Tests.Common;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.BugFinding.Tests
{
    public class CustomTaskLogTests : BaseBugFindingTest
    {
        public CustomTaskLogTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestCustomLogger()
        {
            InMemoryLogger log = new InMemoryLogger();

            var config = this.GetConfiguration().WithTestingIterations(3);
            TestingEngine engine = TestingEngine.Create(config, (ICoyoteRuntime runtime) =>
            {
                runtime.Logger.WriteLine("Hi mom!");
            });

            engine.Logger = log;
            engine.Run();

            var result = log.ToString();
            result = result.RemoveNonDeterministicValues();
            var expected = @"... Task 0 is using 'random' strategy (seed:4005173804).
..... Iteration #1
<TestLog> Running test.
Hi mom!
<TestLog> Exploration finished [reached the end of the test method].
..... Iteration #2
<TestLog> Running test.
Hi mom!
<TestLog> Exploration finished [reached the end of the test method].
..... Iteration #3
<TestLog> Running test.
Hi mom!
<TestLog> Exploration finished [reached the end of the test method].
";
            expected = expected.RemoveNonDeterministicValues();

            Assert.Equal(expected, result);
        }

        private async Task RunAsync(ICoyoteRuntime r)
        {
            await Task.Run(async () =>
            {
                r.Logger.WriteLine($"Task '{Task.CurrentId}' is running.");
                await Task.Delay(0);
                r.Logger.WriteLine($"Task '{Task.CurrentId}' completed.");
            });

            await Task.Run(async () =>
            {
                r.Logger.WriteLine($"Task '{Task.CurrentId}' is running.");
                await Task.Delay(0);
                r.Logger.WriteLine($"Task '{Task.CurrentId}' completed.");
            });

            Specification.Assert(false, "Reached test assertion.");
        }

        [Fact(Timeout = 5000)]
        public void TestCustomTaskRuntimeLog()
        {
            var config = this.GetConfiguration().WithRandomGeneratorSeed(0);
            TestingEngine engine = TestingEngine.Create(config, this.RunAsync);

            try
            {
                engine.Run();

                var numErrors = engine.TestReport.NumOfFoundBugs;
                Assert.True(numErrors is 1, GetBugReport(engine));
                Assert.True(engine.ReadableTrace != null, "Readable trace is null.");
                Assert.True(engine.ReadableTrace.Length > 0, "Readable trace is empty.");

                string expected = @"<TestLog> Running test.
Task '' is running.
Task '' completed.
Task '' is running.
Task '' completed.
<ErrorLog> Reached test assertion.
<TestLog> Exploration finished [found a bug using the 'random' strategy].
<StrategyLog> Testing statistics:
<StrategyLog> Found 1 bug.
<StrategyLog> Scheduling statistics:
<StrategyLog> Explored 1 schedule: 1 fair and 0 unfair.
<StrategyLog> Found 100.00% buggy schedules.
<StrategyLog> Controlled 3 operations: 3 (), 3 (), 3 ().
<StrategyLog> Number of scheduling decisions in fair terminating schedules: 4 (), 4 (), 4 ().";

                string actual = engine.ReadableTrace.ToString();
                actual = actual.RemoveNonDeterministicValues();
                expected = expected.RemoveNonDeterministicValues();
                Assert.Equal(expected, actual);
            }
            catch (Exception ex)
            {
                Assert.False(true, ex.Message + "\n" + ex.StackTrace);
            }
        }
    }
}
