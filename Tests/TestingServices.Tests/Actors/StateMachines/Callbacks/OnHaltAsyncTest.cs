// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Actors;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests.Actors.StateMachines
{
    public class OnHaltAsyncTest : BaseTest
    {
        public OnHaltAsyncTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class E : Event
        {
            public ActorId Id;

            public E()
            {
            }

            public E(ActorId id)
            {
                this.Id = id;
            }
        }

        private class M1 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.RaiseEvent(new HaltEvent());
            }

            protected override Task OnHaltAsync()
            {
                this.Assert(false);
                return Task.CompletedTask;
            }
        }

        private class M2a : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.RaiseEvent(new HaltEvent());
            }

            protected override async Task OnHaltAsync()
            {
                await this.ReceiveEventAsync(typeof(Event));
            }
        }

        private class M2b : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.RaiseEvent(new HaltEvent());
            }

            protected override Task OnHaltAsync()
            {
                this.RaiseEvent(new E());
                return Task.CompletedTask;
            }
        }

        private class M2c : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.RaiseEvent(new HaltEvent());
            }

            protected override Task OnHaltAsync()
            {
                this.Goto<Init>();
                return Task.CompletedTask;
            }
        }

        private class Dummy : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.RaiseEvent(new HaltEvent());
            }
        }

        private class M3 : StateMachine
        {
            private ActorId sender;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.sender = (this.ReceivedEvent as E).Id;
                this.RaiseEvent(new HaltEvent());
            }

            protected override Task OnHaltAsync()
            {
                // no-ops but no failure
                this.SendEvent(this.sender, new E());
                this.Random();
                this.Assert(true);
                this.CreateActor(typeof(Dummy));
                return Task.CompletedTask;
            }
        }

        private class M4 : StateMachine
        {
            [Start]
            [IgnoreEvents(typeof(E))]
            private class Init : State
            {
            }
        }

        [Fact(Timeout=5000)]
        public void TestHaltCalled()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M1));
            },
            expectedError: "Detected an assertion failure.",
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestReceiveOnHalt()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M2a));
            },
            expectedError: "'M2a()' invoked ReceiveEventAsync while halted.",
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestRaiseOnHalt()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M2b));
            },
            expectedError: "'M2b()' invoked RaiseEvent while halted.",
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestGotoOnHalt()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M2c));
            },
            expectedError: "'M2c()' invoked Goto while halted.",
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestAPIsOnHalt()
        {
            this.Test(r =>
            {
                var m = r.CreateActor(typeof(M4));
                r.CreateActor(typeof(M3), new E(m));
            });
        }
    }
}
