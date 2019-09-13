// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.Coyote.Machines;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests
{
    public class IdempotentRegisterMonitorTest : BaseTest
    {
        public IdempotentRegisterMonitorTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class Counter
        {
            public int Value;

            public Counter()
            {
                this.Value = 0;
            }
        }

        private class E : Event
        {
            public Counter Counter;

            public E(Counter counter)
            {
                this.Counter = counter;
            }
        }

        private class M : Monitor
        {
            [Start]
            [OnEventDoAction(typeof(E), nameof(Check))]
            private class Init : MonitorState
            {
            }

            private void Check()
            {
                var counter = (this.ReceivedEvent as E).Counter;
                counter.Value++;
            }
        }

        private class N : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                Counter counter = new Counter();
                this.Monitor(typeof(M), new E(counter));
                this.Assert(counter.Value == 1, "Monitor created more than once.");
            }
        }

        [Fact(Timeout=5000)]
        public void TestIdempotentRegisterMonitorInvocation()
        {
            this.Test(r =>
            {
                r.RegisterMonitor(typeof(M));
                r.RegisterMonitor(typeof(M));
                MachineId n = r.CreateMachine(typeof(N));
            });
        }
    }
}
