// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Coyote.Actors.Timers;
using Microsoft.Coyote.Specifications;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Actors.BugFinding.Tests
{
    public class StartStopTimerTests : BaseActorBugFindingTest
    {
        public StartStopTimerTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private class TimeoutReceivedEvent : Event
        {
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

        [OnEventDoAction(typeof(TimerElapsedEvent), nameof(HandleTimeout))]
        private class ClientActor : Actor
        {
            protected override Task OnInitializeAsync(Event initialEvent)
            {
                // Start a timer, and then stop it immediately.
                var timer = this.StartPeriodicTimer(TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(10));
                this.StopTimer(timer);
                return Task.CompletedTask;
            }

            private void HandleTimeout()
            {
                // Timeout in the interval between starting and disposing the timer.
                this.Monitor<LivenessMonitor>(new TimeoutReceivedEvent());
            }
        }

        [Fact(Timeout = 5000)]
        public void TestStartStopTimerInActor()
        {
            this.TestWithError(r =>
            {
                r.RegisterMonitor<LivenessMonitor>();
                r.CreateActor(typeof(ClientActor));
            },
            configuration: GetConfiguration().WithMaxSchedulingSteps(300).WithTestingIterations(1000).
                WithLivenessTemperatureThreshold(150),
            expectedError: "LivenessMonitor detected liveness bug in hot state " +
                "'NoTimeoutReceived' at the end of program execution.",
            replay: true);
        }

        private class ClientStateMachine : StateMachine
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

        [Fact(Timeout = 5000)]
        public void TestStartStopTimerInStateMachine()
        {
            this.TestWithError(r =>
            {
                r.RegisterMonitor<LivenessMonitor>();
                r.CreateActor(typeof(ClientStateMachine));
            },
            configuration: GetConfiguration().WithMaxSchedulingSteps(300).WithTestingIterations(1000).
                WithLivenessTemperatureThreshold(150),
            expectedError: "LivenessMonitor detected liveness bug in hot state " +
                "'NoTimeoutReceived' at the end of program execution.",
            replay: true);
        }
    }
}
