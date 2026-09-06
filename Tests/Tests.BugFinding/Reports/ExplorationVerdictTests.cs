// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.Specifications;
using Microsoft.Coyote.SystematicTesting;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.BugFinding.Tests.Reports
{
    public class ExplorationVerdictTests : BaseBugFindingTest
    {
        public ExplorationVerdictTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestCompleteExplorationVerdict()
        {
            var report = this.RunSystematicTest(async () =>
            {
                var entry = new SharedEntry();
                var task1 = entry.GetWriteResultWithDelayAsync(3);
                var task2 = entry.GetWriteResultWithDelayAsync(5);
                await Task.WhenAll(task1, task2);
            },
            configuration: this.GetConfiguration().WithTestingIterations(10));

            var verdict = report.GetVerdict();
            Assert.Equal(ExplorationStatus.Complete, verdict.Status);
            Assert.Empty(verdict.Reasons);
            Assert.Equal(0, report.NumOfTruncatedPaths);
            Assert.DoesNotContain(ExplorationWarning.ExceededUnfairStepsBound, verdict.Warnings);
            Assert.Equal("Complete", verdict.ToString());
        }

        [Fact(Timeout = 5000)]
        public void TestUncontrolledInvocationVerdict()
        {
            var report = this.RunSystematicTest(() =>
            {
                var task = new Task(() => { });
                task.ContinueWith(_ => { }, TaskScheduler.Current);
            },
            configuration: this.GetConfiguration()
                .WithPartiallyControlledConcurrencyAllowed()
                .WithTestingIterations(10));

            var verdict = report.GetVerdict();
            Assert.True(report.UncontrolledInvocations.Count > 0);
            Assert.Equal(ExplorationStatus.Incomplete, verdict.Status);
            Assert.Contains(IncompleteExplorationReason.UncontrolledInvocations, verdict.Reasons);
            Assert.StartsWith("Incomplete (", verdict.ToString());
        }

        [Fact(Timeout = 5000)]
        public void TestTruncatedExecutionPathsVerdict()
        {
            var report = this.RunSystematicTest(async () =>
            {
                var task = Task.Run(() =>
                {
                    while (true)
                    {
                        SchedulingPoint.Interleave();
                    }
                });

                await task;
            },
            configuration: this.GetConfiguration().WithMaxSchedulingSteps(10).WithTestingIterations(3));

            var verdict = report.GetVerdict();
            Assert.True(report.NumOfTruncatedPaths > 0);
            Assert.Equal(ExplorationStatus.Incomplete, verdict.Status);
            Assert.Contains(IncompleteExplorationReason.TruncatedExecutionPaths, verdict.Reasons);
        }

        [Fact(Timeout = 5000)]
        public void TestExceededUnfairStepsBoundIsWarningByDefault()
        {
            var report = this.RunSystematicTest(async () =>
            {
                for (int i = 0; i < 20; i++)
                {
                    await Task.Yield();
                }
            },
            configuration: this.GetConfiguration()
                .WithPrioritizationStrategy(isFair: true)
                .WithMaxSchedulingSteps(5, 1000)
                .WithTestingIterations(3));

            var verdict = report.GetVerdict();
            Assert.True(report.MaxUnfairStepsHitInFairTests > 0);
            Assert.Equal(0, report.NumOfTruncatedPaths);
            Assert.Equal(ExplorationStatus.Complete, verdict.Status);
            Assert.Contains(ExplorationWarning.ExceededUnfairStepsBound, verdict.Warnings);
        }

        [Fact(Timeout = 5000)]
        public void TestExceededUnfairStepsBoundIsReasonWithStrictBoundChecking()
        {
            var report = this.RunSystematicTest(async () =>
            {
                for (int i = 0; i < 20; i++)
                {
                    await Task.Yield();
                }
            },
            configuration: this.GetConfiguration()
                .WithPrioritizationStrategy(isFair: true)
                .WithMaxSchedulingSteps(5, 1000)
                .WithStrictBoundCheckingEnabled()
                .WithTestingIterations(3));

            var verdict = report.GetVerdict();
            Assert.True(report.MaxUnfairStepsHitInFairTests > 0);
            Assert.Equal(ExplorationStatus.Incomplete, verdict.Status);
            Assert.Contains(IncompleteExplorationReason.ExceededUnfairStepsBound, verdict.Reasons);
            Assert.DoesNotContain(ExplorationWarning.ExceededUnfairStepsBound, verdict.Warnings);
        }

        [Fact(Timeout = 5000)]
        public void TestNoSchedulingDecisionsVerdict()
        {
            var report = this.RunSystematicTest(() => { },
                configuration: this.GetConfiguration().WithTestingIterations(3));

            var verdict = report.GetVerdict();
            Assert.Equal(ExplorationStatus.Incomplete, verdict.Status);
            Assert.Contains(IncompleteExplorationReason.NoSchedulingDecisions, verdict.Reasons);
        }

        [Fact(Timeout = 5000)]
        public void TestBugFoundVerdict()
        {
            var report = this.RunSystematicTest(() =>
            {
                Specification.Assert(false, "Reachable.");
            },
            configuration: this.GetConfiguration()
                .WithTestIterationsRunToCompletion()
                .WithTestingIterations(2));

            var verdict = report.GetVerdict();
            Assert.Equal(2, report.NumOfFoundBugs);
            Assert.Equal(ExplorationStatus.BugFound, verdict.Status);
            Assert.Equal("BugFound", verdict.ToString());
        }
    }
}
