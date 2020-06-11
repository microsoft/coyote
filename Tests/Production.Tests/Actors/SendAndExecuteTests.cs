// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Specifications;
using Microsoft.Coyote.Tasks;
using Xunit;
using Xunit.Abstractions;
using SystemTasks = System.Threading.Tasks;

namespace Microsoft.Coyote.Production.Tests.Actors.Operations
{
    public class SendAndExecuteTests : BaseProductionTest
    {
        public SendAndExecuteTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private class ConfigEvent : Event
        {
            public bool HandleException;

            public ConfigEvent(bool handleEx)
            {
                this.HandleException = handleEx;
            }
        }

        private class E1 : Event
        {
            public int Value;

            public E1()
            {
                this.Value = 0;
            }
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

        private class MHalts : Event
        {
        }

        private class SEReturns : Event
        {
        }

        private class M1 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private async Task InitOnEntry()
            {
                var e1 = new E1();
                var op = new Operation<bool>();
                var m = this.CreateActor(typeof(N1), null, op);
                await op.Completion.Task;

                op = new Operation<bool>();
                this.Runtime.SendEvent(m, e1, op);
                await op.Completion.Task;

                this.Assert(e1.Value == 1);
                ((Operation<bool>)this.CurrentOperation).SetResult(true);
            }
        }

