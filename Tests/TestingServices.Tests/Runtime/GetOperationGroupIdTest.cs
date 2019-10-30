// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Coyote.Machines;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests
{
    public class GetOperationGroupIdTest : BaseTest
    {
        public GetOperationGroupIdTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private static Guid OperationGroup = Guid.NewGuid();

        private class E : Event
        {
            public MachineId Id;

            public E(MachineId id)
            {
                this.Id = id;
            }
        }

        private class M1 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                var id = this.Runtime.GetCurrentOperationGroupId(this.Id);
                this.Assert(id == Guid.Empty, $"OperationGroupId is not '{Guid.Empty}', but {id}.");
            }
        }

        private class M2 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E), nameof(CheckEvent))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Runtime.SendEvent(this.Id, new E(this.Id), OperationGroup);
            }

            private void CheckEvent()
            {
                var id = this.Runtime.GetCurrentOperationGroupId(this.Id);
                this.Assert(id == OperationGroup, $"OperationGroupId is not '{OperationGroup}', but {id}.");
            }
        }

        private class M3 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                var target = this.CreateMachine(typeof(M4));
                this.Runtime.GetCurrentOperationGroupId(target);
            }
        }

        private class M4 : StateMachine
        {
            [Start]
            private class Init : MachineState
            {
            }
        }

        [Fact(Timeout=5000)]
        public void TestGetOperationGroupIdNotSet()
        {
            this.Test(r =>
            {
                r.CreateMachine(typeof(M1));
            });
        }

        [Fact(Timeout=5000)]
        public void TestGetOperationGroupIdSet()
        {
            this.Test(r =>
            {
                r.CreateMachine(typeof(M2));
            });
        }

        [Fact(Timeout=5000)]
        public void TestGetOperationGroupIdOfNotCurrentMachine()
        {
            this.TestWithError(r =>
            {
                r.CreateMachine(typeof(M3));
            },
            expectedError: "Trying to access the operation group id of 'M4()', which is not the currently executing machine.",
            replay: true);
        }
    }
}
