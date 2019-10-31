// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Actors;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests
{
    public class PushApiTest : BaseTest
    {
        public PushApiTest(ITestOutputHelper output)
            : base(output)
        {
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
                this.Push<Done>();
            }

            [OnEntry(nameof(EntryDone))]
            private class Done : State
            {
            }

            private void EntryDone()
            {
                // This assert is reachable.
                this.Assert(false, "Bug found.");
            }
        }

        private class E : Event
        {
        }

        private class M2 : StateMachine
        {
            private int cnt = 0;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [IgnoreEvents(typeof(E))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.Assert(this.cnt == 0); // called once
                this.cnt++;

                this.Push<Done>();
            }

            [OnEntry(nameof(EntryDone))]
            private class Done : State
            {
            }

            private void EntryDone()
            {
                this.Pop();
            }
        }

        private class M3 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnExit(nameof(ExitInit))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.Push<Done>();
            }

            private void ExitInit()
            {
                // This assert is not reachable.
                this.Assert(false, "Bug found.");
            }

            private class Done : State
            {
            }
        }

        private class M4a : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                // Added a different failure mode here; try to Goto a state from another machine.
                this.Push<M4b.Done>();
            }

            private class Done : State
            {
            }
        }

        private class M4b : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
            }

            internal class Done : State
            {
            }
        }

        [Fact(Timeout=5000)]
        public void TestPushSimple()
        {
            this.TestWithError(r =>
            {
                r.CreateMachine(typeof(M1));
            },
            expectedError: "Bug found.",
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestPushPopSimple()
        {
            this.Test(r =>
            {
                var m = r.CreateMachine(typeof(M2));
                r.SendEvent(m, new E());
            });
        }

        [Fact(Timeout=5000)]
        public void TestPushStateExit()
        {
            this.Test(r =>
            {
                r.CreateMachine(typeof(M3));
            });
        }

        [Fact(Timeout=5000)]
        public void TestPushBadStateFail()
        {
            this.TestWithError(r =>
            {
                r.CreateMachine(typeof(M4a));
            },
            expectedError: "Machine 'M4a()' is trying to transition to non-existing state 'Done'.",
            replay: true);
        }
    }
}
