// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Tasks;
using Microsoft.Coyote.Tests.Common.Actors.Operations;
using Xunit;
using Xunit.Abstractions;
using SystemTasks = System.Threading.Tasks;

namespace Microsoft.Coyote.Production.Tests.Actors.StateMachines
{
    public class GotoStateTransitionTests : BaseProductionTest
    {
        public GotoStateTransitionTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private class Message : Event
        {
        }

        private class M1 : TraceableStateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.Trace("InitOnEntry");
                this.RaiseGotoStateEvent<Final>();
            }

            [OnEntry(nameof(FinalOnEntry))]
            private class Final : State
            {
            }

            private void FinalOnEntry()
            {
                this.Trace("FinalOnEntry");
                this.TraceOp.SetResult(true);
            }
        }

        private class M2 : TraceableStateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventGotoState(typeof(Message), typeof(Final))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.Trace("InitOnEntry");
            }

            [OnEntry(nameof(FinalOnEntry))]
            private class Final : State
            {
            }

            private void FinalOnEntry()
            {
                this.Trace("FinalOnEntry");
                this.OnFinalEvent();
            }
        }

        private class M3 : TraceableStateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventGotoState(typeof(Message), typeof(Final))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.Trace("InitOnEntry");
                this.RaiseEvent(new Message());
            }

            [OnEntry(nameof(FinalOnEntry))]
            private class Final : State
            {
            }

            private void FinalOnEntry()
            {
                this.Trace("FinalOnEntry");
                this.OnFinalEvent();
            }
        }

        [Fact(Timeout = 5000)]
        public void TestGotoStateTransition()
        {
            this.Test(async (IActorRuntime runtime) =>
            {
                var op = new OperationTrace();
                runtime.CreateActor(typeof(M1), null, op);
                await op.Completion.Task;
                string actual = op.ToString();
                Assert.Equal("InitOnEntry, CurrentState=Final, FinalOnEntry", actual);
            });
        }

        [Fact(Timeout = 5000)]
        public void TestGotoStateTransitionAfterSend()
        {
            this.Test(async (IActorRuntime runtime) =>
            {
                var op = new OperationTrace();
                var id = runtime.CreateActor(typeof(M2), null, op);
                runtime.SendEvent(id, new Message());
                await op.Completion.Task;
                string actual = op.ToString();
                Assert.Equal("InitOnEntry, CurrentState=Final, FinalOnEntry", actual);
            });
        }

        [Fact(Timeout = 5000)]
        public void TestGotoStateTransitionAfterRaise()
        {
            this.Test(async (IActorRuntime runtime) =>
            {
                var op = new OperationTrace();
                var id = runtime.CreateActor(typeof(M3), null, op);
                await op.Completion.Task;
                string actual = op.ToString();
                Assert.Equal("InitOnEntry, CurrentState=Final, FinalOnEntry", actual);
            });
        }
    }
}
