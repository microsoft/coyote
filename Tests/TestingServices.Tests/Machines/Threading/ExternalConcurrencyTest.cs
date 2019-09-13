// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using Microsoft.Coyote.Machines;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests
{
    public class ExternalConcurrencyInMachineTest : BaseTest
    {
        public ExternalConcurrencyInMachineTest(ITestOutputHelper output)
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
            private class Init : MachineState
            {
            }

            private async Task InitOnEntry()
            {
                await Task.Run(() =>
                {
                    this.Send(this.Id, new E());
                });
            }
        }

        [Fact(Timeout = 5000)]
        public void TestExternalTaskSendingEvent()
        {
            this.TestWithError(r =>
            {
                r.CreateMachine(typeof(M1));
            },
            expectedError: "Task with id '' that is not controlled by the Coyote runtime invoked a runtime method.",
            replay: true);
        }

        private class M2 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private async Task InitOnEntry()
            {
                await Task.Run(() =>
                {
                    this.Random();
                });
            }
        }

        [Fact(Timeout=5000)]
        public void TestExternalTaskInvokingRandom()
        {
            this.TestWithError(r =>
            {
                r.CreateMachine(typeof(M2));
            },
            expectedError: "Task with id '' that is not controlled by the Coyote runtime invoked a runtime method.",
            replay: true);
        }
    }
}
