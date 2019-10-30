// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Coyote.Machines;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.Specifications;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests
{
    public class SendAndExecuteTest : BaseTest
    {
        public SendAndExecuteTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class Configure : Event
        {
            public bool ExecuteSynchronously;

            public Configure(bool executeSynchronously)
            {
                this.ExecuteSynchronously = executeSynchronously;
            }
        }

        private class E1 : Event
        {
        }

        private class E2 : Event
        {
            public MachineId Id;

            public E2(MachineId id)
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
            private class Init : MachineState
            {
            }

            private async Task InitOnEntry()
            {
                var e = this.ReceivedEvent as Configure;
                MachineId b;

                if (e.ExecuteSynchronously)
                {
                     b = await this.Runtime.CreateMachineAndExecuteAsync(typeof(M1B));
                }
                else
                {
                    b = this.Runtime.CreateMachine(typeof(M1B));
                }

                this.Send(b, new E1());
            }
        }

        private class M1B : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private async Task InitOnEntry()
            {
                await this.Receive(typeof(E1));
            }
        }

        [Fact(Timeout=5000)]
        public void TestSendAndExecuteNoDeadlockWithReceive()
        {
            this.Test(r =>
            {
                r.CreateMachine(typeof(M1A), new Configure(false));
            });
        }

        [Fact(Timeout=5000)]
        public void TestSendAndExecuteDeadlockWithReceive()
        {
            this.TestWithError(r =>
            {
                r.CreateMachine(typeof(M1A), new Configure(true));
            },
            configuration: Configuration.Create().WithNumberOfIterations(10),
            expectedError: "Deadlock detected. 'M1A()' and 'M1B()' are waiting to receive " +
                "an event, but no other controlled tasks are enabled.",
            replay: true);
        }

        private class M2A : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private async Task InitOnEntry()
            {
                var b = this.CreateMachine(typeof(M2B));
                var handled = await this.Runtime.SendEventAndExecuteAsync(b, new E1());
                this.Assert(!handled);
            }
        }

        private class M2B : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private async Task InitOnEntry()
            {
                await this.Receive(typeof(E1));
            }
        }

        private class M2C : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private async Task InitOnEntry()
            {
                var d = this.CreateMachine(typeof(M2D));
                var handled = await this.Runtime.SendEventAndExecuteAsync(d, new E1());
                this.Assert(handled);
            }
        }

        private class M2D : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E1), nameof(Handle))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Send(this.Id, new E1());
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
                r.CreateMachine(typeof(M2A));
            },
            configuration: Configuration.Create().WithNumberOfIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestSyncSendSometimesDoesNotHandle()
        {
            this.TestWithError(r =>
            {
                r.CreateMachine(typeof(M2C));
            },
            configuration: Configuration.Create().WithNumberOfIterations(200),
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
            private class Init : MachineState
            {
            }

            private async Task InitOnEntry()
            {
                var e = new E4();
                var m = await this.Runtime.CreateMachineAndExecuteAsync(typeof(M3B));
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
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Send(this.Id, new E1());
            }

            private void HandleEventE1()
            {
                this.E1Handled = true;
            }

            private void HandleEventE4()
            {
                this.Assert(this.E1Handled);
                var e = this.ReceivedEvent as E4;
                e.X = 1;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestSendBlocks()
        {
            this.Test(r =>
            {
                r.CreateMachine(typeof(M3A));
            },
            configuration: Configuration.Create().WithNumberOfIterations(100));
        }

        private class M4A : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [IgnoreEvents(typeof(E1))]
            private class Init : MachineState
            {
            }

            private async Task InitOnEntry()
            {
                var m = await this.Runtime.CreateMachineAndExecuteAsync(typeof(M4B), new E2(this.Id));
                var handled = await this.Runtime.SendEventAndExecuteAsync(m, new E1());
                this.Assert(handled);
            }
        }

        private class M4B : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [IgnoreEvents(typeof(E1))]
            private class Init : MachineState
            {
            }

            private async Task InitOnEntry()
            {
                var creator = (this.ReceivedEvent as E2).Id;
                var handled = await this.Id.Runtime.SendEventAndExecuteAsync(creator, new E1());
                this.Assert(!handled);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestSendCycleDoesNotDeadlock()
        {
            this.Test(r =>
            {
                r.CreateMachine(typeof(M4A));
            },
            configuration: Configuration.Create().WithNumberOfIterations(100));
        }

        private class M5A : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private async Task InitOnEntry()
            {
                var m = await this.Runtime.CreateMachineAndExecuteAsync(typeof(M5B));
                var handled = await this.Runtime.SendEventAndExecuteAsync(m, new E1());
                this.Monitor<M5SafetyMonitor>(new SE_Returns());
                this.Assert(handled);
            }
        }

        private class M5B : StateMachine
        {
            [Start]
            [OnEventDoAction(typeof(E1), nameof(HandleE))]
            private class Init : MachineState
            {
            }

            private void HandleE()
            {
                this.Raise(new Halt());
            }

            protected override void OnHalt()
            {
                this.Monitor<M5SafetyMonitor>(new M_Halts());
            }
        }

        private class M_Halts : Event
        {
        }

        private class SE_Returns : Event
        {
        }

        private class M5SafetyMonitor : Monitor
        {
            private bool MHalted = false;
            private bool SEReturned = false;

            [Start]
            [Hot]
            [OnEventDoAction(typeof(M_Halts), nameof(OnMHalts))]
            [OnEventDoAction(typeof(SE_Returns), nameof(OnSEReturns))]
            private class Init : MonitorState
            {
            }

            [Cold]
            private class Done : MonitorState
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
                this.Goto<Done>();
            }
        }

        [Fact(Timeout = 5000)]
        public void TestMachineHaltsOnSendExec()
        {
            this.Test(r =>
            {
                r.RegisterMonitor(typeof(M5SafetyMonitor));
                r.CreateMachine(typeof(M5A));
            },
            configuration: Configuration.Create().WithNumberOfIterations(100));
        }

        private class Config : Event
        {
            public bool HandleException;

            public Config(bool handleEx)
            {
                this.HandleException = handleEx;
            }
        }

        private class M6A : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private async Task InitOnEntry()
            {
                var m = await this.Runtime.CreateMachineAndExecuteAsync(typeof(M6B), this.ReceivedEvent);
                var handled = await this.Runtime.SendEventAndExecuteAsync(m, new E1());
                this.Monitor<M6SafetyMonitor>(new SE_Returns());
                this.Assert(handled);
            }

            protected override OnExceptionOutcome OnException(string methodName, Exception ex)
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
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.HandleException = (this.ReceivedEvent as Config).HandleException;
            }

            private void HandleE()
            {
                throw new InvalidOperationException();
            }

            protected override OnExceptionOutcome OnException(string methodName, Exception ex)
            {
                return this.HandleException ? OnExceptionOutcome.HandledException : OnExceptionOutcome.ThrowException;
            }
        }

        private class M6SafetyMonitor : Monitor
        {
            [Start]
            [Hot]
            [OnEventGotoState(typeof(SE_Returns), typeof(Done))]
            private class Init : MonitorState
            {
            }

            [Cold]
            private class Done : MonitorState
            {
            }
        }

        [Fact(Timeout = 5000)]
        public void TestHandledExceptionOnSendExec()
        {
            this.Test(r =>
            {
                r.RegisterMonitor(typeof(M6SafetyMonitor));
                r.CreateMachine(typeof(M6A), new Config(true));
            },
            configuration: Configuration.Create().WithNumberOfIterations(100));
        }

        [Fact(Timeout = 5000)]
        public void TestUnhandledExceptionOnSendExec()
        {
            this.TestWithException<InvalidOperationException>(r =>
            {
                r.RegisterMonitor(typeof(M6SafetyMonitor));
                r.CreateMachine(typeof(M6A), new Config(false));
            },
            replay: true);
        }

        private class M7A : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private async Task InitOnEntry()
            {
                var m = await this.Runtime.CreateMachineAndExecuteAsync(typeof(M7B));
                var handled = await this.Runtime.SendEventAndExecuteAsync(m, new E1());
                this.Assert(handled);
            }
        }

        private class M7B : StateMachine
        {
            [Start]
            private class Init : MachineState
            {
            }
        }

        [Fact(Timeout = 5000)]
        public void TestUnhandledEventOnSendExec1()
        {
            this.TestWithError(r =>
            {
                r.CreateMachine(typeof(M7A));
            },
            expectedError: "Machine 'M7B()' received event 'E1' that cannot be handled.",
            replay: true);
        }

        private class M8A : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private async Task InitOnEntry()
            {
                var m = await this.Runtime.CreateMachineAndExecuteAsync(typeof(M8B));
                var handled = await this.Runtime.SendEventAndExecuteAsync(m, new E1());
                this.Assert(handled);
            }
        }

        private class M8B : StateMachine
        {
            [Start]
            [OnEventDoAction(typeof(E1), nameof(Handle))]
            [IgnoreEvents(typeof(E3))]
            private class Init : MachineState
            {
            }

            private void Handle()
            {
                this.Raise(new E3());
            }
        }

        [Fact(Timeout = 5000)]
        public void TestUnhandledEventOnSendExec2()
        {
            this.Test(r =>
            {
                r.CreateMachine(typeof(M8A));
            });
        }
    }
}