        private class N1 : StateMachine
        {
            private bool LEHandled = false;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E1), nameof(HandleEventE))]
            [OnEventDoAction(typeof(E3), nameof(HandleEventLE))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.SendEvent(this.Id, new E3());
                ((Operation<bool>)this.CurrentOperation).SetResult(true);
            }

            private void HandleEventLE()
            {
                this.LEHandled = true;
            }

            private void HandleEventE(Event e)
            {
                this.Assert(this.LEHandled);
                (e as E1).Value = 1;
                ((Operation<bool>)this.CurrentOperation).SetResult(true);
            }
        }

        //--------------------------------------------------------------------------------------------------------
        [Fact(Timeout = 5000)]
        public async Task TestSyncSendBlocks()
        {
            await this.RunAsync(async r =>
            {
                var failed = false;
                var op = new Operation<bool>();
                r.OnFailure += (ex) =>
                {
                    failed = true;
                    op.SetResult(true);
                };

                r.CreateActor(typeof(M1), null, op);

                await WaitAsync(op.Completion.Task);
                Assert.False(failed);
            });
        }

        private class M2 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [IgnoreEvents(typeof(E3))]
            private class Init : State
            {
            }

            private async Task InitOnEntry()
            {
                var op = new Operation<bool>();
                var m = this.Runtime.CreateActor(typeof(N2), new E2(this.Id), op);
                await op.Completion.Task;

                this.Runtime.SendEvent(m, new E3());
                var handled = await op.Completion.Task;

                this.Assert(handled);
                ((Operation<bool>)this.CurrentOperation).SetResult(true);
            }
        }

        private class N2 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E3), nameof(HandleE3))]
            private class Init : State
            {
            }

            private async Task InitOnEntry(Event e)
            {
                var creator = (e as E2).Id;
                var op = new Operation<bool>();
                this.SendEvent(creator, new E3(), op);
                var handled = await op.Completion.Task;
                this.Assert(!handled);
            }

            private void HandleE3()
            {
                ((Operation<bool>)this.CurrentOperation).SetResult(true);
            }
        }

        //--------------------------------------------------------------------------------------------------------
        [Fact(Timeout = 5000)]
        public async Task TestSendCycleDoesNotDeadlock()
        {
            await this.RunAsync(async r =>
            {
                var failed = false;
                var op = new Operation<bool>();
                r.OnFailure += (ex) =>
                {
                    failed = true;
                    op.SetResult(false);
                };

                r.CreateActor(typeof(M2), null, op);

                await WaitAsync(op.Completion.Task);
                Assert.False(failed);
            });
        }

        private class M3 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private async Task InitOnEntry()
            {
                var op = new Operation<bool>();
                var m = this.CreateActor(typeof(N3), null, op);
                this.SendEvent(m, new E3());
                var handled = await op.Completion.Task;
                this.Monitor<SafetyMonitor>(new SEReturns());
                this.Assert(handled);
                ((Operation<bool>)this.CurrentOperation).SetResult(true);
            }
        }

        private class N3 : StateMachine
        {
            [Start]
            [OnEventDoAction(typeof(E3), nameof(HandleE))]
            private class Init : State
            {
            }

            private void HandleE() => this.RaiseHaltEvent();

            protected override SystemTasks.Task OnHaltAsync(Event e)
            {
                this.Monitor<SafetyMonitor>(new MHalts());
                ((Operation<bool>)this.CurrentOperation).SetResult(true);
                return SystemTasks.Task.CompletedTask;
            }
        }

        private class SafetyMonitor : Monitor
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

        //--------------------------------------------------------------------------------------------------------
        [Fact(Timeout = 5000)]
        public async Task TestMachineHaltsOnSendExec()
        {
            var config = GetConfiguration();
            config.IsMonitoringEnabledInInProduction = true;
            await this.RunAsync(async r =>
            {
                var failed = false;
                var op = new Operation<bool>();
                r.OnFailure += (ex) =>
                {
                    failed = true;
                    op.SetResult(false);
                };

                r.RegisterMonitor<SafetyMonitor>();
                r.CreateActor(typeof(M3), null, op);

                await WaitAsync(op.Completion.Task);
                Assert.False(failed);
            }, config);
        }

        private class M4 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private async Task InitOnEntry(Event e)
            {
                var op = new Operation<bool>();
                var m = this.CreateActor(typeof(N4), e, op);
                this.SendEvent(m, new E3());
                var handled = await op.Completion.Task;
                this.Assert(handled);
                ((Operation<bool>)this.CurrentOperation).SetResult(true);
            }

            protected override OnExceptionOutcome OnException(Exception ex, string methodName, Event e)
            {
                this.Assert(false);
                return OnExceptionOutcome.ThrowException;
            }
        }

        private class N4 : StateMachine
        {
            private bool HandleException = false;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E3), nameof(HandleE))]
            private class Init : State
            {
            }

            private void InitOnEntry(Event e)
            {
                this.HandleException = (e as ConfigEvent).HandleException;
            }

            private void HandleE() => throw new Exception();

            protected override OnExceptionOutcome OnException(Exception ex, string methodName, Event e)
            {
                if (this.HandleException)
                {
                    ((Operation<bool>)this.CurrentOperation).SetResult(true);
                    return OnExceptionOutcome.HandledException;
                }

                return OnExceptionOutcome.ThrowException;
            }
        }

        //--------------------------------------------------------------------------------------------------------
        [Fact(Timeout = 5000)]
        public async Task TestHandledExceptionOnSendExec()
        {
            await this.RunAsync(async r =>
            {
                var failed = false;
                var op = new Operation<bool>();
                r.OnFailure += (ex) =>
                {
                    failed = true;
                    op.SetResult(false);
                };

                r.CreateActor(typeof(M4), new ConfigEvent(true), op);

                await WaitAsync(op.Completion.Task);
                Assert.False(failed);
            });
        }

        //--------------------------------------------------------------------------------------------------------
        [Fact(Timeout = 5000)]
        public async Task TestUnHandledExceptionOnSendExec()
        {
            await this.RunAsync(async r =>
            {
                var failed = false;
                var op = new Operation<bool>();
                var message = string.Empty;

                r.OnFailure += (ex) =>
                {
                    if (!failed)
                    {
                        message = (ex is ActionExceptionFilterException) ? ex.InnerException.Message : ex.Message;
                        failed = true;
                        op.TrySetResult(true);
                    }
                };

                r.CreateActor(typeof(M4), new ConfigEvent(false), op);

                await WaitAsync(op.Completion.Task);
                Assert.True(failed);
                Assert.StartsWith("Exception of type 'System.Exception' was thrown", message);
            });
        }

        private class M5 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private async Task InitOnEntry()
            {
                var op = new Operation<bool>();
                var m = this.CreateActor(typeof(N5), null, op);
                this.Runtime.SendEvent(m, new E3()); // should raise unhandled event exception
                var handled = await op.Completion.Task;
                this.Assert(handled);
                ((Operation<bool>)this.CurrentOperation).SetResult(true);
            }
        }

        private class N5 : StateMachine
        {
            [Start]
            private class Init : State
            {
            }
        }

        //--------------------------------------------------------------------------------------------------------
        [Fact(Timeout = 5000)]
        public async Task TestUnhandledEventOnSendExec()
        {
            await this.RunAsync(async r =>
            {
                var failed = false;
                var op = new Operation<bool>();
                var message = string.Empty;

                r.OnFailure += (ex) =>
                {
                    if (!failed)
                    {
                        message = (ex is ActionExceptionFilterException) ? ex.InnerException.Message : ex.Message;
                        failed = true;
                        op.TrySetResult(false);
                    }
                };

                r.CreateActor(typeof(M5), null, op);

                await WaitAsync(op.Completion.Task);
                Assert.True(failed);
                Assert.Equal(
                    "Microsoft.Coyote.Production.Tests.Actors.SendAndExecuteTests+N5(1) received event " +
                    "'Microsoft.Coyote.Production.Tests.Actors.SendAndExecuteTests+E3' that cannot be handled.", message);
            });
        }
    }
}
