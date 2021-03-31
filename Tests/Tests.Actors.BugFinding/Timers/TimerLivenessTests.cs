// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Coyote.Actors.Timers;
using Microsoft.Coyote.Specifications;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Actors.BugFinding.Tests
{
    public class TimerLivenessTests : BaseActorBugFindingTest
    {
        public TimerLivenessTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private class TimeoutReceivedEvent : Event
        {
        }

        private class Client : StateMachine
        {
            [Start]
            [OnEntry(nameof(Initialize))]
            [OnEventDoAction(typeof(TimerElapsedEvent), nameof(HandleTimeout))]
            private class Init : State
            {
            }

            private void Initialize()
            {
                this.StartTimer(TimeSpan.FromMilliseconds(10));
            }

            private void HandleTimeout()
            {
                this.Monitor<LivenessMonitor>(new TimeoutReceivedEvent());
            }
        }

        private class LivenessMonitor : Monitor
        {
            [Start]
            [Hot]
            [OnEventGotoState(typeof(TimeoutReceivedEvent), typeof(TimeoutReceived))]
            private class NoTimeoutReceived : State
            {
            }

            [Cold]
            private class TimeoutReceived : State
            {
            }
        }

        [Fact(Timeout = 5000)]
        public void TestTimerLiveness()
        {
            this.Test(r =>
            {
                r.RegisterMonitor<LivenessMonitor>();
                r.CreateActor(typeof(Client));
            },
            configuration: this.GetConfiguration().WithMaxSchedulingSteps(300).WithTestingIterations(1000).
                WithLivenessTemperatureThreshold(150).WithTimeoutDelay(1));
        }
    }
}
