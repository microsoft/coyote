// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.Tests.Common.Actors;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests.Actors.StateMachines
{
    public class OnEventUnhandledTest : BaseTest
    {
        public OnEventUnhandledTest(ITestOutputHelper output)
            : base(output)
        {
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
                this.Assert(false, "The 'OnEventUnhandled' callback was called.");
                return Task.CompletedTask;
            }

            protected override OnExceptionOutcome OnException(string methodName, Exception ex)
            {
                return OnExceptionOutcome.Halt;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestOnEventUnhandledCalled()
        {
            this.TestWithError(r =>
            {
                var m = r.CreateActor(typeof(M1));
                r.SendEvent(m, new UnitEvent());
            },
            expectedError: "The 'OnEventUnhandled' callback was called.");
        }

        private class M2 : StateMachine
        {
            private int x = 0;

            [Start]
            private class S : State
            {
            }

            protected override Task OnEventUnhandledAsync(Event e, string currentState)
            {
                this.Assert(this.x == 0, "The 'OnEventUnhandled' callback was not called first.");
                this.x++;
                return Task.CompletedTask;
            }

            protected override OnExceptionOutcome OnException(string methodName, Exception ex)
            {
                this.Assert(this.x == 1, "The 'OnException' callback was not called second.");
                return OnExceptionOutcome.Halt;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestOnExceptionCalledAfterOnEventUnhandled()
        {
            this.Test(r =>
            {
                var m = r.CreateActor(typeof(M2));
                r.SendEvent(m, new UnitEvent());
            });
        }

        private class M3 : StateMachine
        {
            private int x = 0;

            [Start]
            private class S : State
            {
            }

            protected override Task OnEventUnhandledAsync(Event e, string currentState)
            {
                this.Assert(this.x == 0, "The 'OnEventUnhandled' callback was not called first.");
                this.x++;
                return Task.CompletedTask;
            }

            protected override OnExceptionOutcome OnException(string methodName, Exception ex)
            {
                this.Assert(this.x == 1, "The 'OnException' callback was not called second.");
                return OnExceptionOutcome.ThrowException;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestEventUnhandledExceptionPropagation()
        {
            this.TestWithError(r =>
            {
                var m = r.CreateActor(typeof(M3));
                r.SendEvent(m, new UnitEvent());
            },
            expectedError: "'M3()' received event 'Actors.UnitEvent' that cannot be handled.");
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
                this.Assert(false, "The 'OnEventUnhandled' callback was called.");
                return Task.CompletedTask;
            }

            protected override OnExceptionOutcome OnException(string methodName, Exception ex)
            {
                return OnExceptionOutcome.Halt;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestOnEventUnhandledNotCalled()
        {
            this.Test(r =>
            {
                var m = r.CreateActor(typeof(M4));
                r.SendEvent(m, new UnitEvent());
            });
        }
    }
}
