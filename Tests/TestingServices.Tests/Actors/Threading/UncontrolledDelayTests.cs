// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Tests.Common.Actors;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests.Actors
{
    public class UncontrolledDelayTests : BaseTest
    {
        public UncontrolledDelayTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private class A1 : Actor
        {
            protected override async Task OnInitializeAsync(Event initialEvent)
            {
                this.IgnoreEvent(typeof(UnitEvent));
                this.SendEvent(this.Id, UnitEvent.Instance);
                await Task.Delay(10);
                this.SendEvent(this.Id, UnitEvent.Instance);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestUncontrolledDelayInActor()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(A1));
            },
            expectedError: "'' is trying to wait for an uncontrolled task or awaiter to complete. Please make sure to " +
                "avoid using concurrency APIs such as 'Task.Run', 'Task.Delay' or 'Task.Yield' inside actor handlers. If you " +
                "are using external libraries that are executing concurrently, you will need to mock them during testing.",
            replay: true);
        }

        private class M1 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [IgnoreEvents(typeof(UnitEvent))]
            private class Init : State
            {
            }

            private async Task InitOnEntry()
            {
                this.SendEvent(this.Id, UnitEvent.Instance);
                await Task.Delay(10);
                this.SendEvent(this.Id, UnitEvent.Instance);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestUncontrolledDelayInStateMachine()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M1));
            },
            expectedError: "'' is trying to wait for an uncontrolled task or awaiter to complete. Please make sure to " +
                "avoid using concurrency APIs such as 'Task.Run', 'Task.Delay' or 'Task.Yield' inside actor handlers. If you " +
                "are using external libraries that are executing concurrently, you will need to mock them during testing.",
            replay: true);
        }

        private class A2 : Actor
        {
            protected override async Task OnInitializeAsync(Event initialEvent)
            {
                this.IgnoreEvent(typeof(UnitEvent));
                this.SendEvent(this.Id, UnitEvent.Instance);
                await Task.Delay(10).ConfigureAwait(false);
                this.SendEvent(this.Id, UnitEvent.Instance);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestUncontrolledDelayWithOtherSynchronizationContextInActor()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(A2));
            },
            expectedErrors: new string[]
            {
                "'' is trying to wait for an uncontrolled task or awaiter to complete. Please make sure to avoid using " +
                "concurrency APIs such as 'Task.Run', 'Task.Delay' or 'Task.Yield' inside actor handlers. If you are " +
                "using external libraries that are executing concurrently, you will need to mock them during testing.",
                "Uncontrolled task with id '' invoked a runtime method. Please make sure to avoid using concurrency APIs " +
                "such as 'Task.Run', 'Task.Delay' or 'Task.Yield' inside actor handlers or controlled tasks. If you are " +
                "using external libraries that are executing concurrently, you will need to mock them during testing.",
            },
            replay: true);
        }

        private class M2 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [IgnoreEvents(typeof(UnitEvent))]
            private class Init : State
            {
            }

            private async Task InitOnEntry()
            {
                this.SendEvent(this.Id, UnitEvent.Instance);
                await Task.Delay(10).ConfigureAwait(false);
                this.SendEvent(this.Id, UnitEvent.Instance);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestUncontrolledDelayWithOtherSynchronizationContextInStateMachine()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M2));
            },
            expectedErrors: new string[]
            {
                "'' is trying to wait for an uncontrolled task or awaiter to complete. Please make sure to avoid using " +
                "concurrency APIs such as 'Task.Run', 'Task.Delay' or 'Task.Yield' inside actor handlers. If you are " +
                "using external libraries that are executing concurrently, you will need to mock them during testing.",
                "Uncontrolled task with id '' invoked a runtime method. Please make sure to avoid using concurrency APIs " +
                "such as 'Task.Run', 'Task.Delay' or 'Task.Yield' inside actor handlers or controlled tasks. If you are " +
                "using external libraries that are executing concurrently, you will need to mock them during testing.",
            },
            replay: true);
        }

        private class A3 : Actor
        {
            protected override async Task OnInitializeAsync(Event initialEvent)
            {
                Task[] tasks = new Task[2];
                for (int i = 0; i < 2; i++)
                {
                    tasks[i] = this.DelayedRandomAsync();
                }

                await Task.WhenAll(tasks);
            }

            private async Task DelayedRandomAsync()
            {
                await Task.Delay(10).ConfigureAwait(false);
                this.Random();
            }
        }

        [Fact(Timeout = 5000)]
        public void TestUncontrolledDelayLoopWithOtherSynchronizationContextInActor()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(A3));
            },
            expectedErrors: new string[]
            {
                "'' is trying to wait for an uncontrolled task or awaiter to complete. Please make sure to avoid using " +
                "concurrency APIs such as 'Task.Run', 'Task.Delay' or 'Task.Yield' inside actor handlers. If you are " +
                "using external libraries that are executing concurrently, you will need to mock them during testing.",
                "Uncontrolled task with id '' invoked a runtime method. Please make sure to avoid using concurrency APIs " +
                "such as 'Task.Run', 'Task.Delay' or 'Task.Yield' inside actor handlers or controlled tasks. If you are " +
                "using external libraries that are executing concurrently, you will need to mock them during testing.",
            },
            replay: true);
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
                Task[] tasks = new Task[2];
                for (int i = 0; i < 2; i++)
                {
                    tasks[i] = this.DelayedRandomAsync();
                }

                await Task.WhenAll(tasks);
            }

            private async Task DelayedRandomAsync()
            {
                await Task.Delay(10).ConfigureAwait(false);
                this.Random();
            }
        }

        [Fact(Timeout = 5000)]
        public void TestUncontrolledDelayLoopWithOtherSynchronizationContextInStateMachine()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M3));
            },
            expectedErrors: new string[]
            {
                "'' is trying to wait for an uncontrolled task or awaiter to complete. Please make sure to avoid using " +
                "concurrency APIs such as 'Task.Run', 'Task.Delay' or 'Task.Yield' inside actor handlers. If you are " +
                "using external libraries that are executing concurrently, you will need to mock them during testing.",
                "Uncontrolled task with id '' invoked a runtime method. Please make sure to avoid using concurrency APIs " +
                "such as 'Task.Run', 'Task.Delay' or 'Task.Yield' inside actor handlers or controlled tasks. If you are " +
                "using external libraries that are executing concurrently, you will need to mock them during testing.",
            },
            replay: true);
        }
    }
}
