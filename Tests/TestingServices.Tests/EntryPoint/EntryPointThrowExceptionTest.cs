// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests
{
    public class EntryPointThrowExceptionTest : BaseTest
    {
        public EntryPointThrowExceptionTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class M : Machine
        {
            [Start]
            private class Init : MachineState
            {
            }
        }

        [Fact(Timeout=5000)]
        public void TestEntryPointThrowException()
        {
            this.TestWithException<InvalidOperationException>(r =>
            {
                MachineId m = r.CreateMachine(typeof(M));
                throw new InvalidOperationException();
            },
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestEntryPointNoMachinesThrowException()
        {
            this.TestWithException<InvalidOperationException>(r =>
            {
                throw new InvalidOperationException();
            },
            replay: true);
        }
    }
}
