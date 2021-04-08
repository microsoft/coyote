// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Specifications;
using Microsoft.Coyote.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Actors.BugFinding.Tests.Specifications
{
    public class TaskLivenessMonitorTests : BaseActorBugFindingTest
    {
        public TaskLivenessMonitorTests(ITestOutputHelper output)
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
            private class Init : State
            {
            }

            [Cold]
            private class Done : State
            {
            }
        }

        [Fact(Timeout = 5000)]
        public void TestLivenessMonitorInvocationInSynchronousTask()
        {
            this.Test(async () =>
            {
                Specification.RegisterMonitor<LivenessMonitor>();
                async Task WriteAsync()
                {
                    await Task.CompletedTask;
                    Specification.Monitor<LivenessMonitor>(new Notify());
                }

                await WriteAsync();
            },
            configuration: this.GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestLivenessMonitorInvocationInAsynchronousTask()
        {
            this.Test(async () =>
            {
                Specification.RegisterMonitor<LivenessMonitor>();
                async Task WriteWithDelayAsync()
                {
                    await Task.Delay(1);
                    Specification.Monitor<LivenessMonitor>(new Notify());
                }

                await WriteWithDelayAsync();
            },
            configuration: this.GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestLivenessMonitorInvocationInParallelTask()
        {
            this.Test(async () =>
            {
                Specification.RegisterMonitor<LivenessMonitor>();
                await Task.Run(() =>
                {
                    Specification.Monitor<LivenessMonitor>(new Notify());
                });
            },
            configuration: this.GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestLivenessMonitorInvocationInParallelSynchronousTask()
        {
            this.Test(async () =>
            {
                Specification.RegisterMonitor<LivenessMonitor>();
                await Task.Run(async () =>
                {
                    await Task.CompletedTask;
                    Specification.Monitor<LivenessMonitor>(new Notify());
                });
            },
            configuration: this.GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestLivenessMonitorInvocationInParallelAsynchronousTask()
        {
            this.Test(async () =>
            {
                Specification.RegisterMonitor<LivenessMonitor>();
                await Task.Run(async () =>
                {
                    await Task.Delay(1);
                    Specification.Monitor<LivenessMonitor>(new Notify());
                });
            },
            configuration: this.GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestLivenessMonitorInvocationInNestedParallelSynchronousTask()
        {
            this.Test(async () =>
            {
                Specification.RegisterMonitor<LivenessMonitor>();
                await Task.Run(async () =>
                {
                    await Task.Run(async () =>
                    {
                        await Task.CompletedTask;
                        Specification.Monitor<LivenessMonitor>(new Notify());
                    });
                });
            },
            configuration: this.GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestLivenessMonitorInvocationInSynchronousTaskFailure()
        {
            this.TestWithError(async () =>
            {
                Specification.RegisterMonitor<LivenessMonitor>();
                async Task WriteAsync()
                {
                    await Task.CompletedTask;
                }

                await WriteAsync();
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
            expectedError: "LivenessMonitor detected liveness bug in hot state 'Init' at the end of program execution.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestLivenessMonitorInvocationInAsynchronousTaskFailure()
        {
            this.TestWithError(async () =>
            {
                Specification.RegisterMonitor<LivenessMonitor>();
                async Task WriteWithDelayAsync()
                {
                    await Task.Delay(1);
                }

                await WriteWithDelayAsync();
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
            expectedError: "LivenessMonitor detected liveness bug in hot state 'Init' at the end of program execution.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestLivenessMonitorInvocationInParallelTaskFailure()
        {
            this.TestWithError(async () =>
            {
                Specification.RegisterMonitor<LivenessMonitor>();
                await Task.Run(() =>
                {
                });
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
            expectedError: "LivenessMonitor detected liveness bug in hot state 'Init' at the end of program execution.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestLivenessMonitorInvocationInParallelSynchronousTaskFailure()
        {
            this.TestWithError(async () =>
            {
                Specification.RegisterMonitor<LivenessMonitor>();
                await Task.Run(async () =>
                {
                    await Task.CompletedTask;
                });
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
            expectedError: "LivenessMonitor detected liveness bug in hot state 'Init' at the end of program execution.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestLivenessMonitorInvocationInParallelAsynchronousTaskFailure()
        {
            this.TestWithError(async () =>
            {
                Specification.RegisterMonitor<LivenessMonitor>();
                await Task.Run(async () =>
                {
                    await Task.Delay(1);
                });
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
            expectedError: "LivenessMonitor detected liveness bug in hot state 'Init' at the end of program execution.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestLivenessMonitorInvocationInNestedParallelSynchronousTaskFailure()
        {
            this.TestWithError(async () =>
            {
                Specification.RegisterMonitor<LivenessMonitor>();
                await Task.Run(async () =>
                {
                    await Task.Run(async () =>
                    {
                        await Task.CompletedTask;
                    });
                });
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
            expectedError: "LivenessMonitor detected liveness bug in hot state 'Init' at the end of program execution.",
            replay: true);
        }
    }
}
