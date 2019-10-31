// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Actors;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests
{
    public class UncontrolledMachineDelayTest : BaseTest
    {
        public UncontrolledMachineDelayTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class E : Event
        {
        }

        private class M1 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [IgnoreEvents(typeof(E))]
            private class Init : State
            {
            }

            private async Task InitOnEntry()
            {
                this.Send(this.Id, new E());
                await Task.Delay(10);
                this.Send(this.Id, new E());
            }
        }

        [Fact(Timeout = 5000)]
        public void TestUncontrolledMachineDelay()
        {
            this.TestWithError(r =>
            {
                r.CreateMachine(typeof(M1));
            },
            expectedError: "Machine '' is trying to wait for an uncontrolled task or awaiter to complete. Please make sure to " +
                "avoid using concurrency APIs such as 'Task.Run', 'Task.Delay' or 'Task.Yield' inside machine handlers. If you " +
                "are using external libraries that are executing concurrently, you will need to mock them during testing.",
            replay: true);
        }

        private class M2 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [IgnoreEvents(typeof(E))]
            private class Init : State
            {
            }

            private async Task InitOnEntry()
            {
                this.Send(this.Id, new E());
                await Task.Delay(10).ConfigureAwait(false);
                this.Send(this.Id, new E());
            }
        }

        [Fact(Timeout = 5000)]
        public void TestUncontrolledMachineDelayWithOtherSynchronizationContext()
        {
            this.TestWithError(r =>
            {
                r.CreateMachine(typeof(M2));
            },
            expectedErrors: new string[]
            {
                "Machine '' is trying to wait for an uncontrolled task or awaiter to complete. Please make sure to avoid using " +
                    "concurrency APIs such as 'Task.Run', 'Task.Delay' or 'Task.Yield' inside machine handlers. If you are " +
                    "using external libraries that are executing concurrently, you will need to mock them during testing.",
                "Uncontrolled task with id '' invoked a runtime method. Please make sure to avoid using concurrency APIs such " +
                    "as 'Task.Run', 'Task.Delay' or 'Task.Yield' inside machine handlers or controlled tasks. If you are " +
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
        public void TestUncontrolledMachineDelayLoopWithOtherSynchronizationContext()
        {
            this.TestWithError(r =>
            {
                r.CreateMachine(typeof(M3));
            },
            expectedErrors: new string[]
            {
                "Machine '' is trying to wait for an uncontrolled task or awaiter to complete. Please make sure to avoid using " +
                    "concurrency APIs such as 'Task.Run', 'Task.Delay' or 'Task.Yield' inside machine handlers. If you are " +
                    "using external libraries that are executing concurrently, you will need to mock them during testing.",
                "Uncontrolled task with id '' invoked a runtime method. Please make sure to avoid using concurrency APIs such " +
                    "as 'Task.Run', 'Task.Delay' or 'Task.Yield' inside machine handlers or controlled tasks. If you are " +
                    "using external libraries that are executing concurrently, you will need to mock them during testing.",
            },
            replay: true);
        }
    }
}
