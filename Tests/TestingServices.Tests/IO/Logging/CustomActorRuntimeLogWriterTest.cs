// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.Runtime.Exploration;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests.IO
{
    public class CustomActorRuntimeLogWriterTest : BaseTest
    {
        public CustomActorRuntimeLogWriterTest(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout=5000)]
        public void TestCustomActorRuntimeLogWriter()
        {
            Action<IActorRuntime> test = r =>
            {
                r.SetLogWriter(new CustomActorRuntimeLogWriter());
                r.CreateActor(typeof(M));
            };

            BugFindingEngine engine = BugFindingEngine.Create(GetConfiguration().WithStrategy(SchedulingStrategy.DFS), test);

            try
            {
                engine.Run();

                var numErrors = engine.TestReport.NumOfFoundBugs;
                Assert.True(numErrors == 1, GetBugReport(engine));
                Assert.True(engine.ReadableTrace != null, "Readable trace is null.");
                Assert.True(engine.ReadableTrace.Length > 0, "Readable trace is empty.");

                string expected = @"<TestLog> Running test.
<CreateLog>.
<StateLog>.
<ActionLog> 'Microsoft.Coyote.TestingServices.Tests.IO.M()' invoked action 'InitOnEntry' in state 'Init'.
<CreateLog>.
<StateLog>.
<DequeueLog> 'Microsoft.Coyote.TestingServices.Tests.IO.N()' dequeued event 'Microsoft.Coyote.TestingServices.Tests.IO.E' in state 'Init'.
<ActionLog> 'Microsoft.Coyote.TestingServices.Tests.IO.N()' invoked action 'Act' in state 'Init'.
<DequeueLog> 'Microsoft.Coyote.TestingServices.Tests.IO.M()' dequeued event 'Microsoft.Coyote.TestingServices.Tests.IO.E' in state 'Init'.
<ActionLog> 'Microsoft.Coyote.TestingServices.Tests.IO.M()' invoked action 'Act' in state 'Init'.
<ErrorLog> Reached test assertion.
<StrategyLog> Found bug using 'DFS' strategy.
<StrategyLog> Testing statistics:
<StrategyLog> Found  bug.
<StrategyLog> Scheduling statistics:
<StrategyLog> Explored  schedule:  fair and  unfair.
<StrategyLog> Found .% buggy schedules.";
                string actual = Regex.Replace(engine.ReadableTrace.ToString(), "[0-9]", string.Empty);

                HashSet<string> expectedSet = new HashSet<string>(Regex.Split(expected, "\r\n|\r|\n"));
                HashSet<string> actualSet = new HashSet<string>(Regex.Split(actual, "\r\n|\r|\n"));

                Assert.Equal(expected, actual);
            }
            catch (Exception ex)
            {
                Assert.False(true, ex.Message + "\n" + ex.StackTrace);
            }
        }
    }
}
