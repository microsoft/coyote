// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Tests.Common.Actors;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests.Actors
{
    public class WildCardEventTests : BaseTest
    {
        public WildCardEventTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private class E1 : Event
        {
        }

        private class E2 : Event
        {
        }

        [OnEventDoAction(typeof(UnitEvent), nameof(Foo))]
        private class Aa : Actor
        {
            protected override Task OnInitializeAsync(Event initialEvent)
            {
                this.DeferEvent(typeof(WildCardEvent));
                return Task.CompletedTask;
            }

            private void Foo()
            {
            }
        }

        private class Ab : Actor
        {
            protected override Task OnInitializeAsync(Event initialEvent)
            {
                var a = this.CreateActor(typeof(Aa));
                this.SendEvent(a, new E2());
                this.SendEvent(a, UnitEvent.Instance);
                this.SendEvent(a, new E1());
                return Task.CompletedTask;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestWildCardEventInActor()
        {
            this.Test(r =>
            {
                r.CreateActor(typeof(Ab));
            });
        }

        private class Ma : StateMachine
        {
            [Start]
            [OnEventDoAction(typeof(UnitEvent), nameof(Foo))]
            [OnEventGotoState(typeof(E1), typeof(S1))]
            [DeferEvents(typeof(WildCardEvent))]
            private class S0 : State
            {
            }

            [OnEventDoAction(typeof(E2), nameof(Bar))]
            private class S1 : State
            {
            }

            private void Foo()
            {
            }

            private void Bar()
            {
            }
        }

        private class Mb : StateMachine
        {
            [Start]
            [OnEntry(nameof(Conf))]
            private class Init : State
            {
            }

            private void Conf()
            {
                var a = this.CreateActor(typeof(Ma));
                this.SendEvent(a, new E2());
                this.SendEvent(a, UnitEvent.Instance);
                this.SendEvent(a, new E1());
            }
        }

        [Fact(Timeout = 5000)]
        public void TestWildCardEventInStateMachine()
        {
            this.Test(r =>
            {
                r.CreateActor(typeof(Mb));
            });
        }
    }
}
