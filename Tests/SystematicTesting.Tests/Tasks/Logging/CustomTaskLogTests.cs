// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
#if BINARY_REWRITE
using System.Threading.Tasks;
#else
using Microsoft.Coyote.Tasks;
#endif
using Microsoft.Coyote.IO;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.Specifications;
using Microsoft.Coyote.SystematicTesting;
using Microsoft.Coyote.Tests.Common;
using Xunit;
using Xunit.Abstractions;

#if BINARY_REWRITE
namespace Microsoft.Coyote.BinaryRewriting.Tests.Tasks
#else
namespace Microsoft.Coyote.Production.Tests.Tasks
#endif
{
    public class CustomTaskLogTests : BaseProductionTest
    {
        public CustomTaskLogTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestCustomLogger()
        {
            InMemoryLogger log = new InMemoryLogger();

            var config = Configuration.Create().WithVerbosityEnabled().WithTestingIterations(3);
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
..... Iteration #2
<TestLog> Running test.
Hi mom!
..... Iteration #3
<TestLog> Running test.
Hi mom!
";
            expected = expected.RemoveNonDeterministicValues();

            Assert.Equal(expected, result);
        }

        private async Task RunAsync(ICoyoteRuntime r)
        {
            await Task.Run(async () =>
            {
                r.Logger.WriteLine($"Task '{Task.CurrentId}' is running.");
                await Task.Delay(10);
                r.Logger.WriteLine($"Task '{Task.CurrentId}' completed.");
            });

            await Task.Run(async () =>
            {
                r.Logger.WriteLine($"Task '{Task.CurrentId}' is running.");
                await Task.Delay(10);
                r.Logger.WriteLine($"Task '{Task.CurrentId}' completed.");
            });

            Specification.Assert(false, "Reached test assertion.");
        }

        [Fact(Timeout = 5000)]
        public void TestCustomTaskRuntimeLog()
        {
            if (!this.IsSystematicTest)
            {
                // assembly has not been rewritten, so skip this test.
                return;
            }

            var config = GetConfiguration().WithRandomGeneratorSeed(0);
            TestingEngine engine = TestingEngine.Create(config, this.RunAsync);

            try
            {
                engine.Run();

                var numErrors = engine.TestReport.NumOfFoundBugs;
                Assert.True(numErrors == 1, GetBugReport(engine));
                Assert.True(engine.ReadableTrace != null, "Readable trace is null.");
                Assert.True(engine.ReadableTrace.Length > 0, "Readable trace is empty.");

                string expected = @"<TestLog> Running test.
Task '' is running.
Task '' completed.
Task '' is running.
Task '' completed.
<ErrorLog> Reached test assertion.
<StackTrace> 
<StrategyLog> Found bug using 'random' strategy.
<StrategyLog> Testing statistics:
<StrategyLog> Found 1 bug.
<StrategyLog> Scheduling statistics:
<StrategyLog> Explored 1 schedule: 1 fair and 0 unfair.
<StrategyLog> Found 100.00% buggy schedules.
<StrategyLog> Number of scheduling points in fair terminating schedules: 9 (), 9 (), 9 ().";

                string actual = engine.ReadableTrace.ToString();
                actual = actual.RemoveStackTrace("<StrategyLog>");
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
