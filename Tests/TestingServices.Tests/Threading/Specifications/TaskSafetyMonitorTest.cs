// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Specifications;
using Microsoft.Coyote.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests
{
    public class TaskSafetyMonitorTest : BaseTest
    {
        public TaskSafetyMonitorTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class Notify : Event
        {
        }

        private class SafetyMonitor : Monitor
        {
            [Start]
            [OnEventDoAction(typeof(Notify), nameof(HandleNotify))]
            private class Init : MonitorState
            {
            }

            private void HandleNotify()
            {
                this.Assert(false, "Bug found!");
            }
        }

        [Fact(Timeout = 5000)]
        public void TestSafetyMonitorInvocationInSynchronousTask()
        {
            this.TestWithError(async () =>
            {
                Specification.RegisterMonitor<SafetyMonitor>();
                async ControlledTask WriteAsync()
                {
                    await ControlledTask.CompletedTask;
                    Specification.Monitor<SafetyMonitor>(new Notify());
                }

                await WriteAsync();
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "Bug found!",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestSafetyMonitorInvocationInAsynchronousTask()
        {
            this.TestWithError(async () =>
            {
                Specification.RegisterMonitor<SafetyMonitor>();
                async ControlledTask WriteWithDelayAsync()
                {
                    await ControlledTask.Delay(1);
                    Specification.Monitor<SafetyMonitor>(new Notify());
                }

                await WriteWithDelayAsync();
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "Bug found!",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestSafetyMonitorInvocationInParallelTask()
        {
            this.TestWithError(async () =>
            {
                Specification.RegisterMonitor<SafetyMonitor>();
                await ControlledTask.Run(() =>
                {
                    Specification.Monitor<SafetyMonitor>(new Notify());
                });
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "Bug found!",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestSafetyMonitorInvocationInParallelSynchronousTask()
        {
            this.TestWithError(async () =>
            {
                Specification.RegisterMonitor<SafetyMonitor>();
                await ControlledTask.Run(async () =>
                {
                    await ControlledTask.CompletedTask;
                    Specification.Monitor<SafetyMonitor>(new Notify());
                });
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "Bug found!",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestSafetyMonitorInvocationInParallelAsynchronousTask()
        {
            this.TestWithError(async () =>
            {
                Specification.RegisterMonitor<SafetyMonitor>();
                await ControlledTask.Run(async () =>
                {
                    await ControlledTask.Delay(1);
                    Specification.Monitor<SafetyMonitor>(new Notify());
                });
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "Bug found!",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestSafetyMonitorInvocationInNestedParallelSynchronousTask()
        {
            this.TestWithError(async () =>
            {
                Specification.RegisterMonitor<SafetyMonitor>();
                await ControlledTask.Run(async () =>
                {
                    await ControlledTask.Run(async () =>
                    {
                        await ControlledTask.CompletedTask;
                        Specification.Monitor<SafetyMonitor>(new Notify());
                    });
                });
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "Bug found!",
            replay: true);
        }
    }
}
