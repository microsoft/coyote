// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.Runtime.Exploration;
using Microsoft.Coyote.Tests.Common.Runtime;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests.Runtime
{
    public class CustomActorRuntimeLogTest : BaseTest
    {
        public CustomActorRuntimeLogTest(ITestOutputHelper output)
            : base(output)
        {
        }

        internal class E : Event
        {
            public ActorId Id;

            public E(ActorId id)
            {
                this.Id = id;
            }
        }

        internal class M : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E), nameof(Act))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                var n = this.CreateActor(typeof(N));
                this.SendEvent(n, new E(this.Id));
            }

            private void Act()
            {
                this.Assert(false, "Reached test assertion.");
            }
        }

        internal class N : StateMachine
        {
            [Start]
            [OnEventDoAction(typeof(E), nameof(Act))]
            private class Init : State
            {
            }

            private void Act(Event e)
            {
                this.SendEvent((e as E).Id, new E(this.Id));
            }
        }

        [Fact(Timeout=5000)]
        public void TestCustomActorRuntimeLogFormatter()
        {
            Action<IActorRuntime> test = r =>
            {
                r.SetLogFormatter(new CustomActorRuntimeLogFormatter());
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
<ActionLog> 'M()' invoked action 'InitOnEntry' in state 'Init'.
<CreateLog>.
<StateLog>.
<DequeueLog> 'N()' dequeued event 'E' in state 'Init'.
<ActionLog> 'N()' invoked action 'Act' in state 'Init'.
<DequeueLog> 'M()' dequeued event 'E' in state 'Init'.
<ActionLog> 'M()' invoked action 'Act' in state 'Init'.
<ErrorLog> Reached test assertion.
<StrategyLog> Found bug using 'DFS' strategy.
<StrategyLog> Testing statistics:
<StrategyLog> Found 1 bug.
<StrategyLog> Scheduling statistics:
<StrategyLog> Explored 1 schedule: 0 fair and 1 unfair.
<StrategyLog> Found 100.00% buggy schedules.";

                string actual = RemoveNonDeterministicValuesFromReport(engine.ReadableTrace.ToString());
                Assert.Equal(expected, actual);
            }
            catch (Exception ex)
            {
                Assert.False(true, ex.Message + "\n" + ex.StackTrace);
            }
        }
    }
}
