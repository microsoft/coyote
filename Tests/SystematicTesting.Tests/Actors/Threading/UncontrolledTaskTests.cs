// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Tests.Common.Actors;
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
                await Task.Run(() =>
                {
                    this.SendEvent(this.Id, UnitEvent.Instance);
                });
            }
        }

        [Fact(Timeout = 5000)]
        public void TestUncontrolledTaskSendingEventInActor()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(A1));
            },
            expectedErrors: new string[]
            {
                "Controlled task '' is trying to wait for an uncontrolled task or awaiter to complete. Please " +
                "make sure to avoid using concurrency APIs () inside actor handlers. If you are using external " +
                "libraries that are executing concurrently, you will need to mock them during testing.",
                "Uncontrolled task '' invoked a runtime method. Please make sure to avoid using concurrency APIs () " +
                "inside actor handlers or controlled tasks. If you are using external libraries that are executing " +
                "concurrently, you will need to mock them during testing.",
            },
            replay: true);
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
                await Task.Run(() =>
                {
                    this.SendEvent(this.Id, UnitEvent.Instance);
                });
            }
        }

        [Fact(Timeout = 5000)]
        public void TestUncontrolledTaskSendingEventInStateMachine()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M1));
            },
            expectedErrors: new string[]
            {
                "Controlled task '' is trying to wait for an uncontrolled task or awaiter to complete. Please " +
                "make sure to avoid using concurrency APIs () inside actor handlers. If you are using external " +
                "libraries that are executing concurrently, you will need to mock them during testing.",
                "Uncontrolled task '' invoked a runtime method. Please make sure to avoid using concurrency APIs () " +
                "inside actor handlers or controlled tasks. If you are using external libraries that are executing " +
                "concurrently, you will need to mock them during testing.",
            },
            replay: true);
        }

        private class A2 : Actor
        {
            protected override async Task OnInitializeAsync(Event initialEvent)
            {
                await Task.Run(() =>
                {
                    this.Random();
                });
            }
        }

        [Fact(Timeout = 5000)]
        public void TestUncontrolledTaskInvokingRandomInActor()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(A2));
            },
            expectedErrors: new string[]
            {
                "Controlled task '' is trying to wait for an uncontrolled task or awaiter to complete. Please " +
                "make sure to avoid using concurrency APIs () inside actor handlers. If you are using external " +
                "libraries that are executing concurrently, you will need to mock them during testing.",
                "Uncontrolled task '' invoked a runtime method. Please make sure to avoid using concurrency APIs () " +
                "inside actor handlers or controlled tasks. If you are using external libraries that are executing " +
                "concurrently, you will need to mock them during testing.",
            },
            replay: true);
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
                await Task.Run(() =>
                {
                    this.Random();
                });
            }
        }

        [Fact(Timeout = 5000)]
        public void TestUncontrolledTaskInvokingRandomInStateMachine()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M2));
            },
            expectedErrors: new string[]
            {
                "Controlled task '' is trying to wait for an uncontrolled task or awaiter to complete. Please " +
                "make sure to avoid using concurrency APIs () inside actor handlers. If you are using external " +
                "libraries that are executing concurrently, you will need to mock them during testing.",
                "Uncontrolled task '' invoked a runtime method. Please make sure to avoid using concurrency APIs () " +
                "inside actor handlers or controlled tasks. If you are using external libraries that are executing " +
                "concurrently, you will need to mock them during testing.",
            },
            replay: true);
        }
    }
}
