// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.Specifications;
using Microsoft.Coyote.Tests.Common.Actors;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests.Actors
{
    public class OnEventDequeueOrHandledTest : BaseTest
    {
        public OnEventDequeueOrHandledTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class E1 : Event
        {
        }

        private class E2 : Event
        {
        }

        private class E3 : Event
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

        // Ensures that A1 or M1 sees the following calls:
        // OnEventDequeueAsync(E1), OnEventHandledAsync(E1), OnEventDequeueAsync(E2), OnEventHandledAsync(E2)
        private class Spec1 : Monitor
        {
            private int counter = 0;

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

            private void Process()
            {
                if (this.counter == 0 && this.ReceivedEvent is Begin && (this.ReceivedEvent as Begin).Event is E1)
                {
                    this.counter++;
                }
                else if (this.counter == 1 && this.ReceivedEvent is End && (this.ReceivedEvent as End).Event is E1)
                {
                    this.counter++;
                }
                else if (this.counter == 2 && this.ReceivedEvent is Begin && (this.ReceivedEvent as Begin).Event is E2)
                {
                    this.counter++;
                }
                else if (this.counter == 3 && this.ReceivedEvent is End && (this.ReceivedEvent as End).Event is E2)
                {
                    this.counter++;
                }
                else
                {
                    this.Assert(false);
                }

                if (this.counter == 4)
                {
                    this.GotoState<S2>();
                }
            }
        }

        private class M1 : StateMachine
        {
            [Start]
            [OnEventDoAction(typeof(E1), nameof(Process))]
            [OnEventDoAction(typeof(E2), nameof(Process))]
            [OnEventDoAction(typeof(E3), nameof(ProcessE3))]
            private class Init : State
            {
            }

            private void Process()
            {
                this.RaiseEvent(new E3());
            }

            private void ProcessE3()
            {
            }

            protected override Task OnEventDequeueAsync(Event e)
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
        public void TestOnProcessingCalledInStateMachine()
        {
            this.Test(r =>
            {
                r.RegisterMonitor(typeof(Spec1));
                var m = r.CreateActor(typeof(M1), new UnitEvent());
                r.SendEvent(m, new E1());
                r.SendEvent(m, new E2());
            });
        }

        // Ensures that A2 or M2 sees the following calls:
        // OnEventDequeueAsync(E1)
        private class Spec2 : Monitor
        {
            private int counter = 0;

            [Start]
            [Hot]
            [OnEventDoAction(typeof(Begin), nameof(Process))]
            private class S1 : State
            {
            }

            [Cold]
            private class S2 : State
            {
            }

            private void Process()
            {
                if (this.counter == 0 && this.ReceivedEvent is Begin && (this.ReceivedEvent as Begin).Event is E1)
                {
                    this.counter++;
                }
                else
                {
                    this.Assert(false);
                }

                if (this.counter == 1)
                {
                    this.GotoState<S2>();
                }
            }
        }

        [OnEventDoAction(typeof(E1), nameof(Process))]
        private class A2 : Actor
        {
            private void Process()
            {
                this.Halt();
            }

            protected override Task OnEventDequeueAsync(Event e)
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
        public void TestOnProcessingNotCalledOnHaltInActor()
        {
            this.Test(r =>
            {
                r.RegisterMonitor(typeof(Spec2));
                var m = r.CreateActor(typeof(A2));
                r.SendEvent(m, new E1());
            });
        }

        private class M2 : StateMachine
        {
            [Start]
            [OnEventDoAction(typeof(E1), nameof(Process))]
            private class Init : State
            {
            }

            private void Process()
            {
                this.RaiseEvent(HaltEvent.Instance);
            }

            protected override Task OnEventDequeueAsync(Event e)
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

        [Fact(Timeout=5000)]
        public void TestOnProcessingNotCalledOnHaltInStateMachine()
        {
            this.Test(r =>
            {
                r.RegisterMonitor(typeof(Spec2));
                var m = r.CreateActor(typeof(M2));
                r.SendEvent(m, new E1());
            });
        }

        private class Spec3 : Monitor
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

        private class M3 : StateMachine
        {
            [Start]
            [OnEventDoAction(typeof(E1), nameof(Process))]
            private class S1 : State
            {
            }

            private class S2 : State
            {
            }

            [OnEntry(nameof(Finish))]
            private class S3 : State
            {
            }

            private void Process()
            {
                this.GotoState<S2>();
            }

            private void Finish()
            {
                this.Monitor<Spec3>(new Done());
            }

            protected override Task OnEventHandledAsync(Event e)
            {
                this.Assert(e is E1);
                this.Assert(this.CurrentState.Name == typeof(S2).Name);
                this.GotoState<S3>();
                return Task.CompletedTask;
            }
        }

        [Fact(Timeout=5000)]
        public void TestOnProcessingCanGotoInStateMachine()
        {
            this.Test(r =>
            {
                r.RegisterMonitor(typeof(Spec3));
                var m = r.CreateActor(typeof(M3));
                r.SendEvent(m, new E1());
            });
        }

        private class Spec4 : Monitor
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

        [OnEventDoAction(typeof(E1), nameof(Process))]
        private class A4 : Actor
        {
            private void Process()
            {
            }

            protected override Task OnEventHandledAsync(Event e)
            {
                this.Halt();
                return Task.CompletedTask;
            }

            protected override Task OnHaltAsync()
            {
                this.Monitor<Spec4>(new Done());
                return Task.CompletedTask;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestOnProcessingCanHaltInActor()
        {
            this.Test(r =>
            {
                r.RegisterMonitor(typeof(Spec4));
                var m = r.CreateActor(typeof(A4));
                r.SendEvent(m, new E1());
                r.SendEvent(m, new E2()); // Dropped silently.
            });
        }

        private class M4 : StateMachine
        {
            [Start]
            [OnEventDoAction(typeof(E1), nameof(Process))]
            private class S1 : State
            {
            }

            private void Process()
            {
            }

            protected override Task OnEventHandledAsync(Event e)
            {
                this.RaiseEvent(HaltEvent.Instance);
                return Task.CompletedTask;
            }

            protected override Task OnHaltAsync()
            {
                this.Monitor<Spec4>(new Done());
                return Task.CompletedTask;
            }
        }

        [Fact(Timeout=5000)]
        public void TestOnProcessingCanHaltInStateMachine()
        {
            this.Test(r =>
            {
                r.RegisterMonitor(typeof(Spec4));
                var m = r.CreateActor(typeof(M4));
                r.SendEvent(m, new E1());
                r.SendEvent(m, new E2()); // Dropped silently.
            });
        }

        [OnEventDoAction(typeof(E1), nameof(Process))]
        private class A5 : Actor
        {
            private void Process()
            {
            }

            protected override Task OnEventDequeueAsync(Event e)
            {
                throw new InvalidOperationException();
            }

            protected override OnExceptionOutcome OnException(string methodName, Exception ex)
            {
                return OnExceptionOutcome.Halt;
            }

            protected override Task OnHaltAsync()
            {
                this.Monitor<Spec4>(new Done());
                return Task.CompletedTask;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestExceptionOnEventDequeueWithHaltOutcomeInActor()
        {
            this.Test(r =>
            {
                r.RegisterMonitor(typeof(Spec4));
                var m = r.CreateActor(typeof(A5));
                r.SendEvent(m, new E1());
                r.SendEvent(m, new E2()); // Dropped silently.
            });
        }

        private class M5 : StateMachine
        {
            [Start]
            [OnEventDoAction(typeof(E1), nameof(Process))]
            private class S1 : State
            {
            }

            private void Process()
            {
            }

            protected override Task OnEventDequeueAsync(Event e)
            {
                throw new InvalidOperationException();
            }

            protected override OnExceptionOutcome OnException(string methodName, Exception ex)
            {
                return OnExceptionOutcome.Halt;
            }

            protected override Task OnHaltAsync()
            {
                this.Monitor<Spec4>(new Done());
                return Task.CompletedTask;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestExceptionOnEventDequeueWithHaltOutcomeInStateMachine()
        {
            this.Test(r =>
            {
                r.RegisterMonitor(typeof(Spec4));
                var m = r.CreateActor(typeof(M5));
                r.SendEvent(m, new E1());
                r.SendEvent(m, new E2()); // Dropped silently.
            });
        }

        [OnEventDoAction(typeof(E1), nameof(Process))]
        private class A6 : Actor
        {
            private void Process()
            {
            }

            protected override Task OnEventDequeueAsync(Event e)
            {
                throw new InvalidOperationException();
            }

            protected override OnExceptionOutcome OnException(string methodName, Exception ex)
            {
                return OnExceptionOutcome.HandledException;
            }

            protected override Task OnHaltAsync()
            {
                this.Monitor<Spec4>(new Done());
                return Task.CompletedTask;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestExceptionOnEventDequeueWithHandledExceptionOutcomeInActor()
        {
            this.TestWithError(r =>
            {
                r.RegisterMonitor(typeof(Spec4));
                var m = r.CreateActor(typeof(A6));
                r.SendEvent(m, new E1());
            },
            configuration: GetConfiguration().WithNumberOfIterations(100),
            expectedError: "Monitor 'Spec4' detected liveness bug in hot state 'S1' at the end of program execution.",
            replay: true);
        }

        private class M6 : StateMachine
        {
            [Start]
            [OnEventDoAction(typeof(E1), nameof(Process))]
            private class S1 : State
            {
            }

            private void Process()
            {
            }

            protected override Task OnEventDequeueAsync(Event e)
            {
                throw new InvalidOperationException();
            }

            protected override OnExceptionOutcome OnException(string methodName, Exception ex)
            {
                return OnExceptionOutcome.HandledException;
            }

            protected override Task OnHaltAsync()
            {
                this.Monitor<Spec4>(new Done());
                return Task.CompletedTask;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestExceptionOnEventDequeueWithHandledExceptionOutcomeInStateMachine()
        {
            this.TestWithError(r =>
            {
                r.RegisterMonitor(typeof(Spec4));
                var m = r.CreateActor(typeof(M6));
                r.SendEvent(m, new E1());
            },
            configuration: GetConfiguration().WithNumberOfIterations(100),
            expectedError: "Monitor 'Spec4' detected liveness bug in hot state 'S1' at the end of program execution.",
            replay: true);
        }

        [OnEventDoAction(typeof(E1), nameof(Process))]
        private class A7 : Actor
        {
            private void Process()
            {
            }

            protected override Task OnEventDequeueAsync(Event e)
            {
                throw new InvalidOperationException();
            }

            protected override OnExceptionOutcome OnException(string methodName, Exception ex)
            {
                return OnExceptionOutcome.ThrowException;
            }

            protected override Task OnHaltAsync()
            {
                this.Monitor<Spec4>(new Done());
                return Task.CompletedTask;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestExceptionOnEventDequeueWithThrowExceptionOutcomeInActor()
        {
            this.TestWithException<InvalidOperationException>(r =>
            {
                r.RegisterMonitor(typeof(Spec4));
                var m = r.CreateActor(typeof(A7));
                r.SendEvent(m, new E1());
            },
            configuration: GetConfiguration().WithNumberOfIterations(100),
            replay: true);
        }

        private class M7 : StateMachine
        {
            [Start]
            [OnEventDoAction(typeof(E1), nameof(Process))]
            private class S1 : State
            {
            }

            private void Process()
            {
            }

            protected override Task OnEventDequeueAsync(Event e)
            {
                throw new InvalidOperationException();
            }

            protected override OnExceptionOutcome OnException(string methodName, Exception ex)
            {
                return OnExceptionOutcome.ThrowException;
            }

            protected override Task OnHaltAsync()
            {
                this.Monitor<Spec4>(new Done());
                return Task.CompletedTask;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestExceptionOnEventDequeueWithThrowExceptionOutcomeInStateMachine()
        {
            this.TestWithException<InvalidOperationException>(r =>
            {
                r.RegisterMonitor(typeof(Spec4));
                var m = r.CreateActor(typeof(M7));
                r.SendEvent(m, new E1());
            },
            configuration: GetConfiguration().WithNumberOfIterations(100),
            replay: true);
        }

        [OnEventDoAction(typeof(E1), nameof(Process))]
        private class A8 : Actor
        {
            private void Process()
            {
            }

            protected override Task OnEventHandledAsync(Event e)
            {
                throw new InvalidOperationException();
            }

            protected override OnExceptionOutcome OnException(string methodName, Exception ex)
            {
                return OnExceptionOutcome.Halt;
            }

            protected override Task OnHaltAsync()
            {
                this.Monitor<Spec4>(new Done());
                return Task.CompletedTask;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestExceptionOnEventHandledWithHaltOutcomeInActor()
        {
            this.Test(r =>
            {
                r.RegisterMonitor(typeof(Spec4));
                var m = r.CreateActor(typeof(A8));
                r.SendEvent(m, new E1());
                r.SendEvent(m, new E2()); // Dropped silently.
            });
        }

        private class M8 : StateMachine
        {
            [Start]
            [OnEventDoAction(typeof(E1), nameof(Process))]
            private class S1 : State
            {
            }

            private void Process()
            {
            }

            protected override Task OnEventHandledAsync(Event e)
            {
                throw new InvalidOperationException();
            }

            protected override OnExceptionOutcome OnException(string methodName, Exception ex)
            {
                return OnExceptionOutcome.Halt;
            }

            protected override Task OnHaltAsync()
            {
                this.Monitor<Spec4>(new Done());
                return Task.CompletedTask;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestExceptionOnEventHandledWithHaltOutcomeInStateMachine()
        {
            this.Test(r =>
            {
                r.RegisterMonitor(typeof(Spec4));
                var m = r.CreateActor(typeof(M8));
                r.SendEvent(m, new E1());
                r.SendEvent(m, new E2()); // Dropped silently.
            });
        }

        [OnEventDoAction(typeof(E1), nameof(Process))]
        private class A9 : Actor
        {
            private void Process()
            {
            }

            protected override Task OnEventHandledAsync(Event e)
            {
                throw new InvalidOperationException();
            }

            protected override OnExceptionOutcome OnException(string methodName, Exception ex)
            {
                return OnExceptionOutcome.HandledException;
            }

            protected override Task OnHaltAsync()
            {
                this.Monitor<Spec4>(new Done());
                return Task.CompletedTask;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestExceptionOnEventHandledWithHandledExceptionOutcomeInActor()
        {
            this.TestWithError(r =>
            {
                r.RegisterMonitor(typeof(Spec4));
                var m = r.CreateActor(typeof(A9));
                r.SendEvent(m, new E1());
            },
            configuration: GetConfiguration().WithNumberOfIterations(100),
            expectedError: "Monitor 'Spec4' detected liveness bug in hot state 'S1' at the end of program execution.",
            replay: true);
        }

        private class M9 : StateMachine
        {
            [Start]
            [OnEventDoAction(typeof(E1), nameof(Process))]
            private class S1 : State
            {
            }

            private void Process()
            {
            }

            protected override Task OnEventHandledAsync(Event e)
            {
                throw new InvalidOperationException();
            }

            protected override OnExceptionOutcome OnException(string methodName, Exception ex)
            {
                return OnExceptionOutcome.HandledException;
            }

            protected override Task OnHaltAsync()
            {
                this.Monitor<Spec4>(new Done());
                return Task.CompletedTask;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestExceptionOnEventHandledWithHandledExceptionOutcomeInStateMachine()
        {
            this.TestWithError(r =>
            {
                r.RegisterMonitor(typeof(Spec4));
                var m = r.CreateActor(typeof(M9));
                r.SendEvent(m, new E1());
            },
            configuration: GetConfiguration().WithNumberOfIterations(100),
            expectedError: "Monitor 'Spec4' detected liveness bug in hot state 'S1' at the end of program execution.",
            replay: true);
        }

        [OnEventDoAction(typeof(E1), nameof(Process))]
        private class A10 : Actor
        {
            private void Process()
            {
            }

            protected override Task OnEventHandledAsync(Event e)
            {
                throw new InvalidOperationException();
            }

            protected override OnExceptionOutcome OnException(string methodName, Exception ex)
            {
                return OnExceptionOutcome.ThrowException;
            }

            protected override Task OnHaltAsync()
            {
                this.Monitor<Spec4>(new Done());
                return Task.CompletedTask;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestExceptionOnEventHandledWithThrowExceptionOutcomeInActor()
        {
            this.TestWithException<InvalidOperationException>(r =>
            {
                r.RegisterMonitor(typeof(Spec4));
                var m = r.CreateActor(typeof(A10));
                r.SendEvent(m, new E1());
            },
            configuration: GetConfiguration().WithNumberOfIterations(100),
            replay: true);
        }

        private class M10 : StateMachine
        {
            [Start]
            [OnEventDoAction(typeof(E1), nameof(Process))]
            private class S1 : State
            {
            }

            private void Process()
            {
            }

            protected override Task OnEventHandledAsync(Event e)
            {
                throw new InvalidOperationException();
            }

            protected override OnExceptionOutcome OnException(string methodName, Exception ex)
            {
                return OnExceptionOutcome.ThrowException;
            }

            protected override Task OnHaltAsync()
            {
                this.Monitor<Spec4>(new Done());
                return Task.CompletedTask;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestExceptionOnEventHandledWithThrowExceptionOutcomeInStateMachine()
        {
            this.TestWithException<InvalidOperationException>(r =>
            {
                r.RegisterMonitor(typeof(Spec4));
                var m = r.CreateActor(typeof(M10));
                r.SendEvent(m, new E1());
            },
            configuration: GetConfiguration().WithNumberOfIterations(100),
            replay: true);
        }
    }
}
