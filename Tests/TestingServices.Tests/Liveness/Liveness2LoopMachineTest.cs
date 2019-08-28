// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests
{
    public class Liveness2LoopMachineTest : BaseTest
    {
        public Liveness2LoopMachineTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class Unit : Event
        {
        }

        private class UserEvent : Event
        {
        }

        private class Done : Event
        {
        }

        private class Waiting : Event
        {
        }

        private class Computing : Event
        {
        }

        private class EventHandler : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventGotoState(typeof(Unit), typeof(WaitForUser))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.CreateMachine(typeof(Loop));
                this.Raise(new Unit());
            }

            [OnEntry(nameof(WaitForUserOnEntry))]
            [OnEventGotoState(typeof(UserEvent), typeof(HandleEvent))]
            private class WaitForUser : MachineState
            {
            }

            private void WaitForUserOnEntry()
            {
                this.Monitor<LivenessMonitor>(new Waiting());
                this.Send(this.Id, new UserEvent());
            }

            [OnEntry(nameof(HandleEventOnEntry))]
            private class HandleEvent : MachineState
            {
            }

            private void HandleEventOnEntry()
            {
                this.Monitor<LivenessMonitor>(new Computing());
            }
        }

        private class Loop : Machine
        {
            [Start]
            [OnEntry(nameof(LoopingOnEntry))]
            [OnEventGotoState(typeof(Done), typeof(Looping))]
            private class Looping : MachineState
            {
            }

            private void LoopingOnEntry()
            {
                this.Send(this.Id, new Done());
            }
        }

        private class LivenessMonitor : Monitor
        {
            [Start]
            [Cold]
            [OnEventGotoState(typeof(Waiting), typeof(CanGetUserInput))]
            [OnEventGotoState(typeof(Computing), typeof(CannotGetUserInput))]
            private class CanGetUserInput : MonitorState
            {
            }

            [Hot]
            [OnEventGotoState(typeof(Waiting), typeof(CanGetUserInput))]
            [OnEventGotoState(typeof(Computing), typeof(CannotGetUserInput))]
            private class CannotGetUserInput : MonitorState
            {
            }
        }

        [Fact(Timeout=5000)]
        public void TestLiveness2LoopMachine()
        {
            var configuration = GetConfiguration();
            configuration.LivenessTemperatureThreshold = 200;
            configuration.SchedulingIterations = 1;

            this.TestWithError(r =>
            {
                r.RegisterMonitor(typeof(LivenessMonitor));
                r.CreateMachine(typeof(EventHandler));
            },
            configuration: configuration,
            expectedError: "Monitor 'LivenessMonitor' detected potential liveness bug in hot state 'CannotGetUserInput'.",
            replay: true);
        }
    }
}
