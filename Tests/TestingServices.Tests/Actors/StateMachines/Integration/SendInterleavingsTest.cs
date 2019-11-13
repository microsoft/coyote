// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Runtime.Exploration;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests.Actors.StateMachines
{
    public class SendInterleavingsTest : BaseTest
    {
        public SendInterleavingsTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class SetupEvent : Event
        {
            public ActorId Id;

            public SetupEvent(ActorId id)
            {
                this.Id = id;
            }
        }

        private class Event1 : Event
        {
        }

        private class Event2 : Event
        {
        }

        private class Receiver : StateMachine
        {
            [Start]
            [OnEntry(nameof(Initialize))]
            [OnEventDoAction(typeof(Event1), nameof(OnEvent1))]
            [OnEventDoAction(typeof(Event2), nameof(OnEvent2))]
            private class Init : State
            {
            }

            private int count1 = 0;

            private void Initialize()
            {
                var s1 = this.CreateActor(typeof(Sender1));
                this.SendEvent(s1, new SetupEvent(this.Id));
                var s2 = this.CreateActor(typeof(Sender2));
                this.SendEvent(s2, new SetupEvent(this.Id));
            }

            private void OnEvent1()
            {
                this.count1++;
            }

            private void OnEvent2()
            {
                this.Assert(this.count1 != 1);
            }
        }

        private class Sender1 : StateMachine
        {
            [Start]
            [OnEventDoAction(typeof(SetupEvent), nameof(Run))]
            private class Init : State
            {
            }

            private void Run()
            {
                this.SendEvent((this.ReceivedEvent as SetupEvent).Id, new Event1());
                this.SendEvent((this.ReceivedEvent as SetupEvent).Id, new Event1());
            }
        }

        private class Sender2 : StateMachine
        {
            [Start]
            [OnEventDoAction(typeof(SetupEvent), nameof(Run))]
            private class Init : State
            {
            }

            private void Run()
            {
                this.SendEvent((this.ReceivedEvent as SetupEvent).Id, new Event2());
            }
        }

        [Fact(Timeout=5000)]
        public void TestSendInterleavingsAssertionFailure()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(Receiver));
            },
            configuration: GetConfiguration().WithStrategy(SchedulingStrategy.DFS).WithNumberOfIterations(600),
            expectedError: "Detected an assertion failure.",
            replay: true);
        }
    }
}
