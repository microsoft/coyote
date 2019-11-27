// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Specifications;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests.Actors
{
    /// <summary>
    /// This is a (much) simplified version of the replicating storage system described
    /// in the following paper:
    ///
    /// https://www.usenix.org/system/files/conference/fast16/fast16-papers-deligiannis.pdf
    ///
    /// This test contains the liveness bug discussed in the above paper.
    /// </summary>
    public class ReplicatingStorageTest : BaseTest
    {
        public ReplicatingStorageTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class Environment : StateMachine
        {
            public class NotifyNode : Event
            {
                public ActorId Node;

                public NotifyNode(ActorId node)
                    : base()
                {
                    this.Node = node;
                }
            }

            public class FaultInject : Event
            {
            }

            private class CreateFailure : Event
            {
            }

            private class LocalEvent : Event
            {
            }

            private ActorId NodeManager;
            private int NumberOfReplicas;

            private List<ActorId> AliveNodes;
            private int NumberOfFaults;

            private ActorId Client;

            private ActorId FailureTimer;

            [Start]
            [OnEntry(nameof(EntryOnInit))]
            [OnEventGotoState(typeof(LocalEvent), typeof(Configuring))]
            private class Init : State
            {
            }

            private void EntryOnInit()
            {
                this.NumberOfReplicas = 3;
                this.NumberOfFaults = 1;
                this.AliveNodes = new List<ActorId>();

                this.Monitor<LivenessMonitor>(new LivenessMonitor.ConfigureEvent(this.NumberOfReplicas));

                this.NodeManager = this.CreateActor(typeof(NodeManager));
                this.Client = this.CreateActor(typeof(Client));

                this.RaiseEvent(new LocalEvent());
            }

            [OnEntry(nameof(ConfiguringOnInit))]
            [OnEventGotoState(typeof(LocalEvent), typeof(Active))]
            [DeferEvents(typeof(FailureTimer.Timeout))]
            private class Configuring : State
            {
            }

            private void ConfiguringOnInit()
            {
                this.SendEvent(this.NodeManager, new NodeManager.ConfigureEvent(this.Id, this.NumberOfReplicas));
                this.SendEvent(this.Client, new Client.ConfigureEvent(this.NodeManager));
                this.RaiseEvent(new LocalEvent());
            }

            [OnEventDoAction(typeof(NotifyNode), nameof(UpdateAliveNodes))]
            [OnEventDoAction(typeof(FailureTimer.Timeout), nameof(InjectFault))]
            private class Active : State
            {
            }

            private void UpdateAliveNodes()
            {
                var node = (this.ReceivedEvent as NotifyNode).Node;
                this.AliveNodes.Add(node);

                if (this.AliveNodes.Count == this.NumberOfReplicas &&
                    this.FailureTimer is null)
                {
                    this.FailureTimer = this.CreateActor(typeof(FailureTimer));
                    this.SendEvent(this.FailureTimer, new FailureTimer.ConfigureEvent(this.Id));
                }
            }

            private void InjectFault()
            {
                if (this.NumberOfFaults == 0 ||
                    this.AliveNodes.Count == 0)
                {
                    return;
                }

                int nodeId = this.RandomInteger(this.AliveNodes.Count);
                var node = this.AliveNodes[nodeId];

                this.SendEvent(node, new FaultInject());
                this.SendEvent(this.NodeManager, new NodeManager.NotifyFailure(node));
                this.AliveNodes.Remove(node);

                this.NumberOfFaults--;
                if (this.NumberOfFaults == 0)
                {
                    this.SendEvent(this.FailureTimer, HaltEvent.Instance);
                }
            }
        }

        private class NodeManager : StateMachine
        {
            public class ConfigureEvent : Event
            {
                public ActorId Environment;
                public int NumberOfReplicas;

                public ConfigureEvent(ActorId env, int numOfReplicas)
                    : base()
                {
                    this.Environment = env;
                    this.NumberOfReplicas = numOfReplicas;
                }
            }

            public class NotifyFailure : Event
            {
                public ActorId Node;

                public NotifyFailure(ActorId node)
                    : base()
                {
                    this.Node = node;
                }
            }

            internal class ShutDown : Event
            {
            }

            private class LocalEvent : Event
            {
            }

            private ActorId Environment;
            private List<ActorId> StorageNodes;
            private int NumberOfReplicas;
            private Dictionary<int, bool> StorageNodeMap;
            private Dictionary<int, int> DataMap;
            private ActorId RepairTimer;

            [Start]
            [OnEntry(nameof(EntryOnInit))]
            [OnEventDoAction(typeof(ConfigureEvent), nameof(SetupEvent))]
            [OnEventGotoState(typeof(LocalEvent), typeof(Active))]
            [DeferEvents(typeof(Client.Request), typeof(RepairTimer.Timeout))]
            private class Init : State
            {
            }

            private void EntryOnInit()
            {
                this.StorageNodes = new List<ActorId>();
                this.StorageNodeMap = new Dictionary<int, bool>();
                this.DataMap = new Dictionary<int, int>();

                this.RepairTimer = this.CreateActor(typeof(RepairTimer));
                this.SendEvent(this.RepairTimer, new RepairTimer.ConfigureEvent(this.Id));
            }

            private void SetupEvent()
            {
                this.Environment = (this.ReceivedEvent as ConfigureEvent).Environment;
                this.NumberOfReplicas = (this.ReceivedEvent as ConfigureEvent).NumberOfReplicas;

                for (int idx = 0; idx < this.NumberOfReplicas; idx++)
                {
                    this.CreateNewNode();
                }

                this.RaiseEvent(new LocalEvent());
            }

            private void CreateNewNode()
            {
                var idx = this.StorageNodes.Count;
                var node = this.CreateActor(typeof(StorageNode));
                this.StorageNodes.Add(node);
                this.StorageNodeMap.Add(idx, true);
                this.SendEvent(node, new StorageNode.ConfigureEvent(this.Environment, this.Id, idx));
            }

            [OnEventDoAction(typeof(Client.Request), nameof(ProcessClientRequest))]
            [OnEventDoAction(typeof(RepairTimer.Timeout), nameof(RepairNodes))]
            [OnEventDoAction(typeof(StorageNode.SyncReport), nameof(ProcessSyncReport))]
            [OnEventDoAction(typeof(NotifyFailure), nameof(ProcessFailure))]
            private class Active : State
            {
            }

            private void ProcessClientRequest()
            {
                var command = (this.ReceivedEvent as Client.Request).Command;
                var aliveNodeIds = this.StorageNodeMap.Where(n => n.Value).Select(n => n.Key);
                foreach (var nodeId in aliveNodeIds)
                {
                    this.SendEvent(this.StorageNodes[nodeId], new StorageNode.StoreRequest(command));
                }
            }

            private void RepairNodes()
            {
                if (this.DataMap.Count == 0)
                {
                    return;
                }

                var latestData = this.DataMap.Values.Max();
                var numOfReplicas = this.DataMap.Count(kvp => kvp.Value == latestData);
                if (numOfReplicas >= this.NumberOfReplicas)
                {
                    return;
                }

                foreach (var node in this.DataMap)
                {
                    if (node.Value != latestData)
                    {
                        this.SendEvent(this.StorageNodes[node.Key], new StorageNode.SyncRequest(latestData));
                        numOfReplicas++;
                    }

                    if (numOfReplicas == this.NumberOfReplicas)
                    {
                        break;
                    }
                }
            }

            private void ProcessSyncReport()
            {
                var nodeId = (this.ReceivedEvent as StorageNode.SyncReport).NodeId;
                var data = (this.ReceivedEvent as StorageNode.SyncReport).Data;

                // LIVENESS BUG: can fail to ever repair again as it thinks there
                // are enough replicas. Enable to introduce a bug fix.
                // if (!this.StorageNodeMap.ContainsKey(nodeId))
                // {
                //    return;
                // }

                if (!this.DataMap.ContainsKey(nodeId))
                {
                    this.DataMap.Add(nodeId, 0);
                }

                this.DataMap[nodeId] = data;
            }

            private void ProcessFailure()
            {
                var node = (this.ReceivedEvent as NotifyFailure).Node;
                var nodeId = this.StorageNodes.IndexOf(node);
                this.StorageNodeMap.Remove(nodeId);
                this.DataMap.Remove(nodeId);
                this.CreateNewNode();
            }
        }

        private class StorageNode : StateMachine
        {
            public class ConfigureEvent : Event
            {
                public ActorId Environment;
                public ActorId NodeManager;
                public int Id;

                public ConfigureEvent(ActorId env, ActorId manager, int id)
                    : base()
                {
                    this.Environment = env;
                    this.NodeManager = manager;
                    this.Id = id;
                }
            }

            public class StoreRequest : Event
            {
                public int Command;

                public StoreRequest(int cmd)
                    : base()
                {
                    this.Command = cmd;
                }
            }

            public class SyncReport : Event
            {
                public int NodeId;
                public int Data;

                public SyncReport(int id, int data)
                    : base()
                {
                    this.NodeId = id;
                    this.Data = data;
                }
            }

            public class SyncRequest : Event
            {
                public int Data;

                public SyncRequest(int data)
                    : base()
                {
                    this.Data = data;
                }
            }

            internal class ShutDown : Event
            {
            }

            private class LocalEvent : Event
            {
            }

            private ActorId Environment;
            private ActorId NodeManager;
            private int NodeId;
            private int Data;
            private ActorId SyncTimer;

            [Start]
            [OnEntry(nameof(EntryOnInit))]
            [OnEventDoAction(typeof(ConfigureEvent), nameof(SetupEvent))]
            [OnEventGotoState(typeof(LocalEvent), typeof(Active))]
            [DeferEvents(typeof(SyncTimer.Timeout))]
            private class Init : State
            {
            }

            private void EntryOnInit()
            {
                this.Data = 0;
                this.SyncTimer = this.CreateActor(typeof(SyncTimer));
                this.SendEvent(this.SyncTimer, new SyncTimer.ConfigureEvent(this.Id));
            }

            private void SetupEvent()
            {
                this.Environment = (this.ReceivedEvent as ConfigureEvent).Environment;
                this.NodeManager = (this.ReceivedEvent as ConfigureEvent).NodeManager;
                this.NodeId = (this.ReceivedEvent as ConfigureEvent).Id;

                this.Monitor<LivenessMonitor>(new LivenessMonitor.NotifyNodeCreated(this.NodeId));
                this.SendEvent(this.Environment, new Environment.NotifyNode(this.Id));

                this.RaiseEvent(new LocalEvent());
            }

            [OnEventDoAction(typeof(StoreRequest), nameof(Store))]
            [OnEventDoAction(typeof(SyncRequest), nameof(Sync))]
            [OnEventDoAction(typeof(SyncTimer.Timeout), nameof(GenerateSyncReport))]
            [OnEventDoAction(typeof(Environment.FaultInject), nameof(Terminate))]
            private class Active : State
            {
            }

            private void Store()
            {
                var cmd = (this.ReceivedEvent as StoreRequest).Command;
                this.Data += cmd;
                this.Monitor<LivenessMonitor>(new LivenessMonitor.NotifyNodeUpdate(this.NodeId, this.Data));
            }

            private void Sync()
            {
                var data = (this.ReceivedEvent as SyncRequest).Data;
                this.Data = data;
                this.Monitor<LivenessMonitor>(new LivenessMonitor.NotifyNodeUpdate(this.NodeId, this.Data));
            }

            private void GenerateSyncReport()
            {
                this.SendEvent(this.NodeManager, new SyncReport(this.NodeId, this.Data));
            }

            private void Terminate()
            {
                this.Monitor<LivenessMonitor>(new LivenessMonitor.NotifyNodeFail(this.NodeId));
                this.SendEvent(this.SyncTimer, HaltEvent.Instance);
                this.RaiseEvent(HaltEvent.Instance);
            }
        }

        private class FailureTimer : StateMachine
        {
            internal class ConfigureEvent : Event
            {
                public ActorId Target;

                public ConfigureEvent(ActorId id)
                    : base()
                {
                    this.Target = id;
                }
            }

            internal class StartTimerEvent : Event
            {
            }

            internal class CancelTimer : Event
            {
            }

            internal class Timeout : Event
            {
            }

            private class TickEvent : Event
            {
            }

            private ActorId Target;

            [Start]
            [OnEventDoAction(typeof(ConfigureEvent), nameof(SetupEvent))]
            [OnEventGotoState(typeof(StartTimerEvent), typeof(Active))]
            private class Init : State
            {
            }

            private void SetupEvent()
            {
                this.Target = (this.ReceivedEvent as ConfigureEvent).Target;
                this.RaiseEvent(new StartTimerEvent());
            }

            [OnEntry(nameof(ActiveOnEntry))]
            [OnEventDoAction(typeof(TickEvent), nameof(Tick))]
            [OnEventGotoState(typeof(CancelTimer), typeof(Inactive))]
            [IgnoreEvents(typeof(StartTimerEvent))]
            private class Active : State
            {
            }

            private void ActiveOnEntry()
            {
                this.SendEvent(this.Id, new TickEvent());
            }

            private void Tick()
            {
                if (this.Random())
                {
                    this.SendEvent(this.Target, new Timeout());
                }

                this.SendEvent(this.Id, new TickEvent());
            }

            [OnEventGotoState(typeof(StartTimerEvent), typeof(Active))]
            [IgnoreEvents(typeof(CancelTimer), typeof(TickEvent))]
            private class Inactive : State
            {
            }
        }

        private class RepairTimer : StateMachine
        {
            internal class ConfigureEvent : Event
            {
                public ActorId Target;

                public ConfigureEvent(ActorId id)
                    : base()
                {
                    this.Target = id;
                }
            }

            internal class StartTimerEvent : Event
            {
            }

            internal class CancelTimer : Event
            {
            }

            internal class Timeout : Event
            {
            }

            private class TickEvent : Event
            {
            }

            private ActorId Target;

            [Start]
            [OnEventDoAction(typeof(ConfigureEvent), nameof(SetupEvent))]
            [OnEventGotoState(typeof(StartTimerEvent), typeof(Active))]
            private class Init : State
            {
            }

            private void SetupEvent()
            {
                this.Target = (this.ReceivedEvent as ConfigureEvent).Target;
                this.RaiseEvent(new StartTimerEvent());
            }

            [OnEntry(nameof(ActiveOnEntry))]
            [OnEventDoAction(typeof(TickEvent), nameof(Tick))]
            [OnEventGotoState(typeof(CancelTimer), typeof(Inactive))]
            [IgnoreEvents(typeof(StartTimerEvent))]
            private class Active : State
            {
            }

            private void ActiveOnEntry()
            {
                this.SendEvent(this.Id, new TickEvent());
            }

            private void Tick()
            {
                if (this.Random())
                {
                    this.SendEvent(this.Target, new Timeout());
                }

                this.SendEvent(this.Id, new TickEvent());
            }

            [OnEventGotoState(typeof(StartTimerEvent), typeof(Active))]
            [IgnoreEvents(typeof(CancelTimer), typeof(TickEvent))]
            private class Inactive : State
            {
            }
        }

        private class SyncTimer : StateMachine
        {
            internal class ConfigureEvent : Event
            {
                public ActorId Target;

                public ConfigureEvent(ActorId id)
                    : base()
                {
                    this.Target = id;
                }
            }

            internal class StartTimerEvent : Event
            {
            }

            internal class CancelTimer : Event
            {
            }

            internal class Timeout : Event
            {
            }

            private class TickEvent : Event
            {
            }

            private ActorId Target;

            [Start]
            [OnEventDoAction(typeof(ConfigureEvent), nameof(SetupEvent))]
            [OnEventGotoState(typeof(StartTimerEvent), typeof(Active))]
            private class Init : State
            {
            }

            private void SetupEvent()
            {
                this.Target = (this.ReceivedEvent as ConfigureEvent).Target;
                this.RaiseEvent(new StartTimerEvent());
            }

            [OnEntry(nameof(ActiveOnEntry))]
            [OnEventDoAction(typeof(TickEvent), nameof(Tick))]
            [OnEventGotoState(typeof(CancelTimer), typeof(Inactive))]
            [IgnoreEvents(typeof(StartTimerEvent))]
            private class Active : State
            {
            }

            private void ActiveOnEntry()
            {
                this.SendEvent(this.Id, new TickEvent());
            }

            private void Tick()
            {
                if (this.Random())
                {
                    this.SendEvent(this.Target, new Timeout());
                }

                this.SendEvent(this.Id, new TickEvent());
            }

            [OnEventGotoState(typeof(StartTimerEvent), typeof(Active))]
            [IgnoreEvents(typeof(CancelTimer), typeof(TickEvent))]
            private class Inactive : State
            {
            }
        }

        private class Client : StateMachine
        {
            public class ConfigureEvent : Event
            {
                public ActorId NodeManager;

                public ConfigureEvent(ActorId manager)
                    : base()
                {
                    this.NodeManager = manager;
                }
            }

            internal class Request : Event
            {
                public ActorId Client;
                public int Command;

                public Request(ActorId client, int cmd)
                    : base()
                {
                    this.Client = client;
                    this.Command = cmd;
                }
            }

            private class LocalEvent : Event
            {
            }

            private ActorId NodeManager;

            private int Counter;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(ConfigureEvent), nameof(SetupEvent))]
            [OnEventGotoState(typeof(LocalEvent), typeof(PumpRequest))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.Counter = 0;
            }

            private void SetupEvent()
            {
                this.NodeManager = (this.ReceivedEvent as ConfigureEvent).NodeManager;
                this.RaiseEvent(new LocalEvent());
            }

            [OnEntry(nameof(PumpRequestOnEntry))]
            [OnEventGotoState(typeof(LocalEvent), typeof(PumpRequest))]
            private class PumpRequest : State
            {
            }

            private void PumpRequestOnEntry()
            {
                int command = this.RandomInteger(100) + 1;
                this.Counter++;

                this.SendEvent(this.NodeManager, new Request(this.Id, command));

                if (this.Counter == 1)
                {
                    this.RaiseEvent(HaltEvent.Instance);
                }
                else
                {
                    this.RaiseEvent(new LocalEvent());
                }
            }
        }

        private class LivenessMonitor : Monitor
        {
            public class ConfigureEvent : Event
            {
                public int NumberOfReplicas;

                public ConfigureEvent(int numOfReplicas)
                    : base()
                {
                    this.NumberOfReplicas = numOfReplicas;
                }
            }

            public class NotifyNodeCreated : Event
            {
                public int NodeId;

                public NotifyNodeCreated(int id)
                    : base()
                {
                    this.NodeId = id;
                }
            }

            public class NotifyNodeFail : Event
            {
                public int NodeId;

                public NotifyNodeFail(int id)
                    : base()
                {
                    this.NodeId = id;
                }
            }

            public class NotifyNodeUpdate : Event
            {
                public int NodeId;
                public int Data;

                public NotifyNodeUpdate(int id, int data)
                    : base()
                {
                    this.NodeId = id;
                    this.Data = data;
                }
            }

            private class LocalEvent : Event
            {
            }

            private Dictionary<int, int> DataMap;
            private int NumberOfReplicas;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(ConfigureEvent), nameof(SetupEvent))]
            [OnEventGotoState(typeof(LocalEvent), typeof(Repaired))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.DataMap = new Dictionary<int, int>();
            }

            private void SetupEvent()
            {
                this.NumberOfReplicas = (this.ReceivedEvent as ConfigureEvent).NumberOfReplicas;
                this.RaiseEvent(new LocalEvent());
            }

            [Cold]
            [OnEventDoAction(typeof(NotifyNodeCreated), nameof(ProcessNodeCreated))]
            [OnEventDoAction(typeof(NotifyNodeFail), nameof(FailAndCheckRepair))]
            [OnEventDoAction(typeof(NotifyNodeUpdate), nameof(ProcessNodeUpdate))]
            [OnEventGotoState(typeof(LocalEvent), typeof(Repairing))]
            private class Repaired : State
            {
            }

            private void ProcessNodeCreated()
            {
                var nodeId = (this.ReceivedEvent as NotifyNodeCreated).NodeId;
                this.DataMap.Add(nodeId, 0);
            }

            private void FailAndCheckRepair()
            {
                this.ProcessNodeFail();
                this.RaiseEvent(new LocalEvent());
            }

            private void ProcessNodeUpdate()
            {
                var nodeId = (this.ReceivedEvent as NotifyNodeUpdate).NodeId;
                var data = (this.ReceivedEvent as NotifyNodeUpdate).Data;
                this.DataMap[nodeId] = data;
            }

            [Hot]
            [OnEventDoAction(typeof(NotifyNodeCreated), nameof(ProcessNodeCreated))]
            [OnEventDoAction(typeof(NotifyNodeFail), nameof(ProcessNodeFail))]
            [OnEventDoAction(typeof(NotifyNodeUpdate), nameof(CheckIfRepaired))]
            [OnEventGotoState(typeof(LocalEvent), typeof(Repaired))]
            private class Repairing : State
            {
            }

            private void ProcessNodeFail()
            {
                var nodeId = (this.ReceivedEvent as NotifyNodeFail).NodeId;
                this.DataMap.Remove(nodeId);
            }

            private void CheckIfRepaired()
            {
                this.ProcessNodeUpdate();
                var consensus = this.DataMap.Select(kvp => kvp.Value).GroupBy(v => v).
                    OrderByDescending(v => v.Count()).FirstOrDefault();

                var numOfReplicas = consensus.Count();
                if (numOfReplicas >= this.NumberOfReplicas)
                {
                    this.RaiseEvent(new LocalEvent());
                }
            }
        }

        [Fact(Timeout=10000)]
        public void TestReplicatingStorageLivenessBug()
        {
            var configuration = GetConfiguration();
            configuration.MaxUnfairSchedulingSteps = 200;
            configuration.MaxFairSchedulingSteps = 2000;
            configuration.LivenessTemperatureThreshold = 1000;
            configuration.RandomSchedulingSeed = 315;
            configuration.SchedulingIterations = 1;

            this.TestWithError(r =>
            {
                r.RegisterMonitor(typeof(LivenessMonitor));
                r.CreateActor(typeof(Environment));
            },
            configuration: configuration,
            expectedError: "Monitor 'LivenessMonitor' detected potential liveness bug in hot state 'Repairing'.",
            replay: true);
        }

        [Theory(Timeout = 10000)]
        [InlineData(855)]
        public void TestReplicatingStorageLivenessBugWithCycleReplay(int seed)
        {
            var configuration = GetConfiguration();
            configuration.EnableCycleDetection = true;
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
