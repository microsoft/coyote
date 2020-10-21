// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Coyote.Specifications;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Actors.SystematicTesting.Tests
{
    public class OnExceptionTests : BaseSystematicActorTest
    {
        public OnExceptionTests(ITestOutputHelper output)
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

        private class Ex1 : Exception
        {
        }

        private class Ex2 : Exception
        {
        }

        private class A1 : Actor
        {
            protected override Task OnInitializeAsync(Event initialEvent)
            {
                throw new Ex1();
            }

            protected override OnExceptionOutcome OnException(Exception ex, string methodName, Event e)
            {
                if (ex is Ex1)
                {
                    return OnExceptionOutcome.HandledException;
                }

                return OnExceptionOutcome.ThrowException;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestExceptionSuppressedDuringInitializationInActor()
        {
            this.Test(r =>
            {
                r.CreateActor(typeof(A1));
            });
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
                throw new Ex1();
            }

            protected override OnExceptionOutcome OnException(Exception ex, string methodName, Event e)
            {
                if (ex is Ex1)
                {
                    return OnExceptionOutcome.HandledException;
                }

                return OnExceptionOutcome.ThrowException;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestExceptionSuppressedInEntryActionInStateMachine()
        {
            this.Test(r =>
            {
                r.CreateActor(typeof(M1));
            });
        }

        [OnEventDoAction(typeof(E), nameof(Act))]
        private class A2 : Actor
        {
            protected override Task OnInitializeAsync(Event initialEvent)
            {
                this.SendEvent(this.Id, new E(this.Id));
                return Task.CompletedTask;
            }

            private void Act()
            {
                throw new Ex1();
            }

            protected override OnExceptionOutcome OnException(Exception ex, string methodName, Event e)
            {
                if (ex is Ex1)
                {
                    return OnExceptionOutcome.HandledException;
                }

                return OnExceptionOutcome.ThrowException;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestExceptionSuppressedInActionInActor()
        {
            this.Test(r =>
            {
                r.CreateActor(typeof(A2));
            });
        }

        private class M2 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E), nameof(Act))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.RaiseEvent(new E(this.Id));
            }

            private void Act()
            {
                throw new Ex1();
            }

            protected override OnExceptionOutcome OnException(Exception ex, string methodName, Event e)
            {
                if (ex is Ex1)
                {
                    return OnExceptionOutcome.HandledException;
                }

                return OnExceptionOutcome.ThrowException;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestExceptionSuppressedInActionInStateMachine()
        {
            this.Test(r =>
            {
                r.CreateActor(typeof(M2));
            });
        }

        [OnEventDoAction(typeof(E), nameof(Act))]
        private class A3 : Actor
        {
            protected override Task OnInitializeAsync(Event initialEvent)
            {
                this.SendEvent(this.Id, new E(this.Id));
                return Task.CompletedTask;
            }

            private async Task Act()
            {
                await Task.Delay(0);
                throw new Ex1();
            }

            protected override OnExceptionOutcome OnException(Exception ex, string methodName, Event e)
            {
                if (ex is Ex1)
                {
                    return OnExceptionOutcome.HandledException;
                }

                return OnExceptionOutcome.ThrowException;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestExceptionSuppressedIAfterAwaitInActor()
        {
            this.Test(r =>
            {
                r.CreateActor(typeof(A3));
            });
        }

        private class M3 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E), nameof(Act))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.RaiseEvent(new E(this.Id));
            }

            private async Task Act()
            {
                await Task.Delay(0);
                throw new Ex1();
            }

            protected override OnExceptionOutcome OnException(Exception ex, string methodName, Event e)
            {
                if (ex is Ex1)
                {
                    return OnExceptionOutcome.HandledException;
                }

                return OnExceptionOutcome.ThrowException;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestExceptionSuppressedIAfterAwaitInStateMachine()
        {
            this.Test(r =>
            {
                r.CreateActor(typeof(M3));
            });
        }

        private class M4 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventGotoState(typeof(E), typeof(Done))]
            [OnExit(nameof(InitOnExit))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.RaiseEvent(new E(this.Id));
            }

            private void InitOnExit()
            {
                throw new Ex1();
            }

            private class Done : State
            {
            }

            protected override OnExceptionOutcome OnException(Exception ex, string methodName, Event e)
            {
                if (ex is Ex1)
                {
                    return OnExceptionOutcome.HandledException;
                }

                return OnExceptionOutcome.ThrowException;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestExceptionSuppressedInExitActionInStateMachine()
        {
            this.Test(r =>
            {
                r.CreateActor(typeof(M4));
            });
        }

        private class A5 : Actor
        {
            protected override Task OnInitializeAsync(Event initialEvent)
            {
                throw new Ex2();
            }

            protected override OnExceptionOutcome OnException(Exception ex, string methodName, Event e)
            {
                if (ex is Ex1)
                {
                    return OnExceptionOutcome.HandledException;
                }

                return OnExceptionOutcome.ThrowException;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestExceptionNotSuppressedDuringInitializationInActor()
        {
            this.TestWithException<Ex2>(r =>
            {
                r.CreateActor(typeof(A5));
            },
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
                throw new Ex2();
            }

            protected override OnExceptionOutcome OnException(Exception ex, string methodName, Event e)
            {
                if (ex is Ex1)
                {
                    return OnExceptionOutcome.HandledException;
                }

                return OnExceptionOutcome.ThrowException;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestExceptionNotSuppressedInEntryActionInStateMachine()
        {
            this.TestWithException<Ex2>(r =>
            {
                r.CreateActor(typeof(M5));
            },
            replay: true);
        }

        [OnEventDoAction(typeof(E), nameof(Act))]
        private class A6 : Actor
        {
            protected override Task OnInitializeAsync(Event initialEvent)
            {
                throw new Ex1();
            }

            private void Act()
            {
                this.Assert(false, "Reached test assertion.");
            }

            protected override OnExceptionOutcome OnException(Exception ex, string methodName, Event e)
            {
                if (ex is Ex1)
                {
                    this.SendEvent(this.Id, new E(this.Id));
                    return OnExceptionOutcome.HandledException;
                }

                return OnExceptionOutcome.ThrowException;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestSendDuringOnExceptionInActor()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(A6));
            },
            expectedError: "Reached test assertion.",
            replay: true);
        }

        private class M6 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E), nameof(Act))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                throw new Ex1();
            }

            private void Act()
            {
                this.Assert(false, "Reached test assertion.");
            }

            protected override OnExceptionOutcome OnException(Exception ex, string methodName, Event e)
            {
                if (ex is Ex1)
                {
                    this.SendEvent(this.Id, new E(this.Id));
                    return OnExceptionOutcome.HandledException;
                }

                return OnExceptionOutcome.ThrowException;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestSendDuringOnExceptionInStateMachine()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M6));
            },
            expectedError: "Reached test assertion.",
            replay: true);
        }

        private class Done : Event
        {
        }

        private class GetsDone : Monitor
        {
            [Start]
            [Hot]
            [OnEventGotoState(typeof(Done), typeof(Ok))]
            private class Init : State
            {
            }

            [Cold]
            private class Ok : State
            {
            }
        }

        private class A7 : Actor
        {
            protected override Task OnInitializeAsync(Event initialEvent)
            {
                throw new NotImplementedException();
            }

            protected override OnExceptionOutcome OnException(Exception ex, string methodName, Event e)
            {
                return OnExceptionOutcome.Halt;
            }

            protected override Task OnHaltAsync(Event e)
            {
                this.Monitor<GetsDone>(new Done());
                return Task.CompletedTask;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestHaltOnUnhandledExceptionInActor()
        {
            this.Test(r =>
            {
                r.RegisterMonitor<GetsDone>();
                r.CreateActor(typeof(A7));
            });
        }

        private class M7 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                throw new NotImplementedException();
            }

            protected override OnExceptionOutcome OnException(Exception ex, string methodName, Event e)
            {
                return OnExceptionOutcome.Halt;
            }

            protected override Task OnHaltAsync(Event e)
            {
                this.Monitor<GetsDone>(new Done());
                return Task.CompletedTask;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestHaltOnUnhandledExceptionInStateMachine()
        {
            this.Test(r =>
            {
                r.RegisterMonitor<GetsDone>();
                r.CreateActor(typeof(M7));
            });
        }

        private class A8 : Actor
        {
            protected override OnExceptionOutcome OnException(Exception ex, string methodName, Event e)
            {
                if (ex is UnhandledEventException)
                {
                    return OnExceptionOutcome.Halt;
                }

                return OnExceptionOutcome.ThrowException;
            }

            protected override Task OnHaltAsync(Event e)
            {
                this.Monitor<GetsDone>(new Done());
                return Task.CompletedTask;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestHaltOnUnhandledEventExceptionInActor()
        {
            this.Test(r =>
            {
                r.RegisterMonitor<GetsDone>();
                var m = r.CreateActor(typeof(A8));
                r.SendEvent(m, new E());
            });
        }

        private class M8 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
            }

            protected override OnExceptionOutcome OnException(Exception ex, string methodName, Event e)
            {
                if (ex is UnhandledEventException)
                {
                    return OnExceptionOutcome.Halt;
                }

                return OnExceptionOutcome.ThrowException;
            }

            protected override Task OnHaltAsync(Event e)
            {
                this.Monitor<GetsDone>(new Done());
                return Task.CompletedTask;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestHaltOnUnhandledEventExceptionInStateMachine()
        {
            this.Test(r =>
            {
                r.RegisterMonitor<GetsDone>();
                var m = r.CreateActor(typeof(M8));
                r.SendEvent(m, new E());
            });
        }

        private class A9 : Actor
        {
            protected override OnExceptionOutcome OnException(Exception ex, string methodName, Event e)
            {
                try
                {
                    this.Assert(ex is UnhandledEventException);
                    this.SendEvent(this.Id, new E(this.Id));
                    this.SendEvent(this.Id, new E());
                }
                catch (Exception)
                {
                    this.Assert(false);
                }

                return OnExceptionOutcome.HandledException;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestSendOnUnhandledEventExceptionInActor()
        {
            this.Test(r =>
            {
                var m = r.CreateActor(typeof(A9));
                r.SendEvent(m, new E());
            });
        }

        private class M9 : StateMachine
        {
            [Start]
            private class Init : State
            {
            }

            protected override OnExceptionOutcome OnException(Exception ex, string methodName, Event e)
            {
                try
                {
                    this.Assert(ex is UnhandledEventException);
                    this.SendEvent(this.Id, new E(this.Id));
                    this.SendEvent(this.Id, new E());
                }
                catch (Exception)
                {
                    this.Assert(false);
                }

                return OnExceptionOutcome.HandledException;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestSendOnUnhandledEventExceptionInStateMachine()
        {
            this.Test(r =>
            {
                var m = r.CreateActor(typeof(M9));
                r.SendEvent(m, new E());
            });
        }
    }
}
