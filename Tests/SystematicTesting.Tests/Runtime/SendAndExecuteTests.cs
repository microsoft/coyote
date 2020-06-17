// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Specifications;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.SystematicTesting.Tests.Runtime
{
    public class SendAndExecuteTests : BaseSystematicTest
    {
        public SendAndExecuteTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private class ExecuteSynchronouslySetupEvent : Event
        {
            public bool ExecuteSynchronously;

            public ExecuteSynchronouslySetupEvent(bool executeSynchronously)
            {
                this.ExecuteSynchronously = executeSynchronously;
            }
        }

        private class E1 : Event
        {
        }

        private class E2 : Event
        {
            public ActorId Id;

            public E2(ActorId id)
            {
                this.Id = id;
            }
        }

        private class E3 : Event
        {
        }

        private class M1A : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private async Task InitOnEntry(Event e)
            {
                ActorId b;
                if ((e as ExecuteSynchronouslySetupEvent).ExecuteSynchronously)
                {
                    b = await this.Runtime.CreateActorAndExecuteAsync(typeof(M1B));
                }
                else
                {
                    b = this.Runtime.CreateActor(typeof(M1B));
                }

                this.SendEvent(b, new E1());
            }
        }

        private class M1B : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private async Task InitOnEntry()
            {
                await this.ReceiveEventAsync(typeof(E1));
            }
        }

        [Fact(Timeout = 5000)]
        public void TestSendAndExecuteNoDeadlockWithReceive()
        {
            this.Test(r =>
            {
                r.CreateActor(typeof(M1A), new ExecuteSynchronouslySetupEvent(false));
            });
        }

        [Fact(Timeout = 5000)]
        public void TestSendAndExecuteDeadlockWithReceive()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M1A), new ExecuteSynchronouslySetupEvent(true));
            },
            configuration: Configuration.Create().WithTestingIterations(10),
            expectedError: "Deadlock detected. M1A() and M1B() are waiting to receive " +
                "an event, but no other controlled tasks are enabled.",
            replay: true);
        }

        private class M2A : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private async Task InitOnEntry()
            {
                var b = this.CreateActor(typeof(M2B));
                var handled = await this.Runtime.SendEventAndExecuteAsync(b, new E1());
                this.Assert(!handled);
            }
        }

        private class M2B : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private async Task InitOnEntry()
            {
                await this.ReceiveEventAsync(typeof(E1));
            }
        }

        private class M2C : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private async Task InitOnEntry()
            {
                var d = this.CreateActor(typeof(M2D));
                var handled = await this.Runtime.SendEventAndExecuteAsync(d, new E1());
                this.Assert(handled);
            }
        }

        private class M2D : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E1), nameof(Handle))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.SendEvent(this.Id, new E1());
            }

            private void Handle()
            {
            }
        }

        [Fact(Timeout = 5000)]
        public void TestSyncSendToReceive()
        {
            this.Test(r =>
            {
                r.CreateActor(typeof(M2A));
            },
            configuration: Configuration.Create().WithTestingIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestSyncSendSometimesDoesNotHandle()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M2C));
            },
            configuration: Configuration.Create().WithTestingIterations(200),
            expectedError: "Detected an assertion failure.",
            replay: true);
        }

        private class E4 : Event
        {
            public int X;

            public E4()
            {
                this.X = 0;
            }
        }

        private class M3A : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private async Task InitOnEntry()
            {
                var e = new E4();
                var m = await this.Runtime.CreateActorAndExecuteAsync(typeof(M3B));
                var handled = await this.Runtime.SendEventAndExecuteAsync(m, e);
                this.Assert(handled);
                this.Assert(e.X == 1);
            }
        }

        private class M3B : StateMachine
        {
            private bool E1Handled = false;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E1), nameof(HandleEventE1))]
            [OnEventDoAction(typeof(E4), nameof(HandleEventE4))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.SendEvent(this.Id, new E1());
            }

            private void HandleEventE1()
            {
                this.E1Handled = true;
            }

            private void HandleEventE4(Event e)
            {
                this.Assert(this.E1Handled);
                (e as E4).X = 1;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestSendBlocks()
        {
            this.Test(r =>
            {
                r.CreateActor(typeof(M3A));
            },
            configuration: Configuration.Create().WithTestingIterations(100));
        }

        private class M4A : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [IgnoreEvents(typeof(E1))]
            private class Init : State
            {
            }

            private async Task InitOnEntry()
            {
                var m = await this.Runtime.CreateActorAndExecuteAsync(typeof(M4B), new E2(this.Id));
                var handled = await this.Runtime.SendEventAndExecuteAsync(m, new E1());
                this.Assert(handled);
            }
        }

        private class M4B : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [IgnoreEvents(typeof(E1))]
            private class Init : State
            {
            }

            private async Task InitOnEntry(Event e)
            {
                var creator = (e as E2).Id;
#pragma warning disable CS0618 // Type or member is obsolete
                var handled = await this.Id.Runtime.SendEventAndExecuteAsync(creator, new E1());
#pragma warning restore CS0618 // Type or member is obsolete
                this.Assert(!handled);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestSendCycleDoesNotDeadlock()
        {
            this.Test(r =>
            {
                r.CreateActor(typeof(M4A));
            },
            configuration: Configuration.Create().WithTestingIterations(100));
        }

        private class M5A : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private async Task InitOnEntry()
            {
                var m = await this.Runtime.CreateActorAndExecuteAsync(typeof(M5B));
                var handled = await this.Runtime.SendEventAndExecuteAsync(m, new E1());
                this.Monitor<M5SafetyMonitor>(new SEReturns());
                this.Assert(handled);
            }
        }

        private class M5B : StateMachine
        {
            [Start]
            [OnEventDoAction(typeof(E1), nameof(HandleE))]
            private class Init : State
            {
            }

            private void HandleE() => this.RaiseHaltEvent();

            protected override Task OnHaltAsync(Event e)
            {
                this.Monitor<M5SafetyMonitor>(new MHalts());
                return Task.CompletedTask;
            }
        }

        private class MHalts : Event
        {
        }

        private class SEReturns : Event
        {
        }

        private class M5SafetyMonitor : Monitor
        {
            private bool MHalted = false;
            private bool SEReturned = false;

            [Start]
            [Hot]
            [OnEventDoAction(typeof(MHalts), nameof(OnMHalts))]
            [OnEventDoAction(typeof(SEReturns), nameof(OnSEReturns))]
            private class Init : State
            {
            }

            [Cold]
            private class Done : State
            {
            }

            private void OnMHalts()
            {
                this.Assert(this.SEReturned == false);
                this.MHalted = true;
            }

            private void OnSEReturns()
            {
                this.Assert(this.MHalted);
                this.SEReturned = true;
                this.RaiseGotoStateEvent<Done>();
            }
        }

        [Fact(Timeout = 5000)]
        public void TestMachineHaltsOnSendExec()
        {
            this.Test(r =>
            {
                r.RegisterMonitor<M5SafetyMonitor>();
                r.CreateActor(typeof(M5A));
            },
            configuration: Configuration.Create().WithTestingIterations(100));
        }

        private class HandleExceptionSetupEvent : Event
        {
            public bool HandleException;

            public HandleExceptionSetupEvent(bool handleEx)
            {
                this.HandleException = handleEx;
            }
        }

        private class M6A : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private async Task InitOnEntry(Event e)
            {
                var m = await this.Runtime.CreateActorAndExecuteAsync(typeof(M6B), e);
                var handled = await this.Runtime.SendEventAndExecuteAsync(m, new E1());
                this.Monitor<M6SafetyMonitor>(new SEReturns());
                this.Assert(handled);
            }

            protected override OnExceptionOutcome OnException(Exception ex, string methodName, Event e)
            {
                this.Assert(false);
                return OnExceptionOutcome.ThrowException;
            }
        }

        private class M6B : StateMachine
        {
            private bool HandleException = false;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E1), nameof(HandleE))]
            private class Init : State
            {
            }

            private void InitOnEntry(Event e)
            {
                this.HandleException = (e as HandleExceptionSetupEvent).HandleException;
            }

            private void HandleE()
            {
                throw new InvalidOperationException();
            }

            protected override OnExceptionOutcome OnException(Exception ex, string methodName, Event e)
            {
                if (this.HandleException)
                {
                    return OnExceptionOutcome.HandledException;
                }

                return OnExceptionOutcome.ThrowException;
            }
        }

        private class M6SafetyMonitor : Monitor
        {
            [Start]
            [Hot]
            [OnEventGotoState(typeof(SEReturns), typeof(Done))]
            private class Init : State
            {
            }

            [Cold]
            private class Done : State
            {
            }
        }

        [Fact(Timeout = 5000)]
        public void TestHandledExceptionOnSendExec()
        {
            this.Test(r =>
            {
                r.RegisterMonitor<M6SafetyMonitor>();
                r.CreateActor(typeof(M6A), new HandleExceptionSetupEvent(true));
            },
            configuration: Configuration.Create().WithTestingIterations(100));
        }

        [Fact(Timeout = 5000)]
        public void TestUnhandledExceptionOnSendExec()
        {
            this.TestWithException<InvalidOperationException>(r =>
            {
                r.RegisterMonitor<M6SafetyMonitor>();
                r.CreateActor(typeof(M6A), new HandleExceptionSetupEvent(false));
            },
            replay: true);
        }

        private class M7A : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private async Task InitOnEntry()
            {
                var m = await this.Runtime.CreateActorAndExecuteAsync(typeof(M7B));
                var handled = await this.Runtime.SendEventAndExecuteAsync(m, new E1());
                this.Assert(handled);
            }
        }

        private class M7B : StateMachine
        {
            [Start]
            private class Init : State
            {
            }
        }

        [Fact(Timeout = 5000)]
        public void TestUnhandledEventOnSendExec1()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M7A));
            },
            expectedError: "M7B() received event 'E1' that cannot be handled.",
            replay: true);
        }

        private class M8A : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private async Task InitOnEntry()
            {
                var m = await this.Runtime.CreateActorAndExecuteAsync(typeof(M8B));
                var handled = await this.Runtime.SendEventAndExecuteAsync(m, new E1());
                this.Assert(handled);
            }
        }

        private class M8B : StateMachine
        {
            [Start]
            [OnEventDoAction(typeof(E1), nameof(Handle))]
            [IgnoreEvents(typeof(E3))]
            private class Init : State
            {
            }

            private void Handle() => this.RaiseEvent(new E3());
        }

        [Fact(Timeout = 5000)]
        public void TestUnhandledEventOnSendExec2()
        {
            this.Test(r =>
            {
                r.CreateActor(typeof(M8A));
            });
        }
    }
}
