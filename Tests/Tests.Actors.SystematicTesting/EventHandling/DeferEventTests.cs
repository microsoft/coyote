// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Actors.SystematicTesting.Tests
{
    public class DeferEventTests : BaseActorSystematicTest
    {
        public DeferEventTests(ITestOutputHelper output)
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
            internal readonly Event DeferredEvent;

            internal E2(ActorId id, Event deferredEvent)
            {
                this.Id = id;
                this.DeferredEvent = deferredEvent;
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
                var deferredEvent = (receivedEvent as E2).DeferredEvent;
                this.Assert(deferredEvent is null, $"Found deferred event of type {deferredEvent?.GetType().Name}.");
            }
        }

        private class M1 : StateMachine
        {
            private Event DeferredEvent;

            [Start]
            [DeferEvents(typeof(E1))]
            [OnEventDoAction(typeof(E2), nameof(Bar))]
            private class Init : State
            {
            }

            private void Bar(Event e)
            {
                this.SendEvent((e as E2).Id, new E2(this.Id, this.DeferredEvent));
            }

            protected override void OnEventDeferred(Event e)
            {
                this.DeferredEvent = e;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestDeferSentEventHandledInStateMachine()
        {
            this.TestWithError(r =>
            {
                var id = r.CreateActor(typeof(M1));
                r.CreateActor(typeof(Harness), new SetupEvent(id));
            },
            configuration: GetConfiguration(),
            expectedError: "Found deferred event of type E1.",
            replay: true);
        }
    }
}
