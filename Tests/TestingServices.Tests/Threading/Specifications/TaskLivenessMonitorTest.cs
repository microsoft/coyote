// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Specifications;
using Microsoft.Coyote.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests
{
    public class TaskLivenessMonitorTest : BaseTest
    {
        public TaskLivenessMonitorTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class Notify : Event
        {
        }

        private class LivenessMonitor : Monitor
        {
            [Start]
            [Hot]
            [OnEventGotoState(typeof(Notify), typeof(Done))]
            private class Init : MonitorState
            {
            }

            [Cold]
            private class Done : MonitorState
            {
            }
        }

        [Fact(Timeout = 5000)]
        public void TestLivenessMonitorInvocationInSynchronousTask()
        {
            this.Test(async () =>
            {
                Specification.RegisterMonitor<LivenessMonitor>();
                async ControlledTask WriteAsync()
                {
                    await ControlledTask.CompletedTask;
                    Specification.Monitor<LivenessMonitor>(new Notify());
                }

                await WriteAsync();
            },
            configuration: GetConfiguration().WithNumberOfIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestLivenessMonitorInvocationInAsynchronousTask()
        {
            this.Test(async () =>
            {
                Specification.RegisterMonitor<LivenessMonitor>();
                async ControlledTask WriteWithDelayAsync()
                {
                    await ControlledTask.Delay(1);
                    Specification.Monitor<LivenessMonitor>(new Notify());
                }

                await WriteWithDelayAsync();
            },
            configuration: GetConfiguration().WithNumberOfIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestLivenessMonitorInvocationInParallelTask()
        {
            this.Test(async () =>
            {
                Specification.RegisterMonitor<LivenessMonitor>();
                await ControlledTask.Run(() =>
                {
                    Specification.Monitor<LivenessMonitor>(new Notify());
                });
            },
            configuration: GetConfiguration().WithNumberOfIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestLivenessMonitorInvocationInParallelSynchronousTask()
        {
            this.Test(async () =>
            {
                Specification.RegisterMonitor<LivenessMonitor>();
                await ControlledTask.Run(async () =>
                {
                    await ControlledTask.CompletedTask;
                    Specification.Monitor<LivenessMonitor>(new Notify());
                });
            },
            configuration: GetConfiguration().WithNumberOfIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestLivenessMonitorInvocationInParallelAsynchronousTask()
        {
            this.Test(async () =>
            {
                Specification.RegisterMonitor<LivenessMonitor>();
                await ControlledTask.Run(async () =>
                {
                    await ControlledTask.Delay(1);
                    Specification.Monitor<LivenessMonitor>(new Notify());
                });
            },
            configuration: GetConfiguration().WithNumberOfIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestLivenessMonitorInvocationInNestedParallelSynchronousTask()
        {
            this.Test(async () =>
            {
                Specification.RegisterMonitor<LivenessMonitor>();
                await ControlledTask.Run(async () =>
                {
                    await ControlledTask.Run(async () =>
                    {
                        await ControlledTask.CompletedTask;
                        Specification.Monitor<LivenessMonitor>(new Notify());
                    });
                });
            },
            configuration: GetConfiguration().WithNumberOfIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestLivenessMonitorInvocationInSynchronousTaskFailure()
        {
            this.TestWithError(async () =>
            {
                Specification.RegisterMonitor<LivenessMonitor>();
                async ControlledTask WriteAsync()
                {
                    await ControlledTask.CompletedTask;
                }

                await WriteAsync();
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "Monitor 'LivenessMonitor' detected liveness bug in hot state 'Init' at the end of program execution.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestLivenessMonitorInvocationInAsynchronousTaskFailure()
        {
            this.TestWithError(async () =>
            {
                Specification.RegisterMonitor<LivenessMonitor>();
                async ControlledTask WriteWithDelayAsync()
                {
                    await ControlledTask.Delay(1);
                }

                await WriteWithDelayAsync();
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "Monitor 'LivenessMonitor' detected liveness bug in hot state 'Init' at the end of program execution.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestLivenessMonitorInvocationInParallelTaskFailure()
        {
            this.TestWithError(async () =>
            {
                Specification.RegisterMonitor<LivenessMonitor>();
                await ControlledTask.Run(() =>
                {
                });
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "Monitor 'LivenessMonitor' detected liveness bug in hot state 'Init' at the end of program execution.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestLivenessMonitorInvocationInParallelSynchronousTaskFailure()
        {
            this.TestWithError(async () =>
            {
                Specification.RegisterMonitor<LivenessMonitor>();
                await ControlledTask.Run(async () =>
                {
                    await ControlledTask.CompletedTask;
                });
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "Monitor 'LivenessMonitor' detected liveness bug in hot state 'Init' at the end of program execution.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestLivenessMonitorInvocationInParallelAsynchronousTaskFailure()
        {
            this.TestWithError(async () =>
            {
                Specification.RegisterMonitor<LivenessMonitor>();
                await ControlledTask.Run(async () =>
                {
                    await ControlledTask.Delay(1);
                });
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "Monitor 'LivenessMonitor' detected liveness bug in hot state 'Init' at the end of program execution.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestLivenessMonitorInvocationInNestedParallelSynchronousTaskFailure()
        {
            this.TestWithError(async () =>
            {
                Specification.RegisterMonitor<LivenessMonitor>();
                await ControlledTask.Run(async () =>
                {
                    await ControlledTask.Run(async () =>
                    {
                        await ControlledTask.CompletedTask;
                    });
                });
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "Monitor 'LivenessMonitor' detected liveness bug in hot state 'Init' at the end of program execution.",
            replay: true);
        }
    }
}
