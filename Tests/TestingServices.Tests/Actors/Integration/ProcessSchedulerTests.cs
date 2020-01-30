// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Specifications;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests.Actors
{
    /// <summary>
    /// A single-process implementation of a process scheduling algorithm.
    /// </summary>
    public class ProcessSchedulerTests : BaseTest
    {
        public ProcessSchedulerTests(ITestOutputHelper output)
            : base(output)
        {
        }

        public enum MType
        {
            WakeUp,
            Run
        }

        private class Environment : Actor
        {
            protected override Task OnInitializeAsync(Event initialEvent)
            {
                var lkActor = this.CreateActor(typeof(LkActor));
                var rLockActor = this.CreateActor(typeof(RLockActor));
                var rWantActor = this.CreateActor(typeof(RWantActor));
                var nodeActor = this.CreateActor(typeof(Node));
                this.CreateActor(typeof(Client), new Client.SetupEvent(lkActor, rLockActor, rWantActor, nodeActor));
                this.CreateActor(typeof(Server), new Server.SetupEvent(lkActor, rLockActor, rWantActor, nodeActor));
                return Task.CompletedTask;
            }
        }

        [OnEventDoAction(typeof(Wakeup), nameof(OnWakeup))]
        private class Server : Actor
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

            protected override Task OnInitializeAsync(Event initialEvent)
            {
                this.LKActorId = (initialEvent as SetupEvent).LKActorId;
                this.RLockActorId = (initialEvent as SetupEvent).RLockActorId;
                this.RWantActorId = (initialEvent as SetupEvent).RWantActorId;
                this.NodeActorId = (initialEvent as SetupEvent).NodeActorId;
                return this.OnWakeup();
            }

            private async Task OnWakeup()
            {
                this.SendEvent(this.RLockActorId, new RLockActor.SetReq(this.Id, false));
                await this.ReceiveEventAsync(typeof(RLockActor.SetResp));
                this.SendEvent(this.LKActorId, new LkActor.Waiting(this.Id, false));
                await this.ReceiveEventAsync(typeof(LkActor.WaitResp));
                this.SendEvent(this.RWantActorId, new RWantActor.ValueReq(this.Id));
                var receivedEvent = await this.ReceiveEventAsync(typeof(RWantActor.ValueResp));

                if ((receivedEvent as RWantActor.ValueResp).Value == true)
                {
                    this.SendEvent(this.RWantActorId, new RWantActor.SetReq(this.Id, false));
                    await this.ReceiveEventAsync(typeof(RWantActor.SetResp));

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

        [OnEventDoAction(typeof(Sleep), nameof(OnSleep))]
        [OnEventDoAction(typeof(Progress), nameof(OnProgress))]
        private class Client : Actor
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

            protected override Task OnInitializeAsync(Event initialEvent)
            {
                this.LKActorId = (initialEvent as SetupEvent).LKActorId;
                this.RLockActorId = (initialEvent as SetupEvent).RLockActorId;
                this.RWantActorId = (initialEvent as SetupEvent).RWantActorId;
                this.NodeActorId = (initialEvent as SetupEvent).NodeActorId;
                return this.OnProgress();
            }

            private async Task OnSleep()
            {
                this.SendEvent(this.LKActorId, new LkActor.AtomicTestSet(this.Id));
                await this.ReceiveEventAsync(typeof(LkActor.AtomicTestSet_Resp));
                while (true)
                {
                    this.SendEvent(this.RLockActorId, new RLockActor.ValueReq(this.Id));
                    var receivedEvent = await this.ReceiveEventAsync(typeof(RLockActor.ValueResp));
                    if ((receivedEvent as RLockActor.ValueResp).Value == true)
                    {
                        this.SendEvent(this.RWantActorId, new RWantActor.SetReq(this.Id, true));
                        await this.ReceiveEventAsync(typeof(RWantActor.SetResp));
                        this.SendEvent(this.NodeActorId, new Node.SetReq(this.Id, MType.WakeUp));
                        await this.ReceiveEventAsync(typeof(Node.SetResp));
                        this.SendEvent(this.LKActorId, new LkActor.SetReq(this.Id, false));
                        await this.ReceiveEventAsync(typeof(LkActor.SetResp));

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
                this.SendEvent(this.RLockActorId, new RLockActor.ValueReq(this.Id));
                var receivedEvent = await this.ReceiveEventAsync(typeof(RLockActor.ValueResp));
                this.Assert((receivedEvent as RLockActor.ValueResp).Value == false);
                this.SendEvent(this.RLockActorId, new RLockActor.SetReq(this.Id, true));
                await this.ReceiveEventAsync(typeof(RLockActor.SetResp));
                this.SendEvent(this.LKActorId, new LkActor.SetReq(this.Id, false));
                await this.ReceiveEventAsync(typeof(LkActor.SetResp));
                this.SendEvent(this.Id, new Sleep());
            }
        }

        [OnEventDoAction(typeof(SetReq), nameof(OnSetReq))]
        [OnEventDoAction(typeof(ValueReq), nameof(OnValueReq))]
        [OnEventDoAction(typeof(Waiting), nameof(OnWaiting))]
        private class Node : Actor
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
            private Dictionary<ActorId, MType> BlockedActors;

            protected override Task OnInitializeAsync(Event initialEvent)
            {
                this.StateType = MType.Run;
                this.BlockedActors = new Dictionary<ActorId, MType>();
                return Task.CompletedTask;
            }

            private void OnSetReq(Event e)
            {
                var evt = e as SetReq;
                this.StateType = evt.Value;
                this.Unblock();
                this.SendEvent(evt.Target, new SetResp());
            }

            private void OnValueReq(Event e)
            {
                this.SendEvent((e as ValueReq).Target, new ValueResp(this.StateType));
            }

            private void OnWaiting(Event e)
            {
                var evt = e as Waiting;
                if (this.StateType == evt.WaitingOn)
                {
                    this.SendEvent(evt.Target, new WaitResp());
                }
                else
                {
                    this.BlockedActors.Add(evt.Target, evt.WaitingOn);
                }
            }

            private void Unblock()
            {
                List<ActorId> remove = new List<ActorId>();
                foreach (var target in this.BlockedActors.Keys)
                {
                    if (this.BlockedActors[target] == this.StateType)
                    {
                        this.SendEvent(target, new WaitResp());
                        remove.Add(target);
                    }
                }

                foreach (var key in remove)
                {
                    this.BlockedActors.Remove(key);
                }
            }
        }

        [OnEventDoAction(typeof(AtomicTestSet), nameof(OnAtomicTestSet))]
        [OnEventDoAction(typeof(SetReq), nameof(OnSetReq))]
        [OnEventDoAction(typeof(Waiting), nameof(OnWaiting))]
        private class LkActor : Actor
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
            private Dictionary<ActorId, bool> BlockedActors;

            protected override Task OnInitializeAsync(Event initialEvent)
            {
                this.LK = false;
                this.BlockedActors = new Dictionary<ActorId, bool>();
                return Task.CompletedTask;
            }

            private void OnAtomicTestSet(Event e)
            {
                var evt = e as AtomicTestSet;
                if (this.LK == false)
                {
                    this.LK = true;
                    this.Unblock();
                }

                this.SendEvent(evt.Target, new AtomicTestSet_Resp());
            }

            private void OnSetReq(Event e)
            {
                var evt = e as SetReq;
                this.LK = evt.Value;
                this.Unblock();
                this.SendEvent(evt.Target, new SetResp());
            }

            private void OnWaiting(Event e)
            {
                var evt = e as Waiting;
                if (this.LK == evt.WaitingOn)
                {
                    this.SendEvent(evt.Target, new WaitResp());
                }
                else
                {
                    this.BlockedActors.Add(evt.Target, evt.WaitingOn);
                }
            }

            private void Unblock()
            {
                List<ActorId> remove = new List<ActorId>();
                foreach (var target in this.BlockedActors.Keys)
                {
                    if (this.BlockedActors[target] == this.LK)
                    {
                        this.SendEvent(target, new WaitResp());
                        remove.Add(target);
                    }
                }

                foreach (var key in remove)
                {
                    this.BlockedActors.Remove(key);
                }
            }
        }

        [OnEventDoAction(typeof(SetReq), nameof(OnSetReq))]
        [OnEventDoAction(typeof(ValueReq), nameof(OnValueReq))]
        private class RLockActor : Actor
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

            protected override Task OnInitializeAsync(Event initialEvent)
            {
                this.RLock = false;
                return Task.CompletedTask;
            }

            private void OnSetReq(Event e)
            {
                var evt = e as SetReq;
                this.RLock = evt.Value;
                this.SendEvent(evt.Target, new SetResp());
            }

            private void OnValueReq(Event e)
            {
                var evt = e as ValueReq;
                this.SendEvent(evt.Target, new ValueResp(this.RLock));
            }
        }

        [OnEventDoAction(typeof(SetReq), nameof(OnSetReq))]
        [OnEventDoAction(typeof(ValueReq), nameof(OnValueReq))]
        private class RWantActor : Actor
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

            protected override Task OnInitializeAsync(Event initialEvent)
            {
                this.RWant = false;
                return Task.CompletedTask;
            }

            private void OnSetReq(Event e)
            {
                var evt = e as SetReq;
                this.RWant = evt.Value;
                this.SendEvent(evt.Target, new SetResp());
            }

            private void OnValueReq(Event e)
            {
                this.SendEvent((e as ValueReq).Target, new ValueResp(this.RWant));
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

            private Transition InitOnEntry() => this.GotoState<Progressing>();
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
            expectedError: "LivenessMonitor detected infinite execution that violates a liveness property.",
            replay: true);
        }
    }
}
