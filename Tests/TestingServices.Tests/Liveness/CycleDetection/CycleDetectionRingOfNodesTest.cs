// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests
{
    public class CycleDetectionRingOfNodesTest : BaseTest
    {
        public CycleDetectionRingOfNodesTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class Configure : Event
        {
            public bool ApplyFix;

            public Configure(bool applyFix)
            {
                this.ApplyFix = applyFix;
            }
        }

        private class Message : Event
        {
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
                var applyFix = (this.ReceivedEvent as Configure).ApplyFix;
                var machine1 = this.CreateMachine(typeof(Node), new Configure(applyFix));
                var machine2 = this.CreateMachine(typeof(Node), new Configure(applyFix));
                this.Send(machine1, new Node.SetNeighbour(machine2));
                this.Send(machine2, new Node.SetNeighbour(machine1));
            }
        }

        private class Node : Machine
        {
            public class SetNeighbour : Event
            {
                public MachineId Next;

                public SetNeighbour(MachineId next)
                {
                    this.Next = next;
                }
            }

            private MachineId Next;
            private bool ApplyFix;

            [Start]
            [OnEntry(nameof(OnInitEntry))]
            [OnEventDoAction(typeof(SetNeighbour), nameof(OnSetNeighbour))]
            [OnEventDoAction(typeof(Message), nameof(OnMessage))]
            private class Init : MachineState
            {
            }

            private void OnInitEntry()
            {
                this.ApplyFix = (this.ReceivedEvent as Configure).ApplyFix;
            }

            private void OnSetNeighbour()
            {
                var e = this.ReceivedEvent as SetNeighbour;
                this.Next = e.Next;
                this.Send(this.Id, new Message());
            }

            private void OnMessage()
            {
                if (this.Next != null)
                {
                    this.Send(this.Next, new Message());
                    if (this.ApplyFix)
                    {
                        this.Monitor<WatchDog>(new WatchDog.NotifyMessage());
                    }
                }
            }
        }

        private class WatchDog : Monitor
        {
            public class NotifyMessage : Event
            {
            }

            [Start]
            [Hot]
            [OnEventGotoState(typeof(NotifyMessage), typeof(ColdState))]
            private class HotState : MonitorState
            {
            }

            [Cold]
            [OnEventGotoState(typeof(NotifyMessage), typeof(HotState))]
            private class ColdState : MonitorState
            {
            }
        }

        [Fact(Timeout=5000)]
        public void TestCycleDetectionRingOfNodesNoBug()
        {
            var configuration = GetConfiguration();
            configuration.EnableCycleDetection = true;
            configuration.SchedulingIterations = 10;
            configuration.MaxSchedulingSteps = 200;

            this.Test(r =>
            {
                r.RegisterMonitor(typeof(WatchDog));
                r.CreateMachine(typeof(Environment), new Configure(true));
            },
            configuration: configuration);
        }

        [Fact(Timeout=5000)]
        public void TestCycleDetectionRingOfNodesBug()
        {
            var configuration = GetConfiguration();
            configuration.EnableCycleDetection = true;
            configuration.MaxSchedulingSteps = 200;

            this.TestWithError(r =>
            {
                r.RegisterMonitor(typeof(WatchDog));
                r.CreateMachine(typeof(Environment), new Configure(false));
            },
            configuration: configuration,
            expectedError: "Monitor 'WatchDog' detected infinite execution that violates a liveness property.",
            replay: true);
        }
    }
}
