// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Actors.BugFinding.Tests
{
    public class ActorTaskRunTests : BaseActorBugFindingTest
    {
        public ActorTaskRunTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private class A1 : Actor
        {
            protected override async Task OnInitializeAsync(Event initialEvent)
            {
                SharedEntry entry = new SharedEntry();
                await Task.Run(() =>
                {
                    entry.Value = 5;
                });

                AssertSharedEntryValue(entry, 5);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestTaskRunInActor()
        {
            this.Test(r =>
            {
                r.CreateActor(typeof(A1));
            },
            configuration: this.GetConfiguration().WithTestingIterations(100));
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
                await Task.Run(() =>
                {
                    entry.Value = 5;
                });

                AssertSharedEntryValue(entry, 5);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestTaskRunInStateMachine()
        {
            this.Test(r =>
            {
                r.CreateActor(typeof(M1));
            },
            configuration: this.GetConfiguration().WithTestingIterations(100));
        }

        private class A2 : Actor
        {
            protected override async Task OnInitializeAsync(Event initialEvent)
            {
                SharedEntry entry = new SharedEntry();
                await Task.Run(async () =>
                {
                    await Task.Delay(100);
                    entry.Value = 5;
                });

                AssertSharedEntryValue(entry, 5);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestTaskRunAsyncInActor()
        {
            this.Test(r =>
            {
                r.CreateActor(typeof(A2));
            },
            configuration: this.GetConfiguration().WithTestingIterations(100));
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
                await Task.Run(async () =>
                {
                    await Task.Delay(100);
                    entry.Value = 5;
                });

                AssertSharedEntryValue(entry, 5);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestTaskRunAsyncInStateMachine()
        {
            this.Test(r =>
            {
                r.CreateActor(typeof(M2));
            },
            configuration: this.GetConfiguration().WithTestingIterations(100));
        }

        private class A3 : Actor
        {
            protected override async Task OnInitializeAsync(Event initialEvent)
            {
                SharedEntry entry = new SharedEntry();
                await Task.Run(async () =>
                {
                    await Task.Run(async () =>
                    {
                        await Task.Delay(100);
                        entry.Value = 3;
                    });

                    entry.Value = 5;
                });

                AssertSharedEntryValue(entry, 5);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestNestedTaskRunInActor()
        {
            this.Test(r =>
            {
                r.CreateActor(typeof(A3));
            },
            configuration: this.GetConfiguration().WithTestingIterations(100));
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
                await Task.Run(async () =>
                {
                    await Task.Run(async () =>
                    {
                        await Task.Delay(100);
                        entry.Value = 3;
                    });

                    entry.Value = 5;
                });

                AssertSharedEntryValue(entry, 5);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestNestedTaskRunInStateMachine()
        {
            this.Test(r =>
            {
                r.CreateActor(typeof(M3));
            },
            configuration: this.GetConfiguration().WithTestingIterations(100));
        }
    }
}
