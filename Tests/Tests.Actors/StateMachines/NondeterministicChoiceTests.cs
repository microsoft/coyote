// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Tasks;
using Xunit;
using Xunit.Abstractions;
using SystemTasks = System.Threading.Tasks;

namespace Microsoft.Coyote.Actors.Tests.StateMachines
{
    public class NondeterministicChoiceTests : BaseActorTest
    {
        public NondeterministicChoiceTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private class ConfigEvent : Event
        {
            public TaskCompletionSource<bool> Tcs;

            public ConfigEvent()
            {
            }

            public ConfigEvent(TaskCompletionSource<bool> tcs)
            {
                this.Tcs = tcs;
            }
        }

        private class M1 : StateMachine
        {
            private TaskCompletionSource<bool> tcs;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private void InitOnEntry(Event e)
            {
                this.tcs = (e as ConfigEvent).Tcs;
                this.tcs.SetResult(this.RandomBoolean());
            }
        }

        [Fact(Timeout = 5000)]
        public async SystemTasks.Task TestNondeterministicBooleanChoiceInMachineHandler()
        {
            await this.RunAsync(async r =>
            {
                var tcs = TaskCompletionSource.Create<bool>();
                r.OnFailure += (ex) =>
                {
                    tcs.TrySetException(ex);
                };

                r.CreateActor(typeof(M1), new ConfigEvent(tcs));

                await this.WaitAsync(tcs.Task);
                Assert.Null(tcs.Task.Exception);
                Assert.False(tcs.Task.IsCanceled);
                Assert.False(tcs.Task.IsFaulted);
                Assert.True(tcs.Task.IsCompleted);
            },
            handleFailures: false);
        }

        private class M2 : StateMachine
        {
            private TaskCompletionSource<bool> tcs;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private void InitOnEntry(Event e)
            {
                this.tcs = (e as ConfigEvent).Tcs;
                this.tcs.SetResult(this.RandomInteger(10) is 5);
            }
        }

        [Fact(Timeout = 5000)]
        public async SystemTasks.Task TestNondeterministicIntegerChoiceInMachineHandler()
        {
            await this.RunAsync(async r =>
            {
                var tcs = TaskCompletionSource.Create<bool>();
                r.OnFailure += (ex) =>
                {
                    tcs.TrySetException(ex);
                };

                r.CreateActor(typeof(M2), new ConfigEvent(tcs));

                await this.WaitAsync(tcs.Task);
                Assert.Null(tcs.Task.Exception);
                Assert.False(tcs.Task.IsCanceled);
                Assert.False(tcs.Task.IsFaulted);
                Assert.True(tcs.Task.IsCompleted);
            },
            handleFailures: false);
        }
    }
}
