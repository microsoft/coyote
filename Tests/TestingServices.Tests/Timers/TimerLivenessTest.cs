// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using Microsoft.Coyote.Timers;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests
{
    public class TimerLivenessTest : BaseTest
    {
        public TimerLivenessTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class TimeoutReceivedEvent : Event
        {
        }

        private class Client : Machine
        {
            [Start]
            [OnEntry(nameof(Initialize))]
            [OnEventDoAction(typeof(TimerElapsedEvent), nameof(HandleTimeout))]
            private class Init : MachineState
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
            private class NoTimeoutReceived : MonitorState
            {
            }

            [Cold]
            private class TimeoutReceived : MonitorState
            {
            }
        }

        [Fact(Timeout=5000)]
        public void TestTimerLiveness()
        {
            var configuration = GetConfiguration();
            configuration.LivenessTemperatureThreshold = 150;
            configuration.MaxSchedulingSteps = 300;
            configuration.SchedulingIterations = 1000;

            this.Test(r =>
            {
                r.RegisterMonitor(typeof(LivenessMonitor));
                r.CreateMachine(typeof(Client));
            },
            configuration: configuration);
        }
    }
}
