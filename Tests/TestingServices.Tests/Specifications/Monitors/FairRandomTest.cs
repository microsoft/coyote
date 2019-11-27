// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.Specifications;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests.Specifications
{
    public class FairRandomTest : BaseTest
    {
        public FairRandomTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class E1 : Event
        {
        }

        private class E2 : Event
        {
        }

        private class Engine
        {
            public static bool FairRandom(IActorRuntime runtime)
            {
                return runtime.FairRandom();
            }
        }

        private class UntilDone : Monitor
        {
            [Start]
            [Hot]
            [OnEventGotoState(typeof(E2), typeof(End))]
            private class Waiting : State
            {
            }

            [Cold]
            private class End : State
            {
            }
        }

        private class M : StateMachine
        {
            [Start]
            [OnEventDoAction(typeof(E1), nameof(HandleEvent1))]
            [OnEventDoAction(typeof(E2), nameof(HandleEvent2))]
            private class Init : State
            {
            }

            private void HandleEvent1()
            {
                if (Engine.FairRandom(this.Id.Runtime))
                {
                    this.SendEvent(this.Id, new E2());
                }
                else
                {
                    this.SendEvent(this.Id, new E1());
                }
            }

            private void HandleEvent2()
            {
                this.Monitor<UntilDone>(new E2());
                this.RaiseEvent(HaltEvent.Instance);
            }
        }

        [Fact(Timeout=5000)]
        public void TestFairRandom()
        {
            this.Test(r =>
            {
                r.RegisterMonitor(typeof(UntilDone));
                var m = r.CreateActor(typeof(M));
                r.SendEvent(m, new E1());
            });
        }
    }
}
