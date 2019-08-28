// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests
{
    /// <summary>
    /// A single-process implementation of a process scheduling algorithm.
    /// </summary>
    public class ProcessSchedulerTest : BaseTest
    {
        public ProcessSchedulerTest(ITestOutputHelper output)
            : base(output)
        {
        }

        public enum MType
        {
            WakeUp,
            Run
        }

        private class Environment : Machine
        {
            [Start]
            [OnEntry(nameof(OnInitEntry))]
            private class Init : MachineState
            {
            }

            private void OnInitEntry()
            {
                var lkMachine = this.CreateMachine(typeof(LkMachine));
                var rLockMachine = this.CreateMachine(typeof(RLockMachine));
                var rWantMachine = this.CreateMachine(typeof(RWantMachine));
                var nodeMachine = this.CreateMachine(typeof(Node));
                this.CreateMachine(typeof(Client), new Client.Configure(lkMachine, rLockMachine, rWantMachine, nodeMachine));
                this.CreateMachine(typeof(Server), new Server.Configure(lkMachine, rLockMachine, rWantMachine, nodeMachine));
            }
        }

        private class Server : Machine
        {
            public class Configure : Event
            {
                public MachineId LKMachineId;
                public MachineId RLockMachineId;
                public MachineId RWantMachineId;
                public MachineId NodeMachineId;

                public Configure(MachineId lkMachineId, MachineId rLockMachineId,
                    MachineId rWantMachineId, MachineId nodeMachineId)
                {
                    this.LKMachineId = lkMachineId;
                    this.RLockMachineId = rLockMachineId;
                    this.RWantMachineId = rWantMachineId;
                    this.NodeMachineId = nodeMachineId;
                }
            }

            public class Wakeup : Event
            {
            }

            private MachineId LKMachineId;
            private MachineId RLockMachineId;
            private MachineId RWantMachineId;
            public MachineId NodeMachineId;

            [Start]
            [OnEntry(nameof(OnInitialize))]
            [OnEventDoAction(typeof(Wakeup), nameof(OnWakeup))]
            private class Init : MachineState
            {
            }

            private void OnInitialize()
            {
                var e = this.ReceivedEvent as Configure;
                this.LKMachineId = e.LKMachineId;
                this.RLockMachineId = e.RLockMachineId;
                this.RWantMachineId = e.RWantMachineId;
                this.NodeMachineId = e.NodeMachineId;
                this.Raise(new Wakeup());
            }

            private async Task OnWakeup()
            {
                this.Send(this.RLockMachineId, new RLockMachine.SetReq(this.Id, false));
                await this.Receive(typeof(RLockMachine.SetResp));
                this.Send(this.LKMachineId, new LkMachine.Waiting(this.Id, false));
                await this.Receive(typeof(LkMachine.WaitResp));
                this.Send(this.RWantMachineId, new RWantMachine.ValueReq(this.Id));
                var receivedEvent = await this.Receive(typeof(RWantMachine.ValueResp));

                if ((receivedEvent as RWantMachine.ValueResp).Value == true)
                {
                    this.Send(this.RWantMachineId, new RWantMachine.SetReq(this.Id, false));
                    await this.Receive(typeof(RWantMachine.SetResp));

                    this.Send(this.NodeMachineId, new Node.ValueReq(this.Id));
                    var receivedEvent1 = await this.Receive(typeof(Node.ValueResp));
                    if ((receivedEvent1 as Node.ValueResp).Value == MType.WakeUp)
                    {
                        this.Send(this.NodeMachineId, new Node.SetReq(this.Id, MType.Run));
                        await this.Receive(typeof(Node.SetResp));
                    }
                }

                this.Send(this.Id, new Wakeup());
            }
        }

        private class Client : Machine
        {
            public class Configure : Event
            {
                public MachineId LKMachineId;
                public MachineId RLockMachineId;
                public MachineId RWantMachineId;
                public MachineId NodeMachineId;

                public Configure(MachineId lkMachineId, MachineId rLockMachineId,
                    MachineId rWantMachineId, MachineId nodeMachineId)
                {
                    this.LKMachineId = lkMachineId;
                    this.RLockMachineId = rLockMachineId;
                    this.RWantMachineId = rWantMachineId;
                    this.NodeMachineId = nodeMachineId;
                }
            }

            public class Sleep : Event
            {
            }

            public class Progress : Event
            {
            }

            private MachineId LKMachineId;
            private MachineId RLockMachineId;
            private MachineId RWantMachineId;
            public MachineId NodeMachineId;

            [Start]
            [OnEntry(nameof(OnInitialize))]
            [OnEventDoAction(typeof(Sleep), nameof(OnSleep))]
            [OnEventDoAction(typeof(Progress), nameof(OnProgress))]
            private class Init : MachineState
            {
            }

            private void OnInitialize()
            {
                var e = this.ReceivedEvent as Configure;
                this.LKMachineId = e.LKMachineId;
                this.RLockMachineId = e.RLockMachineId;
                this.RWantMachineId = e.RWantMachineId;
                this.NodeMachineId = e.NodeMachineId;
                this.Raise(new Progress());
            }

            private async Task OnSleep()
            {
                this.Send(this.LKMachineId, new LkMachine.AtomicTestSet(this.Id));
                await this.Receive(typeof(LkMachine.AtomicTestSet_Resp));
                while (true)
                {
                    this.Send(this.RLockMachineId, new RLockMachine.ValueReq(this.Id));
                    var receivedEvent = await this.Receive(typeof(RLockMachine.ValueResp));
                    if ((receivedEvent as RLockMachine.ValueResp).Value == true)
                    {
                        this.Send(this.RWantMachineId, new RWantMachine.SetReq(this.Id, true));
                        await this.Receive(typeof(RWantMachine.SetResp));
                        this.Send(this.NodeMachineId, new Node.SetReq(this.Id, MType.WakeUp));
                        await this.Receive(typeof(Node.SetResp));
                        this.Send(this.LKMachineId, new LkMachine.SetReq(this.Id, false));
                        await this.Receive(typeof(LkMachine.SetResp));

                        this.Monitor<LivenessMonitor>(new LivenessMonitor.NotifyClientSleep());

                        this.Send(this.NodeMachineId, new Node.Waiting(this.Id, MType.Run));
                        await this.Receive(typeof(Node.WaitResp));

                        this.Monitor<LivenessMonitor>(new LivenessMonitor.NotifyClientProgress());
                    }
                    else
                    {
                        break;
                    }
                }

                this.Send(this.Id, new Progress());
            }

            private async Task OnProgress()
            {
                this.Send(this.RLockMachineId, new RLockMachine.ValueReq(this.Id));
                var receivedEvent = await this.Receive(typeof(RLockMachine.ValueResp));
                this.Assert((receivedEvent as RLockMachine.ValueResp).Value == false);
                this.Send(this.RLockMachineId, new RLockMachine.SetReq(this.Id, true));
                await this.Receive(typeof(RLockMachine.SetResp));
                this.Send(this.LKMachineId, new LkMachine.SetReq(this.Id, false));
                await this.Receive(typeof(LkMachine.SetResp));
                this.Send(this.Id, new Sleep());
            }
        }

        private class Node : Machine
        {
            public class ValueReq : Event
            {
                public MachineId Target;

                public ValueReq(MachineId target)
                {
                    this.Target = target;
                }
            }

            public class ValueResp : Event
            {
                public MType Value;

                public ValueResp(MType value)
                {
                    this.Value = value;
                }
            }

            public class SetReq : Event
            {
                public MachineId Target;
                public MType Value;

                public SetReq(MachineId target, MType value)
                {
                    this.Target = target;
                    this.Value = value;
                }
            }

            public class SetResp : Event
            {
            }

            public class Waiting : Event
            {
                public MachineId Target;
                public MType WaitingOn;

                public Waiting(MachineId target, MType waitingOn)
                {
                    this.Target = target;
                    this.WaitingOn = waitingOn;
                }
            }

            public class WaitResp : Event
            {
            }

            private MType State;
            private Dictionary<MachineId, MType> blockedMachines;

            [Start]
            [OnEntry(nameof(OnInitialize))]
            [OnEventDoAction(typeof(SetReq), nameof(OnSetReq))]
            [OnEventDoAction(typeof(ValueReq), nameof(OnValueReq))]
            [OnEventDoAction(typeof(Waiting), nameof(OnWaiting))]
            private class Init : MachineState
            {
            }

            private void OnInitialize()
            {
                this.State = MType.Run;
                this.blockedMachines = new Dictionary<MachineId, MType>();
            }

            private void OnSetReq()
            {
                var e = this.ReceivedEvent as SetReq;
                this.State = e.Value;
                this.Unblock();
                this.Send(e.Target, new SetResp());
            }

            private void OnValueReq()
            {
                var e = this.ReceivedEvent as ValueReq;
                this.Send(e.Target, new ValueResp(this.State));
            }

            private void OnWaiting()
            {
                var e = this.ReceivedEvent as Waiting;
                if (this.State == e.WaitingOn)
                {
                    this.Send(e.Target, new WaitResp());
                }
                else
                {
                    this.blockedMachines.Add(e.Target, e.WaitingOn);
                }
            }

            private void Unblock()
            {
                List<MachineId> remove = new List<MachineId>();
                foreach (var target in this.blockedMachines.Keys)
                {
                    if (this.blockedMachines[target] == this.State)
                    {
                        this.Send(target, new WaitResp());
                        remove.Add(target);
                    }
                }

                foreach (var key in remove)
                {
                    this.blockedMachines.Remove(key);
                }
            }
        }

        private class LkMachine : Machine
        {
            public class AtomicTestSet : Event
            {
                public MachineId Target;

                public AtomicTestSet(MachineId target)
                {
                    this.Target = target;
                }
            }

            public class AtomicTestSet_Resp : Event
            {
            }

            public class SetReq : Event
            {
                public MachineId Target;
                public bool Value;

                public SetReq(MachineId target, bool value)
                {
                    this.Target = target;
                    this.Value = value;
                }
            }

            public class SetResp : Event
            {
            }

            public class Waiting : Event
            {
                public MachineId Target;
                public bool WaitingOn;

                public Waiting(MachineId target, bool waitingOn)
                {
                    this.Target = target;
                    this.WaitingOn = waitingOn;
                }
            }

            public class WaitResp : Event
            {
            }

            private bool LK;
            private Dictionary<MachineId, bool> BlockedMachines;

            [Start]
            [OnEntry(nameof(OnInitialize))]
            [OnEventDoAction(typeof(AtomicTestSet), nameof(OnAtomicTestSet))]
            [OnEventDoAction(typeof(SetReq), nameof(OnSetReq))]
            [OnEventDoAction(typeof(Waiting), nameof(OnWaiting))]
            private class Init : MachineState
            {
            }

            private void OnInitialize()
            {
                this.LK = false;
                this.BlockedMachines = new Dictionary<MachineId, bool>();
            }

            private void OnAtomicTestSet()
            {
                var e = this.ReceivedEvent as AtomicTestSet;
                if (this.LK == false)
                {
                    this.LK = true;
                    this.Unblock();
                }

                this.Send(e.Target, new AtomicTestSet_Resp());
            }

            private void OnSetReq()
            {
                var e = this.ReceivedEvent as SetReq;
                this.LK = e.Value;
                this.Unblock();
                this.Send(e.Target, new SetResp());
            }

            private void OnWaiting()
            {
                var e = this.ReceivedEvent as Waiting;
                if (this.LK == e.WaitingOn)
                {
                    this.Send(e.Target, new WaitResp());
                }
                else
                {
                    this.BlockedMachines.Add(e.Target, e.WaitingOn);
                }
            }

            private void Unblock()
            {
                List<MachineId> remove = new List<MachineId>();
                foreach (var target in this.BlockedMachines.Keys)
                {
                    if (this.BlockedMachines[target] == this.LK)
                    {
                        this.Send(target, new WaitResp());
                        remove.Add(target);
                    }
                }

                foreach (var key in remove)
                {
                    this.BlockedMachines.Remove(key);
                }
            }
        }

        private class RLockMachine : Machine
        {
            public class ValueReq : Event
            {
                public MachineId Target;

                public ValueReq(MachineId target)
                {
                    this.Target = target;
                }
            }

            public class ValueResp : Event
            {
                public bool Value;

                public ValueResp(bool value)
                {
                    this.Value = value;
                }
            }

            public class SetReq : Event
            {
                public MachineId Target;
                public bool Value;

                public SetReq(MachineId target, bool value)
                {
                    this.Target = target;
                    this.Value = value;
                }
            }

            public class SetResp : Event
            {
            }

            private bool RLock;

            [Start]
            [OnEntry(nameof(OnInitialize))]
            [OnEventDoAction(typeof(SetReq), nameof(OnSetReq))]
            [OnEventDoAction(typeof(ValueReq), nameof(OnValueReq))]
            private class Init : MachineState
            {
            }

            private void OnInitialize()
            {
                this.RLock = false;
            }

            private void OnSetReq()
            {
                var e = this.ReceivedEvent as SetReq;
                this.RLock = e.Value;
                this.Send(e.Target, new SetResp());
            }

            private void OnValueReq()
            {
                var e = this.ReceivedEvent as ValueReq;
                this.Send(e.Target, new ValueResp(this.RLock));
            }
        }

        private class RWantMachine : Machine
        {
            public class ValueReq : Event
            {
                public MachineId Target;

                public ValueReq(MachineId target)
                {
                    this.Target = target;
                }
            }

            public class ValueResp : Event
            {
                public bool Value;

                public ValueResp(bool value)
                {
                    this.Value = value;
                }
            }

            public class SetReq : Event
            {
                public MachineId Target;
                public bool Value;

                public SetReq(MachineId target, bool value)
                {
                    this.Target = target;
                    this.Value = value;
                }
            }

            public class SetResp : Event
            {
            }

            private bool RWant;

            [Start]
            [OnEntry(nameof(OnInitialize))]
            [OnEventDoAction(typeof(SetReq), nameof(OnSetReq))]
            [OnEventDoAction(typeof(ValueReq), nameof(OnValueReq))]
            private class Init : MachineState
            {
            }

            private void OnInitialize()
            {
                this.RWant = false;
            }

            private void OnSetReq()
            {
                var e = this.ReceivedEvent as SetReq;
                this.RWant = e.Value;
                this.Send(e.Target, new SetResp());
            }

            private void OnValueReq()
            {
                var e = this.ReceivedEvent as ValueReq;
                this.Send(e.Target, new ValueResp(this.RWant));
            }
        }

        private class LivenessMonitor : Monitor
        {
            public class NotifyClientSleep : Event
            {
            }

            public class NotifyClientProgress : Event
            {
            }

            [Start]
            [OnEntry(nameof(InitOnEntry))]

            private class Init : MonitorState
            {
            }

            [Hot]
            [OnEventGotoState(typeof(NotifyClientProgress), typeof(Progressing))]
            private class Suspended : MonitorState
            {
            }

            [Cold]
            [OnEventGotoState(typeof(NotifyClientSleep), typeof(Suspended))]
            private class Progressing : MonitorState
            {
            }

            private void InitOnEntry()
            {
                this.Goto<Progressing>();
            }
        }

        [Theory(Timeout = 5000)]
        // [ClassData(typeof(SeedGenerator))]
        [InlineData(3163)]
        public void TestProcessSchedulerLivenessBugWithCycleReplay(int seed)
        {
            var configuration = GetConfiguration();
            configuration.EnableCycleDetection = true;
            configuration.PrioritySwitchBound = 1;
            configuration.MaxUnfairSchedulingSteps = 100;
            configuration.MaxFairSchedulingSteps = 1000;
            configuration.LivenessTemperatureThreshold = 500;
            configuration.RandomSchedulingSeed = seed;
            configuration.SchedulingIterations = 1;

            this.TestWithError(r =>
            {
                r.RegisterMonitor(typeof(LivenessMonitor));
                r.CreateMachine(typeof(Environment));
            },
            configuration: configuration,
            expectedError: "Monitor 'LivenessMonitor' detected infinite execution that violates a liveness property.",
            replay: true);
        }
    }
}
