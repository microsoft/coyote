// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Tests.Common.Actors.Operations;
using Xunit;
using Xunit.Abstractions;
using SystemTasks = System.Threading.Tasks;

namespace Microsoft.Coyote.Production.Tests.Actors.StateMachines
{
    public class ReceiveEventTests : BaseProductionTest
    {
        public ReceiveEventTests(ITestOutputHelper output)
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

        private class M1 : TraceableStateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private async Task InitOnEntry()
            {
                this.Trace("Receiving");
                Event e = await this.ReceiveEventAsync(typeof(E1));
                this.Trace("Received:{0}", e.GetType().Name);

                this.TraceOp.SetResult(true);
            }
        }

        private class M2 : TraceableStateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private async Task InitOnEntry()
            {
                this.Trace("Receiving");
                var e = await this.ReceiveEventAsync(typeof(E1));
                this.Trace("Received:{0}", e.GetType().Name);
                e = await this.ReceiveEventAsync(typeof(E2));
                this.Trace("Received:{0}", e.GetType().Name);
                e = await this.ReceiveEventAsync(typeof(E3));
                this.Trace("Received:{0}", e.GetType().Name);

                this.TraceOp.SetResult(true);
            }
        }

        private class M3 : TraceableStateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private async Task InitOnEntry()
            {
                this.Trace("Receiving");
                var e = await this.ReceiveEventAsync(typeof(E1), typeof(E2), typeof(E3));
                this.Trace("Received:{0}", e.GetType().Name);

                this.TraceOp.SetResult(true);
            }
        }

        private class M4 : TraceableStateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private async Task InitOnEntry()
            {
                this.Trace("Receiving");
                var e = await this.ReceiveEventAsync(typeof(E1), typeof(E2), typeof(E3));
                this.Trace("Received:{0}", e.GetType().Name);
                e = await this.ReceiveEventAsync(typeof(E1), typeof(E2), typeof(E3));
                this.Trace("Received:{0}", e.GetType().Name);
                e = await this.ReceiveEventAsync(typeof(E1), typeof(E2), typeof(E3));
                this.Trace("Received:{0}", e.GetType().Name);

                this.TraceOp.SetResult(true);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestReceiveEventStatement()
        {
            this.Test(async (IActorRuntime runtime) =>
            {
                var op = new OperationList();
                var id = runtime.CreateActor(typeof(M1), null, op);
                runtime.SendEvent(id, new E1());
                var actual = await op.WaitForResult();
                Assert.Equal("Receiving, Received:E1", actual);
            });
        }

        [Fact(Timeout = 5000)]
        public void TestMultipleReceiveEventStatements()
        {
            this.Test(async (IActorRuntime runtime) =>
            {
                var op = new OperationList();
                var id = runtime.CreateActor(typeof(M2), null, op);
                runtime.SendEvent(id, new E1());
                runtime.SendEvent(id, new E2());
                runtime.SendEvent(id, new E3());
                var actual = await op.WaitForResult();
                Assert.Equal("Receiving, Received:E1, Received:E2, Received:E3", actual);
            });
        }

        [Fact(Timeout = 5000)]
        public void TestMultipleReceiveEventStatementsUnordered()
        {
            this.Test(async (IActorRuntime runtime) =>
            {
                var op = new OperationList();
                var id = runtime.CreateActor(typeof(M2), null, op);
                runtime.SendEvent(id, new E2());
                runtime.SendEvent(id, new E3());
                runtime.SendEvent(id, new E1());
                var actual = await op.WaitForResult();
                Assert.Equal("Receiving, Received:E1, Received:E2, Received:E3", actual);
            });
        }

        [Fact(Timeout = 5000)]
        public void TestReceiveEventStatementWithMultipleTypes()
        {
            this.Test(async (IActorRuntime runtime) =>
            {
                var op = new OperationList();
                var id = runtime.CreateActor(typeof(M3), null, op);
                runtime.SendEvent(id, new E2());
                var actual = await op.WaitForResult();
                Assert.Equal("Receiving, Received:E2", actual);
            });
        }

        [Fact(Timeout = 5000)]
        public void TestMultipleReceiveEventStatementsWithMultipleTypes()
        {
            this.Test(async (IActorRuntime runtime) =>
            {
                var op = new OperationList();
                var id = runtime.CreateActor(typeof(M4), null, op);
                runtime.SendEvent(id, new E1());
                runtime.SendEvent(id, new E3());
                runtime.SendEvent(id, new E2());
                var actual = await op.WaitForResult();
                Assert.Equal("Receiving, Received:E1, Received:E3, Received:E2", actual);
            });
        }
    }
}
