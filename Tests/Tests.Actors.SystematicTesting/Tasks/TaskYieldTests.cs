// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Tests.Common.Events;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Actors.SystematicTesting.Tests
{
    public class TaskYieldTests : BaseActorSystematicTest
    {
        public TaskYieldTests(ITestOutputHelper output)
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

            private void IgnoreUnitEvent()
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
            configuration: GetConfiguration().WithTestingIterations(100));
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
            configuration: GetConfiguration().WithTestingIterations(100));
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
            configuration: GetConfiguration().WithTestingIterations(100));
        }
    }
}
