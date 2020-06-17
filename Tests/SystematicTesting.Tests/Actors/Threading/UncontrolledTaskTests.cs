// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Tests.Common.Events;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.SystematicTesting.Tests.Actors
{
    public class UncontrolledTaskTests : BaseSystematicTest
    {
        public UncontrolledTaskTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private class A1 : Actor
        {
            protected override async Task OnInitializeAsync(Event initialEvent)
            {
                var g = this.CurrentEventGroup as AwaitableEventGroup<bool>;
                await Task.Run(async () =>
                {
                    await Task.Yield();
                    this.SendEvent(this.Id, UnitEvent.Instance);
                    g.SetResult(true);
                });
            }
        }

        [Fact(Timeout = 5000)]
        public void TestUncontrolledTaskSendingEventInActor()
        {
            this.TestWithError(async r =>
            {
                var g = new AwaitableEventGroup<bool>();
                r.CreateActor(typeof(A1));
                await g;
            },
            expectedErrors: GetUncontrolledTaskErrorMessages());
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
                var g = this.CurrentEventGroup as AwaitableEventGroup<bool>;
                await Task.Run(async () =>
                {
                    await Task.Yield();
                    this.SendEvent(this.Id, UnitEvent.Instance);
                    g.SetResult(true);
                });
            }
        }

        [Fact(Timeout = 5000)]
        public void TestUncontrolledTaskSendingEventInStateMachine()
        {
            this.TestWithError(async r =>
            {
                var g = new AwaitableEventGroup<bool>();
                r.CreateActor(typeof(M1), null, g);
                await g;
            },
            expectedErrors: GetUncontrolledTaskErrorMessages());
        }

        private class A2 : Actor
        {
            protected override async Task OnInitializeAsync(Event initialEvent)
            {
                var g = this.CurrentEventGroup as AwaitableEventGroup<bool>;
                await Task.Run(async () =>
                {
                    await Task.Yield();
                    this.RandomBoolean();
                    g.SetResult(true);
                });
            }
        }

        [Fact(Timeout = 5000)]
        public void TestUncontrolledTaskInvokingRandomInActor()
        {
            this.TestWithError(async r =>
            {
                var g = new AwaitableEventGroup<bool>();
                r.CreateActor(typeof(A2));
                await g;
            },
            expectedErrors: GetUncontrolledTaskErrorMessages());
        }

        private class M2 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private async Task InitOnEntry()
            {
                var g = this.CurrentEventGroup as AwaitableEventGroup<bool>;
                await Task.Run(async () =>
                {
                    await Task.Yield();
                    this.RandomBoolean();
                    g.SetResult(true);
                });
            }
        }

        [Fact(Timeout = 5000)]
        public void TestUncontrolledTaskInvokingRandomInStateMachine()
        {
            this.TestWithError(async r =>
            {
                var g = new AwaitableEventGroup<bool>();
                r.CreateActor(typeof(M2));
                await g;
            },
            expectedErrors: GetUncontrolledTaskErrorMessages());
        }
    }
}
