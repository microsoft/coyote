// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Specifications;
using Microsoft.Coyote.Testing;
using Microsoft.Coyote.Tests.Common.Events;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Actors.BugFinding.Tests.Specifications
{
    public class Liveness2BugFoundTests : BaseActorBugFindingTest
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

        private class EventHandler : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventGotoState(typeof(UnitEvent), typeof(WaitForUser))]
            private class Init : State
            {
            }

            private void InitOnEntry() => this.RaiseEvent(UnitEvent.Instance);

            [OnEntry(nameof(WaitForUserOnEntry))]
            [OnEventGotoState(typeof(UserEvent), typeof(HandleEvent))]
            private class WaitForUser : State
            {
            }

            private void WaitForUserOnEntry()
            {
                this.Monitor<WatchDog>(new WatchDog.Waiting());
                this.SendEvent(this.Id, new UserEvent());
            }

            [OnEntry(nameof(HandleEventOnEntry))]
            [OnEventGotoState(typeof(Done), typeof(HandleEvent))]
            private class HandleEvent : State
            {
            }

            private void HandleEventOnEntry()
            {
                this.Monitor<WatchDog>(new WatchDog.Computing());
            }
        }

        private class WatchDog : Monitor
        {
            internal class Waiting : Event
            {
            }

            internal class Computing : Event
            {
            }

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
            var configuration = this.GetConfiguration().WithDFSStrategy();
            this.TestWithError(r =>
            {
                r.RegisterMonitor<WatchDog>();
                r.CreateActor(typeof(EventHandler));
            },
            configuration: configuration,
            expectedError: "WatchDog detected liveness bug in hot state 'CannotGetUserInput' at the end of program execution.",
            replay: true);
        }
    }
}
