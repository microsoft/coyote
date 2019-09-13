// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.Coyote.Machines;
using Microsoft.Coyote.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests
{
    public class ActionsFailTest : BaseTest
    {
        public ActionsFailTest(ITestOutputHelper output)
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
        }

        private class E3 : Event
        {
        }

        private class E4 : Event
        {
        }

        private class E5 : Event
        {
            public int Value;

            public E5(int value)
            {
                this.Value = value;
            }
        }

        private class Unit : Event
        {
        }

        private class M1A : Machine
        {
            private MachineId GhostMachine;
            private bool Test = false;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnExit(nameof(ExitInit))]
            [OnEventGotoState(typeof(E2), typeof(S1))] // exit actions are performed before transition to S1
            [OnEventDoAction(typeof(E4), nameof(Action1))] // E4, E3 have no effect on reachability of assert(false)
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.GhostMachine = this.CreateMachine(typeof(M1B));
                this.Send(this.GhostMachine, new Config(this.Id));
                this.Send(this.GhostMachine, new E1(), options: new SendOptions(assert: 1));
            }

            private void ExitInit()
            {
                this.Test = true;
            }

            [OnEntry(nameof(EntryS1))]
            [OnEventGotoState(typeof(Unit), typeof(S2))]
            private class S1 : MachineState
            {
            }

            private void EntryS1()
            {
                this.Assert(this.Test == true); // holds
                this.Raise(new Unit());
            }

            [OnEntry(nameof(EntryS2))]
            private class S2 : MachineState
            {
            }

            private void EntryS2()
            {
                // this assert is reachable: M1A -E1-> M1B -E2-> M1A;
                // then Real_S1 (assert holds), Real_S2 (assert fails)
                this.Assert(false);
            }

            private void Action1()
            {
                this.Send(this.GhostMachine, new E3(), options: new SendOptions(assert: 1));
            }
        }

        private class M1B : Machine
        {
            private MachineId RealMachine;

            [Start]
            [OnEventDoAction(typeof(Config), nameof(Configure))]
            [OnEventGotoState(typeof(E1), typeof(S1))]
            private class Init : MachineState
            {
            }

            private void Configure()
            {
                this.RealMachine = (this.ReceivedEvent as Config).Id;
            }

            [OnEntry(nameof(EntryS1))]
            [OnEventGotoState(typeof(E3), typeof(S2))]
            private class S1 : MachineState
            {
            }

            private void EntryS1()
            {
                this.Send(this.RealMachine, new E4(), options: new SendOptions(assert: 1));
                this.Send(this.RealMachine, new E2(), options: new SendOptions(assert: 1));
            }

            private class S2 : MachineState
            {
            }
        }

        /// <summary>
        /// Tests basic semantics of actions and goto transitions.
        /// </summary>
        [Fact(Timeout=5000)]
        public void TestActionsFail1()
        {
            this.TestWithError(r =>
            {
                r.CreateMachine(typeof(M1A));
            },
            configuration: GetConfiguration().WithStrategy(SchedulingStrategy.DFS),
            expectedError: "Detected an assertion failure.",
            replay: true);
        }

        private class M2A : Machine
        {
            private MachineId GhostMachine;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventGotoState(typeof(E4), typeof(S2))]
            [OnEventPushState(typeof(Unit), typeof(S1))]
            [OnEventDoAction(typeof(E2), nameof(Action1))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.GhostMachine = this.CreateMachine(typeof(M2B));
                this.Send(this.GhostMachine, new Config(this.Id));
                this.Raise(new Unit());
            }

            [OnEntry(nameof(EntryS1))]
            private class S1 : MachineState
            {
            }

            private void EntryS1()
            {
                this.Send(this.GhostMachine, new E1(), options: new SendOptions(assert: 1));

                // We wait in this state until E2 comes from M2B,
                // then handle E2 using the inherited handler Action1
                // installed by Init.
                // Then wait until E4 comes from M2B, and since
                // there's no handler for E4 in this pushed state,
                // this state is popped, and E4 goto handler from Init
                // is invoked.
            }

            [OnEntry(nameof(EntryS2))]
            private class S2 : MachineState
            {
            }

            private void EntryS2()
            {
                // this assert is reachable
                this.Assert(false);
            }

            private void Action1()
            {
                this.Send(this.GhostMachine, new E3(), options: new SendOptions(assert: 1));
            }
        }

        private class M2B : Machine
        {
            private MachineId RealMachine;

            [Start]
            [OnEventDoAction(typeof(Config), nameof(Configure))]
            [OnEventGotoState(typeof(E1), typeof(S1))]
            private class Init : MachineState
            {
            }

            private void Configure()
            {
                this.RealMachine = (this.ReceivedEvent as Config).Id;
            }

            [OnEntry(nameof(EntryS1))]
            [OnEventGotoState(typeof(E3), typeof(S2))]
            private class S1 : MachineState
            {
            }

            private void EntryS1()
            {
                this.Send(this.RealMachine, new E2(), options: new SendOptions(assert: 1));
            }

            [OnEntry(nameof(EntryS2))]
            private class S2 : MachineState
            {
            }

            private void EntryS2()
            {
                this.Send(this.RealMachine, new E4(), options: new SendOptions(assert: 1));
            }
        }

        [Fact(Timeout = 5000)]
        public void TestActionsFail2()
        {
            this.TestWithError(r =>
            {
                r.CreateMachine(typeof(M2A));
            },
            configuration: GetConfiguration().WithStrategy(SchedulingStrategy.DFS),
            expectedError: "Detected an assertion failure.",
            replay: true);
        }

        private class M3A : Machine
        {
            private MachineId GhostMachine;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventGotoState(typeof(E4), typeof(S2))]
            [OnEventPushState(typeof(Unit), typeof(S1))]
            [OnEventDoAction(typeof(E5), nameof(Action1))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.GhostMachine = this.CreateMachine(typeof(M3B));
                this.Send(this.GhostMachine, new Config(this.Id));
                this.Raise(new Unit());
            }

            [OnEntry(nameof(EntryS1))]
            private class S1 : MachineState
            {
            }

            private void EntryS1()
            {
                this.Send(this.GhostMachine, new E1(), options: new SendOptions(assert: 1));
            }

            [OnEntry(nameof(EntryS2))]
            private class S2 : MachineState
            {
            }

            private void EntryS2()
            {
                // this assert is reachable
                this.Assert(false);
            }

            private void Action1()
            {
                this.Send(this.GhostMachine, new E3(), options: new SendOptions(assert: 1));
            }
        }

        private class M3B : Machine
        {
            private MachineId RealMachine;

            [Start]
            [OnEventDoAction(typeof(Config), nameof(Configure))]
            [OnEventGotoState(typeof(E1), typeof(S1))]
            private class Init : MachineState
            {
            }

            private void Configure()
            {
                this.RealMachine = (this.ReceivedEvent as Config).Id;
            }

            [OnEntry(nameof(EntryS1))]
            [OnEventGotoState(typeof(E3), typeof(S2))]
            private class S1 : MachineState
            {
            }

            private void EntryS1()
            {
                this.Send(this.RealMachine, new E5(100), options: new SendOptions(assert: 1));
            }

            [OnEntry(nameof(EntryS2))]
            private class S2 : MachineState
            {
            }

            private void EntryS2()
            {
                this.Send(this.RealMachine, new E4(), options: new SendOptions(assert: 1));
            }
        }

        [Fact(Timeout = 5000)]
        public void TestActionsFail3()
        {
            this.TestWithError(r =>
            {
                r.CreateMachine(typeof(M3A));
            },
            configuration: GetConfiguration().WithStrategy(SchedulingStrategy.DFS),
            expectedError: "Detected an assertion failure.",
            replay: true);
        }
    }
}
