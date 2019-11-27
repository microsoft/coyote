// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Actors;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests.Actors
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

        private class A1 : Actor
        {
            protected override Task OnInitializeAsync(Event initialEvent)
            {
                this.SendEvent(this.Id, HaltEvent.Instance);
                return Task.CompletedTask;
            }

            protected override Task OnHaltAsync()
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

            protected override Task OnHaltAsync()
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

            private void InitOnEntry()
            {
                this.RaiseEvent(HaltEvent.Instance);
            }

            protected override Task OnHaltAsync()
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

            protected override async Task OnHaltAsync()
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
            expectedError: "'A3()' invoked ReceiveEventAsync while halted.",
            replay: true);
        }

        private class M3 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.RaiseEvent(HaltEvent.Instance);
            }

            protected override async Task OnHaltAsync()
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
            expectedError: "'M3()' invoked ReceiveEventAsync while halted.",
            replay: true);
        }

        private class M4 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.RaiseEvent(HaltEvent.Instance);
            }

            protected override Task OnHaltAsync()
            {
                this.RaiseEvent(new E());
                return Task.CompletedTask;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestOnHaltAsyncWithRaiseInStateMachine()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M4));
            },
            expectedError: "'M4()' invoked RaiseEvent while halted.",
            replay: true);
        }

        private class M5 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.RaiseEvent(HaltEvent.Instance);
            }

            protected override Task OnHaltAsync()
            {
                this.GotoState<Init>();
                return Task.CompletedTask;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestOnHaltAsyncWithGotoInStateMachine()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M5));
            },
            expectedError: "'M5()' invoked Goto while halted.",
            replay: true);
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
                this.RaiseEvent(HaltEvent.Instance);
            }
        }

        private class M6Receiver : StateMachine
        {
            [Start]
            [IgnoreEvents(typeof(E))]
            private class Init : State
            {
            }
        }

        private class A6Sender : Actor
        {
            private ActorId Receiver;

            protected override Task OnInitializeAsync(Event initialEvent)
            {
                this.Receiver = (initialEvent as E).Id;
                this.SendEvent(this.Id, HaltEvent.Instance);
                return Task.CompletedTask;
            }

            protected override Task OnHaltAsync()
            {
                // No-ops but no failure.
                this.SendEvent(this.Receiver, new E());
                this.Random();
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
                var m = r.CreateActor(typeof(M6Receiver));
                r.CreateActor(typeof(A6Sender), new E(m));
            });
        }

        private class M6Sender : StateMachine
        {
            private ActorId Receiver;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.Receiver = (this.ReceivedEvent as E).Id;
                this.RaiseEvent(HaltEvent.Instance);
            }

            protected override Task OnHaltAsync()
            {
                // No-ops but no failure.
                this.SendEvent(this.Receiver, new E());
                this.Random();
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
                var m = r.CreateActor(typeof(M6Receiver));
                r.CreateActor(typeof(M6Sender), new E(m));
            });
        }
    }
}
