// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Coyote.Specifications;
using Microsoft.Coyote.Tests.Common.Events;
using Xunit;
using Xunit.Abstractions;
using MonitorEvent = Microsoft.Coyote.Specifications.Monitor.Event;

namespace Microsoft.Coyote.Actors.BugFinding.Tests
{
    public class OnEventHandledTests : BaseActorBugFindingTest
    {
        public OnEventHandledTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private class E1 : Event
        {
        }

        private class E2 : Event
        {
        }

        private class Begin : MonitorEvent
        {
            public Event Event;

            public Begin(Event e)
            {
                this.Event = e;
            }
        }

        private class End : MonitorEvent
        {
            public Event Event;

            public End(Event e)
            {
                this.Event = e;
            }
        }

        private class Spec1 : Monitor
        {
            private int Counter = 0;

            [Start]
            [Hot]
            [OnEventDoAction(typeof(Begin), nameof(Process))]
            [OnEventDoAction(typeof(End), nameof(Process))]
            private class S1 : State
            {
            }

            [Cold]
            private class S2 : State
            {
            }

            private void Process(Event e)
            {
                // Asserts that the following calls are seen in-order:
                //   OnEventDequeueAsync(UnitEvent)
                //   OnEventHandledAsync(UnitEvent)
                //   OnEventDequeueAsync(E1)
                //   OnEventHandledAsync(E1)
                if (this.Counter is 0 && e is Begin beginE1 && beginE1.Event is UnitEvent)
                {
                    this.Counter++;
                }
                else if (this.Counter is 1 && e is End endE1 && endE1.Event is UnitEvent)
                {
                    this.Counter++;
                }
                else if (this.Counter is 2 && e is Begin beginE2 && beginE2.Event is E1)
                {
                    this.Counter++;
                }
                else if (this.Counter is 3 && e is End endE2 && endE2.Event is E1)
                {
                    this.Counter++;
                }
                else
                {
                    this.Assert(false);
                }

                if (this.Counter is 4)
                {
                    this.RaiseGotoStateEvent<S2>();
                }
            }
        }

        private class M1 : StateMachine
        {
            [Start]
            [OnEventDoAction(typeof(UnitEvent), nameof(Process))]
            [OnEventDoAction(typeof(E1), nameof(Process))]
            [OnEventDoAction(typeof(E2), nameof(ProcessE3))]
            private class Init : State
            {
            }

            private void Process()
            {
                this.RaiseEvent(new E2());
            }

#pragma warning disable CA1822 // Mark members as static
            private void ProcessE3()
#pragma warning restore CA1822 // Mark members as static
            {
            }

            protected override Task OnEventDequeuedAsync(Event e)
            {
                this.Monitor<Spec1>(new Begin(e));
                return Task.CompletedTask;
            }

