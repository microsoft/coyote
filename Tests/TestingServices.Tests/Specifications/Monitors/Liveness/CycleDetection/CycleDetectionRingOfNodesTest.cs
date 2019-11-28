// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Specifications;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests.Specifications
{
    public class CycleDetectionRingOfNodesTest : BaseTest
    {
        public CycleDetectionRingOfNodesTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class SetupEvent : Event
        {
            public bool ApplyFix;

            public SetupEvent(bool applyFix)
            {
                this.ApplyFix = applyFix;
            }
        }

        private class Message : Event
        {
        }

        private class Environment : StateMachine
        {
            [Start]
            [OnEntry(nameof(OnInitEntry))]
            private class Init : State
            {
            }

            private void OnInitEntry(Event e)
            {
                var applyFix = (e as SetupEvent).ApplyFix;
                var machine1 = this.CreateActor(typeof(Node), new SetupEvent(applyFix));
                var machine2 = this.CreateActor(typeof(Node), new SetupEvent(applyFix));
                this.SendEvent(machine1, new Node.SetNeighbour(machine2));
                this.SendEvent(machine2, new Node.SetNeighbour(machine1));
            }
        }

        private class Node : StateMachine
        {
            public class SetNeighbour : Event
            {
                public ActorId Next;

                public SetNeighbour(ActorId next)
                {
                    this.Next = next;
                }
            }

            private ActorId Next;
            private bool ApplyFix;

            [Start]
            [OnEntry(nameof(OnInitEntry))]
            [OnEventDoAction(typeof(SetNeighbour), nameof(OnSetNeighbour))]
            [OnEventDoAction(typeof(Message), nameof(OnMessage))]
            private class Init : State
            {
            }

            private void OnInitEntry(Event e)
            {
                this.ApplyFix = (e as SetupEvent).ApplyFix;
            }

            private void OnSetNeighbour(Event e)
            {
                this.Next = (e as SetNeighbour).Next;
                this.SendEvent(this.Id, new Message());
            }

            private void OnMessage()
            {
                if (this.Next != null)
                {
                    this.SendEvent(this.Next, new Message());
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
            private class HotState : State
            {
            }

            [Cold]
            [OnEventGotoState(typeof(NotifyMessage), typeof(HotState))]
            private class ColdState : State
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
                r.CreateActor(typeof(Environment), new SetupEvent(true));
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
                r.CreateActor(typeof(Environment), new SetupEvent(false));
            },
            configuration: configuration,
            expectedError: "Monitor 'WatchDog' detected infinite execution that violates a liveness property.",
            replay: true);
        }
    }
}
