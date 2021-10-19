// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Tests.Common.Events;
using Microsoft.Coyote.Tests.Common.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Actors.BugFinding.Tests
{
    public class ActorTaskYieldTests : BaseActorBugFindingTest
    {
        private static string ExpectedMethodName { get; } =
            GetFullyQualifiedMethodName(typeof(AsyncProvider), nameof(AsyncProvider.DelayAsync));

        public ActorTaskYieldTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [OnEventDoAction(typeof(UnitEvent), nameof(IgnoreUnitEvent))]
        private class A1 : Actor
        {
            protected override async Task OnInitializeAsync(Event initialEvent)
            {
                this.SendEvent(this.Id, UnitEvent.Instance);
                await Task.Yield();
                this.SendEvent(this.Id, UnitEvent.Instance);
            }

#pragma warning disable CA1822 // Mark members as static
            private void IgnoreUnitEvent()
#pragma warning restore CA1822 // Mark members as static
            {
            }
        }

        [Fact(Timeout = 5000)]
        public void TestYieldInActor()
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
            [IgnoreEvents(typeof(UnitEvent))]
            private class Init : State
            {
            }

            private async Task InitOnEntry()
            {
                this.SendEvent(this.Id, UnitEvent.Instance);
                await Task.Yield();
                this.SendEvent(this.Id, UnitEvent.Instance);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestYieldInStateMachine()
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
                Task[] tasks = new Task[2];
                for (int i = 0; i < 2; i++)
                {
                    tasks[i] = this.YieldedRandomAsync();
                }

                await Task.WhenAll(tasks);
            }

            private async Task YieldedRandomAsync()
            {
                await Task.Yield();
                this.RandomBoolean();
            }
        }

        [Fact(Timeout = 5000)]
        public void TestYieldLoopInActor()
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

            private async Task InitOnEntry()
            {
                Task[] tasks = new Task[2];
                for (int i = 0; i < 2; i++)
                {
                    tasks[i] = this.YieldedRandomAsync();
                }

                await Task.WhenAll(tasks);
            }

            private async Task YieldedRandomAsync()
            {
                await Task.Yield();
                this.RandomBoolean();
            }
        }

        [Fact(Timeout = 5000)]
        public void TestYieldLoopInStateMachine()
        {
            this.Test(r =>
            {
                r.CreateActor(typeof(M2));
            },
            configuration: this.GetConfiguration().WithTestingIterations(100));
        }

        [OnEventDoAction(typeof(UnitEvent), nameof(IgnoreUnitEvent))]
        private class A3 : Actor
        {
            protected override async Task OnInitializeAsync(Event initialEvent)
            {
                this.SendEvent(this.Id, UnitEvent.Instance);
                await AsyncProvider.YieldAsync();
                this.SendEvent(this.Id, UnitEvent.Instance);
            }

#pragma warning disable CA1822 // Mark members as static
            private void IgnoreUnitEvent()
#pragma warning restore CA1822 // Mark members as static
            {
            }
        }

        [Fact(Timeout = 5000)]
        public void TestUncontrolledYieldInActor()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(A3));
            },
            configuration: this.GetConfiguration().WithTestingIterations(100),
            errorChecker: (e) =>
            {
                Assert.StartsWith($"Invoking '{ExpectedMethodName}' returned task", e);
            });
        }

        private class M3 : StateMachine
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
                await AsyncProvider.YieldAsync();
                this.SendEvent(this.Id, UnitEvent.Instance);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestUncontrolledYieldInStateMachine()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M3));
            },
            configuration: this.GetConfiguration().WithTestingIterations(100),
            errorChecker: (e) =>
            {
                Assert.StartsWith($"Invoking '{ExpectedMethodName}' returned task", e);
            });
        }

        private class A4 : Actor
        {
            protected override async Task OnInitializeAsync(Event initialEvent)
            {
                Task[] tasks = new Task[2];
                for (int i = 0; i < 2; i++)
                {
                    tasks[i] = this.YieldedRandomAsync();
                }

                await Task.WhenAll(tasks);
            }

            private async Task YieldedRandomAsync()
            {
                await AsyncProvider.YieldAsync();
                this.RandomBoolean();
            }
        }

        [Fact(Timeout = 5000)]
        public void TestUncontrolledYieldLoopInActor()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(A4));
            },
            configuration: this.GetConfiguration().WithTestingIterations(100),
            errorChecker: (e) =>
            {
                Assert.StartsWith($"Invoking '{ExpectedMethodName}' returned task", e);
            });
        }

        private class M4 : StateMachine
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
                    tasks[i] = this.YieldedRandomAsync();
                }

                await Task.WhenAll(tasks);
            }

            private async Task YieldedRandomAsync()
            {
                await AsyncProvider.YieldAsync();
                this.RandomBoolean();
            }
        }

        [Fact(Timeout = 5000)]
        public void TestUncontrolledYieldLoopInStateMachine()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M4));
            },
            configuration: this.GetConfiguration().WithTestingIterations(100),
            errorChecker: (e) =>
            {
                Assert.StartsWith($"Invoking '{ExpectedMethodName}' returned task", e);
            });
        }
    }
}
