// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Coyote.Machines;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Core.Tests
{
    public class GetOperationGroupIdTest : BaseTest
    {
        public GetOperationGroupIdTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private static Guid OperationGroup = Guid.NewGuid();

        private class SetupEvent : Event
        {
            public TaskCompletionSource<bool> Tcs;

            public SetupEvent(TaskCompletionSource<bool> tcs)
            {
                this.Tcs = tcs;
            }
        }

        private class E : Event
        {
            public MachineId Id;

            public E(MachineId id)
            {
                this.Id = id;
            }
        }

        private class M1 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                var tcs = (this.ReceivedEvent as SetupEvent).Tcs;
                tcs.SetResult(this.OperationGroupId == Guid.Empty);
            }
        }

        [Fact(Timeout = 5000)]
        public async Task TestGetOperationGroupIdNotSet()
        {
            await this.RunAsync(async r =>
            {
                var tcs = new TaskCompletionSource<bool>();
                r.CreateMachine(typeof(M1), new SetupEvent(tcs));

                var result = await GetResultAsync(tcs.Task);
                Assert.True(result);
            });
        }

        private class M2 : Machine
        {
            private TaskCompletionSource<bool> Tcs;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E), nameof(CheckEvent))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Tcs = (this.ReceivedEvent as SetupEvent).Tcs;
                this.Runtime.SendEvent(this.Id, new E(this.Id), OperationGroup);
            }

            private void CheckEvent()
            {
                this.Tcs.SetResult(this.OperationGroupId == OperationGroup);
            }
        }

        [Fact(Timeout=5000)]
        public async Task TestGetOperationGroupIdSet()
        {
            await this.RunAsync(async r =>
            {
                var tcs = new TaskCompletionSource<bool>();
                r.CreateMachine(typeof(M2), new SetupEvent(tcs));

                var result = await GetResultAsync(tcs.Task);
                Assert.True(result);
            });
        }
    }
}
