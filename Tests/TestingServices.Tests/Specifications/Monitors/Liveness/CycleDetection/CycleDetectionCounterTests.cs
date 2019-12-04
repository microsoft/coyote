// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Specifications;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests.Specifications
{
    public class CycleDetectionCounterTests : BaseTest
    {
        public CycleDetectionCounterTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private class Message : Event
        {
        }

        private class EventHandler : StateMachine
        {
            private int Counter;

            [Start]
            [OnEntry(nameof(OnInitEntry))]
            [OnEventDoAction(typeof(Message), nameof(OnMessage))]
            private class Init : State
            {
            }

            private void OnInitEntry()
            {
                this.Counter = 0;
                this.SendEvent(this.Id, new Message());
            }

            private void OnMessage()
            {
                this.SendEvent(this.Id, new Message());
                this.Counter++;
            }

            protected override int HashedState
            {
                get
                {
                    // The counter contributes to the cached state.
                    // This allows the liveness checker to detect progress.
                    return this.Counter;
                }
            }
        }

        private class WatchDog : Monitor
        {
            [Start]
            [Hot]
            private class HotState : State
            {
            }
        }

        [Fact(Timeout=5000)]
        public void TestCycleDetectionCounterNoBug()
        {
            var configuration = GetConfiguration();
            configuration.EnableCycleDetection = true;
            configuration.EnableUserDefinedStateHashing = true;
            configuration.SchedulingIterations = 10;
            configuration.MaxSchedulingSteps = 200;

            this.Test(r =>
            {
                r.RegisterMonitor(typeof(WatchDog));
                r.CreateActor(typeof(EventHandler));
            },
            configuration: configuration);
        }

        [Fact(Timeout=5000)]
        public void TestCycleDetectionCounterBug()
        {
            var configuration = GetConfiguration();
            configuration.EnableCycleDetection = true;
            configuration.MaxSchedulingSteps = 200;

            this.TestWithError(r =>
            {
                r.RegisterMonitor(typeof(WatchDog));
                r.CreateActor(typeof(EventHandler));
            },
            configuration: configuration,
            expectedError: "Monitor 'WatchDog' detected infinite execution that violates a liveness property.",
            replay: true);
        }
    }
}
