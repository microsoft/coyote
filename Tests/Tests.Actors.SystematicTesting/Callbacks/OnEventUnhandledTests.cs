// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Coyote.Tests.Common.Events;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Actors.SystematicTesting.Tests
{
    public class OnEventUnhandledTests : BaseActorSystematicTest
    {
        public OnEventUnhandledTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private class A1 : Actor
        {
            protected override Task OnEventUnhandledAsync(Event e, string currentState)
            {
                this.Assert(e is UnitEvent);
                this.Assert(false, "Reached test assertion.");
                return Task.CompletedTask;
            }

            protected override OnExceptionOutcome OnException(Exception ex, string methodName, Event e)
            {
                return OnExceptionOutcome.Halt;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestOnEventUnhandledCalledInActor()
        {
            this.TestWithError(r =>
            {
                var m = r.CreateActor(typeof(A1));
                r.SendEvent(m, UnitEvent.Instance);
            },
            expectedError: "Reached test assertion.");
        }

        private class M1 : StateMachine
        {
            [Start]
            private class S : State
            {
            }

            protected override Task OnEventUnhandledAsync(Event e, string currentState)
            {
                this.Assert(currentState == "S");
                this.Assert(e is UnitEvent);
                this.Assert(false, "Reached test assertion.");
                return Task.CompletedTask;
            }

            protected override OnExceptionOutcome OnException(Exception ex, string methodName, Event e)
            {
                return OnExceptionOutcome.Halt;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestOnEventUnhandledCalledInStateMachine()
        {
            this.TestWithError(r =>
            {
                var m = r.CreateActor(typeof(M1));
                r.SendEvent(m, UnitEvent.Instance);
            },
            expectedError: "Reached test assertion.");
        }

        private class A2 : Actor
        {
            private int Value = 0;

            protected override Task OnEventUnhandledAsync(Event e, string currentState)
            {
                this.Assert(this.Value == 0, "The 'OnEventUnhandled' callback was not called first.");
                this.Value++;
                return Task.CompletedTask;
            }

            protected override OnExceptionOutcome OnException(Exception ex, string methodName, Event e)
            {
                this.Assert(this.Value == 1, "The 'OnException' callback was not called second.");
                return OnExceptionOutcome.Halt;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestOnExceptionCalledAfterOnEventUnhandledInActor()
        {
            this.Test(r =>
            {
                var m = r.CreateActor(typeof(A2));
                r.SendEvent(m, UnitEvent.Instance);
            });
        }

        private class M2 : StateMachine
        {
            private int Value = 0;

            [Start]
            private class S : State
            {
            }

            protected override Task OnEventUnhandledAsync(Event e, string currentState)
            {
                this.Assert(this.Value == 0, "The 'OnEventUnhandled' callback was not called first.");
                this.Value++;
                return Task.CompletedTask;
            }

            protected override OnExceptionOutcome OnException(Exception ex, string methodName, Event e)
            {
                this.Assert(this.Value == 1, "The 'OnException' callback was not called second.");
                return OnExceptionOutcome.Halt;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestOnExceptionCalledAfterOnEventUnhandledInStateMachine()
        {
            this.Test(r =>
            {
                var m = r.CreateActor(typeof(M2));
                r.SendEvent(m, UnitEvent.Instance);
            });
        }

        private class A3 : Actor
        {
            private int Value = 0;

            protected override Task OnEventUnhandledAsync(Event e, string currentState)
            {
                this.Assert(this.Value == 0, "The 'OnEventUnhandled' callback was not called first.");
                this.Value++;
                return Task.CompletedTask;
            }

            protected override OnExceptionOutcome OnException(Exception ex, string methodName, Event e)
            {
                this.Assert(this.Value == 1, "The 'OnException' callback was not called second.");
                return OnExceptionOutcome.ThrowException;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestEventUnhandledExceptionPropagationInActor()
        {
            this.TestWithError(r =>
            {
                var m = r.CreateActor(typeof(A3));
                r.SendEvent(m, UnitEvent.Instance);
            },
            expectedError: "A3() received event 'Events.UnitEvent' that cannot be handled.");
        }

        private class M3 : StateMachine
        {
            private int Value = 0;

            [Start]
            private class S : State
            {
            }

            protected override Task OnEventUnhandledAsync(Event e, string currentState)
            {
                this.Assert(this.Value == 0, "The 'OnEventUnhandled' callback was not called first.");
                this.Value++;
                return Task.CompletedTask;
            }

            protected override OnExceptionOutcome OnException(Exception ex, string methodName, Event e)
            {
                this.Assert(this.Value == 1, "The 'OnException' callback was not called second.");
                return OnExceptionOutcome.ThrowException;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestEventUnhandledExceptionPropagationInStateMachine()
        {
            this.TestWithError(r =>
            {
                var m = r.CreateActor(typeof(M3));
                r.SendEvent(m, UnitEvent.Instance);
            },
            expectedError: "M3() received event 'Events.UnitEvent' that cannot be handled.");
        }

        [OnEventDoAction(typeof(UnitEvent), nameof(HandleE))]
        private class A4 : Actor
        {
            private void HandleE()
            {
                throw new Exception();
            }

            protected override Task OnEventUnhandledAsync(Event e, string currentState)
            {
                this.Assert(false);
                return Task.CompletedTask;
            }

            protected override OnExceptionOutcome OnException(Exception ex, string methodName, Event e)
            {
                return OnExceptionOutcome.Halt;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestOnEventUnhandledNotCalledInActor()
        {
            this.Test(r =>
            {
                var m = r.CreateActor(typeof(A4));
                r.SendEvent(m, UnitEvent.Instance);
            });
        }

        private class M4 : StateMachine
        {
            [Start]
            [OnEventDoAction(typeof(UnitEvent), nameof(HandleE))]
            private class S : State
            {
            }

            private void HandleE()
            {
                throw new Exception();
            }

            protected override Task OnEventUnhandledAsync(Event e, string currentState)
            {
                this.Assert(false);
                return Task.CompletedTask;
            }

            protected override OnExceptionOutcome OnException(Exception ex, string methodName, Event e)
            {
                return OnExceptionOutcome.Halt;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestOnEventUnhandledNotCalledInStateMachine()
        {
            this.Test(r =>
            {
                var m = r.CreateActor(typeof(M4));
                r.SendEvent(m, UnitEvent.Instance);
            });
        }
    }
}
