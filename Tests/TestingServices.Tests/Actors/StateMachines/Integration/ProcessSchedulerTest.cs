// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Specifications;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests.Actors.StateMachines
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

        private class Environment : StateMachine
        {
            [Start]
            [OnEntry(nameof(OnInitEntry))]
            private class Init : State
            {
            }

            private void OnInitEntry()
            {
                var lkMachine = this.CreateActor(typeof(LkMachine));
                var rLockMachine = this.CreateActor(typeof(RLockMachine));
                var rWantMachine = this.CreateActor(typeof(RWantMachine));
                var nodeMachine = this.CreateActor(typeof(Node));
                this.CreateActor(typeof(Client), new Client.SetupEvent(lkMachine, rLockMachine, rWantMachine, nodeMachine));
                this.CreateActor(typeof(Server), new Server.SetupEvent(lkMachine, rLockMachine, rWantMachine, nodeMachine));
            }
        }

        private class Server : StateMachine
        {
            public class SetupEvent : Event
            {
                public ActorId LKActorId;
                public ActorId RLockActorId;
                public ActorId RWantActorId;
                public ActorId NodeActorId;

                public SetupEvent(ActorId lkActorId, ActorId rLockActorId,
                    ActorId rWantActorId, ActorId nodeActorId)
                {
                    this.LKActorId = lkActorId;
                    this.RLockActorId = rLockActorId;
                    this.RWantActorId = rWantActorId;
                    this.NodeActorId = nodeActorId;
                }
            }

            public class Wakeup : Event
            {
            }

            private ActorId LKActorId;
            private ActorId RLockActorId;
            private ActorId RWantActorId;
            public ActorId NodeActorId;

            [Start]
            [OnEntry(nameof(OnInitialize))]
            [OnEventDoAction(typeof(Wakeup), nameof(OnWakeup))]
            private class Init : State
            {
            }

            private void OnInitialize()
            {
                var e = this.ReceivedEvent as SetupEvent;
                this.LKActorId = e.LKActorId;
                this.RLockActorId = e.RLockActorId;
                this.RWantActorId = e.RWantActorId;
                this.NodeActorId = e.NodeActorId;
                this.RaiseEvent(new Wakeup());
            }

            private async Task OnWakeup()
            {
                this.SendEvent(this.RLockActorId, new RLockMachine.SetReq(this.Id, false));
                await this.ReceiveEventAsync(typeof(RLockMachine.SetResp));
                this.SendEvent(this.LKActorId, new LkMachine.Waiting(this.Id, false));
                await this.ReceiveEventAsync(typeof(LkMachine.WaitResp));
                this.SendEvent(this.RWantActorId, new RWantMachine.ValueReq(this.Id));
                var receivedEvent = await this.ReceiveEventAsync(typeof(RWantMachine.ValueResp));

                if ((receivedEvent as RWantMachine.ValueResp).Value == true)
                {
                    this.SendEvent(this.RWantActorId, new RWantMachine.SetReq(this.Id, false));
                    await this.ReceiveEventAsync(typeof(RWantMachine.SetResp));

                    this.SendEvent(this.NodeActorId, new Node.ValueReq(this.Id));
                    var receivedEvent1 = await this.ReceiveEventAsync(typeof(Node.ValueResp));
                    if ((receivedEvent1 as Node.ValueResp).Value == MType.WakeUp)
                    {
                        this.SendEvent(this.NodeActorId, new Node.SetReq(this.Id, MType.Run));
                        await this.ReceiveEventAsync(typeof(Node.SetResp));
                    }
                }

                this.SendEvent(this.Id, new Wakeup());
            }
        }

        private class Client : StateMachine
        {
            public class SetupEvent : Event
            {
                public ActorId LKActorId;
                public ActorId RLockActorId;
                public ActorId RWantActorId;
                public ActorId NodeActorId;

                public SetupEvent(ActorId lkActorId, ActorId rLockActorId,
                    ActorId rWantActorId, ActorId nodeActorId)
                {
                    this.LKActorId = lkActorId;
                    this.RLockActorId = rLockActorId;
                    this.RWantActorId = rWantActorId;
                    this.NodeActorId = nodeActorId;
                }
            }

            public class Sleep : Event
            {
            }

            public class Progress : Event
            {
            }

            private ActorId LKActorId;
            private ActorId RLockActorId;
            private ActorId RWantActorId;
            public ActorId NodeActorId;

            [Start]
            [OnEntry(nameof(OnInitialize))]
            [OnEventDoAction(typeof(Sleep), nameof(OnSleep))]
            [OnEventDoAction(typeof(Progress), nameof(OnProgress))]
            private class Init : State
            {
            }

            private void OnInitialize()
            {
                var e = this.ReceivedEvent as SetupEvent;
                this.LKActorId = e.LKActorId;
                this.RLockActorId = e.RLockActorId;
                this.RWantActorId = e.RWantActorId;
                this.NodeActorId = e.NodeActorId;
                this.RaiseEvent(new Progress());
            }

            private async Task OnSleep()
            {
                this.SendEvent(this.LKActorId, new LkMachine.AtomicTestSet(this.Id));
                await this.ReceiveEventAsync(typeof(LkMachine.AtomicTestSet_Resp));
                while (true)
                {
                    this.SendEvent(this.RLockActorId, new RLockMachine.ValueReq(this.Id));
                    var receivedEvent = await this.ReceiveEventAsync(typeof(RLockMachine.ValueResp));
                    if ((receivedEvent as RLockMachine.ValueResp).Value == true)
                    {
                        this.SendEvent(this.RWantActorId, new RWantMachine.SetReq(this.Id, true));
                        await this.ReceiveEventAsync(typeof(RWantMachine.SetResp));
                        this.SendEvent(this.NodeActorId, new Node.SetReq(this.Id, MType.WakeUp));
                        await this.ReceiveEventAsync(typeof(Node.SetResp));
                        this.SendEvent(this.LKActorId, new LkMachine.SetReq(this.Id, false));
                        await this.ReceiveEventAsync(typeof(LkMachine.SetResp));

                        this.Monitor<LivenessMonitor>(new LivenessMonitor.NotifyClientSleep());

                        this.SendEvent(this.NodeActorId, new Node.Waiting(this.Id, MType.Run));
                        await this.ReceiveEventAsync(typeof(Node.WaitResp));

                        this.Monitor<LivenessMonitor>(new LivenessMonitor.NotifyClientProgress());
                    }
                    else
                    {
                        break;
                    }
                }

                this.SendEvent(this.Id, new Progress());
            }

            private async Task OnProgress()
            {
                this.SendEvent(this.RLockActorId, new RLockMachine.ValueReq(this.Id));
                var receivedEvent = await this.ReceiveEventAsync(typeof(RLockMachine.ValueResp));
                this.Assert((receivedEvent as RLockMachine.ValueResp).Value == false);
                this.SendEvent(this.RLockActorId, new RLockMachine.SetReq(this.Id, true));
                await this.ReceiveEventAsync(typeof(RLockMachine.SetResp));
                this.SendEvent(this.LKActorId, new LkMachine.SetReq(this.Id, false));
                await this.ReceiveEventAsync(typeof(LkMachine.SetResp));
                this.SendEvent(this.Id, new Sleep());
            }
        }

        private class Node : StateMachine
        {
            public class ValueReq : Event
            {
                public ActorId Target;

                public ValueReq(ActorId target)
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
                public ActorId Target;
                public MType Value;

                public SetReq(ActorId target, MType value)
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
                public ActorId Target;
                public MType WaitingOn;

                public Waiting(ActorId target, MType waitingOn)
                {
                    this.Target = target;
                    this.WaitingOn = waitingOn;
                }
            }

            public class WaitResp : Event
            {
            }

            private MType StateType;
            private Dictionary<ActorId, MType> blockedMachines;

            [Start]
            [OnEntry(nameof(OnInitialize))]
            [OnEventDoAction(typeof(SetReq), nameof(OnSetReq))]
            [OnEventDoAction(typeof(ValueReq), nameof(OnValueReq))]
            [OnEventDoAction(typeof(Waiting), nameof(OnWaiting))]
            private class Init : State
            {
            }

            private void OnInitialize()
            {
                this.StateType = MType.Run;
                this.blockedMachines = new Dictionary<ActorId, MType>();
            }

            private void OnSetReq()
            {
                var e = this.ReceivedEvent as SetReq;
                this.StateType = e.Value;
                this.Unblock();
                this.SendEvent(e.Target, new SetResp());
            }

            private void OnValueReq()
            {
                var e = this.ReceivedEvent as ValueReq;
                this.SendEvent(e.Target, new ValueResp(this.StateType));
            }

            private void OnWaiting()
            {
                var e = this.ReceivedEvent as Waiting;
                if (this.StateType == e.WaitingOn)
                {
                    this.SendEvent(e.Target, new WaitResp());
                }
                else
                {
                    this.blockedMachines.Add(e.Target, e.WaitingOn);
                }
            }

            private void Unblock()
            {
                List<ActorId> remove = new List<ActorId>();
                foreach (var target in this.blockedMachines.Keys)
                {
                    if (this.blockedMachines[target] == this.StateType)
                    {
                        this.SendEvent(target, new WaitResp());
                        remove.Add(target);
                    }
                }

                foreach (var key in remove)
                {
                    this.blockedMachines.Remove(key);
                }
            }
        }

        private class LkMachine : StateMachine
        {
            public class AtomicTestSet : Event
            {
                public ActorId Target;

                public AtomicTestSet(ActorId target)
                {
                    this.Target = target;
                }
            }

            public class AtomicTestSet_Resp : Event
            {
            }

            public class SetReq : Event
            {
                public ActorId Target;
                public bool Value;

                public SetReq(ActorId target, bool value)
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
                public ActorId Target;
                public bool WaitingOn;

                public Waiting(ActorId target, bool waitingOn)
                {
                    this.Target = target;
                    this.WaitingOn = waitingOn;
                }
            }

            public class WaitResp : Event
            {
            }

            private bool LK;
            private Dictionary<ActorId, bool> BlockedMachines;

            [Start]
            [OnEntry(nameof(OnInitialize))]
            [OnEventDoAction(typeof(AtomicTestSet), nameof(OnAtomicTestSet))]
            [OnEventDoAction(typeof(SetReq), nameof(OnSetReq))]
            [OnEventDoAction(typeof(Waiting), nameof(OnWaiting))]
            private class Init : State
            {
            }

            private void OnInitialize()
            {
                this.LK = false;
                this.BlockedMachines = new Dictionary<ActorId, bool>();
            }

            private void OnAtomicTestSet()
            {
                var e = this.ReceivedEvent as AtomicTestSet;
                if (this.LK == false)
                {
                    this.LK = true;
                    this.Unblock();
                }

                this.SendEvent(e.Target, new AtomicTestSet_Resp());
            }

            private void OnSetReq()
            {
                var e = this.ReceivedEvent as SetReq;
                this.LK = e.Value;
                this.Unblock();
                this.SendEvent(e.Target, new SetResp());
            }

            private void OnWaiting()
            {
                var e = this.ReceivedEvent as Waiting;
                if (this.LK == e.WaitingOn)
                {
                    this.SendEvent(e.Target, new WaitResp());
                }
                else
                {
                    this.BlockedMachines.Add(e.Target, e.WaitingOn);
                }
            }

            private void Unblock()
            {
                List<ActorId> remove = new List<ActorId>();
                foreach (var target in this.BlockedMachines.Keys)
                {
                    if (this.BlockedMachines[target] == this.LK)
                    {
                        this.SendEvent(target, new WaitResp());
                        remove.Add(target);
                    }
                }

                foreach (var key in remove)
                {
                    this.BlockedMachines.Remove(key);
                }
            }
        }

        private class RLockMachine : StateMachine
        {
            public class ValueReq : Event
            {
                public ActorId Target;

                public ValueReq(ActorId target)
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
                public ActorId Target;
                public bool Value;

                public SetReq(ActorId target, bool value)
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
            private class Init : State
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
                this.SendEvent(e.Target, new SetResp());
            }

            private void OnValueReq()
            {
                var e = this.ReceivedEvent as ValueReq;
                this.SendEvent(e.Target, new ValueResp(this.RLock));
            }
        }

        private class RWantMachine : StateMachine
        {
            public class ValueReq : Event
            {
                public ActorId Target;

                public ValueReq(ActorId target)
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
                public ActorId Target;
                public bool Value;

                public SetReq(ActorId target, bool value)
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
            private class Init : State
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
                this.SendEvent(e.Target, new SetResp());
            }

            private void OnValueReq()
            {
                var e = this.ReceivedEvent as ValueReq;
                this.SendEvent(e.Target, new ValueResp(this.RWant));
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

            private class Init : State
            {
            }

            [Hot]
            [OnEventGotoState(typeof(NotifyClientProgress), typeof(Progressing))]
            private class Suspended : State
            {
            }

            [Cold]
            [OnEventGotoState(typeof(NotifyClientSleep), typeof(Suspended))]
            private class Progressing : State
            {
            }

            private void InitOnEntry()
            {
                this.Goto<Progressing>();
            }
        }

        [Theory(Timeout = 10000)]
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
                r.CreateActor(typeof(Environment));
            },
            configuration: configuration,
            expectedError: "Monitor 'LivenessMonitor' detected infinite execution that violates a liveness property.",
            replay: true);
        }
    }
}
