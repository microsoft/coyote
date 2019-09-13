// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using Microsoft.Coyote.Machines;
using Microsoft.Coyote.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests
{
    public class MachineYieldTest : BaseTest
    {
        public MachineYieldTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class E : Event
        {
        }

        private class M1 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [IgnoreEvents(typeof(E))]
            private class Init : MachineState
            {
            }

            private async ControlledTask InitOnEntry()
            {
                this.Send(this.Id, new E());
                await ControlledTask.Yield();
                this.Send(this.Id, new E());
            }
        }

        [Fact(Timeout = 5000)]
        public void TestMachineYield()
        {
            this.Test(r =>
            {
                r.CreateMachine(typeof(M1));
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000));
        }

        private class M2 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [IgnoreEvents(typeof(E))]
            private class Init : MachineState
            {
            }

            private async ControlledTask InitOnEntry()
            {
                this.Send(this.Id, new E());
                await ControlledTask.Yield();
                this.Assert(false);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestMachineYieldWithAssertionFailure()
        {
            this.TestWithError(r =>
            {
                r.CreateMachine(typeof(M2));
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000),
            expectedError: "Detected an assertion failure.",
            replay: true);
        }

        private class M3 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [IgnoreEvents(typeof(E))]
            private class Init : MachineState
            {
            }

            private async Task InitOnEntry()
            {
                this.Send(this.Id, new E());
                await ControlledTask.Yield();
            }
        }

        [Fact(Timeout = 5000)]
        public void TestMachineYieldInTaskHandler()
        {
            this.TestWithError(r =>
            {
                r.CreateMachine(typeof(M3));
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000),
            expectedError: "Machine 'M3()' is executing a yield operation inside a handler that does not return a 'ControlledTask'.",
            replay: true);
        }

        private class M4 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [IgnoreEvents(typeof(E))]
            private class Init : MachineState
            {
            }

            private async Task InitOnEntry()
            {
                this.Send(this.Id, new E());
                await ControlledTask.Yield();
                this.Send(this.Id, new E());
            }
        }

        [Fact(Timeout = 5000)]
        public void TestMachineYieldInTaskHandler2()
        {
            this.TestWithError(r =>
            {
                r.CreateMachine(typeof(M4));
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000),
            expectedError: "Machine 'M4()' is executing a yield operation inside a handler that does not return a 'ControlledTask'.",
            replay: true);
        }
    }
}
