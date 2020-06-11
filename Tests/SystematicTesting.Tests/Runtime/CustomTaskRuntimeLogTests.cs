// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.Specifications;
using Microsoft.Coyote.Tasks;
using Microsoft.Coyote.Tests.Common;
using Microsoft.Coyote.Tests.Common.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.SystematicTesting.Tests.Runtime
{
    public class CustomTaskRuntimeLogTests : BaseSystematicTest
    {
        public CustomTaskRuntimeLogTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private async Task RunAsync(ICoyoteRuntime r)
        {
            SharedEntry entry = new SharedEntry();

            Task task1 = Task.Run(async () =>
            {
                r.Logger.WriteLine($"Task '{Task.CurrentId}' is running.");
                await Task.Delay(10);
                r.Logger.WriteLine($"Task '{Task.CurrentId}' completed.");
            });

            await task1;

            Task task2 = Task.Run(async () =>
            {
                r.Logger.WriteLine($"Task '{Task.CurrentId}' is running.");
                await Task.Delay(10);
                r.Logger.WriteLine($"Task '{Task.CurrentId}' completed.");
            });

            await task2;

            Specification.Assert(false, "Reached test assertion.");
        }

        [Fact(Timeout = 5000)]
        public void TestCustomTaskRuntimeLog()
        {
            TestingEngine engine = TestingEngine.Create(GetConfiguration().WithDFSStrategy(), this.RunAsync);

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
<StrategyLog> Found bug using 'dfs' strategy.
<StrategyLog> Testing statistics:
<StrategyLog> Found 1 bug.
<StrategyLog> Scheduling statistics:
<StrategyLog> Explored 1 schedule: 0 fair and 1 unfair.
<StrategyLog> Found 100.00% buggy schedules.";

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
