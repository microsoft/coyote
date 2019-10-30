// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Machines;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests
{
    public class UncontrolledMachineTaskTest : BaseTest
    {
        public UncontrolledMachineTaskTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class E : Event
        {
        }

        private class M1 : StateMachine
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
        public void TestUncontrolledTaskSendingEvent()
        {
            this.TestWithError(r =>
            {
                r.CreateMachine(typeof(M1));
            },
            expectedErrors: new string[]
            {
                "Machine '' is trying to wait for an uncontrolled task or awaiter to complete. Please make sure to avoid using " +
                    "concurrency APIs such as 'Task.Run', 'Task.Delay' or 'Task.Yield' inside machine handlers. If you are " +
                    "using external libraries that are executing concurrently, you will need to mock them during testing.",
                "Uncontrolled task with id '' invoked a runtime method. Please make sure to avoid using concurrency APIs such " +
                    "as 'Task.Run', 'Task.Delay' or 'Task.Yield' inside machine handlers or controlled tasks. If you are " +
                    "using external libraries that are executing concurrently, you will need to mock them during testing.",
            },
            replay: true);
        }

        private class M2 : StateMachine
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
        public void TestUncontrolledTaskInvokingRandom()
        {
            this.TestWithError(r =>
            {
                r.CreateMachine(typeof(M2));
            },
            expectedErrors: new string[]
            {
                "Machine '' is trying to wait for an uncontrolled task or awaiter to complete. Please make sure to avoid using " +
                    "concurrency APIs such as 'Task.Run', 'Task.Delay' or 'Task.Yield' inside machine handlers. If you are " +
                    "using external libraries that are executing concurrently, you will need to mock them during testing.",
                "Uncontrolled task with id '' invoked a runtime method. Please make sure to avoid using concurrency APIs such " +
                    "as 'Task.Run', 'Task.Delay' or 'Task.Yield' inside machine handlers or controlled tasks. If you are " +
                    "using external libraries that are executing concurrently, you will need to mock them during testing.",
            },
            replay: true);
        }
    }
}
