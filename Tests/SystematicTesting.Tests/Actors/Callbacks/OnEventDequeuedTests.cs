// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Specifications;
using Microsoft.Coyote.Tests.Common.Events;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.SystematicTesting.Tests.Actors
{
    public class OnEventDequeuedTests : BaseSystematicTest
    {
        public OnEventDequeuedTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private class E : Event
        {
        }

        private class Begin : Event
        {
            public Event Event;

            public Begin(Event e)
            {
                this.Event = e;
            }
        }

        private class End : Event
        {
            public Event Event;

            public End(Event e)
            {
                this.Event = e;
            }
        }

        private class Done : Event
        {
        }

        private class Spec : Monitor
        {
            [Start]
            [Hot]
            [OnEventGotoState(typeof(Done), typeof(S2))]
            private class S1 : State
            {
            }

            [Cold]
            private class S2 : State
            {
            }
        }

        [OnEventDoAction(typeof(UnitEvent), nameof(Process))]
        private class A1 : Actor
        {
            private void Process()
            {
            }

            protected override Task OnEventDequeuedAsync(Event e) =>
                throw new InvalidOperationException();

            protected override OnExceptionOutcome OnException(Exception ex, string methodName, Event e) =>
                OnExceptionOutcome.Halt;

            protected override Task OnHaltAsync(Event e)
            {
                this.Monitor<Spec>(new Done());
                return Task.CompletedTask;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestOnEventDequeuedWithHaltOutcomeInActor()
        {
            this.Test(r =>
            {
                r.RegisterMonitor<Spec>();
                var m = r.CreateActor(typeof(A1));
                r.SendEvent(m, UnitEvent.Instance);
                r.SendEvent(m, new E()); // Dropped silently.
            });
        }

        private class M1 : StateMachine
        {
            [Start]
            [OnEventDoAction(typeof(UnitEvent), nameof(Process))]
            private class S1 : State
            {
            }

            private void Process()
            {
            }

            protected override Task OnEventDequeuedAsync(Event e) =>
                throw new InvalidOperationException();

            protected override OnExceptionOutcome OnException(Exception ex, string methodName, Event e) =>
                OnExceptionOutcome.Halt;

            protected override Task OnHaltAsync(Event e)
            {
                this.Monitor<Spec>(new Done());
                return Task.CompletedTask;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestOnEventDequeuedWithHaltOutcomeInStateMachine()
        {
            this.Test(r =>
            {
                r.RegisterMonitor<Spec>();
                var m = r.CreateActor(typeof(M1));
                r.SendEvent(m, UnitEvent.Instance);
                r.SendEvent(m, new E()); // Dropped silently.
            });
        }

        [OnEventDoAction(typeof(UnitEvent), nameof(Process))]
        private class A2 : Actor
        {
            private void Process()
            {
            }

            protected override Task OnEventDequeuedAsync(Event e) =>
                throw new InvalidOperationException();

            protected override OnExceptionOutcome OnException(Exception ex, string methodName, Event e) =>
                OnExceptionOutcome.HandledException;

            protected override Task OnHaltAsync(Event e)
            {
                this.Monitor<Spec>(new Done());
                return Task.CompletedTask;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestOnEventDequeuedWithHandledExceptionOutcomeInActor()
        {
            this.TestWithError(r =>
            {
                r.RegisterMonitor<Spec>();
                var m = r.CreateActor(typeof(A2));
                r.SendEvent(m, UnitEvent.Instance);
            },
            configuration: GetConfiguration().WithTestingIterations(100),
            expectedError: "Spec detected liveness bug in hot state 'S1' at the end of program execution.",
            replay: true);
        }

        private class M2 : StateMachine
        {
            [Start]
            [OnEventDoAction(typeof(UnitEvent), nameof(Process))]
            private class S1 : State
            {
            }

            private void Process()
            {
            }

            protected override Task OnEventDequeuedAsync(Event e) =>
                throw new InvalidOperationException();

            protected override OnExceptionOutcome OnException(Exception ex, string methodName, Event e) =>
                OnExceptionOutcome.HandledException;

            protected override Task OnHaltAsync(Event e)
            {
                this.Monitor<Spec>(new Done());
                return Task.CompletedTask;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestOnEventDequeuedWithHandledExceptionOutcomeInStateMachine()
        {
            this.TestWithError(r =>
            {
                r.RegisterMonitor<Spec>();
                var m = r.CreateActor(typeof(M2));
                r.SendEvent(m, UnitEvent.Instance);
            },
            configuration: GetConfiguration().WithTestingIterations(100),
            expectedError: "Spec detected liveness bug in hot state 'S1' at the end of program execution.",
            replay: true);
        }

        [OnEventDoAction(typeof(UnitEvent), nameof(Process))]
        private class A3 : Actor
        {
            private void Process()
            {
            }

            protected override Task OnEventDequeuedAsync(Event e) =>
                throw new InvalidOperationException();

            protected override OnExceptionOutcome OnException(Exception ex, string methodName, Event e) =>
                OnExceptionOutcome.ThrowException;

            protected override Task OnHaltAsync(Event e)
            {
                this.Monitor<Spec>(new Done());
                return Task.CompletedTask;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestOnEventDequeuedWithThrowExceptionOutcomeInActor()
        {
            this.TestWithException<InvalidOperationException>(r =>
            {
                r.RegisterMonitor<Spec>();
                var m = r.CreateActor(typeof(A3));
                r.SendEvent(m, UnitEvent.Instance);
            },
            configuration: GetConfiguration().WithTestingIterations(100),
            replay: true);
        }

        private class M3 : StateMachine
        {
            [Start]
            [OnEventDoAction(typeof(UnitEvent), nameof(Process))]
            private class S1 : State
            {
            }

            private void Process()
            {
            }

            protected override Task OnEventDequeuedAsync(Event e) =>
                throw new InvalidOperationException();

            protected override OnExceptionOutcome OnException(Exception ex, string methodName, Event e) =>
                OnExceptionOutcome.ThrowException;

            protected override Task OnHaltAsync(Event e)
            {
                this.Monitor<Spec>(new Done());
                return Task.CompletedTask;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestOnEventDequeuedWithThrowExceptionOutcomeInStateMachine()
        {
            this.TestWithException<InvalidOperationException>(r =>
            {
                r.RegisterMonitor<Spec>();
                var m = r.CreateActor(typeof(M3));
                r.SendEvent(m, UnitEvent.Instance);
            },
            configuration: GetConfiguration().WithTestingIterations(100),
            replay: true);
        }
    }
}
