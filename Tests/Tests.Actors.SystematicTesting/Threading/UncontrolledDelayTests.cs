// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Tests.Common.Events;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Actors.SystematicTesting.Tests
{
    public class UncontrolledDelayTests : BaseSystematicActorTest
    {
        public UncontrolledDelayTests(ITestOutputHelper output)
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

            private void IgnoreUnitEvent()
            {
            }
        }

        [Fact(Timeout = 5000)]
        public void TestUncontrolledDelayInActor()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(A1));
            },
            configuration: GetConfiguration().WithTestingIterations(100),
            errorChecker: (e) =>
            {
                Assert.True(e.StartsWith("Method 'OnInitializeAsync' returned an uncontrolled task"),
                    "Expected uncontrolled task from invoking the async method.");
            });
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
            configuration: GetConfiguration().WithTestingIterations(100),
            errorChecker: (e) =>
            {
                Assert.True(e.StartsWith("Method 'InitOnEntry' returned an uncontrolled task"),
                    "Expected uncontrolled task from invoking the async method.");
            });
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

            private void IgnoreUnitEvent()
            {
            }
        }

        [Fact(Timeout = 5000)]
        public void TestUncontrolledDelayWithOtherSynchronizationContextInActor()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(A2));
            },
            configuration: GetConfiguration().WithTestingIterations(100),
            errorChecker: (e) =>
            {
                Assert.True(e.StartsWith("Method 'OnInitializeAsync' returned an uncontrolled task"),
                    "Expected uncontrolled task from invoking the async method.");
            });
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
            configuration: GetConfiguration().WithTestingIterations(100),
            errorChecker: (e) =>
            {
                Assert.True(e.StartsWith("Method 'InitOnEntry' returned an uncontrolled task"),
                    "Expected uncontrolled task from invoking the async method.");
            });
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
        public void TestUncontrolledDelayLoopWithOtherSynchronizationContextInActor()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(A3));
            },
            configuration: GetConfiguration().WithTestingIterations(100),
            errorChecker: (e) =>
            {
                Assert.True(e.StartsWith("Method 'OnInitializeAsync' returned an uncontrolled task"),
                    "Expected uncontrolled task from invoking the async method.");
            });
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
        public void TestUncontrolledDelayLoopWithOtherSynchronizationContextInStateMachine()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M3));
            },
            configuration: GetConfiguration().WithTestingIterations(100),
            errorChecker: (e) =>
            {
                Assert.True(e.StartsWith("Method 'InitOnEntry' returned an uncontrolled task"),
                    "Expected uncontrolled task from invoking the async method.");
            });
        }
    }
}
