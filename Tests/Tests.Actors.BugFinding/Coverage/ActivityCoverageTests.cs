// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using Microsoft.Coyote.Specifications;
using Microsoft.Coyote.Tests.Common;
using Microsoft.Coyote.Tests.Common.Events;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Actors.BugFinding.Tests.Coverage
{
    public class ActivityCoverageTests : BaseActorBugFindingTest
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

#pragma warning disable CA1822 // Mark members as static
            private void InitOnEntry()
#pragma warning restore CA1822 // Mark members as static
            {
            }
        }

        [Fact(Timeout = 5000)]
        public void TestTrivialActivityCoverage()
        {
            var configuration = this.GetConfiguration();
            configuration.IsActivityCoverageReported = true;

            string report = this.TestCoverage(r =>
            {
                r.CreateActor(typeof(M0));
            },
            configuration);

            string result = report.RemoveExcessiveEmptySpace();

            var expected = @"Total event coverage: 100.0%
============================
StateMachine: M0
========================================================================================
Event coverage: 100.0%

	State: Init
		State has no expected events, so coverage is 100%
";

            expected = expected.RemoveExcessiveEmptySpace();
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
            var configuration = this.GetConfiguration();
            configuration.IsActivityCoverageReported = true;

            string report = this.TestCoverage(r =>
            {
                r.CreateActor(typeof(M1));
            },
            configuration);

            string result = report.RemoveExcessiveEmptySpace();

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

            expected = expected.RemoveExcessiveEmptySpace();
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
            var configuration = this.GetConfiguration();
            configuration.IsActivityCoverageReported = true;

            string report = this.TestCoverage(r =>
            {
                r.CreateActor(typeof(M2));
            },
            configuration);

            string result = report.RemoveExcessiveEmptySpace();
            var expected = @"Total event coverage: 100.0%
============================
StateMachine: M2
========================================================================================
Event coverage: 100.0%

	State: Init
		State event coverage: 100.0%
		Events received: Events.UnitEvent
		Events sent: Events.UnitEvent
		Next states: Done

	State: Done
		State has no expected events, so coverage is 100%
		Previous states: Init
";

            expected = expected.RemoveExcessiveEmptySpace();
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

#pragma warning disable CA1822 // Mark members as static
            private void OnHello()
#pragma warning restore CA1822 // Mark members as static
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
            var configuration = this.GetConfiguration();
            configuration.IsActivityCoverageReported = true;

            string report = this.TestCoverage(r =>
            {
                r.CreateActor(typeof(M3A));
            },
            configuration);

            string result = report.RemoveExcessiveEmptySpace();

            var expected = @"Total event coverage: 100.0%
============================
StateMachine: M3A
=========================================================================================
Event coverage: 100.0%

	State: Init
		State event coverage: 100.0%
		Events received: HelloEvent, Events.UnitEvent
		Next states: Done

	State: Done
		State has no expected events, so coverage is 100%
		Previous states: Init

StateMachine: M3B
=========================================================================================
Event coverage: 100.0%

	State: Init
		State has no expected events, so coverage is 100%
		Events sent: HelloEvent, Events.UnitEvent
";

            expected = expected.RemoveExcessiveEmptySpace();
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
            var configuration = this.GetConfiguration();
            configuration.IsActivityCoverageReported = true;

            string report1 = this.TestCoverage(r =>
            {
                var m = r.CreateActor(typeof(M4));
                r.SendEvent(m, UnitEvent.Instance);
            },
            configuration);

            var expected = @"Total event coverage: 100.0%
============================
StateMachine: M4
========================================================================================
Event coverage: 100.0%

	State: Init
		State event coverage: 100.0%
		Events received: Events.UnitEvent
		Next states: Done

	State: Done
		State has no expected events, so coverage is 100%
		Previous states: Init

StateMachine: ExternalCode
==========================
Event coverage: 100.0%

	State: ExternalState
		State has no expected events, so coverage is 100%
		Events sent: Events.UnitEvent
";

            expected = expected.RemoveExcessiveEmptySpace();
            string result = report1.RemoveExcessiveEmptySpace();
            Assert.Equal(expected, result);

            // Make sure second run is not confused by the first.
            string report2 = this.TestCoverage(r =>
            {
                var m = r.CreateActor(typeof(M4));
                r.SendEvent(m, UnitEvent.Instance);
            },
            configuration);

            Assert.Equal(report1, report2);
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
            var configuration = this.GetConfiguration();
            configuration.IsActivityCoverageReported = true;

            string report = this.TestCoverage(r =>
            {
                var m = r.CreateActor(typeof(M5));
                r.SendEvent(m, new E1());
            },
            configuration);

            string result = report.RemoveExcessiveEmptySpace();
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

            expected = expected.RemoveExcessiveEmptySpace();
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
        public void TestPushStateActivityCoverage()
        {
            var configuration = this.GetConfiguration();
            configuration.IsActivityCoverageReported = true;

            string report = this.TestCoverage(r =>
            {
                var actor = r.CreateActor(typeof(M6));
                r.SendEvent(actor, new E1());  // even though Ready state is pushed E1 can still be handled by Init state because Init state is still active.
                r.SendEvent(actor, new E2());  // and that handling does not pop the Ready state, so Ready can still handle E2.
            },
            configuration);

            string result = report.RemoveExcessiveEmptySpace();

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

            expected = expected.RemoveExcessiveEmptySpace();
            Assert.Equal(expected, result);
        }

        internal class Monitor1 : Monitor
        {
            internal class E1 : Event
            {
            }

            internal class E2 : Event
            {
            }

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

#pragma warning disable CA1822 // Mark members as static
            private void OnInit()
#pragma warning restore CA1822 // Mark members as static
            {
            }

            private void HandleE1()
            {
                this.Monitor<Monitor1>(new Monitor1.E1());
                this.RaiseGotoStateEvent<Ready>();
            }

            [OnEventDoAction(typeof(E2), nameof(HandleE2))]
            public class Ready : State
            {
            }

            private void HandleE2()
            {
                this.Monitor<Monitor1>(new Monitor1.E2());
            }
        }

        // Make sure we get coverage information for Monitors.
        [Fact(Timeout = 5000)]
        public void TestMonitorActivityCoverage()
        {
            var configuration = this.GetConfiguration();
            configuration.IsActivityCoverageReported = true;

            string result = this.TestCoverage(r =>
            {
                r.RegisterMonitor<Monitor1>();
                var actor = r.CreateActor(typeof(M7));
                r.SendEvent(actor, new E1());
                r.SendEvent(actor, new E2());
            },
            configuration);

            result = result.RemoveExcessiveEmptySpace();

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
		Next states: Busy[hot]

	State: Busy
		State event coverage: 100.0%
		Events received: E2
		Next states: Idle[cold]

StateMachine: ExternalCode
==========================
Event coverage: 100.0%

	State: ExternalState
		State has no expected events, so coverage is 100%
		Events sent: E1, E2
";

            result = result.RemoveExcessiveEmptySpace();
            expected = expected.RemoveExcessiveEmptySpace();
            Assert.Equal(expected, result);
        }
    }
}
