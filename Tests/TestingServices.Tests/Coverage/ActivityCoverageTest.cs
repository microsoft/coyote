// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using Microsoft.Coyote.Machines;
using Microsoft.Coyote.TestingServices.Coverage;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests
{
    public class ActivityCoverageTest : BaseTest
    {
        public ActivityCoverageTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class Setup : Event
        {
            public readonly MachineId Id;

            public Setup(MachineId id)
            {
                this.Id = id;
            }
        }

        private class E : Event
        {
        }

        private class M1 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Goto<Done>();
            }

            private class Done : MachineState
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
                r.CreateMachine(typeof(M1));
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
Machine: M1
***************
Machine event coverage: 100.0%
        State: Init
                State event coverage: 100.0%
                Next states: Done
        State: Done
                State event coverage: 100.0%
                Previous states: Init
";

            expected = RemoveExcessiveEmptySpaceFromReport(expected);
            Assert.Equal(expected, result);
        }

        private class M2 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventGotoState(typeof(E), typeof(Done))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Raise(new E());
            }

            private class Done : MachineState
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
                r.CreateMachine(typeof(M2));
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
Machine: M2
***************
Machine event coverage: 100.0%
        State: Init
                State event coverage: 100.0%
                Next states: Done
        State: Done
                State event coverage: 100.0%
                Previous states: Init
";

            expected = RemoveExcessiveEmptySpaceFromReport(expected);
            Assert.Equal(expected, result);
        }

        private class M3A : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventGotoState(typeof(E), typeof(Done))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.CreateMachine(typeof(M3B), new Setup(this.Id));
            }

            private class Done : MachineState
            {
            }
        }

        private class M3B : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                var id = (this.ReceivedEvent as Setup).Id;
                this.Send(id, new E());
            }
        }

        [Fact(Timeout = 5000)]
        public void TestMachineSendEventActivityCoverage()
        {
            var configuration = Configuration.Create();
            configuration.ReportActivityCoverage = true;

            ITestingEngine testingEngine = this.Test(r =>
            {
                r.CreateMachine(typeof(M3A));
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
Machine: M3A
***************
Machine event coverage: 100.0%
        State: Init
                State event coverage: 100.0%
                Events received: E
                Next states: Done
        State: Done
                State event coverage: 100.0%
                Previous states: Init

Machine: M3B
***************
Machine event coverage: 100.0%
        State: Init
                State event coverage: 100.0%
                Events sent: E
";

            expected = RemoveExcessiveEmptySpaceFromReport(expected);
            Assert.Equal(expected, result);
        }

        internal class M4 : Machine
        {
            [Start]
            [OnEventGotoState(typeof(E), typeof(Done))]
            internal class Init : MachineState
            {
            }

            internal class Done : MachineState
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
                var m = r.CreateMachine(typeof(M4));
                r.SendEvent(m, new E());
            },
            configuration);

            // Assert that the coverage is as expected.
            var coverage1 = testingEngine1.TestReport.CoverageInfo;
            Assert.Contains(typeof(M4).FullName, coverage1.MachinesToStates.Keys);
            Assert.Contains(typeof(M4.Init).Name, coverage1.MachinesToStates[typeof(M4).FullName]);
            Assert.Contains(typeof(M4.Done).Name, coverage1.MachinesToStates[typeof(M4).FullName]);
            Assert.Contains(coverage1.RegisteredEvents, tup => tup.Item3 == typeof(E).FullName);

            ITestingEngine testingEngine2 = this.Test(r =>
            {
                var m = r.CreateMachine(typeof(M4));
                r.SendEvent(m, new E());
            },
            configuration);

            // Assert that the coverage is the same as before.
            var coverage2 = testingEngine2.TestReport.CoverageInfo;
            Assert.Contains(typeof(M4).FullName, coverage2.MachinesToStates.Keys);
            Assert.Contains(typeof(M4.Init).Name, coverage2.MachinesToStates[typeof(M4).FullName]);
            Assert.Contains(typeof(M4.Done).Name, coverage2.MachinesToStates[typeof(M4).FullName]);
            Assert.Contains(coverage2.RegisteredEvents, tup => tup.Item3 == typeof(E).FullName);

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

        internal class M5 : Machine
        {
            [Start]
            [OnEventGotoState(typeof(E1), typeof(Done))]
            [OnEventGotoState(typeof(E2), typeof(Done))]
            internal class Init : MachineState
            {
            }

            internal class Done : MachineState
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
                var m = r.CreateMachine(typeof(M5));
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
Machine: M5
***************
Machine event coverage: 50.0%

	State: Init
		State event coverage: 50.0%
		Events received: E1 
		Events not covered: E2 
		Next states: Done 

	State: Done
		State event coverage: 100.0%
		Previous states: Init 

Machine: Env
***************
Machine event coverage: 100.0%

	State: Env
		State event coverage: 100.0%
		Events sent: E1 
";

            expected = RemoveExcessiveEmptySpaceFromReport(expected);
            Assert.Equal(expected, result);
        }
    }
}
