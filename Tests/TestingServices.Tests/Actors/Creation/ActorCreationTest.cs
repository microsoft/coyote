// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Tests.Common.Actors;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests.Actors
{
    public class ActorCreationTest : BaseTest
    {
        public ActorCreationTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class SetupEvent : Event
        {
            public int Value;

            public SetupEvent(int value)
            {
                this.Value = value;
            }
        }

        private class A1 : Actor
        {
        }

        [Fact(Timeout=5000)]
        public void TestActorCreation()
        {
            this.Test(r =>
            {
                var id = r.CreateActor(typeof(A1));
                r.Assert(id != null, "The actor id is null.");
            },
            configuration: GetConfiguration());
        }

        [OnEventDoAction(typeof(UnitEvent), nameof(HandleE))]
        private class A2 : Actor
        {
            private int Value = 0;

            protected override Task OnInitializeAsync(Event initialEvent)
            {
                this.Value = 1;
                return Task.CompletedTask;
            }

            private void HandleE()
            {
                this.Assert(this.Value == 0, $"Value is {this.Value}.");
            }
        }

        [Fact(Timeout = 5000)]
        public void TestActorCreationWithInitialization()
        {
            this.TestWithError(r =>
            {
                var id = r.CreateActor(typeof(A2));
                r.SendEvent(id, new UnitEvent());
            },
            configuration: GetConfiguration(),
            expectedError: "Value is 1.",
            replay: true);
        }

        [OnEventDoAction(typeof(UnitEvent), nameof(HandleE))]
        private class A3 : Actor
        {
            private int Value = 0;

            protected override Task OnInitializeAsync(Event e)
            {
                this.Value = (e as SetupEvent).Value;
                this.SendEvent(this.Id, new UnitEvent());
                return Task.CompletedTask;
            }

            private void HandleE()
            {
                this.Assert(this.Value == 0, $"Value is {this.Value}.");
            }
        }

        [Fact(Timeout = 5000)]
        public void TestActorCreationWithInitializationAndEvent()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(A3), new SetupEvent(1));
            },
            configuration: GetConfiguration(),
            expectedError: "Value is 1.",
            replay: true);
        }
    }
}
