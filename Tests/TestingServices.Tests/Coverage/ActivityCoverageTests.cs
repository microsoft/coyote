// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.TestingServices.Coverage;
using Microsoft.Coyote.Tests.Common.Actors;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests.Coverage
{
    public class ActivityCoverageTests : BaseTest
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

        private class M1 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private Transition InitOnEntry() => this.GotoState<Done>();

            private class Done : State
            {
            }
        }

        [Fact(Timeout=5000)]
        public void TestMachineStateTransitionActivityCoverage()
        {
            var configuration = Configuration.Create();
            configuration.ReportActivityCoverage = true;

            ITestingEngine testingEngine = this.Test(r =>
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
======================================================================================
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

            private Transition InitOnEntry() => this.RaiseEvent(UnitEvent.Instance);

            private class Done : State
            {
            }
        }

        [Fact(Timeout = 5000)]
        public void TestMachineRaiseEventActivityCoverage()
        {
            var configuration = Configuration.Create();
            configuration.ReportActivityCoverage = true;

            ITestingEngine testingEngine = this.Test(r =>
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
======================================================================================
Event coverage: 100.0%

	State: Init
		State event coverage: 100.0%
		Next states: Done

	State: Done
		State has no expected events, so coverage is 100%
		Previous states: Init
";

            expected = RemoveExcessiveEmptySpaceFromReport(expected);
            Assert.Equal(expected, result);
        }

        private class M3A : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventGotoState(typeof(UnitEvent), typeof(Done))]
            private class Init : State
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
                this.SendEvent((e as Setup).Id, UnitEvent.Instance);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestMachineSendEventActivityCoverage()
        {
            var configuration = Configuration.Create();
            configuration.ReportActivityCoverage = true;

            ITestingEngine testingEngine = this.Test(r =>
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
=======================================================================================
Event coverage: 100.0%

	State: Init
		State event coverage: 100.0%
		Events received: Actors.UnitEvent
		Events sent: Actors.UnitEvent
		Previous states: Init
		Next states: Done

	State: Done
		State has no expected events, so coverage is 100%
		Events received: Actors.UnitEvent
		Previous states: Init

StateMachine: M3B
=======================================================================================
Event coverage: 100.0%

	State: Init
		State has no expected events, so coverage is 100%
		Events sent: Actors.UnitEvent
		Next states: Init

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

            ITestingEngine testingEngine1 = this.Test(r =>
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

            ITestingEngine testingEngine2 = this.Test(r =>
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

            ITestingEngine testingEngine = this.Test(r =>
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
======================================================================================
Event coverage: 50.0%

	State: Init
		State event coverage: 50.0%
		Events received: E1
		Events sent: E1
		Events not covered: E2
		Next states: Done

	State: Done
		State has no expected events, so coverage is 100%
		Events received: E1
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
    }
}
