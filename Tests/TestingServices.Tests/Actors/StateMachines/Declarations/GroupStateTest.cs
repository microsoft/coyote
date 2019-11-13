// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Specifications;
using Microsoft.Coyote.Tests.Common.Actors;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests.Actors.StateMachines
{
    public class GroupStateTest : BaseTest
    {
        public GroupStateTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class M : StateMachine
        {
            private class States1 : StateGroup
            {
                [Start]
                [OnEntry(nameof(States1S1OnEntry))]
                [OnEventGotoState(typeof(UnitEvent), typeof(S2))]
                public class S1 : State
                {
                }

                [OnEntry(nameof(States1S2OnEntry))]
                [OnEventGotoState(typeof(UnitEvent), typeof(States2.S1))]
                public class S2 : State
                {
                }
            }

            private class States2 : StateGroup
            {
                [OnEntry(nameof(States2S1OnEntry))]
                [OnEventGotoState(typeof(UnitEvent), typeof(S2))]
                public class S1 : State
                {
                }

                [OnEntry(nameof(States2S2OnEntry))]
                public class S2 : State
                {
                }
            }

            private void States1S1OnEntry()
            {
                this.RaiseEvent(new UnitEvent());
            }

            private void States1S2OnEntry()
            {
                this.RaiseEvent(new UnitEvent());
            }

            private void States2S1OnEntry()
            {
                this.RaiseEvent(new UnitEvent());
            }

            private void States2S2OnEntry()
            {
                this.Monitor<Safety>(new UnitEvent());
            }
        }

        private class Safety : Monitor
        {
            private class States1 : StateGroup
            {
                [Start]
                [OnEventGotoState(typeof(UnitEvent), typeof(S2))]
                public class S1 : State
                {
                }

                [OnEntry(nameof(States1S2OnEntry))]
                [OnEventGotoState(typeof(UnitEvent), typeof(States2.S1))]
                public class S2 : State
                {
                }
            }

            private class States2 : StateGroup
            {
                [OnEntry(nameof(States2S1OnEntry))]
                [OnEventGotoState(typeof(UnitEvent), typeof(S2))]
                public class S1 : State
                {
                }

                [OnEntry(nameof(States2S2OnEntry))]
                public class S2 : State
                {
                }
            }

            private void States1S2OnEntry()
            {
                this.RaiseEvent(new UnitEvent());
            }

            private void States2S1OnEntry()
            {
                this.RaiseEvent(new UnitEvent());
            }

            private void States2S2OnEntry()
            {
                this.Assert(false);
            }
        }

        [Fact(Timeout=5000)]
        public void TestGroupState()
        {
            this.TestWithError(r =>
            {
                r.RegisterMonitor(typeof(Safety));
                r.CreateActor(typeof(M));
            },
            expectedError: "Detected an assertion failure.",
            replay: true);
        }
    }
}
