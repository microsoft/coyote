// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Tests.Common.Actors;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests.Actors.StateMachines
{
    public class IgnoreRaisedTest : BaseTest
    {
        public IgnoreRaisedTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class E1 : Event
        {
        }

        private class E2 : Event
        {
            public ActorId Mid;

            public E2(ActorId id)
            {
                this.Mid = id;
            }
        }

        private class A : StateMachine
        {
            [Start]
            [OnEventDoAction(typeof(E1), nameof(Foo))]
            [IgnoreEvents(typeof(UnitEvent))]
            [OnEventDoAction(typeof(E2), nameof(Bar))]
            private class Init : State
            {
            }

            private void Foo()
            {
                this.RaiseEvent(new UnitEvent());
            }

            private void Bar()
            {
                var e = this.ReceivedEvent as E2;
                this.SendEvent(e.Mid, new E2(this.Id));
            }
        }

        private class Harness : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private async Task InitOnEntry()
            {
                var m = this.CreateActor(typeof(A));
                this.SendEvent(m, new E1());
                this.SendEvent(m, new E2(this.Id));
                var e = await this.ReceiveEventAsync(typeof(E2)) as E2;
            }
        }

        /// <summary>
        /// Coyote semantics test: testing for ignore of a raised event.
        /// </summary>
        [Fact(Timeout=5000)]
        public void TestIgnoreRaisedEventHandled()
        {
            this.Test(r =>
            {
                r.CreateActor(typeof(Harness));
            },
            configuration: GetConfiguration().WithNumberOfIterations(5));
        }
    }
}
