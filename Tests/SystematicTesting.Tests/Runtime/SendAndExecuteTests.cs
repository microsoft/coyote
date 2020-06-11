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
                    // this will deadlock since our await stops the SendEvent at the bottom of this method which ReceiveEventAsync is waiting for.
                    var op = new Operation<bool>();
                    b = this.CreateActor(typeof(M1B), null, op);
                    await op.Completion.Task;
                }
                else
                {
                    b = this.CreateActor(typeof(M1B));
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
                ((Operation<bool>)this.CurrentOperation).SetResult(true);
            }
        }

        //------------------------------------------------------------------------------------------------------------
        [Fact(Timeout = 5000)]
        public void TestOperationNoDeadlockWithReceive()
        {
            this.Test(r =>
            {
                r.CreateActor(typeof(M1A), new ExecuteSynchronouslySetupEvent(false));
            });
        }

        //------------------------------------------------------------------------------------------------------------
        [Fact(Timeout = 5000)]
        public void TestOperationDeadlockWithReceive()
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
                var op = new Operation<bool>();
                this.SendEvent(b, new E1(), op);
                var handled = await op.Completion.Task;
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
                ((Operation<bool>)this.CurrentOperation).SetResult(true);
            }
        }

        //------------------------------------------------------------------------------------------------------------
        [Fact(Timeout = 5000)]
        public void TestSyncSendToReceive()
        {
            this.Test(r =>
            {
                r.CreateActor(typeof(M2A));
            },
            configuration: Configuration.Create().WithTestingIterations(200));
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
                var op = new Operation<bool>();
                this.SendEvent(d, new E1(), op);
                var handled = await op.Completion.Task;
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
                ((Operation<bool>)this.CurrentOperation).SetResult(true);
            }
        }

        //------------------------------------------------------------------------------------------------------------
        [Fact(Timeout = 5000)]
        public void TestOperationCompletedTwiceError()
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

        private static Guid Stage1Operation = Guid.NewGuid();
        private static Guid Stage2Operation = Guid.NewGuid();

        private class M3A : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private async Task InitOnEntry()
            {
                var op = new Operation<bool>() { Id = Stage1Operation };
                var m = this.CreateActor(typeof(M3B), null, op);
                await op.Completion.Task;

                op = new Operation<bool>() { Id = Stage2Operation };
                var e = new E4();
                this.SendEvent(m, e, op);
                var handled = await op.Completion.Task;
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
                // stage 1 complete
                var op = this.CurrentOperation as Operation<bool>;
                this.Assert(op.Id == Stage1Operation);
                op.SetResult(true);
            }

            private void HandleEventE4(Event e)
            {
                this.Assert(this.E1Handled);
                (e as E4).X = 1;
                // stage 2 complete.
                var op = this.CurrentOperation as Operation<bool>;
                this.Assert(op.Id == Stage2Operation);
                op.SetResult(true);
            }
        }

        //------------------------------------------------------------------------------------------------------------
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
            [OnEventDoAction(typeof(E1), nameof(HandleE1))]
            private class Init : State
            {
            }

            private async Task InitOnEntry()
            {
                var op = new Operation<bool>();
                var m = this.CreateActor(typeof(M4B), new E2(this.Id), op);
                // this will cause a deadlock since we are expecting M4B to be able
                // to call us back, but we can't handle the E1 even because we are
                // waiting for the operation to complete here.
                await op.Completion.Task;
            }

            private void HandleE1()
            {
                var op = this.CurrentOperation as Operation<bool>;
                op.SetResult(true);
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
                var op = this.CurrentOperation as Operation<bool>;
                var creator = (e as E2).Id;
                this.SendEvent(creator, new E1());
                var handled = await op.Completion.Task;
                this.Assert(!handled);
            }
        }

        //------------------------------------------------------------------------------------------------------------
        [Fact(Timeout = 5000)]
        public void TestOperationCreateSendCycleDeadlocks()
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
                var op = new Operation<bool>();
                var m = this.CreateActor(typeof(M5B), null, op);
                this.SendEvent(m, new E1());
                var handled = await op.Completion.Task;
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
                var op = this.CurrentOperation as Operation<bool>;
                op.SetResult(true);
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

        //------------------------------------------------------------------------------------------------------------
        [Fact(Timeout = 5000)]
        public void TestOperationInHaltedMachine()
        {
            this.Test(r =>
            {
                // an Operation can be completed in OnHaltAsync
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
                var op = new Operation<bool>();
                var m = this.CreateActor(typeof(M6B), e, op);
                this.SendEvent(m, new E1());
                var handled = await op.Completion.Task;
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
                var op = this.CurrentOperation as Operation<bool>;
                op.SetResult(true);

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

        //------------------------------------------------------------------------------------------------------------
        [Fact(Timeout = 5000)]
        public void TestHandledExceptionOnSendExec()
        {
            this.Test(r =>
            {
                // tests an Operation can be completed in an unhandled exception handler.
                r.RegisterMonitor<M6SafetyMonitor>();
                r.CreateActor(typeof(M6A), new HandleExceptionSetupEvent(true));
            },
            configuration: Configuration.Create().WithTestingIterations(100));
        }

        //------------------------------------------------------------------------------------------------------------
        [Fact(Timeout = 5000)]
        public void TestUnhandledExceptionOnSendExec()
        {
            this.TestWithException<InvalidOperationException>(r =>
            {
                // tests an Operation can be completed in an unhandled exception handler.
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
                var op = new Operation<bool>();
                var m = this.CreateActor(typeof(M7B), null, op);
                this.SendEvent(m, new E1());  // unhandled event!
                var handled = await op.Completion.Task;
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

        //------------------------------------------------------------------------------------------------------------
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
                var op = new Operation<bool>();
                var m = this.CreateActor(typeof(M8B), null, op);
                this.SendEvent(m, new E1()); // indirect unhandled event E3
                var handled = await op.Completion.Task;
                this.Assert(handled);
            }
        }

        private class M8B : StateMachine
        {
            [Start]
            [OnEventDoAction(typeof(E1), nameof(Handle))]
            private class Init : State
            {
            }

            private void Handle() => this.RaiseEvent(new E3());
        }

        //------------------------------------------------------------------------------------------------------------
        [Fact(Timeout = 5000)]
        public void TestUnhandledEventOnSendExec2()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M8A));
            },
            expectedError: "M8B() received event 'E3' that cannot be handled.",
            replay: true);
        }
    }
}
