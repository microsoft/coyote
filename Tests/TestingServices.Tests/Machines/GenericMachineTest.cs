// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests
{
    public class GenericMachineTest : BaseTest
    {
        public GenericMachineTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class M<T> : Machine
        {
            private T Item;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Item = default;
                this.Goto<Active>();
            }

            [OnEntry(nameof(ActiveInit))]
            private class Active : MachineState
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

        [Fact(Timeout=5000)]
        public void TestGenericMachine1()
        {
            this.Test(r =>
            {
                r.CreateMachine(typeof(M<int>));
            });
        }

        [Fact(Timeout=5000)]
        public void TestGenericMachine2()
        {
            this.Test(r =>
            {
                r.CreateMachine(typeof(N));
            });
        }
    }
}
