// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Runtime.Exploration;
using Microsoft.Coyote.Specifications;
using Microsoft.Coyote.Tests.Common.Actors;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests.Specifications
{
    public class UnfairExecutionTest : BaseTest
    {
        public UnfairExecutionTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class E : Event
        {
            public ActorId A;

            public E(ActorId a)
            {
                this.A = a;
            }
        }

        private class M : StateMachine
        {
            private ActorId N;

            [Start]
            [OnEntry(nameof(SOnEntry))]
            [OnEventGotoState(typeof(UnitEvent), typeof(S2))]
            private class S : State
            {
            }

            private void SOnEntry()
            {
                this.N = this.CreateActor(typeof(N));
                this.SendEvent(this.N, new E(this.Id));
                this.RaiseEvent(new UnitEvent());
            }

            [OnEntry(nameof(S2OnEntry))]
            [OnEventGotoState(typeof(UnitEvent), typeof(S2))]
            [OnEventGotoState(typeof(E), typeof(S3))]
            private class S2 : State
            {
            }

            private void S2OnEntry()
            {
                this.SendEvent(this.Id, new UnitEvent());
            }

            [OnEntry(nameof(S3OnEntry))]
            private class S3 : State
            {
            }

            private void S3OnEntry()
            {
                this.Monitor<LivenessMonitor>(new E(this.Id));
                this.RaiseEvent(new HaltEvent());
            }
        }

        private class N : StateMachine
        {
            [Start]
            [OnEventDoAction(typeof(E), nameof(Foo))]
            private class S : State
            {
            }

            private void Foo()
            {
                this.SendEvent((this.ReceivedEvent as E).A, new E(this.Id));
            }
        }

        private class LivenessMonitor : Monitor
        {
            [Start]
            [Hot]
            [OnEventGotoState(typeof(E), typeof(S2))]
            private class S : State
            {
            }

            [Cold]
            private class S2 : State
            {
            }
        }

        [Fact(Timeout=5000)]
        public void TestUnfairExecution()
        {
            var configuration = GetConfiguration();
            configuration.LivenessTemperatureThreshold = 150;
            configuration.SchedulingStrategy = SchedulingStrategy.PCT;
            configuration.MaxSchedulingSteps = 300;

            this.Test(r =>
            {
                r.RegisterMonitor(typeof(LivenessMonitor));
                r.CreateActor(typeof(M));
            },
            configuration: configuration);
        }
    }
}
