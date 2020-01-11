// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Tests.Common.Actors;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests.Actors
{
    public class IgnoreEventTests : BaseTest
    {
        public IgnoreEventTests(ITestOutputHelper output)
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
            public ActorId Id;

            public E2(ActorId id)
            {
                this.Id = id;
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
                this.SendEvent(m, new E2(this.Id));
                await this.ReceiveEventAsync(typeof(E2));
            }
        }

        [OnEventDoAction(typeof(E1), nameof(Foo))]
        [OnEventDoAction(typeof(E2), nameof(Bar))]
        private class A1 : Actor
        {
            protected override Task OnInitializeAsync(Event initialEvent)
            {
                this.IgnoreEvent(typeof(UnitEvent));
                return Task.CompletedTask;
            }

            private void Foo()
            {
                this.SendEvent(this.Id, UnitEvent.Instance);
            }

            private void Bar(Event e)
            {
                this.SendEvent((e as E2).Id, new E2(this.Id));
            }
        }

        [Fact(Timeout = 5000)]
        public void TestIgnoreSentEventHandledInActor()
        {
            this.Test(r =>
            {
                var id = r.CreateActor(typeof(A1));
                r.CreateActor(typeof(Harness), new SetupEvent(id));
            },
            configuration: GetConfiguration().WithNumberOfIterations(5));
        }

        private class M1 : StateMachine
        {
            [Start]
            [OnEventDoAction(typeof(E1), nameof(Foo))]
            [IgnoreEvents(typeof(UnitEvent))]
            [OnEventDoAction(typeof(E2), nameof(Bar))]
            private class Init : State
            {
            }

            private Transition Foo() => this.RaiseEvent(UnitEvent.Instance);

            private void Bar(Event e)
            {
                this.SendEvent((e as E2).Id, new E2(this.Id));
            }
        }

        [Fact(Timeout = 5000)]
        public void TestIgnoreRaisedEventHandledInStateMachine()
        {
            this.Test(r =>
            {
                var id = r.CreateActor(typeof(M1));
                r.CreateActor(typeof(Harness), new SetupEvent(id));
            },
            configuration: GetConfiguration().WithNumberOfIterations(5));
        }
    }
}
