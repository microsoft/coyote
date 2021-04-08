// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Actors.BugFinding.Tests
{
    public class NameofTests : BaseActorBugFindingTest
    {
        public NameofTests(ITestOutputHelper output)
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

#pragma warning disable CA1822 // Mark members as static
            protected void Coyote_Init_on_exit_action()
#pragma warning restore CA1822 // Mark members as static
            {
                WithNameofValue += 10;
            }

            protected void Coyote_Next_on_entry_action()
            {
                WithNameofValue += 1000;
                this.RaiseEvent(new E2());
            }

#pragma warning disable CA1822 // Mark members as static
            protected void Coyote_Init_E1_action()
#pragma warning restore CA1822 // Mark members as static
            {
                WithNameofValue += 100;
            }

#pragma warning disable CA1822 // Mark members as static
            protected void Coyote_Next_E2_action()
#pragma warning restore CA1822 // Mark members as static
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

#pragma warning disable CA1822 // Mark members as static
            protected void Coyote_Init_on_exit_action()
#pragma warning restore CA1822 // Mark members as static
            {
                WithoutNameofValue += 10;
            }

            protected void Coyote_Next_on_entry_action()
            {
                WithoutNameofValue += 1000;
                this.RaiseEvent(new E2());
            }

#pragma warning disable CA1822 // Mark members as static
            protected void Coyote_Init_E1_action()
#pragma warning restore CA1822 // Mark members as static
            {
                WithoutNameofValue += 100;
            }

#pragma warning disable CA1822 // Mark members as static
            protected void Coyote_Next_E2_action()
#pragma warning restore CA1822 // Mark members as static
            {
                WithoutNameofValue += 10000;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestAllNameofWithNameof()
        {
            this.Test(r =>
            {
                r.CreateActor(typeof(M_With_nameof));
            });

            Assert.Equal(11111, WithNameofValue);
        }

        [Fact(Timeout = 5000)]
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
