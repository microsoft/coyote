// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Coyote.Machines;
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

        private class M : StateMachine
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
