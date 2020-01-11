// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Specifications;
using Microsoft.Coyote.Tests.Common.Actors;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests.Specifications
{
    public class Liveness3Tests : BaseTest
    {
        public Liveness3Tests(ITestOutputHelper output)
            : base(output)
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

        private class EventHandler : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventGotoState(typeof(UnitEvent), typeof(WaitForUser))]
            private class Init : State
            {
            }

            private Transition InitOnEntry()
            {
                this.CreateActor(typeof(Loop));
                return this.RaiseEvent(UnitEvent.Instance);
            }

            [OnEntry(nameof(WaitForUserOnEntry))]
            [OnEventGotoState(typeof(UserEvent), typeof(HandleEvent))]
            private class WaitForUser : State
            {
            }

            private void WaitForUserOnEntry()
            {
                this.Monitor<WatchDog>(new Waiting());
                this.SendEvent(this.Id, new UserEvent());
            }

            [OnEntry(nameof(HandleEventOnEntry))]
            [OnEventGotoState(typeof(Done), typeof(HandleEvent))]
            private class HandleEvent : State
            {
            }

            private void HandleEventOnEntry()
            {
                this.Monitor<WatchDog>(new Computing());
            }
        }

        private class Loop : StateMachine
        {
            [Start]
            [OnEntry(nameof(LoopingOnEntry))]
            [OnEventGotoState(typeof(Done), typeof(Looping))]
            private class Looping : State
            {
            }

            private void LoopingOnEntry()
            {
                this.SendEvent(this.Id, new Done());
            }
        }

        private class WatchDog : Monitor
        {
            [Start]
            [Cold]
            [OnEventGotoState(typeof(Waiting), typeof(CanGetUserInput))]
            [OnEventGotoState(typeof(Computing), typeof(CannotGetUserInput))]
            private class CanGetUserInput : State
            {
            }

            [Hot]
            [OnEventGotoState(typeof(Waiting), typeof(CanGetUserInput))]
            [OnEventGotoState(typeof(Computing), typeof(CannotGetUserInput))]
            private class CannotGetUserInput : State
            {
            }
        }

        [Fact(Timeout = 5000)]
        public void TestLiveness3()
        {
            var configuration = GetConfiguration();
            configuration.EnableCycleDetection = true;
            configuration.SchedulingIterations = 100;

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
