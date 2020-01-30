// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Runtime.Exploration;
using Microsoft.Coyote.Tests.Common.Actors;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests.Actors
{
    public class MaxEventInstancesTests : BaseTest
    {
        public MaxEventInstancesTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private class SetupEvent : Event
        {
            public ActorId Id;

            public SetupEvent(ActorId id)
            {
                this.Id = id;
            }
        }

        private class E1 : Event
        {
        }

        private class E2 : Event
        {
            public int Value;

            public E2(int value)
            {
                this.Value = value;
            }
        }

        private class E3 : Event
        {
        }

        private class E4 : Event
        {
        }

        private class M : StateMachine
        {
            private ActorId N;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventPushState(typeof(UnitEvent), typeof(S1))]
            [OnEventGotoState(typeof(E4), typeof(S2))]
            [OnEventDoAction(typeof(E2), nameof(Action1))]
            private class Init : State
            {
            }

            private Transition InitOnEntry()
            {
                this.N = this.CreateActor(typeof(N));
                this.SendEvent(this.N, new SetupEvent(this.Id));
                return this.RaiseEvent(UnitEvent.Instance);
            }

            [OnEntry(nameof(EntryS1))]
            private class S1 : State
            {
            }

            private void EntryS1()
            {
                this.SendEvent(this.N, new E1(), options: new SendOptions(assert: 1));
                this.SendEvent(this.N, new E1(), options: new SendOptions(assert: 1)); // Error.
            }

            [OnEntry(nameof(EntryS2))]
            [OnEventGotoState(typeof(UnitEvent), typeof(S3))]
            private class S2 : State
            {
            }

            private Transition EntryS2() => this.RaiseEvent(UnitEvent.Instance);

            [OnEventGotoState(typeof(E4), typeof(S3))]
            private class S3 : State
            {
            }

            private void Action1(Event e)
            {
                this.Assert((e as E2).Value == 100);
                this.SendEvent(this.N, new E3());
                this.SendEvent(this.N, new E3());
            }
        }

        private class N : StateMachine
        {
            private ActorId M;

            [Start]
            [OnEventDoAction(typeof(SetupEvent), nameof(SetupEvent))]
            [OnEventGotoState(typeof(UnitEvent), typeof(GhostInit))]
            private class Init : State
            {
            }

            private Transition SetupEvent(Event e)
            {
                this.M = (e as SetupEvent).Id;
                return this.RaiseEvent(UnitEvent.Instance);
            }

            [OnEventGotoState(typeof(E1), typeof(S1))]
            private class GhostInit : State
            {
            }

            [OnEntry(nameof(EntryS1))]
            [OnEventGotoState(typeof(E3), typeof(S2))]
            [IgnoreEvents(typeof(E1))]
            private class S1 : State
            {
            }

            private void EntryS1()
            {
                this.SendEvent(this.M, new E2(100), options: new SendOptions(assert: 1));
            }

            [OnEntry(nameof(EntryS2))]
            [OnEventGotoState(typeof(E3), typeof(GhostInit))]
            private class S2 : State
            {
            }

            private void EntryS2()
            {
                this.SendEvent(this.M, new E4());
                this.SendEvent(this.M, new E4());
                this.SendEvent(this.M, new E4());
            }
        }

        [Fact(Timeout = 5000)]
        public void TestMaxEventInstancesAssertionFailure()
        {
            var configuration = GetConfiguration();
            configuration.SchedulingStrategy = SchedulingStrategy.DFS;
            configuration.MaxSchedulingSteps = 6;

            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M));
            },
            configuration: configuration,
            expectedError: "There are more than 1 instances of 'E1' in the input queue of N().",
            replay: true);
        }
    }
}
