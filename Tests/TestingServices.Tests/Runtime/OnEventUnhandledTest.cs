// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Coyote.Machines;
using Microsoft.Coyote.Runtime;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests
{
    public class OnEventUnhandledTest : BaseTest
    {
        public OnEventUnhandledTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class E : Event
        {
        }

        private class M1 : Machine
        {
            [Start]
            private class S : MachineState
            {
            }

            protected override Task OnEventUnhandledAsync(Event e, string currentState)
            {
                this.Assert(currentState == "S");
                this.Assert(e is E);
                this.Assert(false, "The 'OnEventUnhandled' callback was called.");
                return Task.CompletedTask;
            }

            protected override OnExceptionOutcome OnException(string methodName, Exception ex)
            {
                return OnExceptionOutcome.HaltMachine;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestOnEventUnhandledCalled()
        {
            this.TestWithError(r =>
            {
                var m = r.CreateMachine(typeof(M1));
                r.SendEvent(m, new E());
            },
            expectedError: "The 'OnEventUnhandled' callback was called.");
        }

        private class M2 : Machine
        {
            private int x = 0;

            [Start]
            private class S : MachineState
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
                return OnExceptionOutcome.HaltMachine;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestOnExceptionCalledAfterOnEventUnhandled()
        {
            this.Test(r =>
            {
                var m = r.CreateMachine(typeof(M2));
                r.SendEvent(m, new E());
            });
        }

        private class M3 : Machine
        {
            private int x = 0;

            [Start]
            private class S : MachineState
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
                var m = r.CreateMachine(typeof(M3));
                r.SendEvent(m, new E());
            },
            expectedError: "Machine 'M3()' received event 'E' that cannot be handled.");
        }

        private class M4 : Machine
        {
            [Start]
            [OnEventDoAction(typeof(E), nameof(HandleE))]
            private class S : MachineState
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
                return OnExceptionOutcome.HaltMachine;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestOnEventUnhandledNotCalled()
        {
            this.Test(r =>
            {
                var m = r.CreateMachine(typeof(M4));
                r.SendEvent(m, new E());
            });
        }
    }
}
