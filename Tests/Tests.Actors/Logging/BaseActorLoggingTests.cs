// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Specifications;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Actors.Tests.Logging
{
    public class BaseActorLoggingTests : BaseActorTest
    {
        public BaseActorLoggingTests(ITestOutputHelper output)
            : base(output)
        {
        }

        internal class TestMonitor : Monitor
        {
            internal class SetupEvent : Event
            {
            }

            internal class CompletedEvent : Event
            {
            }

            [Start]
            [OnEventDoAction(typeof(SetupEvent), nameof(OnSetup))]
            [OnEventDoAction(typeof(CompletedEvent), nameof(OnCompleted))]
            private class Init : State
            {
            }

#pragma warning disable CA1822 // Mark members as static
            private void OnSetup()
            {
            }

            private void OnCompleted()
            {
            }
#pragma warning restore CA1822 // Mark members as static
        }

        internal class E : Event
        {
            public ActorId Id;

            public E(ActorId id)
            {
                this.Id = id;
            }
        }

        [OnEventDoAction(typeof(E), nameof(Act))]
        internal class M : Actor
        {
            protected override async Task OnInitializeAsync(Event e)
            {
                await base.OnInitializeAsync(e);
                var n = this.CreateActor(typeof(N));
                this.SendEvent(n, new E(this.Id));
            }

            private void Act()
            {
                this.Monitor<TestMonitor>(new TestMonitor.CompletedEvent());
            }
        }

        internal class S : Monitor
        {
            internal class E : Event
            {
                public ActorId Id;

                public E(ActorId id)
                {
                    this.Id = id;
                }
            }

            [Start]
            [Hot]
            [OnEventDoAction(typeof(E), nameof(OnE))]
            private class Init : State
            {
            }

            [Cold]
            private class Done : State
            {
            }

            private void OnE() => this.RaiseGotoStateEvent<Done>();
        }

        internal class N : StateMachine
        {
            [Start]
            [OnEntry(nameof(OnInitEntry))]
            [OnEventGotoState(typeof(E), typeof(Act))]
            private class Init : State
            {
            }

#pragma warning disable CA1822 // Mark members as static
            private void OnInitEntry()
#pragma warning restore CA1822 // Mark members as static
            {
            }

            [OnEntry(nameof(ActOnEntry))]
            private class Act : State
            {
            }

            private void ActOnEntry(Event e)
            {
                ActorId m = (e as E).Id;
                this.Monitor<S>(new S.E(m));
                this.SendEvent(m, new E(this.Id));
            }
        }
    }
}
