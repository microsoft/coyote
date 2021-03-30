// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Specifications;
using Microsoft.Coyote.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Actors.BugFinding.Tests.Specifications
{
    public class TaskSafetyMonitorTests : BaseActorBugFindingTest
    {
        public TaskSafetyMonitorTests(ITestOutputHelper output)
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
            private class Init : State
            {
            }

            private void HandleNotify()
            {
                this.Assert(false, "Reached test assertion.");
            }
        }

        [Fact(Timeout = 5000)]
        public void TestSafetyMonitorInvocationInSynchronousTask()
        {
            this.TestWithError(async () =>
            {
                Specification.RegisterMonitor<SafetyMonitor>();
                async Task WriteAsync()
                {
                    await Task.CompletedTask;
                    Specification.Monitor<SafetyMonitor>(new Notify());
                }

                await WriteAsync();
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            expectedError: "Reached test assertion.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestSafetyMonitorInvocationInAsynchronousTask()
        {
            this.TestWithError(async () =>
            {
                Specification.RegisterMonitor<SafetyMonitor>();
                async Task WriteWithDelayAsync()
                {
                    await Task.Delay(1);
                    Specification.Monitor<SafetyMonitor>(new Notify());
                }

                await WriteWithDelayAsync();
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            expectedError: "Reached test assertion.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestSafetyMonitorInvocationInParallelTask()
        {
            this.TestWithError(async () =>
            {
                Specification.RegisterMonitor<SafetyMonitor>();
                await Task.Run(() =>
                {
                    Specification.Monitor<SafetyMonitor>(new Notify());
                });
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            expectedError: "Reached test assertion.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestSafetyMonitorInvocationInParallelSynchronousTask()
        {
            this.TestWithError(async () =>
            {
                Specification.RegisterMonitor<SafetyMonitor>();
                await Task.Run(async () =>
                {
                    await Task.CompletedTask;
                    Specification.Monitor<SafetyMonitor>(new Notify());
                });
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            expectedError: "Reached test assertion.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestSafetyMonitorInvocationInParallelAsynchronousTask()
        {
            this.TestWithError(async () =>
            {
                Specification.RegisterMonitor<SafetyMonitor>();
                await Task.Run(async () =>
                {
                    await Task.Delay(1);
                    Specification.Monitor<SafetyMonitor>(new Notify());
                });
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            expectedError: "Reached test assertion.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestSafetyMonitorInvocationInNestedParallelSynchronousTask()
        {
            this.TestWithError(async () =>
            {
                Specification.RegisterMonitor<SafetyMonitor>();
                await Task.Run(async () =>
                {
                    await Task.Run(async () =>
                    {
                        await Task.CompletedTask;
                        Specification.Monitor<SafetyMonitor>(new Notify());
                    });
                });
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            expectedError: "Reached test assertion.",
            replay: true);
        }
    }
}
