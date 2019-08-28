// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.Coyote.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests
{
    public class MaxEventInstancesTest : BaseTest
    {
        public MaxEventInstancesTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class Config : Event
        {
            public MachineId Id;

            public Config(MachineId id)
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

        private class Unit : Event
        {
        }

        private class M : Machine
        {
            private MachineId N;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventPushState(typeof(Unit), typeof(S1))]
            [OnEventGotoState(typeof(E4), typeof(S2))]
            [OnEventDoAction(typeof(E2), nameof(Action1))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.N = this.CreateMachine(typeof(N));
                this.Send(this.N, new Config(this.Id));
                this.Raise(new Unit());
            }

            [OnEntry(nameof(EntryS1))]
            private class S1 : MachineState
            {
            }

            private void EntryS1()
            {
                this.Send(this.N, new E1(), options: new SendOptions(assert: 1));
                this.Send(this.N, new E1(), options: new SendOptions(assert: 1)); // Error.
            }

            [OnEntry(nameof(EntryS2))]
            [OnEventGotoState(typeof(Unit), typeof(S3))]
            private class S2 : MachineState
            {
            }

            private void EntryS2()
            {
                this.Raise(new Unit());
            }

            [OnEventGotoState(typeof(E4), typeof(S3))]
            private class S3 : MachineState
            {
            }

            private void Action1()
            {
                this.Assert((this.ReceivedEvent as E2).Value == 100);
                this.Send(this.N, new E3());
                this.Send(this.N, new E3());
            }
        }

        private class N : Machine
        {
            private MachineId M;

            [Start]
            [OnEventDoAction(typeof(Config), nameof(Configure))]
            [OnEventGotoState(typeof(Unit), typeof(GhostInit))]
            private class Init : MachineState
            {
            }

            private void Configure()
            {
                this.M = (this.ReceivedEvent as Config).Id;
                this.Raise(new Unit());
            }

            [OnEventGotoState(typeof(E1), typeof(S1))]
            private class GhostInit : MachineState
            {
            }

            [OnEntry(nameof(EntryS1))]
            [OnEventGotoState(typeof(E3), typeof(S2))]
            [IgnoreEvents(typeof(E1))]
            private class S1 : MachineState
            {
            }

            private void EntryS1()
            {
                this.Send(this.M, new E2(100), options: new SendOptions(assert: 1));
            }

            [OnEntry(nameof(EntryS2))]
            [OnEventGotoState(typeof(E3), typeof(GhostInit))]
            private class S2 : MachineState
            {
            }

            private void EntryS2()
            {
                this.Send(this.M, new E4());
                this.Send(this.M, new E4());
                this.Send(this.M, new E4());
            }
        }

        [Fact(Timeout=5000)]
        public void TestMaxEventInstancesAssertionFailure()
        {
            var configuration = GetConfiguration();
            configuration.ReductionStrategy = ReductionStrategy.None;
            configuration.SchedulingStrategy = SchedulingStrategy.DFS;
            configuration.MaxSchedulingSteps = 6;

            this.TestWithError(r =>
            {
                r.CreateMachine(typeof(M));
            },
            configuration: configuration,
            expectedError: "There are more than 1 instances of 'E1' in the input queue of machine 'N()'.",
            replay: true);
        }
    }
}
