// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Tests.Common.Events;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Actors.BugFinding.Tests
{
    public class IgnoreEventTests : BaseActorBugFindingTest
    {
        public IgnoreEventTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private class SetupEvent : Event
        {
            internal readonly ActorId Id;

            internal SetupEvent(ActorId id)
            {
                this.Id = id;
            }
        }

        private class E1 : Event
        {
        }

        private class E2 : Event
        {
            internal readonly ActorId Id;
            internal readonly Event IgnoredEvent;

            internal E2(ActorId id, Event ignoredEvent)
            {
                this.Id = id;
                this.IgnoredEvent = ignoredEvent;
            }
        }

        private class Harness : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private async Task InitOnEntry(Event e)
            {
                var m = (e as SetupEvent).Id;
                this.SendEvent(m, new E1());
                this.SendEvent(m, new E2(this.Id, null));
                var receivedEvent = await this.ReceiveEventAsync(typeof(E2));
                var ignoredEvent = (receivedEvent as E2).IgnoredEvent;
                this.Assert(ignoredEvent is null, $"Found ignored event of type {ignoredEvent?.GetType().Name}.");
            }
        }

        private class M1 : StateMachine
        {
            private Event IgnoredEvent;

            [Start]
            [OnEventDoAction(typeof(E1), nameof(Foo))]
            [IgnoreEvents(typeof(UnitEvent))]
            [OnEventDoAction(typeof(E2), nameof(Bar))]
            private class Init : State
            {
            }

            private void Foo() => this.RaiseEvent(UnitEvent.Instance);

            private void Bar(Event e)
            {
                this.SendEvent((e as E2).Id, new E2(this.Id, this.IgnoredEvent));
            }

            protected override void OnEventIgnored(Event e)
            {
                this.IgnoredEvent = e;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestIgnoreRaisedEventHandledInStateMachine()
        {
            this.TestWithError(r =>
            {
                var id = r.CreateActor(typeof(M1));
                r.CreateActor(typeof(Harness), new SetupEvent(id));
            },
            configuration: this.GetConfiguration(),
            expectedError: "Found ignored event of type UnitEvent.",
            replay: true);
        }

        private class M2 : StateMachine
        {
            private Event IgnoredEvent;

            [Start]
            [IgnoreEvents(typeof(E1))]
            [OnEventDoAction(typeof(E2), nameof(Bar))]
            private class Init : State
            {
            }

            private void Bar(Event e)
            {
                this.SendEvent((e as E2).Id, new E2(this.Id, this.IgnoredEvent));
            }

            protected override void OnEventIgnored(Event e)
            {
                this.IgnoredEvent = e;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestIgnoreSentEventHandledInStateMachine()
        {
            this.TestWithError(r =>
            {
                var id = r.CreateActor(typeof(M2));
                r.CreateActor(typeof(Harness), new SetupEvent(id));
            },
            configuration: this.GetConfiguration(),
            expectedError: "Found ignored event of type E1.",
            replay: true);
        }
    }
}
