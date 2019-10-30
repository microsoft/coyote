// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Machines;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Core.Tests
{
    public class NondeterministicChoiceTest : BaseTest
    {
        public NondeterministicChoiceTest(ITestOutputHelper output)
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
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.tcs = (this.ReceivedEvent as ConfigEvent).Tcs;
                this.tcs.SetResult(this.Random());
            }
        }

        [Fact(Timeout=5000)]
        public async Task TestNondeterministicBooleanChoiceInMachineHandler()
        {
            await this.RunAsync(async r =>
            {
                var tcs = new TaskCompletionSource<bool>();
                r.OnFailure += (ex) =>
                {
                    tcs.TrySetException(ex);
                };

                r.CreateMachine(typeof(M1), new ConfigEvent(tcs));

                await WaitAsync(tcs.Task);
                Assert.Null(tcs.Task.Exception);
                Assert.False(tcs.Task.IsCanceled);
                Assert.False(tcs.Task.IsFaulted);
                Assert.True(tcs.Task.IsCompleted);
            });
        }

        private class M2 : StateMachine
        {
            private TaskCompletionSource<bool> tcs;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.tcs = (this.ReceivedEvent as ConfigEvent).Tcs;
                this.tcs.SetResult(this.RandomInteger(10) == 5);
            }
        }

        [Fact(Timeout = 5000)]
        public async Task TestNondeterministicIntegerChoiceInMachineHandler()
        {
            await this.RunAsync(async r =>
            {
                var tcs = new TaskCompletionSource<bool>();
                r.OnFailure += (ex) =>
                {
                    tcs.TrySetException(ex);
                };

                r.CreateMachine(typeof(M2), new ConfigEvent(tcs));

                await WaitAsync(tcs.Task);
                Assert.Null(tcs.Task.Exception);
                Assert.False(tcs.Task.IsCanceled);
                Assert.False(tcs.Task.IsFaulted);
                Assert.True(tcs.Task.IsCompleted);
            });
        }
    }
}
