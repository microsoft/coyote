// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.Specifications;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests
{
    public class OnEventDequeueOrHandledTest : BaseTest
    {
        public OnEventDequeueOrHandledTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class E : Event
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
            public Event Ev;

            public Begin(Event ev)
            {
                this.Ev = ev;
            }
        }

        private class End : Event
        {
            public Event Ev;

            public End(Event ev)
            {
                this.Ev = ev;
            }
        }

        private class Done : Event
        {
        }

        // Ensures that machine M1 sees the following calls:
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
                if (this.counter == 0 && this.ReceivedEvent is Begin && (this.ReceivedEvent as Begin).Ev is E1)
                {
                    this.counter++;
                }
                else if (this.counter == 1 && this.ReceivedEvent is End && (this.ReceivedEvent as End).Ev is E1)
                {
                    this.counter++;
                }
                else if (this.counter == 2 && this.ReceivedEvent is Begin && (this.ReceivedEvent as Begin).Ev is E2)
                {
                    this.counter++;
                }
                else if (this.counter == 3 && this.ReceivedEvent is End && (this.ReceivedEvent as End).Ev is E2)
                {
                    this.counter++;
                }
                else
                {
                    this.Assert(false);
                }

                if (this.counter == 4)
                {
                    this.Goto<S2>();
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
                this.Raise(new E3());
            }

            private void ProcessE3()
            {
            }

            protected override Task OnEventDequeueAsync(Event ev)
            {
                this.Monitor<Spec1>(new Begin(ev));
                return Task.CompletedTask;
            }

            protected override Task OnEventHandledAsync(Event ev)
            {
                this.Monitor<Spec1>(new End(ev));
                return Task.CompletedTask;
            }
        }

        [Fact(Timeout=5000)]
        public void TestOnProcessingCalled()
        {
            this.Test(r =>
            {
                r.RegisterMonitor(typeof(Spec1));
                var m = r.CreateMachine(typeof(M1), new E());
                r.SendEvent(m, new E1());
                r.SendEvent(m, new E2());
            });
        }

        // Ensures that machine M2 sees the following calls:
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
                if (this.counter == 0 && this.ReceivedEvent is Begin && (this.ReceivedEvent as Begin).Ev is E1)
                {
                    this.counter++;
                }
                else
                {
                    this.Assert(false);
                }

                if (this.counter == 1)
                {
                    this.Goto<S2>();
                }
            }
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
                this.Raise(new Halt());
            }

            protected override Task OnEventDequeueAsync(Event ev)
            {
                this.Monitor<Spec2>(new Begin(ev));
                return Task.CompletedTask;
            }

            protected override Task OnEventHandledAsync(Event ev)
            {
                this.Monitor<Spec2>(new End(ev));
                return Task.CompletedTask;
            }
        }

        [Fact(Timeout=5000)]
        public void TestOnProcessingNotCalledOnHalt()
        {
            this.Test(r =>
            {
                r.RegisterMonitor(typeof(Spec2));
                var m = r.CreateMachine(typeof(M2));
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
                this.Goto<S2>();
            }

            private void Finish()
            {
                this.Monitor<Spec3>(new Done());
            }

            protected override Task OnEventHandledAsync(Event ev)
            {
                this.Assert(ev is E1);
                this.Assert(this.CurrentState.Name == typeof(S2).Name);
                this.Goto<S3>();
                return Task.CompletedTask;
            }
        }

        [Fact(Timeout=5000)]
        public void TestOnProcessingCanGoto()
        {
            this.Test(r =>
            {
                r.RegisterMonitor(typeof(Spec3));
                var m = r.CreateMachine(typeof(M3));
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

            protected override Task OnEventHandledAsync(Event ev)
            {
                this.Raise(new Halt());
                return Task.CompletedTask;
            }

            protected override void OnHalt()
            {
                this.Monitor<Spec4>(new Done());
            }
        }

        [Fact(Timeout=5000)]
        public void TestOnProcessingCanHalt()
        {
            this.Test(r =>
            {
                r.RegisterMonitor(typeof(Spec4));
                var m = r.CreateMachine(typeof(M4));
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
                return OnExceptionOutcome.HaltMachine;
            }

            protected override void OnHalt()
            {
                this.Monitor<Spec4>(new Done());
            }
        }

        [Fact(Timeout = 5000)]
        public void TestExceptionOnEventDequeueWithHaltMachineOutcome()
        {
            this.Test(r =>
            {
                r.RegisterMonitor(typeof(Spec4));
                var m = r.CreateMachine(typeof(M5));
                r.SendEvent(m, new E1());
                r.SendEvent(m, new E2()); // Dropped silently.
            });
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

            protected override void OnHalt()
            {
                this.Monitor<Spec4>(new Done());
            }
        }

        [Fact(Timeout = 5000)]
        public void TestExceptionOnEventDequeueWithHandledExceptionOutcome()
        {
            this.TestWithError(r =>
            {
                r.RegisterMonitor(typeof(Spec4));
                var m = r.CreateMachine(typeof(M6));
                r.SendEvent(m, new E1());
            },
            configuration: GetConfiguration().WithNumberOfIterations(100),
            expectedError: "Monitor 'Spec4' detected liveness bug in hot state 'S1' at the end of program execution.",
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

            protected override void OnHalt()
            {
                this.Monitor<Spec4>(new Done());
            }
        }

        [Fact(Timeout = 5000)]
        public void TestExceptionOnEventDequeueWithThrowExceptionOutcome()
        {
            this.TestWithException<InvalidOperationException>(r =>
            {
                r.RegisterMonitor(typeof(Spec4));
                var m = r.CreateMachine(typeof(M7));
                r.SendEvent(m, new E1());
            },
            configuration: GetConfiguration().WithNumberOfIterations(100),
            replay: true);
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
                return OnExceptionOutcome.HaltMachine;
            }

            protected override void OnHalt()
            {
                this.Monitor<Spec4>(new Done());
            }
        }

        [Fact(Timeout = 5000)]
        public void TestExceptionOnEventHandledWithHaltMachineOutcome()
        {
            this.Test(r =>
            {
                r.RegisterMonitor(typeof(Spec4));
                var m = r.CreateMachine(typeof(M8));
                r.SendEvent(m, new E1());
                r.SendEvent(m, new E2()); // Dropped silently.
            });
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

            protected override void OnHalt()
            {
                this.Monitor<Spec4>(new Done());
            }
        }

        [Fact(Timeout = 5000)]
        public void TestExceptionOnEventHandledWithHandledExceptionOutcome()
        {
            this.TestWithError(r =>
            {
                r.RegisterMonitor(typeof(Spec4));
                var m = r.CreateMachine(typeof(M9));
                r.SendEvent(m, new E1());
            },
            configuration: GetConfiguration().WithNumberOfIterations(100),
            expectedError: "Monitor 'Spec4' detected liveness bug in hot state 'S1' at the end of program execution.",
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

            protected override void OnHalt()
            {
                this.Monitor<Spec4>(new Done());
            }
        }

        [Fact(Timeout = 5000)]
        public void TestExceptionOnEventHandledWithThrowExceptionOutcome()
        {
            this.TestWithException<InvalidOperationException>(r =>
            {
                r.RegisterMonitor(typeof(Spec4));
                var m = r.CreateMachine(typeof(M10));
                r.SendEvent(m, new E1());
            },
            configuration: GetConfiguration().WithNumberOfIterations(100),
            replay: true);
        }
    }
}
