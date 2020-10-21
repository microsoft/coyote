// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Tests.Common.Actors;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Actors.Tests.Actors
{
    public class HandleEventTests : BaseActorTest
    {
        public HandleEventTests(ITestOutputHelper output)
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
            [OnEventDoAction(typeof(E1), nameof(HandleE1))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.Trace("InitOnEntry");
            }

            private void HandleE1()
            {
                this.Trace("HandleE1");
                this.OnFinalEvent();
            }
        }

        private class M2 : TraceableStateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E1), nameof(HandleE1))]
            [OnEventDoAction(typeof(E2), nameof(HandleE2))]
            [OnEventDoAction(typeof(E3), nameof(HandleE3))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.Trace("InitOnEntry");
            }

            private void HandleE1()
            {
                this.Trace("HandleE1");
            }

            private void HandleE2()
            {
                this.Trace("HandleE2");
            }

            private void HandleE3()
            {
                this.Trace("HandleE3");
                this.OnFinalEvent();
            }
        }

        [Fact(Timeout = 5000)]
        public void TestHandleEventInStateMachine()
        {
            this.Test(async (IActorRuntime runtime) =>
            {
                var op = new EventGroupList();
                var id = runtime.CreateActor(typeof(M1), null, op);
                runtime.SendEvent(id, new E1());
                await this.GetResultAsync(op.Task);
                var actual = op.ToString();
                Assert.Equal("InitOnEntry, HandleE1", actual);
            });
        }

        [Fact(Timeout = 5000)]
        public void TestHandleMultipleEventsInStateMachine()
        {
            this.Test(async (IActorRuntime runtime) =>
            {
                var op = new EventGroupList();
                var id = runtime.CreateActor(typeof(M2), null, op);
                runtime.SendEvent(id, new E1());
                runtime.SendEvent(id, new E2());
                runtime.SendEvent(id, new E3());
                await this.GetResultAsync(op.Task);
                var actual = op.ToString();
                Assert.Equal("InitOnEntry, HandleE1, HandleE2, HandleE3", actual);
            });
        }
    }
}
