// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using System.IO;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Coverage;
using Microsoft.Coyote.Specifications;
using Microsoft.Coyote.Tests.Common.Actors;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.SystematicTesting.Tests.Coverage
{
    public class ActivityCoverageTests : BaseSystematicTest
    {
        public ActivityCoverageTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private class Setup : Event
        {
            public readonly ActorId Id;

            public Setup(ActorId id)
            {
                this.Id = id;
            }
        }

        private class M0 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
            }
        }

        [Fact(Timeout = 5000)]
        public void TestTrivialActivityCoverage()
        {
            var configuration = Configuration.Create();
            configuration.ReportActivityCoverage = true;

            TestingEngine testingEngine = this.Test(r =>
            {
                r.CreateActor(typeof(M0));
            },
            configuration);

            string result;
            var activityCoverageReporter = new ActivityCoverageReporter(testingEngine.TestReport.CoverageInfo);
            using (var writer = new StringWriter())
            {
                activityCoverageReporter.WriteCoverageText(writer);
                result = RemoveNamespaceReferencesFromReport(writer.ToString());
                result = RemoveExcessiveEmptySpaceFromReport(result);
            }

            var expected = @"Total event coverage: 100.0%
============================
StateMachine: M0
========================================================================================
Event coverage: 100.0%

	State: Init
		State has no expected events, so coverage is 100%
";

            expected = RemoveExcessiveEmptySpaceFromReport(expected);
            Assert.Equal(expected, result);
        }

        private class M1 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private void InitOnEntry() => this.RaiseGotoStateEvent<Done>();

            private class Done : State
            {
            }
        }

        [Fact(Timeout = 5000)]
        public void TestMachineStateTransitionActivityCoverage()
        {
            var configuration = Configuration.Create();
            configuration.ReportActivityCoverage = true;

            TestingEngine testingEngine = this.Test(r =>
            {
                r.CreateActor(typeof(M1));
            },
            configuration);

            string result;
            var activityCoverageReporter = new ActivityCoverageReporter(testingEngine.TestReport.CoverageInfo);
            using (var writer = new StringWriter())
            {
                activityCoverageReporter.WriteCoverageText(writer);
                result = RemoveNamespaceReferencesFromReport(writer.ToString());
                result = RemoveExcessiveEmptySpaceFromReport(result);
            }

            var expected = @"Total event coverage: 100.0%
============================
StateMachine: M1
========================================================================================
Event coverage: 100.0%

	State: Init
		State has no expected events, so coverage is 100%
		Next states: Done

	State: Done
		State has no expected events, so coverage is 100%
		Previous states: Init
";

            expected = RemoveExcessiveEmptySpaceFromReport(expected);
            Assert.Equal(expected, result);
        }

        private class M2 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventGotoState(typeof(UnitEvent), typeof(Done))]
            private class Init : State
            {
            }

            private void InitOnEntry() => this.RaiseEvent(UnitEvent.Instance);

            private class Done : State
            {
            }
        }

        [Fact(Timeout = 5000)]
        public void TestMachineRaiseEventActivityCoverage()
        {
            var configuration = Configuration.Create();
            configuration.ReportActivityCoverage = true;

            TestingEngine testingEngine = this.Test(r =>
            {
                r.CreateActor(typeof(M2));
            },
            configuration);

            string result;
            var activityCoverageReporter = new ActivityCoverageReporter(testingEngine.TestReport.CoverageInfo);
            using (var writer = new StringWriter())
            {
                activityCoverageReporter.WriteCoverageText(writer);
                result = RemoveNamespaceReferencesFromReport(writer.ToString());
                result = RemoveExcessiveEmptySpaceFromReport(result);
            }

            var expected = @"Total event coverage: 100.0%
============================
StateMachine: M2
========================================================================================
Event coverage: 100.0%

	State: Init
		State event coverage: 100.0%
		Events received: Actors.UnitEvent
		Events sent: Actors.UnitEvent
		Next states: Done

	State: Done
		State has no expected events, so coverage is 100%
		Previous states: Init
";

            expected = RemoveExcessiveEmptySpaceFromReport(expected);
            Assert.Equal(expected, result);
        }

        private class HelloEvent : Event
        {
        }

        private class M3A : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(HelloEvent), nameof(OnHello))]
            [OnEventGotoState(typeof(UnitEvent), typeof(Done))]
            private class Init : State
            {
            }

            private void OnHello()
            {
            }

            private void InitOnEntry()
            {
                this.CreateActor(typeof(M3B), new Setup(this.Id));
            }

            private class Done : State
            {
            }
        }

        private class M3B : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private void InitOnEntry(Event e)
            {
                ActorId target = ((Setup)e).Id;
                this.SendEvent(target, new HelloEvent());
                this.SendEvent(target, UnitEvent.Instance);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestMachineSendEventActivityCoverage()
        {
            var configuration = Configuration.Create();
            configuration.ReportActivityCoverage = true;

            TestingEngine testingEngine = this.Test(r =>
            {
                r.CreateActor(typeof(M3A));
            },
            configuration);

            string result;
            var activityCoverageReporter = new ActivityCoverageReporter(testingEngine.TestReport.CoverageInfo);
            using (var writer = new StringWriter())
            {
                activityCoverageReporter.WriteCoverageText(writer);
                result = RemoveNamespaceReferencesFromReport(writer.ToString());
                result = RemoveExcessiveEmptySpaceFromReport(result);
            }

            var expected = @"Total event coverage: 100.0%
============================
StateMachine: M3A
=========================================================================================
Event coverage: 100.0%

	State: Init
		State event coverage: 100.0%
		Events received: HelloEvent, Actors.UnitEvent
		Next states: Done

	State: Done
		State has no expected events, so coverage is 100%
		Previous states: Init

StateMachine: M3B
=========================================================================================
Event coverage: 100.0%

	State: Init
		State has no expected events, so coverage is 100%
		Events sent: HelloEvent, Actors.UnitEvent
";

            expected = RemoveExcessiveEmptySpaceFromReport(expected);
            Assert.Equal(expected, result);
        }

        internal class M4 : StateMachine
        {
            [Start]
            [OnEventGotoState(typeof(UnitEvent), typeof(Done))]
            internal class Init : State
            {
            }

            internal class Done : State
            {
            }
        }

        [Fact(Timeout = 5000)]
        public void TestCoverageOnMultipleTests()
        {
            var configuration = Configuration.Create();
            configuration.ReportActivityCoverage = true;

            TestingEngine testingEngine1 = this.Test(r =>
            {
                var m = r.CreateActor(typeof(M4));
                r.SendEvent(m, UnitEvent.Instance);
            },
            configuration);

            // Assert that the coverage is as expected.
            var coverage1 = testingEngine1.TestReport.CoverageInfo;
            Assert.Contains(typeof(M4).FullName, coverage1.MachinesToStates.Keys);
            Assert.Contains(typeof(M4.Init).Name, coverage1.MachinesToStates[typeof(M4).FullName]);
            Assert.Contains(typeof(M4.Done).Name, coverage1.MachinesToStates[typeof(M4).FullName]);
            Assert.Contains(coverage1.RegisteredEvents, tup => tup.Value.Contains(typeof(UnitEvent).FullName));

            TestingEngine testingEngine2 = this.Test(r =>
            {
                var m = r.CreateActor(typeof(M4));
                r.SendEvent(m, UnitEvent.Instance);
            },
            configuration);

            // Assert that the coverage is the same as before.
            var coverage2 = testingEngine2.TestReport.CoverageInfo;
            Assert.Contains(typeof(M4).FullName, coverage2.MachinesToStates.Keys);
            Assert.Contains(typeof(M4.Init).Name, coverage2.MachinesToStates[typeof(M4).FullName]);
            Assert.Contains(typeof(M4.Done).Name, coverage2.MachinesToStates[typeof(M4).FullName]);
            Assert.Contains(coverage2.RegisteredEvents, tup => tup.Value.Contains(typeof(UnitEvent).FullName));

            string coverageReport1, coverageReport2;

            var activityCoverageReporter = new ActivityCoverageReporter(coverage1);
            using (var writer = new StringWriter())
            {
                activityCoverageReporter.WriteCoverageText(writer);
                coverageReport1 = writer.ToString();
            }

            activityCoverageReporter = new ActivityCoverageReporter(coverage2);
            using (var writer = new StringWriter())
            {
                activityCoverageReporter.WriteCoverageText(writer);
                coverageReport2 = writer.ToString();
            }

            Assert.Equal(coverageReport1, coverageReport2);
        }

        private class E1 : Event
        {
        }

        private class E2 : Event
        {
        }

        internal class M5 : StateMachine
        {
            [Start]
            [OnEventGotoState(typeof(E1), typeof(Done))]
            [OnEventGotoState(typeof(E2), typeof(Done))]
            internal class Init : State
            {
            }

            internal class Done : State
            {
            }
        }

        [Fact(Timeout = 5000)]
        public void TestUncoveredEvents()
        {
            var configuration = Configuration.Create();
            configuration.ReportActivityCoverage = true;

            TestingEngine testingEngine = this.Test(r =>
            {
                var m = r.CreateActor(typeof(M5));
                r.SendEvent(m, new E1());
            },
            configuration);

            string result;
            var activityCoverageReporter = new ActivityCoverageReporter(testingEngine.TestReport.CoverageInfo);
            using (var writer = new StringWriter())
            {
                activityCoverageReporter.WriteCoverageText(writer);
                result = RemoveNamespaceReferencesFromReport(writer.ToString());
                result = RemoveExcessiveEmptySpaceFromReport(result);
            }

            var expected = @"Total event coverage: 50.0%
===========================
StateMachine: M5
========================================================================================
Event coverage: 50.0%

	State: Init
		State event coverage: 50.0%
		Events received: E1
		Events not covered: E2
		Next states: Done

	State: Done
		State has no expected events, so coverage is 100%
		Previous states: Init

StateMachine: ExternalCode
==========================
Event coverage: 100.0%

	State: ExternalState
		State has no expected events, so coverage is 100%
		Events sent: E1
";

            expected = RemoveExcessiveEmptySpaceFromReport(expected);
            Assert.Equal(expected, result);
        }

        internal class M6 : StateMachine
        {
            [Start]
            [OnEntry(nameof(OnInit))]
            [OnEventDoAction(typeof(E1), nameof(HandleE1))]

            public class Init : State
            {
            }

            private void HandleE1()
            {
                Debug.WriteLine("Handling E1 in State {0}", this.CurrentState);
            }

            private void OnInit()
            {
                this.RaisePushStateEvent<Ready>();
            }

            [OnEventDoAction(typeof(E2), nameof(HandleE2))]
            public class Ready : State
            {
            }

            private void HandleE2()
            {
                Debug.WriteLine("Handling E2 in State {0}", this.CurrentState);
            }
        }

        [Fact(Timeout = 5000)]
        private void TestPushStateActivityCoverage()
        {
            var configuration = Configuration.Create();
            configuration.ReportActivityCoverage = true;

            TestingEngine testingEngine = this.Test(r =>
            {
                var actor = r.CreateActor(typeof(M6));
                r.SendEvent(actor, new E1());  // even though Ready state is pushed E1 can still be handled by Init state because Init state is still active.
                r.SendEvent(actor, new E2());  // and that handling does not pop the Ready state, so Ready can still handle E2.
            },
            configuration);

            string result;
            var activityCoverageReporter = new ActivityCoverageReporter(testingEngine.TestReport.CoverageInfo);
            using (var writer = new StringWriter())
            {
                activityCoverageReporter.WriteCoverageText(writer);
                result = RemoveNamespaceReferencesFromReport(writer.ToString());
                result = RemoveExcessiveEmptySpaceFromReport(result);
            }

            var expected = @"Total event coverage: 100.0%
============================
StateMachine: M6
========================================================================================
Event coverage: 100.0%

	State: Init
		State event coverage: 100.0%
		Events received: E1
		Next states: Ready

	State: Ready
		State event coverage: 100.0%
		Events received: E2
		Previous states: Init

StateMachine: ExternalCode
==========================
Event coverage: 100.0%

	State: ExternalState
		State has no expected events, so coverage is 100%
		Events sent: E1, E2
";

            expected = RemoveExcessiveEmptySpaceFromReport(expected);
            Assert.Equal(expected, result);
        }

        internal class Monitor1 : Monitor
        {
            [Cold]
            [Start]
            [OnEventGotoState(typeof(E1), typeof(Busy))]
            internal class Idle : State
            {
            }

            [Hot]
            [OnEventGotoState(typeof(E2), typeof(Idle))]
            internal class Busy : State
            {
            }
        }

        internal class M7 : StateMachine
        {
            [Start]
            [OnEntry(nameof(OnInit))]
            [OnEventDoAction(typeof(E1), nameof(HandleE1))]

            public class Init : State
            {
            }

            private void OnInit()
            {
            }

            private void HandleE1(Event e)
            {
                this.Monitor<Monitor1>(e);
                this.RaiseGotoStateEvent<Ready>();
            }

            [OnEventDoAction(typeof(E2), nameof(HandleE2))]
            public class Ready : State
            {
            }

            private void HandleE2(Event e)
            {
                this.Monitor<Monitor1>(e);
            }
        }

        // Make sure we get coverage information for Monitors.
        [Fact(Timeout = 5000)]
        public void TestMonitorActivityCoverage()
        {
            var configuration = Configuration.Create();
            configuration.ReportActivityCoverage = true;

            TestingEngine testingEngine = this.Test(r =>
            {
                r.RegisterMonitor(typeof(Monitor1));
                var actor = r.CreateActor(typeof(M7));
                r.SendEvent(actor, new E1());
                r.SendEvent(actor, new E2());
            },
            configuration);

            string result;
            var activityCoverageReporter = new ActivityCoverageReporter(testingEngine.TestReport.CoverageInfo);
            using (var writer = new StringWriter())
            {
                activityCoverageReporter.WriteCoverageText(writer);
                result = RemoveNamespaceReferencesFromReport(writer.ToString());
            }

            var expected = @"Total event coverage: 100.0%
============================
StateMachine: M7
========================================================================================
Event coverage: 100.0%

	State: Init
		State event coverage: 100.0%
		Events received: E1
		Next states: Ready

	State: Ready
		State event coverage: 100.0%
		Events received: E2
		Previous states: Init

Monitor: Monitor1
=========================================================================================
Event coverage: 100.0%

	State: Idle
		State event coverage: 100.0%
		Events received: E1
		Previous states: Busy
		Next states: Busy

	State: Busy
		State event coverage: 100.0%
		Events received: E2
		Previous states: Idle
		Next states: Idle

StateMachine: ExternalCode
==========================
Event coverage: 100.0%

	State: ExternalState
		State has no expected events, so coverage is 100%
		Events sent: E1, E2
";

            result = RemoveExcessiveEmptySpaceFromReport(result);
            expected = RemoveExcessiveEmptySpaceFromReport(expected);
            Assert.Equal(expected, result);
        }
    }
}
