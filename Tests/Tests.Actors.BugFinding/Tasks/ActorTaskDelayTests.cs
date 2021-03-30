// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Tests.Common.Events;
using Microsoft.Coyote.Tests.Common.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Actors.BugFinding.Tests
{
    public class ActorTaskDelayTests : BaseActorBugFindingTest
    {
        private static string ExpectedMethodName { get; } = $"{typeof(AsyncProvider).FullName}.{nameof(AsyncProvider.DelayAsync)}";

        public ActorTaskDelayTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [OnEventDoAction(typeof(UnitEvent), nameof(IgnoreUnitEvent))]
        private class A1 : Actor
        {
            protected override async Task OnInitializeAsync(Event initialEvent)
            {
                this.SendEvent(this.Id, UnitEvent.Instance);
                await Task.Delay(10);
                this.SendEvent(this.Id, UnitEvent.Instance);
            }

#pragma warning disable CA1822 // Mark members as static
            private void IgnoreUnitEvent()
#pragma warning restore CA1822 // Mark members as static
            {
            }
        }

        [Fact(Timeout = 5000)]
        public void TestDelayInActor()
        {
            this.Test(r =>
            {
                r.CreateActor(typeof(A1));
            },
            configuration: GetConfiguration().WithTestingIterations(100));
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
        public void TestDelayInStateMachine()
        {
            this.Test(r =>
            {
                r.CreateActor(typeof(M1));
            },
            configuration: GetConfiguration().WithTestingIterations(100));
        }

        [OnEventDoAction(typeof(UnitEvent), nameof(IgnoreUnitEvent))]
        private class A2 : Actor
        {
            protected override async Task OnInitializeAsync(Event initialEvent)
            {
                this.SendEvent(this.Id, UnitEvent.Instance);
                await Task.Delay(10).ConfigureAwait(false);
                this.SendEvent(this.Id, UnitEvent.Instance);
            }

#pragma warning disable CA1822 // Mark members as static
            private void IgnoreUnitEvent()
#pragma warning restore CA1822 // Mark members as static
            {
            }
        }

        [Fact(Timeout = 5000)]
        public void TestDelayWithOtherSynchronizationContextInActor()
        {
            this.Test(r =>
            {
                r.CreateActor(typeof(A2));
            },
            configuration: GetConfiguration().WithTestingIterations(100));
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
        public void TestDelayWithOtherSynchronizationContextInStateMachine()
        {
            this.Test(r =>
            {
                r.CreateActor(typeof(M2));
            },
            configuration: GetConfiguration().WithTestingIterations(100));
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
                this.RandomBoolean();
            }
        }

        [Fact(Timeout = 5000)]
        public void TestDelayLoopWithOtherSynchronizationContextInActor()
        {
            this.Test(r =>
            {
                r.CreateActor(typeof(A3));
            },
            configuration: GetConfiguration().WithTestingIterations(100));
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
                this.RandomBoolean();
            }
        }

        [Fact(Timeout = 5000)]
        public void TestDelayLoopWithOtherSynchronizationContextInStateMachine()
        {
            this.Test(r =>
            {
                r.CreateActor(typeof(M3));
            },
            configuration: GetConfiguration().WithTestingIterations(100));
        }

        [OnEventDoAction(typeof(UnitEvent), nameof(IgnoreUnitEvent))]
        private class A4 : Actor
        {
            protected override async Task OnInitializeAsync(Event initialEvent)
            {
                this.SendEvent(this.Id, UnitEvent.Instance);
                await AsyncProvider.DelayAsync(10);
                this.SendEvent(this.Id, UnitEvent.Instance);
            }

#pragma warning disable CA1822 // Mark members as static
            private void IgnoreUnitEvent()
#pragma warning restore CA1822 // Mark members as static
            {
            }
        }

        [Fact(Timeout = 5000)]
        public void TestUncontrolledDelayInActor()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(A4));
            },
            configuration: GetConfiguration().WithTestingIterations(100),
            errorChecker: (e) =>
            {
                Assert.StartsWith($"Method '{ExpectedMethodName}' returned an uncontrolled task", e);
            });
        }

        private class M4 : StateMachine
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
                await AsyncProvider.DelayAsync(10);
                this.SendEvent(this.Id, UnitEvent.Instance);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestUncontrolledDelayInStateMachine()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M4));
            },
            configuration: GetConfiguration().WithTestingIterations(100),
            errorChecker: (e) =>
            {
                Assert.StartsWith($"Method '{ExpectedMethodName}' returned an uncontrolled task", e);
            });
        }

        [OnEventDoAction(typeof(UnitEvent), nameof(IgnoreUnitEvent))]
        private class A5 : Actor
        {
            protected override async Task OnInitializeAsync(Event initialEvent)
            {
                this.SendEvent(this.Id, UnitEvent.Instance);
                await AsyncProvider.DelayAsync(10).ConfigureAwait(false);
                this.SendEvent(this.Id, UnitEvent.Instance);
            }

#pragma warning disable CA1822 // Mark members as static
            private void IgnoreUnitEvent()
#pragma warning restore CA1822 // Mark members as static
            {
            }
        }

        [Fact(Timeout = 5000)]
        public void TestUncontrolledDelayWithOtherSynchronizationContextInActor()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(A5));
            },
            configuration: GetConfiguration().WithTestingIterations(100),
            errorChecker: (e) =>
            {
                Assert.StartsWith($"Method '{ExpectedMethodName}' returned an uncontrolled task", e);
            });
        }

        private class M5 : StateMachine
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
                await AsyncProvider.DelayAsync(10).ConfigureAwait(false);
                this.SendEvent(this.Id, UnitEvent.Instance);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestUncontrolledDelayWithOtherSynchronizationContextInStateMachine()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M5));
            },
            configuration: GetConfiguration().WithTestingIterations(100),
            errorChecker: (e) =>
            {
                Assert.StartsWith($"Method '{ExpectedMethodName}' returned an uncontrolled task", e);
            });
        }

        private class A6 : Actor
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
                await AsyncProvider.DelayAsync(10).ConfigureAwait(false);
                this.RandomBoolean();
            }
        }

        [Fact(Timeout = 5000)]
        public void TestUncontrolledDelayLoopWithOtherSynchronizationContextInActor()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(A6));
            },
            configuration: GetConfiguration().WithTestingIterations(100),
            errorChecker: (e) =>
            {
                Assert.StartsWith($"Method '{ExpectedMethodName}' returned an uncontrolled task", e);
            });
        }

        private class M6 : StateMachine
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
                await AsyncProvider.DelayAsync(10).ConfigureAwait(false);
                this.RandomBoolean();
            }
        }

        [Fact(Timeout = 5000)]
        public void TestUncontrolledDelayLoopWithOtherSynchronizationContextInStateMachine()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M6));
            },
            configuration: GetConfiguration().WithTestingIterations(100),
            errorChecker: (e) =>
            {
                Assert.StartsWith($"Method '{ExpectedMethodName}' returned an uncontrolled task", e);
            });
        }
    }
}
