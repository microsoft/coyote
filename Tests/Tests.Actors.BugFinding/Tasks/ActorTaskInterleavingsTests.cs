// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Actors.BugFinding.Tests
{
    public class ActorTaskInterleavingsTests : BaseActorBugFindingTest
    {
        public ActorTaskInterleavingsTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private static async Task WriteAsync(SharedEntry entry, int value)
        {
            await Task.CompletedTask;
            entry.Value = value;
        }

        private static async Task WriteWithDelayAsync(SharedEntry entry, int value)
        {
            await Task.Delay(1);
            entry.Value = value;
        }

        private class A1 : Actor
        {
            protected override async Task OnInitializeAsync(Event initialEvent)
            {
                SharedEntry entry = new SharedEntry();
                Task task = WriteAsync(entry, 3);
                entry.Value = 5;
                await task;
                AssertSharedEntryValue(entry, 5);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestInterleavingsWithOneSynchronousTaskInActor()
        {
            this.Test(r =>
            {
                r.CreateActor(typeof(A1));
            },
            configuration: GetConfiguration().WithTestingIterations(200));
        }

        private class M1 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

#pragma warning disable CA1822 // Mark members as static
            private async Task InitOnEntry()
#pragma warning restore CA1822 // Mark members as static
            {
                SharedEntry entry = new SharedEntry();
                Task task = WriteAsync(entry, 3);
                entry.Value = 5;
                await task;
                AssertSharedEntryValue(entry, 5);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestInterleavingsWithOneSynchronousTaskInStateMachine()
        {
            this.Test(r =>
            {
                r.CreateActor(typeof(M1));
            },
            configuration: GetConfiguration().WithTestingIterations(200));
        }

        private class A2 : Actor
        {
            protected override async Task OnInitializeAsync(Event initialEvent)
            {
                SharedEntry entry = new SharedEntry();

                Task task = WriteWithDelayAsync(entry, 3);
                entry.Value = 5;
                await task;

                AssertSharedEntryValue(entry, 5);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestInterleavingsWithOneAsynchronousTaskInActor()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(A2));
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        private class M2 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

#pragma warning disable CA1822 // Mark members as static
            private async Task InitOnEntry()
#pragma warning restore CA1822 // Mark members as static
            {
                SharedEntry entry = new SharedEntry();

                Task task = WriteWithDelayAsync(entry, 3);
                entry.Value = 5;
                await task;

                AssertSharedEntryValue(entry, 5);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestInterleavingsWithOneAsynchronousTaskInStateMachine()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M2));
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        private class A3 : Actor
        {
            protected override async Task OnInitializeAsync(Event initialEvent)
            {
                SharedEntry entry = new SharedEntry();

                Task task = Task.Run(async () =>
                {
                    await WriteAsync(entry, 3);
                });

                await WriteAsync(entry, 5);
                await task;
                AssertSharedEntryValue(entry, 5);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestInterleavingsWithOneParallelTaskInActor()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(A3));
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        private class M3 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

#pragma warning disable CA1822 // Mark members as static
            private async Task InitOnEntry()
#pragma warning restore CA1822 // Mark members as static
            {
                SharedEntry entry = new SharedEntry();

                Task task = Task.Run(async () =>
                {
                    await WriteAsync(entry, 3);
                });

                await WriteAsync(entry, 5);
                await task;
                AssertSharedEntryValue(entry, 5);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestInterleavingsWithOneParallelTaskInStateMachine()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M3));
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        private class A4 : Actor
        {
            protected override async Task OnInitializeAsync(Event initialEvent)
            {
                SharedEntry entry = new SharedEntry();

                Task task1 = WriteAsync(entry, 3);
                Task task2 = WriteAsync(entry, 5);

                await task1;
                await task2;
                AssertSharedEntryValue(entry, 5);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestInterleavingsWithTwoSynchronousTasksInActor()
        {
            this.Test(r =>
            {
                r.CreateActor(typeof(A4));
            },
            configuration: GetConfiguration().WithTestingIterations(200));
        }

        private class M4 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

#pragma warning disable CA1822 // Mark members as static
            private async Task InitOnEntry()
#pragma warning restore CA1822 // Mark members as static
            {
                SharedEntry entry = new SharedEntry();

                Task task1 = WriteAsync(entry, 3);
                Task task2 = WriteAsync(entry, 5);

                await task1;
                await task2;
                AssertSharedEntryValue(entry, 5);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestInterleavingsWithTwoSynchronousTasksInStateMachine()
        {
            this.Test(r =>
            {
                r.CreateActor(typeof(M4));
            },
            configuration: GetConfiguration().WithTestingIterations(200));
        }

        private class A5 : Actor
        {
            protected override async Task OnInitializeAsync(Event initialEvent)
            {
                SharedEntry entry = new SharedEntry();

                Task task1 = WriteWithDelayAsync(entry, 3);
                Task task2 = WriteWithDelayAsync(entry, 5);

                await task1;
                await task2;
                AssertSharedEntryValue(entry, 5);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestInterleavingsWithTwoAsynchronousTasksInActor()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(A5));
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        private class M5 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

#pragma warning disable CA1822 // Mark members as static
            private async Task InitOnEntry()
#pragma warning restore CA1822 // Mark members as static
            {
                SharedEntry entry = new SharedEntry();

                Task task1 = WriteWithDelayAsync(entry, 3);
                Task task2 = WriteWithDelayAsync(entry, 5);

                await task1;
                await task2;
                AssertSharedEntryValue(entry, 5);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestInterleavingsWithTwoAsynchronousTasksInStateMachine()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M5));
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        private class A6 : Actor
        {
            protected override async Task OnInitializeAsync(Event initialEvent)
            {
                SharedEntry entry = new SharedEntry();

                Task task1 = Task.Run(async () =>
                {
                    await WriteAsync(entry, 3);
                });

                Task task2 = Task.Run(async () =>
                {
                    await WriteAsync(entry, 5);
                });

                await task1;
                await task2;
                AssertSharedEntryValue(entry, 5);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestInterleavingsWithTwoParallelTasksInActor()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(A6));
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        private class M6 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

#pragma warning disable CA1822 // Mark members as static
            private async Task InitOnEntry()
#pragma warning restore CA1822 // Mark members as static
            {
                SharedEntry entry = new SharedEntry();

                Task task1 = Task.Run(async () =>
                {
                    await WriteAsync(entry, 3);
                });

                Task task2 = Task.Run(async () =>
                {
                    await WriteAsync(entry, 5);
                });

                await task1;
                await task2;
                AssertSharedEntryValue(entry, 5);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestInterleavingsWithTwoParallelTasksInStateMachine()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M6));
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        private class A7 : Actor
        {
            protected override async Task OnInitializeAsync(Event initialEvent)
            {
                SharedEntry entry = new SharedEntry();

                Task task1 = Task.Run(async () =>
                {
                    Task task2 = Task.Run(async () =>
                    {
                        await WriteAsync(entry, 5);
                    });

                    await WriteAsync(entry, 3);
                    await task2;
                });

                await task1;
                AssertSharedEntryValue(entry, 5);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestInterleavingsWithNestedParallelTasksInActor()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(A7));
            },
            configuration: GetConfiguration().WithTestingIterations(500),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        private class M7 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

#pragma warning disable CA1822 // Mark members as static
            private async Task InitOnEntry()
#pragma warning restore CA1822 // Mark members as static
            {
                SharedEntry entry = new SharedEntry();

                Task task1 = Task.Run(async () =>
                {
                    Task task2 = Task.Run(async () =>
                    {
                        await WriteAsync(entry, 5);
                    });

                    await WriteAsync(entry, 3);
                    await task2;
                });

                await task1;
                AssertSharedEntryValue(entry, 5);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestInterleavingsWithNestedParallelTasksInStateMachine()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M7));
            },
            configuration: GetConfiguration().WithTestingIterations(500),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }
    }
}
