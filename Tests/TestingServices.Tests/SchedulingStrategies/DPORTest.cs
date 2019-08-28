// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using Microsoft.Coyote.Utilities;

using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests
{
    public class DPORTest : BaseTest
    {
        public DPORTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class Ping : Event
        {
        }

        private class SenderInitEvent : Event
        {
            public readonly MachineId WaiterMachineId;
            public readonly bool SendPing;
            public readonly bool DoNonDet;

            public SenderInitEvent(MachineId waiter, bool sendPing = false, bool doNonDet = false)
            {
                this.WaiterMachineId = waiter;
                this.SendPing = sendPing;
                this.DoNonDet = doNonDet;
            }
        }

        private class InitEvent : Event
        {
        }

        private class DummyEvent : Event
        {
        }

        private class Waiter : Machine
        {
            [Start]
            [OnEventDoAction(typeof(Ping), nameof(Nothing))]
            private class Init : MachineState
            {
            }

            private void Nothing()
            {
            }
        }

        private class Sender : Machine
        {
            private SenderInitEvent initEvent;

            [Start]
            [OnEntry(nameof(Initialize))]
            [OnEventDoAction(typeof(Ping), nameof(SendPing))]
            [OnEventDoAction(typeof(DummyEvent), nameof(Nothing))]
            private class Init : MachineState
            {
            }

            private void Initialize()
            {
                this.initEvent = (SenderInitEvent)this.ReceivedEvent;
            }

            private void SendPing()
            {
                if (this.initEvent.SendPing)
                {
                    this.Send(this.initEvent.WaiterMachineId, new Ping());
                }

                if (this.initEvent.DoNonDet)
                {
                    this.Random();
                    this.Random();
                }

                this.Send(this.Id, new DummyEvent());
            }

            private void Nothing()
            {
            }
        }

        private class ReceiverAddressEvent : Event
        {
            public readonly MachineId Receiver;

            public ReceiverAddressEvent(MachineId receiver)
            {
                this.Receiver = receiver;
            }
        }

        private class LevelOne : Machine
        {
            [Start]
            [OnEntry(nameof(Initialize))]
            private class Init : MachineState
            {
            }

            private void Initialize()
            {
                var r = (ReceiverAddressEvent)this.ReceivedEvent;
                this.CreateMachine(typeof(LevelTwo), r);
                this.CreateMachine(typeof(LevelTwo), r);
            }
        }

        private class LevelTwo : Machine
        {
            [Start]
            [OnEntry(nameof(Initialize))]
            private class Init : MachineState
            {
            }

            private void Initialize()
            {
                var r = (ReceiverAddressEvent)this.ReceivedEvent;
                var a = this.CreateMachine(typeof(Sender), new SenderInitEvent(r.Receiver, true));
                this.Send(a, new Ping());
                var b = this.CreateMachine(typeof(Sender), new SenderInitEvent(r.Receiver, true));
                this.Send(b, new Ping());
            }
        }

        private class ReceiveWaiter : Machine
        {
            [Start]
            [OnEntry(nameof(Initialize))]
            private class Init : MachineState
            {
            }

            private async Task Initialize()
            {
                await this.Receive(typeof(Ping));
                await this.Receive(typeof(Ping));
            }
        }

        [Fact(Timeout=5000)]
        public void TestDPOR1Reduces()
        {
            // DPOR: 1 schedule.
            var runtime = this.Test(r =>
            {
                MachineId waiter = r.CreateMachine(typeof(Waiter));
                MachineId sender1 = r.CreateMachine(typeof(Sender), new SenderInitEvent(waiter));
                MachineId sender2 = r.CreateMachine(typeof(Sender), new SenderInitEvent(waiter));
                MachineId sender3 = r.CreateMachine(typeof(Sender), new SenderInitEvent(waiter));
                r.SendEvent(sender1, new Ping());
                r.SendEvent(sender2, new Ping());
                r.SendEvent(sender3, new Ping());
            },
            configuration: Configuration.Create().WithStrategy(SchedulingStrategy.DPOR).WithNumberOfIterations(10));

            Assert.Equal(1, runtime.TestReport.NumOfExploredUnfairSchedules);

            // DFS: at least 6 schedules.
            runtime = this.Test(r =>
            {
                MachineId waiter = r.CreateMachine(typeof(Waiter));
                MachineId sender1 = r.CreateMachine(typeof(Sender), new SenderInitEvent(waiter));
                MachineId sender2 = r.CreateMachine(typeof(Sender), new SenderInitEvent(waiter));
                MachineId sender3 = r.CreateMachine(typeof(Sender), new SenderInitEvent(waiter));
                r.SendEvent(sender1, new Ping());
                r.SendEvent(sender2, new Ping());
                r.SendEvent(sender3, new Ping());
            },
            configuration: Configuration.Create().WithStrategy(SchedulingStrategy.DFS).WithNumberOfIterations(10));

            Assert.True(runtime.TestReport.NumOfExploredUnfairSchedules >= 6);
        }

        [Fact(Timeout=5000)]
        public void TestDPOR2NonDet()
        {
            // DPOR: 4 schedules because there are 2 nondet choices.
            var runtime = this.Test(r =>
            {
                MachineId waiter = r.CreateMachine(typeof(Waiter));
                MachineId sender1 = r.CreateMachine(typeof(Sender), new SenderInitEvent(waiter, false, true));
                MachineId sender2 = r.CreateMachine(typeof(Sender), new SenderInitEvent(waiter));
                MachineId sender3 = r.CreateMachine(typeof(Sender), new SenderInitEvent(waiter));
                r.SendEvent(sender1, new Ping());
                r.SendEvent(sender2, new Ping());
                r.SendEvent(sender3, new Ping());
            },
            configuration: Configuration.Create().WithStrategy(SchedulingStrategy.DPOR).WithNumberOfIterations(10));

            Assert.Equal(4, runtime.TestReport.NumOfExploredUnfairSchedules);
        }

        [Fact(Timeout=5000)]
        public void TestDPOR3CreatingMany()
        {
            this.Test(r =>
            {
                MachineId waiter = r.CreateMachine(typeof(Waiter));
                r.CreateMachine(typeof(LevelOne), new ReceiverAddressEvent(waiter));
                r.CreateMachine(typeof(LevelOne), new ReceiverAddressEvent(waiter));
            },
            configuration: Configuration.Create().WithStrategy(SchedulingStrategy.DPOR).WithNumberOfIterations(10));
        }

        [Fact(Timeout=5000)]
        public void TestDPOR4UseReceive()
        {
            this.Test(r =>
            {
                MachineId waiter = r.CreateMachine(typeof(ReceiveWaiter));

                var a = r.CreateMachine(typeof(Sender), new SenderInitEvent(waiter, true));
                r.SendEvent(a, new Ping());
                var b = r.CreateMachine(typeof(Sender), new SenderInitEvent(waiter, true));
                r.SendEvent(b, new Ping());
            },
            configuration: Configuration.Create().WithStrategy(SchedulingStrategy.DPOR).WithNumberOfIterations(1000));
        }
    }
}
