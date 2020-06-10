// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Production.Tests.Actors.StateMachines
{
    public class GetOperationGroupIdTests : BaseProductionTest
    {
        public GetOperationGroupIdTests(ITestOutputHelper output)
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
            public ActorId Id;

            public E(ActorId id)
            {
                this.Id = id;
            }
        }

        private class M1 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private void InitOnEntry(Event e)
            {
                var tcs = (e as SetupEvent).Tcs;
                tcs.SetResult(this.OperationGroupId == Guid.Empty);
            }
        }

        [Fact(Timeout = 5000)]
        public async Task TestGetOperationGroupIdNotSet()
        {
            await this.RunAsync(async r =>
            {
                var tcs = TaskCompletionSource.Create<bool>();
                r.CreateActor(typeof(M1), new SetupEvent(tcs));

                var result = await this.GetResultAsync(tcs);
                Assert.True(result);
            });
        }

        private class M2 : StateMachine
        {
            private TaskCompletionSource<bool> Tcs;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E), nameof(CheckEvent))]
            private class Init : State
            {
            }

            private void InitOnEntry(Event e)
            {
                this.Tcs = (e as SetupEvent).Tcs;
                this.Runtime.SendEvent(this.Id, new E(this.Id), OperationGroup);
            }

            private void CheckEvent()
            {
                this.Tcs.SetResult(this.OperationGroupId == OperationGroup);
            }
        }

        [Fact(Timeout = 5000)]
        public async Task TestGetOperationGroupIdSet()
        {
            await this.RunAsync(async r =>
            {
                var tcs = TaskCompletionSource.Create<bool>();
                r.CreateActor(typeof(M2), new SetupEvent(tcs));

                var result = await this.GetResultAsync(tcs);
                Assert.True(result);
            });
        }
    }
}
