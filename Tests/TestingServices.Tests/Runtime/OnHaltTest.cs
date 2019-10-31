// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Actors;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests
{
    public class OnHaltTest : BaseTest
    {
        public OnHaltTest(ITestOutputHelper output)
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
                this.RaiseEvent(new Halt());
            }

            protected override void OnHalt()
            {
                this.Assert(false);
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
                this.RaiseEvent(new Halt());
            }

            protected override void OnHalt()
            {
                this.ReceiveEventAsync(typeof(Event)).Wait();
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
                this.RaiseEvent(new Halt());
            }

            protected override void OnHalt()
            {
                this.RaiseEvent(new E());
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
                this.RaiseEvent(new Halt());
            }

            protected override void OnHalt()
            {
                this.Goto<Init>();
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
                this.RaiseEvent(new Halt());
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
                this.RaiseEvent(new Halt());
            }

            protected override void OnHalt()
            {
                // no-ops but no failure
                this.SendEvent(this.sender, new E());
                this.Random();
                this.Assert(true);
                this.CreateStateMachine(typeof(Dummy));
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
                r.CreateStateMachine(typeof(M1));
            },
            expectedError: "Detected an assertion failure.",
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestReceiveOnHalt()
        {
            this.TestWithError(r =>
            {
                r.CreateStateMachine(typeof(M2a));
            },
            expectedError: "Machine 'M2a()' invoked ReceiveEventAsync while halted.",
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestRaiseOnHalt()
        {
            this.TestWithError(r =>
            {
                r.CreateStateMachine(typeof(M2b));
            },
            expectedError: "Machine 'M2b()' invoked RaiseEvent while halted.",
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestGotoOnHalt()
        {
            this.TestWithError(r =>
            {
                r.CreateStateMachine(typeof(M2c));
            },
            expectedError: "Machine 'M2c()' invoked Goto while halted.",
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestAPIsOnHalt()
        {
            this.Test(r =>
            {
                var m = r.CreateStateMachine(typeof(M4));
                r.CreateStateMachine(typeof(M3), new E(m));
            });
        }
    }
}
