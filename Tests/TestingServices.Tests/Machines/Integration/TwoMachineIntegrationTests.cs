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
    public class TwoMachineIntegrationTests : BaseTest
    {
        public TwoMachineIntegrationTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private class E1 : Event
        {
        }

        private class E2 : Event
        {
        }

        private class E3 : Event
        {
            public bool Value;

            public E3(bool value)
            {
                this.Value = value;
            }
        }

        private class E4 : Event
        {
            public MachineId Id;

            public E4(MachineId id)
            {
                this.Id = id;
            }
        }

        private class SuccessE : Event
        {
        }

        private class IgnoredE : Event
        {
        }

        private class M1 : Machine
        {
            private bool Test = false;
            private MachineId TargetId;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnExit(nameof(InitOnExit))]
            [OnEventGotoState(typeof(Default), typeof(S1))]
            [OnEventDoAction(typeof(E1), nameof(Action1))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.TargetId = this.CreateMachine(typeof(M2));
                this.Raise(new E1());
            }

            private void InitOnExit()
            {
                this.Send(this.TargetId, new E3(this.Test), options: new SendOptions(assert: 1));
            }

            private class S1 : MachineState
            {
            }

            private void Action1()
            {
                this.Test = true;
            }
        }

        private class M2 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E3), nameof(EntryAction))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
            }

            private void EntryAction()
            {
                if (this.ReceivedEvent.GetType() == typeof(E3))
                {
                    this.Action2();
                }
            }

            private void Action2()
            {
                this.Assert((this.ReceivedEvent as E3).Value == false);
            }
        }

        private class M3 : Machine
        {
            private MachineId TargetId;
            private int Count;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventGotoState(typeof(SuccessE), typeof(Active))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.TargetId = this.CreateMachine(typeof(M4));
                this.Raise(new SuccessE());
            }

            [OnEntry(nameof(ActiveOnEntry))]
            [OnEventGotoState(typeof(SuccessE), typeof(WaitEvent))]
            private class Active : MachineState
            {
            }

            private void ActiveOnEntry()
            {
                this.Count += 1;
                if (this.Count == 1)
                {
                    this.Send(this.TargetId, new E4(this.Id), options: new SendOptions(assert: 1));
                }

                if (this.Count == 2)
                {
                    this.Send(this.TargetId, new IgnoredE());
                }

                this.Raise(new SuccessE());
            }

            [OnEventGotoState(typeof(E1), typeof(Active))]
            private class WaitEvent : MachineState
            {
            }

            private class Done : MachineState
            {
            }
        }

        private class M4 : Machine
        {
            [Start]
            [OnEventGotoState(typeof(E4), typeof(Active))]
            [OnEventDoAction(typeof(IgnoredE), nameof(Action1))]
            private class Waiting : MachineState
            {
            }

            private void Action1()
            {
                this.Assert(false);
            }

            [OnEntry(nameof(ActiveOnEntry))]
            [OnEventGotoState(typeof(SuccessE), typeof(Waiting))]
            private class Active : MachineState
            {
            }

            private void ActiveOnEntry()
            {
                this.Send((this.ReceivedEvent as E4).Id, new E1(), options: new SendOptions(assert: 1));
                this.Raise(new SuccessE());
            }
        }

        private class M5 : Machine
        {
            private MachineId TargetId;
            private int Count;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventGotoState(typeof(SuccessE), typeof(Active))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.TargetId = this.CreateMachine(typeof(M6));
                this.Raise(new SuccessE());
            }

            [OnEntry(nameof(ActiveOnEntry))]
            [OnEventGotoState(typeof(SuccessE), typeof(WaitEvent))]
            private class Active : MachineState
            {
            }

            private void ActiveOnEntry()
            {
                this.Count += 1;
                if (this.Count == 1)
                {
                    this.Send(this.TargetId, new E4(this.Id), options: new SendOptions(assert: 1));
                }

                if (this.Count == 2)
                {
                    this.Send(this.TargetId, new Halt());
                    this.Send(this.TargetId, new IgnoredE());
                }

                this.Raise(new SuccessE());
            }

            [OnEventGotoState(typeof(E1), typeof(Active))]
            private class WaitEvent : MachineState
            {
            }

            private class Done : MachineState
            {
            }
        }

        private class M6 : Machine
        {
            [Start]
            [OnEventGotoState(typeof(E4), typeof(Active))]
            [OnEventGotoState(typeof(Halt), typeof(Inactive))]
            private class Waiting : MachineState
            {
            }

            [OnEntry(nameof(ActiveOnEntry))]
            [OnEventGotoState(typeof(SuccessE), typeof(Waiting))]
            private class Active : MachineState
            {
            }

            private void ActiveOnEntry()
            {
                this.Send((this.ReceivedEvent as E4).Id, new E1(), options: new SendOptions(assert: 1));
                this.Raise(new SuccessE());
            }

            [OnEventDoAction(typeof(IgnoredE), nameof(Action1))]
            [IgnoreEvents(typeof(E4))]
            private class Inactive : MachineState
            {
            }

            private void Action1()
            {
                this.Assert(false);
            }
        }

        private class M7 : Machine
        {
            private MachineId TargetId;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventGotoState(typeof(SuccessE), typeof(Active))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.TargetId = this.CreateMachine(typeof(M8));
                this.Raise(new SuccessE());
            }

            [OnEntry(nameof(ActiveOnEntry))]
            [OnEventGotoState(typeof(SuccessE), typeof(Waiting))]
            private class Active : MachineState
            {
            }

            private void ActiveOnEntry()
            {
                this.Send(this.TargetId, new E4(this.Id), options: new SendOptions(assert: 1));
                this.Raise(new SuccessE());
            }

            [OnEventGotoState(typeof(E1), typeof(Active))]
            private class Waiting : MachineState
            {
            }

            private class Done : MachineState
            {
            }
        }

        private class M8 : Machine
        {
            private int Count2 = 0;

            [Start]
            [OnEntry(nameof(EntryWaitPing))]
            [OnEventGotoState(typeof(E4), typeof(Active))]
            private class Waiting : MachineState
            {
            }

            private void EntryWaitPing()
            {
            }

            [OnEntry(nameof(ActiveOnEntry))]
            [OnEventGotoState(typeof(SuccessE), typeof(Waiting))]
            [OnEventDoAction(typeof(Halt), nameof(Action1))]
            private class Active : MachineState
            {
            }

            private void ActiveOnEntry()
            {
                this.Count2 += 1;

                if (this.Count2 == 1)
                {
                    this.Send((this.ReceivedEvent as E4).Id, new E1(), options: new SendOptions(assert: 1));
                }

                if (this.Count2 == 2)
                {
                    this.Send((this.ReceivedEvent as E4).Id, new E1(), options: new SendOptions(assert: 1));
                    this.Raise(new Halt());
                    return;
                }

                this.Raise(new SuccessE());
            }

            private void Action1()
            {
                this.Assert(false);
            }
        }

        private class M9 : Machine
        {
            private MachineId TargetId;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnExit(nameof(InitOnExit))]
            [OnEventGotoState(typeof(E1), typeof(Active))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Raise(new E1());
            }

            private void InitOnExit()
            {
                this.TargetId = this.CreateMachine(typeof(M10));
                this.Send(this.TargetId, new E1(), options: new SendOptions(assert: 1));
            }

            [OnEntry(nameof(ActiveOnEntry))]
            private class Active : MachineState
            {
            }

            private void ActiveOnEntry()
            {
                this.Send(this.TargetId, new E2(), options: new SendOptions(assert: 1));
            }
        }

        private class M10 : Machine
        {
            [Start]
            [OnEventDoAction(typeof(E1), nameof(HandleE1))]
            [OnEventDoAction(typeof(E2), nameof(HandleE2))]
            private class Init : MachineState
            {
            }

            private void HandleE1()
            {
            }

            private void HandleE2()
            {
                this.Assert(false);
            }
        }

        [Fact(Timeout=5000)]
        public void TestTwoMachineIntegration1()
        {
            this.TestWithError(r =>
            {
                r.CreateMachine(typeof(M1));
            },
            configuration: GetConfiguration().WithStrategy(SchedulingStrategy.DFS),
            expectedError: "Detected an assertion failure.",
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestTwoMachineIntegration2()
        {
            this.TestWithError(r =>
            {
                r.CreateMachine(typeof(M3));
            },
            configuration: GetConfiguration().WithStrategy(SchedulingStrategy.DFS),
            expectedError: "Detected an assertion failure.",
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestTwoMachineIntegration3()
        {
            this.TestWithError(r =>
            {
                r.CreateMachine(typeof(M5));
            },
            configuration: GetConfiguration().WithStrategy(SchedulingStrategy.DFS),
            expectedError: "Detected an assertion failure.",
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestTwoMachineIntegration4()
        {
            this.TestWithError(r =>
            {
                r.CreateMachine(typeof(M7));
            },
            configuration: GetConfiguration().WithStrategy(SchedulingStrategy.DFS),
            expectedError: "Detected an assertion failure.",
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestTwoMachineIntegration5()
        {
            this.TestWithError(r =>
            {
                r.CreateMachine(typeof(M9));
            },
            expectedError: "Detected an assertion failure.",
            replay: true);
        }
    }
}
