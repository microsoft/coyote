// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.Coyote.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests.LogMessages
{
    public class CustomLogWriterTest : BaseTest
    {
        public CustomLogWriterTest(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout=5000)]
        public void TestCustomLogWriter()
        {
            Configuration configuration = GetConfiguration().WithStrategy(SchedulingStrategy.DFS);
            BugFindingEngine engine = BugFindingEngine.Create(configuration,
                r =>
                {
                    r.SetLogWriter(new CustomLogWriter());
                    r.CreateMachine(typeof(M));
                });

            try
            {
                engine.Run();

                var numErrors = engine.TestReport.NumOfFoundBugs;
                Assert.True(numErrors == 1, GetBugReport(engine));
                Assert.True(engine.ReadableTrace != null, "Readable trace is null.");
                Assert.True(engine.ReadableTrace.Length > 0, "Readable trace is empty.");

                string expected = @"<TestHarnessLog> Running anonymous test.
<CreateLog>.
<StateLog>.
<ActionLog> Machine 'Microsoft.Coyote.TestingServices.Tests.LogMessages.M()' in state 'Init' invoked action 'InitOnEntry'.
<CreateLog>.
<StateLog>.
<DequeueLog> Machine 'Microsoft.Coyote.TestingServices.Tests.LogMessages.N()' in state 'Init' dequeued event 'Microsoft.Coyote.TestingServices.Tests.LogMessages.E'.
<ActionLog> Machine 'Microsoft.Coyote.TestingServices.Tests.LogMessages.N()' in state 'Init' invoked action 'Act'.
<DequeueLog> Machine 'Microsoft.Coyote.TestingServices.Tests.LogMessages.M()' in state 'Init' dequeued event 'Microsoft.Coyote.TestingServices.Tests.LogMessages.E'.
<ActionLog> Machine 'Microsoft.Coyote.TestingServices.Tests.LogMessages.M()' in state 'Init' invoked action 'Act'.
<ErrorLog> Bug found!
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
