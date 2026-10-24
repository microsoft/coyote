// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Coyote.Logging;
using Microsoft.Coyote.Rewriting;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.SystematicTesting;
using Microsoft.Coyote.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.BugFinding.Tests.Reports
{
    public class ExplorationRegressionTests : BaseBugFindingTest
    {
        public ExplorationRegressionTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Theory(Timeout = 10000)]
        [InlineData(false)]
        [InlineData(true)]
        public void TestNormalCompletionExactlyAtStepBound(bool isFair)
        {
            Action test = () =>
            {
                SchedulingPoint.Interleave();
                SchedulingPoint.Interleave();
            };
            using var baseline = TestingEngine.Create(this.GetConfiguration().WithTestingIterations(1)
                .WithPrioritizationStrategy(isFair).WithMaxSchedulingSteps(1000, 1000), test);
            baseline.Run();
            Assert.Equal(ExplorationStatus.Complete, baseline.TestReport.GetVerdict().Status);
            int steps = Math.Max(baseline.TestReport.MaxExploredFairSteps, baseline.TestReport.MaxExploredUnfairSteps);
            Assert.True(steps > 1);

            using var bounded = TestingEngine.Create(this.GetConfiguration().WithTestingIterations(1)
                .WithPrioritizationStrategy(isFair).WithStrictBoundCheckingEnabled()
                .WithMaxSchedulingSteps(isFair ? 0 : (uint)steps, (uint)steps), test);
            bounded.Run();
            Assert.Equal(steps, Math.Max(bounded.TestReport.MaxExploredFairSteps, bounded.TestReport.MaxExploredUnfairSteps));
            Assert.Equal(0, bounded.TestReport.NumOfTruncatedPaths);
            Assert.Equal(ExplorationStatus.Complete, bounded.TestReport.GetVerdict().Status);

            using var truncated = TestingEngine.Create(this.GetConfiguration().WithTestingIterations(1)
                .WithPrioritizationStrategy(isFair)
                .WithMaxSchedulingSteps((uint)steps - 1, (uint)steps - 1), test);
            truncated.Run();
            Assert.Equal(1, truncated.TestReport.NumOfTruncatedPaths);
            Assert.Contains(IncompleteExplorationReason.TruncatedExecutionPaths, truncated.TestReport.GetVerdict().Reasons);
        }

        [Theory(Timeout = 5000)]
        [InlineData(false)]
        [InlineData(true)]
        public void TestDisabledStepBoundsDoNotLimitExploration(bool strictBounds)
        {
            using var engine = TestingEngine.Create(this.GetConfiguration().WithTestingIterations(3)
                .WithMaxSchedulingSteps(0).WithStrictBoundCheckingEnabled(strictBounds),
                () => SchedulingPoint.Interleave());
            engine.Run();

            Assert.Equal(0, engine.TestReport.NumOfTruncatedPaths);
            Assert.Equal(0, engine.TestReport.MaxUnfairStepsHitInFairTests);
            Assert.Equal(ExplorationStatus.Complete, engine.TestReport.GetVerdict().Status);
            Assert.Empty(engine.TestReport.GetVerdict().Warnings);
        }

        [Fact(Timeout = 5000)]
        public void TestFailureOnStepBoundRetainsTruncation()
        {
            using var engine = TestingEngine.Create(this.GetConfiguration().WithTestingIterations(1)
                .WithMaxSchedulingSteps(1, 1).WithFailureOnMaxStepsBoundEnabled(), () =>
                {
                    SchedulingPoint.Interleave();
                    SchedulingPoint.Interleave();
                });
            engine.Run();
            Assert.Equal(ExplorationStatus.BugFound, engine.TestReport.GetVerdict().Status);
            Assert.Equal(1, engine.TestReport.NumOfTruncatedPaths);
            Assert.Contains(IncompleteExplorationReason.TruncatedExecutionPaths, engine.TestReport.GetVerdict().Reasons);
        }

        [Fact(Timeout = 10000)]
        public void TestDFSChoiceExhaustionDoesNotTruncatePath()
        {
            using var engine = TestingEngine.Create(this.GetConfiguration().WithDFSStrategy()
                .WithTestingIterations(100), () =>
                {
                    SchedulingPoint.Interleave();
                    Microsoft.Coyote.Random.Generator.Create().NextBoolean();
                });
            engine.Run();
            Assert.True(engine.TestReport.IsExplorationExhausted);
            Assert.Equal(0, engine.TestReport.NumOfTruncatedPaths);
            Assert.Equal(ExplorationStatus.Complete, engine.TestReport.GetVerdict().Status);
        }

        [Theory(Timeout = 10000)]
        [InlineData(false)]
        [InlineData(true)]
        public void TestUncontrolledTaskWithoutSourceIsIncomplete(bool allowPartialControl)
        {
            var task = (Task)NativeTasks.CreatePending(out Action complete);
            using var writer = new UncontrolledWaitWriter(complete);
            using var engine = TestingEngine.Create(this.GetConfiguration().WithTestingIterations(1)
                .WithPartiallyControlledConcurrencyAllowed(allowPartialControl).WithVerbosityEnabled(), () =>
                {
                    task.Wait();
                    SchedulingPoint.Interleave();
                });
            engine.SetLogger(new TextWriterLogger(writer, VerbosityLevel.Debug));
            engine.Run();

            Assert.True(engine.TestReport.NumOfFoundBugs == (allowPartialControl ? 0 : 1), writer.ToString());
            Assert.Empty(engine.TestReport.UncontrolledInvocations);
            var expected = allowPartialControl ? ExplorationStatus.Incomplete : ExplorationStatus.BugFound;
            Assert.Equal(expected, engine.TestReport.GetVerdict().Status);
            Assert.Contains(IncompleteExplorationReason.UncontrolledInvocations, engine.TestReport.GetVerdict().Reasons);
            using var document = JsonDocument.Parse(engine.GetJsonReport());
            Assert.True(document.RootElement.GetProperty("uncontrolledConcurrencyDetected").GetBoolean());

            var clone = engine.TestReport.Clone();
            Assert.True(clone.IsUncontrolledConcurrencyDetected);
            Assert.Equal(expected, clone.GetVerdict().Status);
            var merged = new TestReport(engine.TestReport.Configuration);
            Assert.True(merged.Merge(engine.TestReport));
            Assert.True(merged.IsUncontrolledConcurrencyDetected);
            Assert.Empty(merged.UncontrolledInvocations);
            Assert.Equal(expected, merged.GetVerdict().Status);
            Assert.True(merged.Merge(new TestReport(engine.TestReport.Configuration)));
            Assert.True(merged.IsUncontrolledConcurrencyDetected);
        }

        [Fact(Timeout = 10000)]
        public void TestJsonReportDuringConcurrentMerges()
        {
            using var engine = TestingEngine.Create(this.GetConfiguration().WithJsonReportEnabled(), () => { });
            using var start = new ManualResetEventSlim();
            Exception writerError = null;
            var writer = new Thread(() =>
            {
                try
                {
                    start.Wait();
                    for (int i = 0; i < 1000; i++)
                    {
                        var iteration = new TestReport(engine.TestReport.Configuration);
                        ((ITestReport)iteration).SetSchedulingStatistics(true, $"Bug {i}", 1, 1, 1, 1, false, true);
                        engine.TestReport.Merge(iteration);
                        Thread.Yield();
                    }
                }
                catch (Exception ex)
                {
                    writerError = ex;
                }
            });
            writer.IsBackground = true;
            string directory = Path.Combine(Path.GetTempPath(), "coyote-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            writer.Start();
            start.Set();
            try
            {
                for (int i = 0; i < 25; i++)
                {
                    using var report = JsonDocument.Parse(engine.GetJsonReport());
                    AssertConsistentBugCounts(report.RootElement);
                    Assert.True(engine.TryEmitReports(directory, "concurrent", out var paths));
                    using var emitted = JsonDocument.Parse(File.ReadAllText(paths.Single()));
                    AssertConsistentBugCounts(emitted.RootElement);
                }
            }
            finally
            {
                Assert.True(writer.Join(5000));
                Directory.Delete(directory, true);
            }

            Assert.Null(writerError);
            using var finalReport = JsonDocument.Parse(engine.GetJsonReport());
            Assert.Equal(1000, finalReport.RootElement.GetProperty("bugs").GetProperty("count").GetInt32());
        }

        [Fact(Timeout = 5000)]
        public void TestEmittedReportUsesCapturedSnapshot()
        {
            using var engine = TestingEngine.Create(this.GetConfiguration().WithJsonReportEnabled(), () => { });
            var snapshot = engine.TestReport.Clone();
            ((ITestReport)engine.TestReport).SetSchedulingStatistics(true, "Late bug", 1, 1, 1, 1, false, true);
            string directory = Path.Combine(Path.GetTempPath(), "coyote-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                Assert.True(engine.TryEmitReports(directory, "snapshot", snapshot, out var paths));
                using var report = JsonDocument.Parse(File.ReadAllText(paths.Single()));
                Assert.Equal(0, report.RootElement.GetProperty("bugs").GetProperty("count").GetInt32());
                Assert.Equal("Incomplete", report.RootElement.GetProperty("verdict").GetProperty("status").GetString());
                Assert.Contains("NoExecutionPaths", engine.GetReport(snapshot));
                Assert.Equal(ExplorationStatus.Incomplete, snapshot.GetVerdict().Status);
                Assert.Equal(ExplorationStatus.BugFound, engine.TestReport.GetVerdict().Status);
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        private static void AssertConsistentBugCounts(JsonElement root)
        {
            int bugs = root.GetProperty("bugs").GetProperty("count").GetInt32();
            Assert.Equal(bugs, root.GetProperty("bugs").GetProperty("reports").GetArrayLength());
            Assert.Equal(bugs, root.GetProperty("exploration").GetProperty("fairPaths").GetInt32());
            Assert.Equal(bugs > 0 ? "BugFound" : "Incomplete",
                root.GetProperty("verdict").GetProperty("status").GetString());
        }

        private sealed class UncontrolledWaitWriter : StringWriter
        {
            private readonly Action Complete;

            internal UncontrolledWaitWriter(Action complete) => this.Complete = complete;

            public override void WriteLine(string value)
            {
                base.WriteLine(value);
                if (value.Contains("Waiting task '", StringComparison.Ordinal))
                {
                    this.Complete();
                }
            }
        }

        [SkipRewriting("Return a native pending task without identifying its source to the rewriter.")]
        private sealed class NativeTasks
        {
            private readonly TaskCompletionSource<bool> Source = new TaskCompletionSource<bool>();

            internal static object CreatePending(out Action complete)
            {
                var pending = new NativeTasks();
                complete = pending.Complete;
                return pending.Source.Task;
            }

            private void Complete() => this.Source.TrySetResult(true);
        }
    }
}
