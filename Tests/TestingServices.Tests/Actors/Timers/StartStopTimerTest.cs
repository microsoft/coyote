// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Actors.Timers;
using Microsoft.Coyote.Specifications;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests.Actors
{
    public class StartStopTimerTest : BaseTest
    {
        public StartStopTimerTest(ITestOutputHelper output)
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
                // Start a timer, and then stop it immediately.
                var timer = this.StartPeriodicTimer(TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(10));
                this.StopTimer(timer);
            }

            private void HandleTimeout()
            {
                // Timeout in the interval between starting and disposing the timer.
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

        [Fact(Timeout=5000)]
        public void TestStartStopTimer()
        {
            var configuration = GetConfiguration();
            configuration.LivenessTemperatureThreshold = 150;
            configuration.MaxSchedulingSteps = 300;
            configuration.SchedulingIterations = 1000;

            this.TestWithError(r =>
            {
                r.RegisterMonitor(typeof(LivenessMonitor));
                r.CreateActor(typeof(Client));
            },
            configuration: configuration,
            expectedError: "Monitor 'LivenessMonitor' detected liveness bug in hot state " +
                "'NoTimeoutReceived' at the end of program execution.",
            replay: true);
        }
    }
}
