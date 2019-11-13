// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.Specifications;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests.Actors.StateMachines
{
    public class OnExceptionTest : BaseTest
    {
        public OnExceptionTest(ITestOutputHelper output)
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

        private class M1a : StateMachine
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

            protected override OnExceptionOutcome OnException(string method, Exception ex)
            {
                if (ex is Ex1)
                {
                    return OnExceptionOutcome.HandledException;
                }

                return OnExceptionOutcome.ThrowException;
            }
        }

        private class M1b : StateMachine
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

            protected override OnExceptionOutcome OnException(string method, Exception ex)
            {
                if (ex is Ex1)
                {
                    return OnExceptionOutcome.HandledException;
                }

                return OnExceptionOutcome.ThrowException;
            }
        }

        private class M1c : StateMachine
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

            protected override OnExceptionOutcome OnException(string method, Exception ex)
            {
                if (ex is Ex1)
                {
                    return OnExceptionOutcome.HandledException;
                }

                return OnExceptionOutcome.ThrowException;
            }
        }

        private class M1d : StateMachine
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

            protected override OnExceptionOutcome OnException(string method, Exception ex)
            {
                if (ex is Ex1)
                {
                    return OnExceptionOutcome.HandledException;
                }

                return OnExceptionOutcome.ThrowException;
            }
        }

        private class M2 : StateMachine
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

            protected override OnExceptionOutcome OnException(string method, Exception ex)
            {
                if (ex is Ex1)
                {
                    return OnExceptionOutcome.HandledException;
                }

                return OnExceptionOutcome.ThrowException;
            }
        }

        private class M3a : StateMachine
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
                this.Assert(false);
            }

            protected override OnExceptionOutcome OnException(string method, Exception ex)
            {
                if (ex is Ex1)
                {
                    this.RaiseEvent(new E(this.Id));
                    return OnExceptionOutcome.HandledException;
                }

                return OnExceptionOutcome.HandledException;
            }
        }

        private class M3b : StateMachine
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
                this.Assert(false);
            }

            protected override OnExceptionOutcome OnException(string method, Exception ex)
            {
                if (ex is Ex1)
                {
                    this.SendEvent(this.Id, new E(this.Id));
                    return OnExceptionOutcome.HandledException;
                }

                return OnExceptionOutcome.ThrowException;
            }
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

        private class M4 : StateMachine
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

            protected override OnExceptionOutcome OnException(string methodName, Exception ex)
            {
                return OnExceptionOutcome.Halt;
            }

            protected override Task OnHaltAsync()
            {
                this.Monitor<GetsDone>(new Done());
                return Task.CompletedTask;
            }
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
            }

            protected override OnExceptionOutcome OnException(string methodName, Exception ex)
            {
                if (ex is UnhandledEventException)
                {
                    return OnExceptionOutcome.Halt;
                }

                return OnExceptionOutcome.ThrowException;
            }

            protected override Task OnHaltAsync()
            {
                this.Monitor<GetsDone>(new Done());
                return Task.CompletedTask;
            }
        }

        private class M6 : StateMachine
        {
            [Start]
            private class Init : State
            {
            }

            protected override OnExceptionOutcome OnException(string method, Exception ex)
            {
                try
                {
                    this.Assert(ex is UnhandledEventException);
                    this.SendEvent(this.Id, new E(this.Id));
                    this.RaiseEvent(new E());
                }
                catch (Exception)
                {
                    this.Assert(false);
                }

                return OnExceptionOutcome.HandledException;
            }
        }

        [Fact(Timeout=5000)]
        public void TestExceptionSuppressed1()
        {
            this.Test(r =>
            {
                r.CreateActor(typeof(M1a));
            });
        }

        [Fact(Timeout=5000)]
        public void TestExceptionSuppressed2()
        {
            this.Test(r =>
            {
                r.CreateActor(typeof(M1b));
            });
        }

        [Fact(Timeout=5000)]
        public void TestExceptionSuppressed3()
        {
            this.Test(r =>
            {
                r.CreateActor(typeof(M1c));
            });
        }

        [Fact(Timeout=5000)]
        public void TestExceptionSuppressed4()
        {
            this.Test(r =>
            {
                r.CreateActor(typeof(M1d));
            });
        }

        [Fact(Timeout=5000)]
        public void TestExceptionNotSuppressed()
        {
            this.TestWithException<Ex2>(r =>
            {
                r.CreateActor(typeof(M2));
            },
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestRaiseOnException()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M3a));
            },
            expectedError: "Detected an assertion failure.",
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestSendOnException()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M3b));
            },
            expectedError: "Detected an assertion failure.",
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestMachineHalt1()
        {
            this.Test(r =>
            {
                r.RegisterMonitor(typeof(GetsDone));
                r.CreateActor(typeof(M4));
            });
        }

        [Fact(Timeout=5000)]
        public void TestMachineHalt2()
        {
            this.Test(r =>
            {
                r.RegisterMonitor(typeof(GetsDone));
                var m = r.CreateActor(typeof(M5));
                r.SendEvent(m, new E());
            });
        }

        [Fact(Timeout=5000)]
        public void TestSendOnUnhandledEventException()
        {
            this.Test(r =>
            {
                var m = r.CreateActor(typeof(M6));
                r.SendEvent(m, new E());
            });
        }
    }
}
