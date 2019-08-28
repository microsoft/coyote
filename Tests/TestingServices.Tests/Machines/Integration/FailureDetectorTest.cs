// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests
{
    /// <summary>
    /// This test implements a failure detection protocol. A failure detector
    /// machine is given a list of machines, each of which represents a daemon
    /// running at a computing node in a distributed system. The failure detector
    /// sends each machine in the list a 'Ping' event and determines whether the
    /// machine has failed if it does not respond with a 'Pong' event within a
    /// certain time period.
    /// </summary>
    public class FailureDetectorTest : BaseTest
    {
        public FailureDetectorTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class Driver : Machine
        {
            internal class Config : Event
            {
                public int NumOfNodes;

                public Config(int numOfNodes)
                {
                    this.NumOfNodes = numOfNodes;
                }
            }

            internal class RegisterClient : Event
            {
                public MachineId Client;

                public RegisterClient(MachineId client)
                {
                    this.Client = client;
                }
            }

            internal class UnregisterClient : Event
            {
                public MachineId Client;

                public UnregisterClient(MachineId client)
                {
                    this.Client = client;
                }
            }

            private MachineId FailureDetector;
            private HashSet<MachineId> Nodes;
            private int NumOfNodes;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.NumOfNodes = (this.ReceivedEvent as Config).NumOfNodes;

                this.Nodes = new HashSet<MachineId>();
                for (int i = 0; i < this.NumOfNodes; i++)
                {
                    var node = this.CreateMachine(typeof(Node));
                    this.Nodes.Add(node);
                }

                this.Monitor<LivenessMonitor>(new LivenessMonitor.RegisterNodes(this.Nodes));

                this.FailureDetector = this.CreateMachine(typeof(FailureDetector), new FailureDetector.Config(this.Nodes));
                this.Send(this.FailureDetector, new RegisterClient(this.Id));

                this.Goto<InjectFailures>();
            }

            [OnEntry(nameof(InjectFailuresOnEntry))]
            [OnEventDoAction(typeof(FailureDetector.NodeFailed), nameof(NodeFailedAction))]
            private class InjectFailures : MachineState
            {
            }

            private void InjectFailuresOnEntry()
            {
                foreach (var node in this.Nodes)
                {
                    this.Send(node, new Halt());
                }
            }

            private void NodeFailedAction()
            {
                this.Monitor<LivenessMonitor>(this.ReceivedEvent);
            }
        }

        private class FailureDetector : Machine
        {
            internal class Config : Event
            {
                public HashSet<MachineId> Nodes;

                public Config(HashSet<MachineId> nodes)
                {
                    this.Nodes = nodes;
                }
            }

            internal class NodeFailed : Event
            {
                public MachineId Node;

                public NodeFailed(MachineId node)
                {
                    this.Node = node;
                }
            }

            private class TimerCancelled : Event
            {
            }

            private class RoundDone : Event
            {
            }

            private class Unit : Event
            {
            }

            private HashSet<MachineId> Nodes;
            private HashSet<MachineId> Clients;
            private int Attempts;
            private HashSet<MachineId> Alive;
            private HashSet<MachineId> Responses;
            private MachineId Timer;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(Driver.RegisterClient), nameof(RegisterClientAction))]
            [OnEventDoAction(typeof(Driver.UnregisterClient), nameof(UnregisterClientAction))]
            [OnEventPushState(typeof(Unit), typeof(SendPing))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                var nodes = (this.ReceivedEvent as Config).Nodes;

                this.Nodes = new HashSet<MachineId>(nodes);
                this.Clients = new HashSet<MachineId>();
                this.Alive = new HashSet<MachineId>();
                this.Responses = new HashSet<MachineId>();

                foreach (var node in this.Nodes)
                {
                    this.Alive.Add(node);
                }

                this.Timer = this.CreateMachine(typeof(Timer), new Timer.Config(this.Id));
                this.Raise(new Unit());
            }

            private void RegisterClientAction()
            {
                var client = (this.ReceivedEvent as Driver.RegisterClient).Client;
                this.Clients.Add(client);
            }

            private void UnregisterClientAction()
            {
                var client = (this.ReceivedEvent as Driver.UnregisterClient).Client;
                if (this.Clients.Contains(client))
                {
                    this.Clients.Remove(client);
                }
            }

            [OnEntry(nameof(SendPingOnEntry))]
            [OnEventGotoState(typeof(RoundDone), typeof(Reset))]
            [OnEventPushState(typeof(TimerCancelled), typeof(WaitForCancelResponse))]
            [OnEventDoAction(typeof(Node.Pong), nameof(PongAction))]
            [OnEventDoAction(typeof(Timer.Timeout), nameof(TimeoutAction))]
            private class SendPing : MachineState
            {
            }

            private void SendPingOnEntry()
            {
                foreach (var node in this.Nodes)
                {
                    if (this.Alive.Contains(node) && !this.Responses.Contains(node))
                    {
                        this.Monitor<Safety>(new Safety.Ping(node));
                        this.Send(node, new Node.Ping(this.Id));
                    }
                }

                this.Send(this.Timer, new Timer.StartTimerEvent(100));
            }

            private void PongAction()
            {
                var node = (this.ReceivedEvent as Node.Pong).Node;
                if (this.Alive.Contains(node))
                {
                    this.Responses.Add(node);

                    if (this.Responses.Count == this.Alive.Count)
                    {
                        this.Send(this.Timer, new Timer.CancelTimer());
                        this.Raise(new TimerCancelled());
                    }
                }
            }

            private void TimeoutAction()
            {
                this.Attempts++;

                if (this.Responses.Count < this.Alive.Count && this.Attempts < 2)
                {
                    this.Goto<SendPing>();
                }
                else
                {
                    foreach (var node in this.Nodes)
                    {
                        if (this.Alive.Contains(node) && !this.Responses.Contains(node))
                        {
                            this.Alive.Remove(node);

                            foreach (var client in this.Clients)
                            {
                                this.Send(client, new NodeFailed(node));
                            }
                        }
                    }

                    this.Raise(new RoundDone());
                }
            }

            [OnEventDoAction(typeof(Timer.CancelSuccess), nameof(CancelSuccessAction))]
            [OnEventDoAction(typeof(Timer.CancelFailure), nameof(CancelFailure))]
            [DeferEvents(typeof(Timer.Timeout), typeof(Node.Pong))]
            private class WaitForCancelResponse : MachineState
            {
            }

            private void CancelSuccessAction()
            {
                this.Raise(new RoundDone());
            }

            private void CancelFailure()
            {
                this.Pop();
            }

            [OnEntry(nameof(ResetOnEntry))]
            [OnEventGotoState(typeof(Timer.Timeout), typeof(SendPing))]
            [IgnoreEvents(typeof(Node.Pong))]
            private class Reset : MachineState
            {
            }

            private void ResetOnEntry()
            {
                this.Attempts = 0;
                this.Responses.Clear();

                this.Send(this.Timer, new Timer.StartTimerEvent(1000));
            }
        }

        private class Node : Machine
        {
            internal class Ping : Event
            {
                public MachineId Client;

                public Ping(MachineId client)
                {
                    this.Client = client;
                }
            }

            internal class Pong : Event
            {
                public MachineId Node;

                public Pong(MachineId node)
                {
                    this.Node = node;
                }
            }

            [Start]
            [OnEventDoAction(typeof(Ping), nameof(SendPong))]
            private class WaitPing : MachineState
            {
            }

            private void SendPong()
            {
                var client = (this.ReceivedEvent as Ping).Client;
                this.Monitor<Safety>(new Safety.Pong(this.Id));
                this.Send(client, new Pong(this.Id));
            }
        }

        private class Timer : Machine
        {
            internal class Config : Event
            {
                public MachineId Target;

                public Config(MachineId target)
                {
                    this.Target = target;
                }
            }

            internal class StartTimerEvent : Event
            {
                public int Timeout;

                public StartTimerEvent(int timeout)
                {
                    this.Timeout = timeout;
                }
            }

            internal class Timeout : Event
            {
            }

            internal class CancelSuccess : Event
            {
            }

            internal class CancelFailure : Event
            {
            }

            internal class CancelTimer : Event
            {
            }

            private MachineId Target;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Target = (this.ReceivedEvent as Config).Target;
                this.Goto<WaitForReq>();
            }

            [OnEventGotoState(typeof(CancelTimer), typeof(WaitForReq), nameof(CancelTimerAction))]
            [OnEventGotoState(typeof(StartTimerEvent), typeof(WaitForCancel))]
            private class WaitForReq : MachineState
            {
            }

            private void CancelTimerAction()
            {
                this.Send(this.Target, new CancelFailure());
            }

            [IgnoreEvents(typeof(StartTimerEvent))]
            [OnEventGotoState(typeof(CancelTimer), typeof(WaitForReq), nameof(CancelTimerAction2))]
            [OnEventGotoState(typeof(Default), typeof(WaitForReq), nameof(DefaultAction))]
            private class WaitForCancel : MachineState
            {
            }

            private void DefaultAction()
            {
                this.Send(this.Target, new Timeout());
            }

            private void CancelTimerAction2()
            {
                if (this.Random())
                {
                    this.Send(this.Target, new CancelSuccess());
                }
                else
                {
                    this.Send(this.Target, new CancelFailure());
                    this.Send(this.Target, new Timeout());
                }
            }
        }

        private class Safety : Monitor
        {
            internal class Ping : Event
            {
                public MachineId Client;

                public Ping(MachineId client)
                {
                    this.Client = client;
                }
            }

            internal class Pong : Event
            {
                public MachineId Node;

                public Pong(MachineId node)
                {
                    this.Node = node;
                }
            }

            private Dictionary<MachineId, int> Pending;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(Ping), nameof(PingAction))]
            [OnEventDoAction(typeof(Pong), nameof(PongAction))]
            private class Init : MonitorState
            {
            }

            private void InitOnEntry()
            {
                this.Pending = new Dictionary<MachineId, int>();
            }

            private void PingAction()
            {
                var client = (this.ReceivedEvent as Ping).Client;
                if (!this.Pending.ContainsKey(client))
                {
                    this.Pending[client] = 0;
                }

                this.Pending[client] = this.Pending[client] + 1;
                this.Assert(this.Pending[client] <= 3, $"'{client}' ping count must be <= 3.");
            }

            private void PongAction()
            {
                var node = (this.ReceivedEvent as Pong).Node;
                this.Assert(this.Pending.ContainsKey(node), $"'{node}' is not in pending set.");
                this.Assert(this.Pending[node] > 0, $"'{node}' ping count must be > 0.");
                this.Pending[node] = this.Pending[node] - 1;
            }
        }

        private class LivenessMonitor : Monitor
        {
            internal class RegisterNodes : Event
            {
                public HashSet<MachineId> Nodes;

                public RegisterNodes(HashSet<MachineId> nodes)
                {
                    this.Nodes = nodes;
                }
            }

            private HashSet<MachineId> Nodes;

            [Start]
            [OnEventDoAction(typeof(RegisterNodes), nameof(RegisterNodesAction))]
            private class Init : MonitorState
            {
            }

            private void RegisterNodesAction()
            {
                var nodes = (this.ReceivedEvent as RegisterNodes).Nodes;
                this.Nodes = new HashSet<MachineId>(nodes);
                this.Goto<Wait>();
            }

            [Hot]
            [OnEventDoAction(typeof(FailureDetector.NodeFailed), nameof(NodeDownAction))]
            private class Wait : MonitorState
            {
            }

            private void NodeDownAction()
            {
                var node = (this.ReceivedEvent as FailureDetector.NodeFailed).Node;
                this.Nodes.Remove(node);
                if (this.Nodes.Count == 0)
                {
                    this.Goto<Done>();
                }
            }

            private class Done : MonitorState
            {
            }
        }

        [Theory(Timeout = 5000)]
        // [ClassData(typeof(SeedGenerator))]
        [InlineData(100813)]
        public void TestFailureDetectorSafetyBug(int seed)
        {
            var configuration = GetConfiguration();
            configuration.MaxUnfairSchedulingSteps = 200;
            configuration.MaxFairSchedulingSteps = 2000;
            configuration.LivenessTemperatureThreshold = 1000;
            configuration.RandomSchedulingSeed = seed;
            configuration.SchedulingIterations = 1;
            configuration.ReductionStrategy = Utilities.ReductionStrategy.ForceSchedule;

            this.TestWithError(r =>
            {
                r.RegisterMonitor(typeof(Safety));
                r.CreateMachine(typeof(Driver), new Driver.Config(2));
            },
            configuration: configuration,
            expectedError: "'Node()' ping count must be <= 3.",
            replay: true);
        }

        [Theory(Timeout = 5000)]
        // [ClassData(typeof(SeedGenerator))]
        [InlineData(4986)]
        public void TestFailureDetectorLivenessBug(int seed)
        {
            var configuration = GetConfiguration();
            configuration.MaxUnfairSchedulingSteps = 200;
            configuration.MaxFairSchedulingSteps = 2000;
            configuration.LivenessTemperatureThreshold = 1000;
            configuration.RandomSchedulingSeed = seed;
            configuration.SchedulingIterations = 1;
            configuration.ReductionStrategy = Utilities.ReductionStrategy.ForceSchedule;

            this.TestWithError(r =>
            {
                r.RegisterMonitor(typeof(LivenessMonitor));
                r.CreateMachine(typeof(Driver), new Driver.Config(2));
            },
            configuration: configuration,
            expectedError: "Monitor 'LivenessMonitor' detected potential liveness bug in hot state 'Wait'.",
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestFailureDetectorLivenessBugWithCycleReplay()
        {
            var configuration = GetConfiguration();
            configuration.EnableCycleDetection = true;
            configuration.SchedulingStrategy = Utilities.SchedulingStrategy.FairPCT;
            configuration.PrioritySwitchBound = 1;
            configuration.MaxSchedulingSteps = 100;
            configuration.RandomSchedulingSeed = 270;
            configuration.SchedulingIterations = 1;

            this.TestWithError(r =>
            {
                r.RegisterMonitor(typeof(LivenessMonitor));
                r.CreateMachine(typeof(Driver), new Driver.Config(2));
            },
            configuration: configuration,
            expectedError: "Monitor 'LivenessMonitor' detected infinite execution that violates a liveness property.",
            replay: true);
        }
    }
}
