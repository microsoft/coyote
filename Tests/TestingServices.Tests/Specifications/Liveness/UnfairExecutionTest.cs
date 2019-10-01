// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Machines;
using Microsoft.Coyote.Runtime.Exploration;
using Microsoft.Coyote.Specifications;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests
{
    public class UnfairExecutionTest : BaseTest
    {
        public UnfairExecutionTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class Unit : Event
        {
        }

        private class E : Event
        {
            public MachineId A;

            public E(MachineId a)
            {
                this.A = a;
            }
        }

        private class M : Machine
        {
            private MachineId N;

            [Start]
            [OnEntry(nameof(SOnEntry))]
            [OnEventGotoState(typeof(Unit), typeof(S2))]
            private class S : MachineState
            {
            }

            private void SOnEntry()
            {
                this.N = this.CreateMachine(typeof(N));
                this.Send(this.N, new E(this.Id));
                this.Raise(new Unit());
            }

            [OnEntry(nameof(S2OnEntry))]
            [OnEventGotoState(typeof(Unit), typeof(S2))]
            [OnEventGotoState(typeof(E), typeof(S3))]
            private class S2 : MachineState
            {
            }

            private void S2OnEntry()
            {
                this.Send(this.Id, new Unit());
            }

            [OnEntry(nameof(S3OnEntry))]
            private class S3 : MachineState
            {
            }

            private void S3OnEntry()
            {
                this.Monitor<LivenessMonitor>(new E(this.Id));
                this.Raise(new Halt());
            }
        }

        private class N : Machine
        {
            [Start]
            [OnEventDoAction(typeof(E), nameof(Foo))]
            private class S : MachineState
            {
            }

            private void Foo()
            {
                this.Send((this.ReceivedEvent as E).A, new E(this.Id));
            }
        }

        private class LivenessMonitor : Monitor
        {
            [Start]
            [Hot]
            [OnEventGotoState(typeof(E), typeof(S2))]
            private class S : MonitorState
            {
            }

            [Cold]
            private class S2 : MonitorState
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
                r.CreateMachine(typeof(M));
            },
            configuration: configuration);
        }
    }
}