            protected override Task OnEventHandledAsync(Event e)
            {
                this.Monitor<Spec1>(new End(e));
                return Task.CompletedTask;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestOnEventHandledInStateMachine()
        {
            this.Test(r =>
            {
                r.RegisterMonitor<Spec1>();
                var m = r.CreateActor(typeof(M1));
                r.SendEvent(m, UnitEvent.Instance);
                r.SendEvent(m, new E1());
            });
        }

        private class Spec2 : Monitor
        {
            private int Counter = 0;

            [Start]
            [Hot]
            [OnEventDoAction(typeof(Begin), nameof(Process))]
            [OnEventDoAction(typeof(End), nameof(Process))]
            private class S1 : State
            {
            }

            [Cold]
            private class S2 : State
            {
            }

            private void Process(Event e)
            {
                // Asserts that the following calls are seen in-order:
                //   OnEventDequeueAsync(UnitEvent)
                //   OnEventHandledAsync(UnitEvent)
                if (this.Counter is 0 && e is Begin beginE1 && beginE1.Event is UnitEvent)
                {
                    this.Counter++;
                }
                else if (this.Counter is 1 && e is End endE1 && endE1.Event is UnitEvent)
                {
                    this.Counter++;
                }
                else
                {
                    this.Assert(false);
                }

                if (this.Counter is 2)
                {
                    this.RaiseGotoStateEvent<S2>();
                }
            }
        }

        [OnEventDoAction(typeof(UnitEvent), nameof(Process))]
        private class A2 : Actor
        {
            private void Process() => this.RaiseHaltEvent();

            protected override Task OnEventDequeuedAsync(Event e)
            {
                this.Monitor<Spec2>(new Begin(e));
                return Task.CompletedTask;
            }

            protected override Task OnEventHandledAsync(Event e)
            {
                this.Monitor<Spec2>(new End(e));
                return Task.CompletedTask;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestOnEventHandledInHaltedActor()
        {
            this.Test(r =>
            {
                r.RegisterMonitor<Spec2>();
                var m = r.CreateActor(typeof(A2));
                r.SendEvent(m, UnitEvent.Instance);
            });
        }

        private class M2 : StateMachine
        {
            [Start]
            [OnEventDoAction(typeof(UnitEvent), nameof(Process))]
            private class Init : State
            {
            }

            private void Process()
            {
                this.RaiseEvent(HaltEvent.Instance);
            }

            protected override Task OnEventDequeuedAsync(Event e)
            {
                this.Monitor<Spec2>(new Begin(e));
                return Task.CompletedTask;
            }

            protected override Task OnEventHandledAsync(Event e)
            {
                this.Monitor<Spec2>(new End(e));
                return Task.CompletedTask;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestOnEventHandledInHaltedStateMachine()
        {
            this.Test(r =>
            {
                r.RegisterMonitor<Spec2>();
                var m = r.CreateActor(typeof(M2));
                r.SendEvent(m, UnitEvent.Instance);
            });
        }

        private class Spec3 : Monitor
        {
            internal class Done : Event
            {
            }

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
        private class A3 : Actor
        {
#pragma warning disable CA1822 // Mark members as static
            private void Process()
#pragma warning restore CA1822 // Mark members as static
            {
            }

            protected override Task OnEventHandledAsync(Event e) =>
                throw new InvalidOperationException();

            protected override OnExceptionOutcome OnException(Exception ex, string methodName, Event e) =>
                OnExceptionOutcome.Halt;

            protected override Task OnHaltAsync(Event e)
            {
                this.Monitor<Spec3>(new Spec3.Done());
                return Task.CompletedTask;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestOnEventHandledWithHaltOutcomeInActor()
        {
            this.Test(r =>
            {
                r.RegisterMonitor<Spec3>();
                var m = r.CreateActor(typeof(A3));
                r.SendEvent(m, UnitEvent.Instance);
                r.SendEvent(m, new E1()); // Dropped silently.
            });
        }

        private class M3 : StateMachine
        {
            [Start]
            [OnEventDoAction(typeof(UnitEvent), nameof(Process))]
            private class S1 : State
            {
            }

#pragma warning disable CA1822 // Mark members as static
            private void Process()
#pragma warning restore CA1822 // Mark members as static
            {
            }

            protected override Task OnEventHandledAsync(Event e) =>
                throw new InvalidOperationException();

            protected override OnExceptionOutcome OnException(Exception ex, string methodName, Event e) =>
                OnExceptionOutcome.Halt;

            protected override Task OnHaltAsync(Event e)
            {
                this.Monitor<Spec3>(new Spec3.Done());
                return Task.CompletedTask;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestOnEventHandledWithHaltOutcomeInStateMachine()
        {
            this.Test(r =>
            {
                r.RegisterMonitor<Spec3>();
                var m = r.CreateActor(typeof(M3));
                r.SendEvent(m, UnitEvent.Instance);
                r.SendEvent(m, new E1()); // Dropped silently.
            });
        }

        [OnEventDoAction(typeof(UnitEvent), nameof(Process))]
        private class A4 : Actor
        {
#pragma warning disable CA1822 // Mark members as static
            private void Process()
#pragma warning restore CA1822 // Mark members as static
            {
            }

            protected override Task OnEventHandledAsync(Event e) =>
                throw new InvalidOperationException();

            protected override OnExceptionOutcome OnException(Exception ex, string methodName, Event e) =>
                OnExceptionOutcome.HandledException;

            protected override Task OnHaltAsync(Event e)
            {
                this.Monitor<Spec3>(new Spec3.Done());
                return Task.CompletedTask;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestOnEventHandledWithHandledExceptionOutcomeInActor()
        {
            this.TestWithError(r =>
            {
                r.RegisterMonitor<Spec3>();
                var m = r.CreateActor(typeof(A4));
                r.SendEvent(m, UnitEvent.Instance);
            },
            configuration: this.GetConfiguration().WithTestingIterations(100),
            expectedError: "Spec3 detected liveness bug in hot state 'S1' at the end of program execution.",
            replay: true);
        }

        private class M4 : StateMachine
        {
            [Start]
            [OnEventDoAction(typeof(UnitEvent), nameof(Process))]
            private class S1 : State
            {
            }

#pragma warning disable CA1822 // Mark members as static
            private void Process()
#pragma warning restore CA1822 // Mark members as static
            {
            }

            protected override Task OnEventHandledAsync(Event e) =>
                throw new InvalidOperationException();

            protected override OnExceptionOutcome OnException(Exception ex, string methodName, Event e) =>
                OnExceptionOutcome.HandledException;

            protected override Task OnHaltAsync(Event e)
            {
                this.Monitor<Spec3>(new Spec3.Done());
                return Task.CompletedTask;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestOnEventHandledWithHandledExceptionOutcomeInStateMachine()
        {
            this.TestWithError(r =>
            {
                r.RegisterMonitor<Spec3>();
                var m = r.CreateActor(typeof(M4));
                r.SendEvent(m, UnitEvent.Instance);
            },
            configuration: this.GetConfiguration().WithTestingIterations(100),
            expectedError: "Spec3 detected liveness bug in hot state 'S1' at the end of program execution.",
            replay: true);
        }

        [OnEventDoAction(typeof(UnitEvent), nameof(Process))]
        private class A5 : Actor
        {
#pragma warning disable CA1822 // Mark members as static
            private void Process()
#pragma warning restore CA1822 // Mark members as static
            {
            }

            protected override Task OnEventHandledAsync(Event e) =>
                throw new InvalidOperationException();

            protected override OnExceptionOutcome OnException(Exception ex, string methodName, Event e) =>
                OnExceptionOutcome.ThrowException;

            protected override Task OnHaltAsync(Event e)
            {
                this.Monitor<Spec3>(new Spec3.Done());
                return Task.CompletedTask;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestOnEventHandledWithThrowExceptionOutcomeInActor()
        {
            this.TestWithException<InvalidOperationException>(r =>
            {
                r.RegisterMonitor<Spec3>();
                var m = r.CreateActor(typeof(A5));
                r.SendEvent(m, UnitEvent.Instance);
            },
            configuration: this.GetConfiguration().WithTestingIterations(100),
            replay: true);
        }

        private class M5 : StateMachine
        {
            [Start]
            [OnEventDoAction(typeof(UnitEvent), nameof(Process))]
            private class S1 : State
            {
            }

#pragma warning disable CA1822 // Mark members as static
            private void Process()
#pragma warning restore CA1822 // Mark members as static
            {
            }

            protected override Task OnEventHandledAsync(Event e) =>
                throw new InvalidOperationException();

            protected override OnExceptionOutcome OnException(Exception ex, string methodName, Event e) =>
                OnExceptionOutcome.ThrowException;

            protected override Task OnHaltAsync(Event e)
            {
                this.Monitor<Spec3>(new Spec3.Done());
                return Task.CompletedTask;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestOnEventHandledWithThrowExceptionOutcomeInStateMachine()
        {
            this.TestWithException<InvalidOperationException>(r =>
            {
                r.RegisterMonitor<Spec3>();
                var m = r.CreateActor(typeof(M5));
                r.SendEvent(m, UnitEvent.Instance);
            },
            configuration: this.GetConfiguration().WithTestingIterations(100),
            replay: true);
        }
    }
}
