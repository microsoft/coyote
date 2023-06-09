// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Actors.BugFinding.Tests
{
    public class OnHaltTests : BaseActorBugFindingTest
    {
        public OnHaltTests(ITestOutputHelper output)
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

        private class A1 : Actor
        {
            protected override Task OnInitializeAsync(Event initialEvent)
            {
                this.SendEvent(this.Id, HaltEvent.Instance);
                return Task.CompletedTask;
            }

            protected override Task OnHaltAsync(Event e)
            {
                this.Assert(false, "Reached test assertion.");
                return Task.CompletedTask;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestOnHaltAsyncAfterSendInActor()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(A1));
            },
            expectedError: "Reached test assertion.",
            replay: true);
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
                this.SendEvent(this.Id, HaltEvent.Instance);
            }

            protected override Task OnHaltAsync(Event e)
            {
                this.Assert(false, "Reached test assertion.");
                return Task.CompletedTask;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestOnHaltAsyncAfterSendInStateMachine()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M1));
            },
            expectedError: "Reached test assertion.",
            replay: true);
        }

        private class M2 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private void InitOnEntry() => this.RaiseHaltEvent();

            protected override Task OnHaltAsync(Event e)
            {
                this.Assert(false, "Reached test assertion.");
                return Task.CompletedTask;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestOnHaltAsyncAfterRaiseInStateMachine()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M2));
            },
            expectedError: "Reached test assertion.",
            replay: true);
        }

        private class A3 : Actor
        {
            protected override Task OnInitializeAsync(Event initialEvent)
            {
                this.SendEvent(this.Id, HaltEvent.Instance);
                return Task.CompletedTask;
            }

            protected override async Task OnHaltAsync(Event e)
            {
                await this.ReceiveEventAsync(typeof(Event));
            }
        }

        [Fact(Timeout = 5000)]
        public void TestOnHaltAsyncWithReceiveInActor()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(A3));
            },
            expectedError: "A3() invoked ReceiveEventAsync while halting.",
            replay: true);
        }

        private class M3 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private void InitOnEntry() => this.RaiseHaltEvent();

            protected override async Task OnHaltAsync(Event e)
            {
                await this.ReceiveEventAsync(typeof(Event));
            }
        }

        [Fact(Timeout = 5000)]
        public void TestOnHaltAsyncWithReceiveInStateMachine()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M3));
            },
            expectedError: "M3() invoked ReceiveEventAsync while halting.",
            replay: true);
        }

        private class Dummy : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private void InitOnEntry() => this.RaiseHaltEvent();
        }

        private class M4Receiver : StateMachine
        {
            [Start]
            [IgnoreEvents(typeof(E))]
            private class Init : State
            {
            }
        }

        private class A4Sender : Actor
        {
            private ActorId Receiver;

            protected override Task OnInitializeAsync(Event initialEvent)
            {
                this.Receiver = (initialEvent as E).Id;
                this.SendEvent(this.Id, HaltEvent.Instance);
                return Task.CompletedTask;
            }

            protected override Task OnHaltAsync(Event e)
            {
                // No-ops but no failure.
                this.SendEvent(this.Receiver, new E());
                this.RandomBoolean();
                this.Assert(true);
                this.CreateActor(typeof(Dummy));
                return Task.CompletedTask;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestOnHaltAsyncWithAPIsInActor()
        {
            this.Test(r =>
            {
                var m = r.CreateActor(typeof(M4Receiver));
                r.CreateActor(typeof(A4Sender), new E(m));
            });
        }

        private class M4Sender : StateMachine
        {
            private ActorId Receiver;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private void InitOnEntry(Event e)
            {
                this.Receiver = (e as E).Id;
                this.RaiseHaltEvent();
            }

            protected override Task OnHaltAsync(Event e)
            {
                // No-ops but no failure.
                this.SendEvent(this.Receiver, new E());
                this.RandomBoolean();
                this.Assert(true);
                this.CreateActor(typeof(Dummy));
                return Task.CompletedTask;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestOnHaltAsyncWithAPIsInStateMachine()
        {
            this.Test(r =>
            {
                var m = r.CreateActor(typeof(M4Receiver));
                r.CreateActor(typeof(M4Sender), new E(m));
            });
        }

        public class M5 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [DeferEvents(typeof(WildCardEvent))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.RaiseEvent(HaltEvent.Instance);
            }

            protected override Task OnHaltAsync(Event e)
            {
                this.Assert(false, "Reached test assertion.");
                return Task.CompletedTask;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestOnHaltAsyncWithWildCardInStateMachine()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M5));
            },
            expectedError: "Reached test assertion.",
            replay: true);
        }
    }
}
