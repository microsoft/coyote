// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Specifications;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests
{
    public class CycleDetectionRandomChoiceTest : BaseTest
    {
        public CycleDetectionRandomChoiceTest(ITestOutputHelper output)
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
                this.Monitor<WatchDog>(new WatchDog.NotifyMessage());
                if (this.Choose())
                {
                    this.Monitor<WatchDog>(new WatchDog.NotifyDone());
                    this.RaiseEvent(new Halt());
                }
            }

            private bool Choose()
            {
                if (this.ApplyFix)
                {
                    return this.FairRandom();
                }
                else
                {
                    return this.Random();
                }
            }
        }

        private class WatchDog : Monitor
        {
            public class NotifyMessage : Event
            {
            }

            public class NotifyDone : Event
            {
            }

            [Start]
            [Hot]
            [OnEventGotoState(typeof(NotifyMessage), typeof(HotState))]
            [OnEventGotoState(typeof(NotifyDone), typeof(ColdState))]
            private class HotState : State
            {
            }

            [Cold]
            private class ColdState : State
            {
            }
        }

        [Theory(Timeout = 5000)]
        [InlineData(906)]
        public void TestCycleDetectionRandomChoiceNoBug(int seed)
        {
            var configuration = GetConfiguration();
            configuration.EnableCycleDetection = true;
            configuration.RandomSchedulingSeed = seed;
            configuration.SchedulingIterations = 7;
            configuration.MaxSchedulingSteps = 200;

            this.Test(r =>
            {
                r.RegisterMonitor(typeof(WatchDog));
                r.CreateStateMachine(typeof(EventHandler), new Configure(true));
            },
            configuration: configuration);
        }

        [Theory(Timeout = 5000)]
        [InlineData(906)]
        public void TestCycleDetectionRandomChoiceBug(int seed)
        {
            var configuration = GetConfiguration();
            configuration.EnableCycleDetection = true;
            configuration.RandomSchedulingSeed = seed;
            configuration.SchedulingIterations = 10;
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
