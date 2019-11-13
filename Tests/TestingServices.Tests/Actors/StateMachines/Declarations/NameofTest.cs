// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Actors;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests.Actors.StateMachines
{
    public class NameofTest : BaseTest
    {
        public NameofTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private static int WithNameofValue;
        private static int WithoutNameofValue;

        private class E1 : Event
        {
        }

        private class E2 : Event
        {
        }

        private class M_With_nameof : StateMachine
        {
            [Start]
            [OnEntry(nameof(Coyote_Init_on_entry_action))]
            [OnExit(nameof(Coyote_Init_on_exit_action))]
            [OnEventGotoState(typeof(E1), typeof(Next), nameof(Coyote_Init_E1_action))]
            private class Init : State
            {
            }

            [OnEntry(nameof(Coyote_Next_on_entry_action))]
            [OnEventDoAction(typeof(E2), nameof(Coyote_Next_E2_action))]
            private class Next : State
            {
            }

            protected void Coyote_Init_on_entry_action()
            {
                WithNameofValue += 1;
                this.RaiseEvent(new E1());
            }

            protected void Coyote_Init_on_exit_action()
            {
                WithNameofValue += 10;
            }

            protected void Coyote_Next_on_entry_action()
            {
                WithNameofValue += 1000;
                this.RaiseEvent(new E2());
            }

            protected void Coyote_Init_E1_action()
            {
                WithNameofValue += 100;
            }

            protected void Coyote_Next_E2_action()
            {
                WithNameofValue += 10000;
            }
        }

        private class M_Without_nameof : StateMachine
        {
            [Start]
            [OnEntry("Coyote_Init_on_entry_action")]
            [OnExit("Coyote_Init_on_exit_action")]
            [OnEventGotoState(typeof(E1), typeof(Next), "Coyote_Init_E1_action")]
            private class Init : State
            {
            }

            [OnEntry("Coyote_Next_on_entry_action")]
            [OnEventDoAction(typeof(E2), "Coyote_Next_E2_action")]
            private class Next : State
            {
            }

            protected void Coyote_Init_on_entry_action()
            {
                WithoutNameofValue += 1;
                this.RaiseEvent(new E1());
            }

            protected void Coyote_Init_on_exit_action()
            {
                WithoutNameofValue += 10;
            }

            protected void Coyote_Next_on_entry_action()
            {
                WithoutNameofValue += 1000;
                this.RaiseEvent(new E2());
            }

            protected void Coyote_Init_E1_action()
            {
                WithoutNameofValue += 100;
            }

            protected void Coyote_Next_E2_action()
            {
                WithoutNameofValue += 10000;
            }
        }

        [Fact(Timeout=5000)]
        public void TestAllNameofWithNameof()
        {
            this.Test(r =>
            {
                r.CreateActor(typeof(M_With_nameof));
            });

            Assert.Equal(11111, WithNameofValue);
        }

        [Fact(Timeout=5000)]
        public void TestAllNameofWithoutNameof()
        {
            this.Test(r =>
            {
                r.CreateActor(typeof(M_Without_nameof));
            });

            Assert.Equal(11111, WithoutNameofValue);
        }
    }
}
