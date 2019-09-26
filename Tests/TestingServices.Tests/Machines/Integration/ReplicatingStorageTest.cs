// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using Microsoft.Coyote.Machines;
using Microsoft.Coyote.Specifications;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests
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

        private class Environment : Machine
        {
            public class NotifyNode : Event
            {
                public MachineId Node;

                public NotifyNode(MachineId node)
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

            private MachineId NodeManager;
            private int NumberOfReplicas;

            private List<MachineId> AliveNodes;
            private int NumberOfFaults;

            private MachineId Client;

            private MachineId FailureTimer;

            [Start]
            [OnEntry(nameof(EntryOnInit))]
            [OnEventGotoState(typeof(LocalEvent), typeof(Configuring))]
            private class Init : MachineState
            {
            }

            private void EntryOnInit()
            {
                this.NumberOfReplicas = 3;
                this.NumberOfFaults = 1;
                this.AliveNodes = new List<MachineId>();

                this.Monitor<LivenessMonitor>(new LivenessMonitor.ConfigureEvent(this.NumberOfReplicas));

                this.NodeManager = this.CreateMachine(typeof(NodeManager));
                this.Client = this.CreateMachine(typeof(Client));

                this.Raise(new LocalEvent());
            }

            [OnEntry(nameof(ConfiguringOnInit))]
            [OnEventGotoState(typeof(LocalEvent), typeof(Active))]
            [DeferEvents(typeof(FailureTimer.Timeout))]
            private class Configuring : MachineState
            {
            }

            private void ConfiguringOnInit()
            {
                this.Send(this.NodeManager, new NodeManager.ConfigureEvent(this.Id, this.NumberOfReplicas));
                this.Send(this.Client, new Client.ConfigureEvent(this.NodeManager));
                this.Raise(new LocalEvent());
            }

            [OnEventDoAction(typeof(NotifyNode), nameof(UpdateAliveNodes))]
            [OnEventDoAction(typeof(FailureTimer.Timeout), nameof(InjectFault))]
            private class Active : MachineState
            {
            }

            private void UpdateAliveNodes()
            {
                var node = (this.ReceivedEvent as NotifyNode).Node;
                this.AliveNodes.Add(node);

                if (this.AliveNodes.Count == this.NumberOfReplicas &&
                    this.FailureTimer is null)
                {
                    this.FailureTimer = this.CreateMachine(typeof(FailureTimer));
                    this.Send(this.FailureTimer, new FailureTimer.ConfigureEvent(this.Id));
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

                this.Send(node, new FaultInject());
                this.Send(this.NodeManager, new NodeManager.NotifyFailure(node));
                this.AliveNodes.Remove(node);

                this.NumberOfFaults--;
                if (this.NumberOfFaults == 0)
                {
                    this.Send(this.FailureTimer, new Halt());
                }
            }
        }

        private class NodeManager : Machine
        {
            public class ConfigureEvent : Event
            {
                public MachineId Environment;
                public int NumberOfReplicas;

                public ConfigureEvent(MachineId env, int numOfReplicas)
                    : base()
                {
                    this.Environment = env;
                    this.NumberOfReplicas = numOfReplicas;
                }
            }

            public class NotifyFailure : Event
            {
                public MachineId Node;

                public NotifyFailure(MachineId node)
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

            private MachineId Environment;
            private List<MachineId> StorageNodes;
            private int NumberOfReplicas;
            private Dictionary<int, bool> StorageNodeMap;
            private Dictionary<int, int> DataMap;
            private MachineId RepairTimer;
            private MachineId Client;

            [Start]
            [OnEntry(nameof(EntryOnInit))]
            [OnEventDoAction(typeof(ConfigureEvent), nameof(Configure))]
            [OnEventGotoState(typeof(LocalEvent), typeof(Active))]
            [DeferEvents(typeof(Client.Request), typeof(RepairTimer.Timeout))]
            private class Init : MachineState
            {
            }

            private void EntryOnInit()
            {
                this.StorageNodes = new List<MachineId>();
                this.StorageNodeMap = new Dictionary<int, bool>();
                this.DataMap = new Dictionary<int, int>();

                this.RepairTimer = this.CreateMachine(typeof(RepairTimer));
                this.Send(this.RepairTimer, new RepairTimer.ConfigureEvent(this.Id));
            }

            private void Configure()
            {
                this.Environment = (this.ReceivedEvent as ConfigureEvent).Environment;
                this.NumberOfReplicas = (this.ReceivedEvent as ConfigureEvent).NumberOfReplicas;

                for (int idx = 0; idx < this.NumberOfReplicas; idx++)
                {
                    this.CreateNewNode();
                }

                this.Raise(new LocalEvent());
            }

            private void CreateNewNode()
            {
                var idx = this.StorageNodes.Count;
                var node = this.CreateMachine(typeof(StorageNode));
                this.StorageNodes.Add(node);
                this.StorageNodeMap.Add(idx, true);
                this.Send(node, new StorageNode.ConfigureEvent(this.Environment, this.Id, idx));
            }

            [OnEventDoAction(typeof(Client.Request), nameof(ProcessClientRequest))]
            [OnEventDoAction(typeof(RepairTimer.Timeout), nameof(RepairNodes))]
            [OnEventDoAction(typeof(StorageNode.SyncReport), nameof(ProcessSyncReport))]
            [OnEventDoAction(typeof(NotifyFailure), nameof(ProcessFailure))]
            private class Active : MachineState
            {
            }

            private void ProcessClientRequest()
            {
                this.Client = (this.ReceivedEvent as Client.Request).Client;
                var command = (this.ReceivedEvent as Client.Request).Command;

                var aliveNodeIds = this.StorageNodeMap.Where(n => n.Value).Select(n => n.Key);
                foreach (var nodeId in aliveNodeIds)
                {
                    this.Send(this.StorageNodes[nodeId], new StorageNode.StoreRequest(command));
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
                        this.Send(this.StorageNodes[node.Key], new StorageNode.SyncRequest(latestData));
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

        private class StorageNode : Machine
        {
            public class ConfigureEvent : Event
            {
                public MachineId Environment;
                public MachineId NodeManager;
                public int Id;

                public ConfigureEvent(MachineId env, MachineId manager, int id)
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

            private MachineId Environment;
            private MachineId NodeManager;
            private int NodeId;
            private int Data;
            private MachineId SyncTimer;

            [Start]
            [OnEntry(nameof(EntryOnInit))]
            [OnEventDoAction(typeof(ConfigureEvent), nameof(Configure))]
            [OnEventGotoState(typeof(LocalEvent), typeof(Active))]
            [DeferEvents(typeof(SyncTimer.Timeout))]
            private class Init : MachineState
            {
            }

            private void EntryOnInit()
            {
                this.Data = 0;
                this.SyncTimer = this.CreateMachine(typeof(SyncTimer));
                this.Send(this.SyncTimer, new SyncTimer.ConfigureEvent(this.Id));
            }

            private void Configure()
            {
                this.Environment = (this.ReceivedEvent as ConfigureEvent).Environment;
                this.NodeManager = (this.ReceivedEvent as ConfigureEvent).NodeManager;
                this.NodeId = (this.ReceivedEvent as ConfigureEvent).Id;

                this.Monitor<LivenessMonitor>(new LivenessMonitor.NotifyNodeCreated(this.NodeId));
                this.Send(this.Environment, new Environment.NotifyNode(this.Id));

                this.Raise(new LocalEvent());
            }

            [OnEventDoAction(typeof(StoreRequest), nameof(Store))]
            [OnEventDoAction(typeof(SyncRequest), nameof(Sync))]
            [OnEventDoAction(typeof(SyncTimer.Timeout), nameof(GenerateSyncReport))]
            [OnEventDoAction(typeof(Environment.FaultInject), nameof(Terminate))]
            private class Active : MachineState
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
                this.Send(this.NodeManager, new SyncReport(this.NodeId, this.Data));
            }

            private void Terminate()
            {
                this.Monitor<LivenessMonitor>(new LivenessMonitor.NotifyNodeFail(this.NodeId));
                this.Send(this.SyncTimer, new Halt());
                this.Raise(new Halt());
            }
        }

        private class FailureTimer : Machine
        {
            internal class ConfigureEvent : Event
            {
                public MachineId Target;

                public ConfigureEvent(MachineId id)
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

            private MachineId Target;

            [Start]
            [OnEventDoAction(typeof(ConfigureEvent), nameof(Configure))]
            [OnEventGotoState(typeof(StartTimerEvent), typeof(Active))]
            private class Init : MachineState
            {
            }

            private void Configure()
            {
                this.Target = (this.ReceivedEvent as ConfigureEvent).Target;
                this.Raise(new StartTimerEvent());
            }

            [OnEntry(nameof(ActiveOnEntry))]
            [OnEventDoAction(typeof(TickEvent), nameof(Tick))]
            [OnEventGotoState(typeof(CancelTimer), typeof(Inactive))]
            [IgnoreEvents(typeof(StartTimerEvent))]
            private class Active : MachineState
            {
            }

            private void ActiveOnEntry()
            {
                this.Send(this.Id, new TickEvent());
            }

            private void Tick()
            {
                if (this.Random())
                {
                    this.Send(this.Target, new Timeout());
                }

                this.Send(this.Id, new TickEvent());
            }

            [OnEventGotoState(typeof(StartTimerEvent), typeof(Active))]
            [IgnoreEvents(typeof(CancelTimer), typeof(TickEvent))]
            private class Inactive : MachineState
            {
            }
        }

        private class RepairTimer : Machine
        {
            internal class ConfigureEvent : Event
            {
                public MachineId Target;

                public ConfigureEvent(MachineId id)
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

            private MachineId Target;

            [Start]
            [OnEventDoAction(typeof(ConfigureEvent), nameof(Configure))]
            [OnEventGotoState(typeof(StartTimerEvent), typeof(Active))]
            private class Init : MachineState
            {
            }

            private void Configure()
            {
                this.Target = (this.ReceivedEvent as ConfigureEvent).Target;
                this.Raise(new StartTimerEvent());
            }

            [OnEntry(nameof(ActiveOnEntry))]
            [OnEventDoAction(typeof(TickEvent), nameof(Tick))]
            [OnEventGotoState(typeof(CancelTimer), typeof(Inactive))]
            [IgnoreEvents(typeof(StartTimerEvent))]
            private class Active : MachineState
            {
            }

            private void ActiveOnEntry()
            {
                this.Send(this.Id, new TickEvent());
            }

            private void Tick()
            {
                if (this.Random())
                {
                    this.Send(this.Target, new Timeout());
                }

                this.Send(this.Id, new TickEvent());
            }

            [OnEventGotoState(typeof(StartTimerEvent), typeof(Active))]
            [IgnoreEvents(typeof(CancelTimer), typeof(TickEvent))]
            private class Inactive : MachineState
            {
            }
        }

        private class SyncTimer : Machine
        {
            internal class ConfigureEvent : Event
            {
                public MachineId Target;

                public ConfigureEvent(MachineId id)
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

            private MachineId Target;

            [Start]
            [OnEventDoAction(typeof(ConfigureEvent), nameof(Configure))]
            [OnEventGotoState(typeof(StartTimerEvent), typeof(Active))]
            private class Init : MachineState
            {
            }

            private void Configure()
            {
                this.Target = (this.ReceivedEvent as ConfigureEvent).Target;
                this.Raise(new StartTimerEvent());
            }

            [OnEntry(nameof(ActiveOnEntry))]
            [OnEventDoAction(typeof(TickEvent), nameof(Tick))]
            [OnEventGotoState(typeof(CancelTimer), typeof(Inactive))]
            [IgnoreEvents(typeof(StartTimerEvent))]
            private class Active : MachineState
            {
            }

            private void ActiveOnEntry()
            {
                this.Send(this.Id, new TickEvent());
            }

            private void Tick()
            {
                if (this.Random())
                {
                    this.Send(this.Target, new Timeout());
                }

                this.Send(this.Id, new TickEvent());
            }

            [OnEventGotoState(typeof(StartTimerEvent), typeof(Active))]
            [IgnoreEvents(typeof(CancelTimer), typeof(TickEvent))]
            private class Inactive : MachineState
            {
            }
        }

        private class Client : Machine
        {
            public class ConfigureEvent : Event
            {
                public MachineId NodeManager;

                public ConfigureEvent(MachineId manager)
                    : base()
                {
                    this.NodeManager = manager;
                }
            }

            internal class Request : Event
            {
                public MachineId Client;
                public int Command;

                public Request(MachineId client, int cmd)
                    : base()
                {
                    this.Client = client;
                    this.Command = cmd;
                }
            }

            private class LocalEvent : Event
            {
            }

            private MachineId NodeManager;

            private int Counter;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(ConfigureEvent), nameof(Configure))]
            [OnEventGotoState(typeof(LocalEvent), typeof(PumpRequest))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Counter = 0;
            }

            private void Configure()
            {
                this.NodeManager = (this.ReceivedEvent as ConfigureEvent).NodeManager;
                this.Raise(new LocalEvent());
            }

            [OnEntry(nameof(PumpRequestOnEntry))]
            [OnEventGotoState(typeof(LocalEvent), typeof(PumpRequest))]
            private class PumpRequest : MachineState
            {
            }

            private void PumpRequestOnEntry()
            {
                int command = this.RandomInteger(100) + 1;
                this.Counter++;

                this.Send(this.NodeManager, new Request(this.Id, command));

                if (this.Counter == 1)
                {
                    this.Raise(new Halt());
                }
                else
                {
                    this.Raise(new LocalEvent());
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
            [OnEventDoAction(typeof(ConfigureEvent), nameof(Configure))]
            [OnEventGotoState(typeof(LocalEvent), typeof(Repaired))]
            private class Init : MonitorState
            {
            }

            private void InitOnEntry()
            {
                this.DataMap = new Dictionary<int, int>();
            }

            private void Configure()
            {
                this.NumberOfReplicas = (this.ReceivedEvent as ConfigureEvent).NumberOfReplicas;
                this.Raise(new LocalEvent());
            }

            [Cold]
            [OnEventDoAction(typeof(NotifyNodeCreated), nameof(ProcessNodeCreated))]
            [OnEventDoAction(typeof(NotifyNodeFail), nameof(FailAndCheckRepair))]
            [OnEventDoAction(typeof(NotifyNodeUpdate), nameof(ProcessNodeUpdate))]
            [OnEventGotoState(typeof(LocalEvent), typeof(Repairing))]
            private class Repaired : MonitorState
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
                this.Raise(new LocalEvent());
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
            private class Repairing : MonitorState
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
                    this.Raise(new LocalEvent());
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
                r.CreateMachine(typeof(Environment));
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
                r.CreateMachine(typeof(Environment));
            },
            configuration: configuration,
            expectedError: "Monitor 'LivenessMonitor' detected infinite execution that violates a liveness property.",
            replay: true);
        }
    }
}
