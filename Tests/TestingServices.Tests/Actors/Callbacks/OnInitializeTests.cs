// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Tests.Common.Actors;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests.Actors
{
    public class OnInitializeTests : BaseTest
    {
        public OnInitializeTests(ITestOutputHelper output)
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

        [OnEventDoAction(typeof(UnitEvent), nameof(Process))]
        private class A1 : Actor
        {
            private int Value = 0;

            protected override Task OnInitializeAsync(Event initialEvent)
            {
                this.Value = 1;
                return Task.CompletedTask;
            }

            private void Process()
            {
                this.Assert(this.Value == 0, $"Value is {this.Value}.");
            }
        }

        [Fact(Timeout = 5000)]
        public void TestOnInitializeAsyncInActor()
        {
            this.TestWithError(r =>
            {
                var id = r.CreateActor(typeof(A1));
                r.SendEvent(id, UnitEvent.Instance);
            },
            configuration: GetConfiguration(),
            expectedError: "Value is 1.",
            replay: true);
        }

        private class M1 : StateMachine
        {
            private int Value = 0;

            [Start]
            [OnEventDoAction(typeof(UnitEvent), nameof(Process))]
            private class S : State
            {
            }

            protected override Task OnInitializeAsync(Event initialEvent)
            {
                this.Value = 1;
                return Task.CompletedTask;
            }

            private void Process()
            {
                this.Assert(this.Value == 0, $"Value is {this.Value}.");
            }
        }

        [Fact(Timeout = 5000)]
        public void TestOnInitializeAsyncInStateMachine()
        {
            this.TestWithError(r =>
            {
                var id = r.CreateActor(typeof(M1));
                r.SendEvent(id, UnitEvent.Instance);
            },
            configuration: GetConfiguration(),
            expectedError: "Value is 1.",
            replay: true);
        }

        [OnEventDoAction(typeof(UnitEvent), nameof(Process))]
        private class A2 : Actor
        {
            private int Value = 0;

            protected override Task OnInitializeAsync(Event e)
            {
                this.Value = (e as SetupEvent).Value;
                this.SendEvent(this.Id, UnitEvent.Instance);
                return Task.CompletedTask;
            }

            private void Process()
            {
                this.Assert(this.Value == 0, $"Value is {this.Value}.");
            }
        }

        [Fact(Timeout = 5000)]
        public void TestOnInitializeAsyncWithEventInActor()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(A2), new SetupEvent(1));
            },
            configuration: GetConfiguration(),
            expectedError: "Value is 1.",
            replay: true);
        }

        private class M2 : StateMachine
        {
            private int Value = 0;

            [Start]
            [OnEventDoAction(typeof(UnitEvent), nameof(Process))]
            private class S : State
            {
            }

            protected override Task OnInitializeAsync(Event e)
            {
                this.Value = (e as SetupEvent).Value;
                this.SendEvent(this.Id, UnitEvent.Instance);
                return Task.CompletedTask;
            }

            private void Process()
            {
                this.Assert(this.Value == 0, $"Value is {this.Value}.");
            }
        }

        [Fact(Timeout = 5000)]
        public void TestOnInitializeAsyncWithEventInStateMachine()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M2), new SetupEvent(1));
            },
            configuration: GetConfiguration(),
            expectedError: "Value is 1.",
            replay: true);
        }

        private class M3 : StateMachine
        {
            private int Value = 0;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(UnitEvent), nameof(Process))]
            private class S : State
            {
            }

            protected override Task OnInitializeAsync(Event e)
            {
                this.Value = (e as SetupEvent).Value;
                this.SendEvent(this.Id, UnitEvent.Instance);
                return Task.CompletedTask;
            }

            private void InitOnEntry(Event e)
            {
                this.Assert(e is SetupEvent);
                this.Value += (e as SetupEvent).Value;
            }

            private void Process()
            {
                this.Assert(this.Value == 0 || this.Value == 1, $"Value is {this.Value}.");
            }
        }

        [Fact(Timeout = 5000)]
        public void TestOnInitializeAsyncWithOnEntryInStateMachine()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M3), new SetupEvent(1));
            },
            configuration: GetConfiguration(),
            expectedError: "Value is 2.",
            replay: true);
        }
    }
}
