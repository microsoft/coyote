// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Coyote.Actors;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.SystematicTesting.Tests.Runtime
{
    public class GetOperationGroupIdTests : BaseSystematicTest
    {
        public GetOperationGroupIdTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private static Guid OperationGroup = Guid.NewGuid();

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

            private void InitOnEntry()
            {
                this.Assert(this.CurrentOperation == null, "CurrentOperation is not null");
            }
        }

        private class M2 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E), nameof(CheckEvent))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.Runtime.SendEvent(this.Id, new E(this.Id), new Operation() { Id = OperationGroup });
            }

            private void CheckEvent()
            {
                var id = this.CurrentOperation.Id;
                this.Assert(id == OperationGroup, $"OperationGroupId is not '{OperationGroup}', but {id}.");
            }
        }

        [Fact(Timeout = 5000)]
        public void TestGetOperationGroupIdNotSet()
        {
            this.Test(r =>
            {
                r.CreateActor(typeof(M1));
            });
        }

        [Fact(Timeout = 5000)]
        public void TestGetOperationGroupIdSet()
        {
            this.Test(r =>
            {
                r.CreateActor(typeof(M2));
            });
        }
    }
}
