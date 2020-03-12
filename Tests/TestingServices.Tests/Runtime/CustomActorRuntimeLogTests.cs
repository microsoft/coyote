// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.Runtime.Exploration;
using Microsoft.Coyote.Runtime.Logging;
using Microsoft.Coyote.Tests.Common.Runtime;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests.Runtime
{
    public class CustomActorRuntimeLogTests : BaseTest
    {
        public CustomActorRuntimeLogTests(ITestOutputHelper output)
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

            [MethodImpl(MethodImplOptions.NoInlining)]
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

        [Fact(Timeout = 5000)]
        public void TestCustomActorRuntimeLogFormatter()
        {
            var logger = new CustomActorRuntimeLog();
            Action<IActorRuntime> test = r =>
            {
                r.RegisterLog(logger);
                r.CreateActor(typeof(M));
            };

            TestingEngine engine = TestingEngine.Create(GetConfiguration().WithStrategy(SchedulingStrategy.DFS), test);

            try
            {
                engine.Run();

                var numErrors = engine.TestReport.NumOfFoundBugs;
                Assert.True(numErrors == 1, GetBugReport(engine));
                Assert.True(engine.ReadableTrace != null, "Readable trace is null.");
                Assert.True(engine.ReadableTrace.Length > 0, "Readable trace is empty.");

                string expected = @"CreateActor
StateTransition
CreateActor
StateTransition
";

                string actual = RemoveNonDeterministicValuesFromReport(logger.ToString());
                Assert.Equal(expected, actual);
            }
            catch (Exception ex)
            {
                Assert.False(true, ex.Message + "\n" + ex.StackTrace);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestCustomActorRuntimeLogTextFormatter()
        {
            var logger = new CustomActorRuntimeLogSubclass();
            Action<IActorRuntime> test = r =>
            {
                r.RegisterLog(logger);
                r.CreateActor(typeof(M));
            };

            TestingEngine engine = TestingEngine.Create(GetConfiguration().WithStrategy(SchedulingStrategy.DFS), test);

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
<ActionLog> M() invoked action 'InitOnEntry' in state 'Init'.
<CreateLog>.
<StateLog>.
<DequeueLog> N() dequeued event 'E' in state 'Init'.
<ActionLog> N() invoked action 'Act' in state 'Init'.
<DequeueLog> M() dequeued event 'E' in state 'Init'.
<ActionLog> M() invoked action 'Act' in state 'Init'.
<ErrorLog> Reached test assertion.
<StackTrace> 
   at Microsoft.Coyote.TestingServices.Tests.Runtime.CustomActorRuntimeLogTests.M.Act()
<StrategyLog> Found bug using 'DFS' strategy.
<StrategyLog> Testing statistics:
<StrategyLog> Found 1 bug.
<StrategyLog> Scheduling statistics:
<StrategyLog> Explored 1 schedule: 0 fair and 1 unfair.
<StrategyLog> Found 100.00% buggy schedules.
";

                string actual = RemoveNonDeterministicValuesFromReport(engine.ReadableTrace.ToString());
                actual = RemoveStackTraceFromReport(actual);
                Assert.Equal(expected, actual);
            }
            catch (Exception ex)
            {
                Assert.False(true, ex.Message + "\n" + ex.StackTrace);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestActorRuntimeXmlLogFormatter()
        {
            StringBuilder builder = new StringBuilder();
            var logger = new ActorRuntimeLogXmlFormatter(XmlWriter.Create(builder, new XmlWriterSettings() { Indent = true, IndentChars = "  " }));
            Action<IActorRuntime> test = r =>
            {
                r.RegisterLog(logger);
                r.CreateActor(typeof(M));
            };

            TestingEngine engine = TestingEngine.Create(GetConfiguration().WithStrategy(SchedulingStrategy.DFS), test);

            try
            {
                engine.Run();

                var numErrors = engine.TestReport.NumOfFoundBugs;
                Assert.True(numErrors == 1, GetBugReport(engine));

                string expected = @"<?xml version='1.0' encoding='utf-16'?>
<Log>
  <CreateActor id='M()' creator='external' />
  <State id='M()' state='Init' isEntry='True' />
  <Action id='M()' state='Init' action='InitOnEntry' />
  <CreateActor id='N()' creator='M()' />
  <Send target='N()' sender='M()' senderState='Init' event='E' isTargetHalted='False' />
  <EnqueueEvent id='N()' event='E' />
  <State id='N()' state='Init' isEntry='True' />
  <DequeueEvent id='N()' state='Init' event='E' />
  <Action id='N()' state='Init' action='Act' />
  <Send target='M()' sender='N()' senderState='Init' event='E' isTargetHalted='False' />
  <EnqueueEvent id='M()' event='E' />
  <DequeueEvent id='M()' state='Init' event='E' />
  <Action id='M()' state='Init' action='Act' />
  <AssertionFailure>&lt;ErrorLog&gt; Reached test assertion.</AssertionFailure>
  <AssertionFailure>StackTrace:
   at Microsoft.Coyote.TestingServices.Tests.Runtime.CustomActorRuntimeLogTests.M.Act()
  </AssertionFailure>
  <Strategy strategy='DFS'>DFS</Strategy>
</Log>
";

                string actual = RemoveNonDeterministicValuesFromReport(builder.ToString()).Replace("\"", "'");
                actual = RemoveStackTraceFromXmlReport(actual);
                Assert.Equal(expected, actual);
            }
            catch (Exception ex)
            {
                Assert.False(true, ex.Message + "\n" + ex.StackTrace);
            }
        }
    }
}
