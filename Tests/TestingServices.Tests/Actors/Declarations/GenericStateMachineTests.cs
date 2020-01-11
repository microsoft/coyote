// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Actors;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests.Actors
{
    public class GenericMachineTests : BaseTest
    {
        public GenericMachineTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private class M<T> : StateMachine
        {
            private T Item;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private Transition InitOnEntry()
            {
                this.Item = default;
                return this.GotoState<Active>();
            }

            [OnEntry(nameof(ActiveInit))]
            private class Active : State
            {
            }

            private void ActiveInit()
            {
                this.Assert(this.Item is int);
            }
        }

        private class N : M<int>
        {
        }

        [Fact(Timeout = 5000)]
        public void TestGenericMachine1()
        {
            this.Test(r =>
            {
                r.CreateActor(typeof(M<int>));
            });
        }

        [Fact(Timeout = 5000)]
        public void TestGenericMachine2()
        {
            this.Test(r =>
            {
                r.CreateActor(typeof(N));
            });
        }
    }
}
