// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Actors.SystematicTesting.Tests
{
    public class TaskCompletionSourceTests : BaseActorSystematicTest
    {
        public TaskCompletionSourceTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private class SetupEvent : Event
        {
            internal readonly TaskCompletionSource<int> Tcs;

            internal SetupEvent(TaskCompletionSource<int> tcs)
            {
                this.Tcs = tcs;
            }
        }

        private class A1 : Actor
        {
            protected override async Task OnInitializeAsync(Event initialEvent)
            {
                var setupEvent = initialEvent as SetupEvent;
                setupEvent.Tcs.SetResult(3);
                await Task.CompletedTask;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestSetTaskCompletionSourceInActorInitializeAsync()
        {
            this.Test(async r =>
            {
                var tcs = new TaskCompletionSource<int>();
                r.CreateActor(typeof(A1), new SetupEvent(tcs));
                var result = await tcs.Task;
                Assert.Equal(3, result);
            },
            configuration: GetConfiguration().WithTestingIterations(100));
        }

        private class M1 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private async Task InitOnEntry(Event e)
            {
                var setupEvent = e as SetupEvent;
                setupEvent.Tcs.SetResult(3);
                await Task.CompletedTask;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestSetTaskCompletionSourceInStateMachineOnEntry()
        {
            this.Test(async r =>
            {
                var tcs = new TaskCompletionSource<int>();
                r.CreateActor(typeof(M1), new SetupEvent(tcs));
                var result = await tcs.Task;
                Assert.Equal(3, result);
            },
            configuration: GetConfiguration().WithTestingIterations(100));
        }

        [OnEventDoAction(typeof(SetupEvent), nameof(HandleSetupEvent))]
        private class A2 : Actor
        {
            private async Task HandleSetupEvent(Event e)
            {
                var setupEvent = e as SetupEvent;
                setupEvent.Tcs.SetResult(3);
                await Task.CompletedTask;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestSetTaskCompletionSourceInActorHandler()
        {
            this.Test(async r =>
            {
                var tcs = new TaskCompletionSource<int>();
                var id = r.CreateActor(typeof(A2));
                r.SendEvent(id, new SetupEvent(tcs));
                var result = await tcs.Task;
                Assert.Equal(3, result);
            },
            configuration: GetConfiguration().WithTestingIterations(100));
        }

        private class M2 : StateMachine
        {
            [Start]
            [OnEventDoAction(typeof(SetupEvent), nameof(HandleSetupEvent))]
            private class Init : State
            {
            }

            private async Task HandleSetupEvent(Event e)
            {
                var setupEvent = e as SetupEvent;
                setupEvent.Tcs.SetResult(3);
                await Task.CompletedTask;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestSetTaskCompletionSourceInStateMachineHandler()
        {
            this.Test(async r =>
            {
                var tcs = new TaskCompletionSource<int>();
                var id = r.CreateActor(typeof(M2));
                r.SendEvent(id, new SetupEvent(tcs));
                var result = await tcs.Task;
                Assert.Equal(3, result);
            },
            configuration: GetConfiguration().WithTestingIterations(100));
        }

        private class A3 : Actor
        {
            protected override async Task OnInitializeAsync(Event initialEvent)
            {
                var setupEvent = initialEvent as SetupEvent;
                var result = await setupEvent.Tcs.Task;
                this.Assert(result is 3);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestAwaitTaskCompletionSourceInActorInitializeAsync()
        {
            this.Test(r =>
            {
                var tcs = new TaskCompletionSource<int>();
                r.CreateActor(typeof(A3), new SetupEvent(tcs));
                tcs.SetResult(3);
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

            private async Task InitOnEntry(Event e)
            {
                var setupEvent = e as SetupEvent;
                var result = await setupEvent.Tcs.Task;
                this.Assert(result is 3);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestAwaitTaskCompletionSourceInStateMachineOnEntry()
        {
            this.Test(r =>
            {
                var tcs = new TaskCompletionSource<int>();
                r.CreateActor(typeof(M3), new SetupEvent(tcs));
                tcs.SetResult(3);
            },
            configuration: GetConfiguration().WithTestingIterations(100));
        }

        [OnEventDoAction(typeof(SetupEvent), nameof(HandleSetupEvent))]
        private class A4 : Actor
        {
            private async Task HandleSetupEvent(Event e)
            {
                var setupEvent = e as SetupEvent;
                var result = await setupEvent.Tcs.Task;
                this.Assert(result is 3);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestAwaitTaskCompletionSourceInActorHandler()
        {
            this.Test(r =>
            {
                var tcs = new TaskCompletionSource<int>();
                r.CreateActor(typeof(A4), new SetupEvent(tcs));
                tcs.SetResult(3);
            },
            configuration: GetConfiguration().WithTestingIterations(100));
        }

        private class M4 : StateMachine
        {
            [Start]
            [OnEventDoAction(typeof(SetupEvent), nameof(HandleSetupEvent))]
            private class Init : State
            {
            }

            private async Task HandleSetupEvent(Event e)
            {
                var setupEvent = e as SetupEvent;
                var result = await setupEvent.Tcs.Task;
                this.Assert(result is 3);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestAwaitTaskCompletionSourceInStateMachineHandler()
        {
            this.Test(r =>
            {
                var tcs = new TaskCompletionSource<int>();
                r.CreateActor(typeof(M3), new SetupEvent(tcs));
                tcs.SetResult(3);
            },
            configuration: GetConfiguration().WithTestingIterations(100));
        }
    }
}
