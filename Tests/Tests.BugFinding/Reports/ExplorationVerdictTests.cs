// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Coyote.Rewriting;
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
        public void TestZeroIterationsVerdict()
        {
            var report = this.RunSystematicTest(() => { },
                configuration: this.GetConfiguration().WithTestingIterations(0));

            Assert.Equal(0, report.NumOfExploredFairPaths + report.NumOfExploredUnfairPaths);
            Assert.Equal(ExplorationStatus.Incomplete, report.GetVerdict().Status);
            Assert.Contains(IncompleteExplorationReason.NoExecutionPaths, report.GetVerdict().Reasons);
        }

        [Fact(Timeout = 5000)]
        public void TestCanceledBeforeExecutionVerdict()
        {
            using var engine = TestingEngine.Create(this.GetConfiguration().WithTestingTimeout(30), () => { });
            engine.Stop();
            engine.Run();

            Assert.Equal(0, engine.TestReport.NumOfExploredFairPaths + engine.TestReport.NumOfExploredUnfairPaths);
            Assert.Equal(ExplorationStatus.Incomplete, engine.TestReport.GetVerdict().Status);
            Assert.Contains(IncompleteExplorationReason.NoExecutionPaths, engine.TestReport.GetVerdict().Reasons);
            Assert.Contains(IncompleteExplorationReason.TestingCanceled, engine.TestReport.GetVerdict().Reasons);
            Assert.DoesNotContain(IncompleteExplorationReason.TestingTimedOut, engine.TestReport.GetVerdict().Reasons);
        }

        [Fact(Timeout = 10000)]
        public void TestTimeoutBeforeExecutionVerdict()
        {
            using var callbackCompleted = new ManualResetEventSlim();
            using var releaseCallback = new ManualResetEventSlim();
            using var engine = TestingEngine.Create(this.GetConfiguration().WithTestingTimeout(1), () => { });
            engine.RegisterStartIterationCallBack(_ =>
            {
                try
                {
                    releaseCallback.Wait();
                    throw new TaskCanceledException();
                }
                finally
                {
                    callbackCompleted.Set();
                }
            });
            try
            {
                engine.Run();
            }
            finally
            {
                releaseCallback.Set();
                Assert.True(callbackCompleted.Wait(5000));
            }

            Assert.Equal(0, engine.TestReport.NumOfExploredFairPaths + engine.TestReport.NumOfExploredUnfairPaths);
            Assert.Equal(ExplorationStatus.Incomplete, engine.TestReport.GetVerdict().Status);
            Assert.Contains(IncompleteExplorationReason.NoExecutionPaths, engine.TestReport.GetVerdict().Reasons);
            Assert.Contains(IncompleteExplorationReason.TestingTimedOut, engine.TestReport.GetVerdict().Reasons);
            Assert.DoesNotContain(IncompleteExplorationReason.TestingCanceled, engine.TestReport.GetVerdict().Reasons);
        }

        [Fact(Timeout = 10000)]
        public void TestTimeBudgetAfterExecutionVerdict()
        {
            using var callbackCompleted = new ManualResetEventSlim();
            using var releaseCallback = new ManualResetEventSlim();
            using var engine = TestingEngine.Create(this.GetConfiguration().WithTestingTimeout(1),
                () => SchedulingPoint.Interleave());
            engine.RegisterStartIterationCallBack(iteration =>
            {
                if (iteration > 0)
                {
                    try
                    {
                        releaseCallback.Wait();
                        throw new TaskCanceledException();
                    }
                    finally
                    {
                        callbackCompleted.Set();
                    }
                }
            });
            try
            {
                engine.Run();
            }
            finally
            {
                releaseCallback.Set();
                Assert.True(callbackCompleted.Wait(5000));
            }

            Assert.Equal(1, engine.TestReport.NumOfExploredFairPaths + engine.TestReport.NumOfExploredUnfairPaths);
            var verdict = engine.TestReport.GetVerdict();
            Assert.Equal(ExplorationStatus.Complete, verdict.Status);
            Assert.Empty(verdict.Reasons);
            Assert.Contains(ExplorationWarning.TestingTimedOut, verdict.Warnings);
        }

        [Theory(Timeout = 5000)]
        [InlineData(true)]
        [InlineData(false)]
        public void TestInterruptedVerdictSurvivesCloneAndMerge(bool isTimedOut)
        {
            var configuration = this.GetConfiguration();
            var interruptedReport = new TestReport(configuration);
            interruptedReport.RecordExplorationTermination(isCanceled: !isTimedOut);
            var mergedReport = new TestReport(configuration)
            {
                NumOfExploredFairPaths = 1,
                MaxExploredFairSteps = 1
            };
            var clonedReport = interruptedReport.Clone();
            Assert.Equal(interruptedReport.IsExplorationTimedOut, clonedReport.IsExplorationTimedOut);
            Assert.Equal(interruptedReport.IsExplorationCanceled, clonedReport.IsExplorationCanceled);
            Assert.Equal(ExplorationStatus.Incomplete, clonedReport.GetVerdict().Status);
            Assert.True(mergedReport.Merge(interruptedReport));

            var verdict = mergedReport.GetVerdict();
            Assert.Equal(isTimedOut ? ExplorationStatus.Complete : ExplorationStatus.Incomplete, verdict.Status);
            if (isTimedOut)
            {
                Assert.Empty(verdict.Reasons);
                Assert.Contains(ExplorationWarning.TestingTimedOut, verdict.Warnings);
            }
            else
            {
                Assert.Contains(IncompleteExplorationReason.TestingCanceled, verdict.Reasons);
                Assert.DoesNotContain(ExplorationWarning.TestingTimedOut, verdict.Warnings);
            }

            Assert.DoesNotContain(IncompleteExplorationReason.NoExecutionPaths, verdict.Reasons);
        }

        [Fact(Timeout = 5000)]
        public void TestInterruptedVerdictPreservesBugAndErrorPriority()
        {
            var report = new TestReport(this.GetConfiguration())
            {
                NumOfFoundBugs = 1
            };
            report.RecordExplorationTermination(isCanceled: false);
            Assert.Equal(ExplorationStatus.BugFound, report.GetVerdict().Status);

            report.InternalErrors.Add("Fixture internal error.");
            Assert.Equal(ExplorationStatus.InternalError, report.GetVerdict().Status);
        }

        [Fact(Timeout = 10000)]
        public void TestCanceledAfterExecutionVerdict()
        {
            using var callbackCompleted = new ManualResetEventSlim();
            using var engine = TestingEngine.Create(this.GetConfiguration().WithTestingTimeout(30),
                () => SchedulingPoint.Interleave());
            engine.RegisterStartIterationCallBack(iteration =>
            {
                if (iteration > 0)
                {
                    try
                    {
                        engine.Stop();
                        throw new TaskCanceledException();
                    }
                    finally
                    {
                        callbackCompleted.Set();
                    }
                }
            });
            engine.Run();
            Assert.True(callbackCompleted.Wait(5000));

            Assert.Equal(1, engine.TestReport.NumOfExploredFairPaths + engine.TestReport.NumOfExploredUnfairPaths);
            var verdict = engine.TestReport.GetVerdict();
            Assert.Equal(ExplorationStatus.Incomplete, verdict.Status);
            Assert.Contains(IncompleteExplorationReason.TestingCanceled, verdict.Reasons);
            Assert.DoesNotContain(IncompleteExplorationReason.NoExecutionPaths, verdict.Reasons);
        }

        [Fact(Timeout = 10000)]
        public async Task TestInterruptionRecordingDuringMergeAsync()
        {
            var configuration = this.GetConfiguration();
            var report = new TestReport(configuration);
            var iterationReport = new TestReport(configuration)
            {
                NumOfExploredFairPaths = 1,
                MaxExploredFairSteps = 1
            };
            await Task.WhenAll(Task.Run(() =>
            {
                for (int i = 0; i < 1000; i++)
                {
                    report.RecordExplorationTermination(isCanceled: false);
                }
            }), Task.Run(() =>
            {
                for (int i = 0; i < 1000; i++)
                {
                    Assert.True(report.Merge(iterationReport));
                }
            }));

            Assert.Equal(ExplorationStatus.Complete, report.GetVerdict().Status);
            Assert.Contains(ExplorationWarning.TestingTimedOut, report.GetVerdict().Warnings);
        }

        [Theory(Timeout = 5000)]
        [InlineData(IncompleteExplorationReason.UncontrolledInvocations)]
        [InlineData(IncompleteExplorationReason.TruncatedExecutionPaths)]
        [InlineData(IncompleteExplorationReason.ExceededUnfairStepsBound)]
        [InlineData(IncompleteExplorationReason.NoSchedulingDecisions)]
        public void TestTimeBudgetPreservesIncompleteReasons(IncompleteExplorationReason reason)
        {
            var report = new TestReport(this.GetConfiguration().WithTestingTimeout(1).WithStrictBoundCheckingEnabled())
            {
                NumOfExploredFairPaths = 1,
                MaxExploredFairSteps = 1
            };
            report.RecordExplorationTermination(isCanceled: false);
            switch (reason)
            {
                case IncompleteExplorationReason.UncontrolledInvocations:
                    report.UncontrolledInvocations.Add("Fixture uncontrolled invocation.");
                    break;
                case IncompleteExplorationReason.TruncatedExecutionPaths:
                    report.MaxFairStepsHitInFairTests = 1;
                    break;
                case IncompleteExplorationReason.ExceededUnfairStepsBound:
                    report.MaxUnfairStepsHitInFairTests = 1;
                    break;
                case IncompleteExplorationReason.NoSchedulingDecisions:
                    report.MaxExploredFairSteps = 0;
                    break;
            }

            var verdict = report.GetVerdict();
            Assert.Equal(ExplorationStatus.Incomplete, verdict.Status);
            Assert.Contains(reason, verdict.Reasons);
            Assert.DoesNotContain(IncompleteExplorationReason.TestingTimedOut, verdict.Reasons);
            Assert.Contains(ExplorationWarning.TestingTimedOut, verdict.Warnings);
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

        [Theory(Timeout = 5000)]
        [InlineData(false)]
        [InlineData(true)]
        public void TestUnknownRewriteStateDoesNotMakeMergedReportIncomplete(bool isUnknownSource)
        {
            var configuration = this.GetConfiguration().WithTestingIterations(1);
            using var engine = TestingEngine.Create(configuration, () => SchedulingPoint.Interleave());
            engine.Run();
            Assert.True(engine.IsTestRewritten());
            var unknownReport = new TestReport(configuration);
            Assert.DoesNotContain(IncompleteExplorationReason.TestAssemblyNotRewritten, unknownReport.GetVerdict().Reasons);
            var destination = isUnknownSource ? engine.TestReport : unknownReport;
            var source = isUnknownSource ? unknownReport : engine.TestReport;
            Assert.True(destination.Merge(source));

            Assert.Equal(ExplorationStatus.Complete, destination.GetVerdict().Status);
            Assert.Empty(destination.GetVerdict().Reasons);
        }

        [Fact(Timeout = 5000)]
        public void TestDelegateUsesActualEntryAssembly()
        {
            var configuredAssembly = typeof(ExplorationVerdictTests).Assembly;
            Assert.True(RewritingEngine.IsAssemblyRewritten(configuredAssembly));
            var configuration = this.GetConfiguration().WithTestingIterations(1);
            configuration.AssemblyToBeAnalyzed = configuredAssembly.Location;
            using var engine = TestingEngine.Create(configuration, SchedulingPoint.Interleave);
            Assert.False(engine.IsTestRewritten());
            engine.Run();

            Assert.Equal(1, engine.TestReport.NumOfExploredFairPaths + engine.TestReport.NumOfExploredUnfairPaths);
            Assert.Equal(ExplorationStatus.Incomplete, engine.TestReport.GetVerdict().Status);
            Assert.Contains(IncompleteExplorationReason.TestAssemblyNotRewritten, engine.TestReport.GetVerdict().Reasons);
            using var document = JsonDocument.Parse(engine.GetJsonReport());
            var assembly = document.RootElement.GetProperty("assembly");
            Assert.False(assembly.GetProperty("isRewritten").GetBoolean());
            Assert.Equal(typeof(SchedulingPoint).Assembly.Location, assembly.GetProperty("path").GetString());
        }
    }
}
