﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Runtime.Exploration;
using Microsoft.Coyote.Specifications;
using Microsoft.Coyote.Tests.Common.Actors;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests.Specifications
{
    public class Liveness2BugFoundTests : BaseTest
    {
        public Liveness2BugFoundTests(ITestOutputHelper output)
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

            private Transition InitOnEntry() => this.RaiseEvent(UnitEvent.Instance);

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
        public void TestLiveness2BugFound()
        {
            var configuration = GetConfiguration();
            configuration.SchedulingStrategy = SchedulingStrategy.DFS;

            this.TestWithError(r =>
            {
                r.RegisterMonitor(typeof(WatchDog));
                r.CreateActor(typeof(EventHandler));
            },
            configuration: configuration,
            expectedError: "WatchDog detected liveness bug in hot state 'CannotGetUserInput' at the end of program execution.",
            replay: true);
        }
    }
}
