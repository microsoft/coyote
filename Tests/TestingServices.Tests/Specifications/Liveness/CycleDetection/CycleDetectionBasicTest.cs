// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Specifications;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests
{
    public class CycleDetectionBasicTest : BaseTest
    {
        public CycleDetectionBasicTest(ITestOutputHelper output)
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

        private class EventHandler : StateMachine
        {
            private bool ApplyFix;

            [Start]
            [OnEntry(nameof(OnInitEntry))]
            [OnEventDoAction(typeof(Message), nameof(OnMessage))]
            private class Init : State
            {
            }

            private void OnInitEntry()
            {
                this.ApplyFix = (this.ReceivedEvent as Configure).ApplyFix;
                this.SendEvent(this.Id, new Message());
            }

            private void OnMessage()
            {
                this.SendEvent(this.Id, new Message());
                if (this.ApplyFix)
                {
                    this.Monitor<WatchDog>(new WatchDog.NotifyMessage());
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
        public void TestCycleDetectionBasicNoBug()
        {
            var configuration = GetConfiguration();
            configuration.EnableCycleDetection = true;
            configuration.SchedulingIterations = 10;
            configuration.MaxSchedulingSteps = 200;

            this.Test(r =>
            {
                r.RegisterMonitor(typeof(WatchDog));
                r.CreateStateMachine(typeof(EventHandler), new Configure(true));
            },
            configuration: configuration);
        }

        [Fact(Timeout=5000)]
        public void TestCycleDetectionBasicBug()
        {
            var configuration = GetConfiguration();
            configuration.EnableCycleDetection = true;
            configuration.MaxSchedulingSteps = 200;

            this.TestWithError(r =>
            {
                r.RegisterMonitor(typeof(WatchDog));
                r.CreateStateMachine(typeof(EventHandler), new Configure(false));
            },
            configuration: configuration,
            expectedError: "Monitor 'WatchDog' detected infinite execution that violates a liveness property.",
            replay: true);
        }
    }
}
