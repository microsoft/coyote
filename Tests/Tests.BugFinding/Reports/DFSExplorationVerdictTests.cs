// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.SystematicTesting;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.BugFinding.Tests.Reports
{
    public class DFSExplorationVerdictTests : BaseBugFindingTest
    {
        public DFSExplorationVerdictTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Theory(Timeout = 10000)]
        [InlineData(false)]
        [InlineData(true)]
        public void TestDFSExhaustionVerdict(bool isStrict)
        {
            using var engine = TestingEngine.Create(this.GetConfiguration().WithDFSStrategy()
                .WithTestingIterations(100).WithStrictExplorationEnabled(isStrict),
                () => SchedulingPoint.Interleave());
            Assert.True(engine.IsTestRewritten());
            engine.Run();

            int paths = engine.TestReport.NumOfExploredFairPaths + engine.TestReport.NumOfExploredUnfairPaths;
            Assert.InRange(paths, 1, 99);
            Assert.True(engine.TestReport.IsExplorationExhausted);
            var verdict = engine.TestReport.GetVerdict();
            Assert.Equal(ExplorationStatus.Complete, verdict.Status);
            Assert.Empty(verdict.Reasons);
            using var document = JsonDocument.Parse(engine.GetJsonReport());
            Assert.True(document.RootElement.GetProperty("assembly").GetProperty("isRewritten").GetBoolean());
            Assert.Equal("Complete", document.RootElement.GetProperty("verdict").GetProperty("status").GetString());
        }

        [Fact(Timeout = 10000)]
        public void TestDFSIterationBudgetDoesNotRecordExhaustion()
        {
            using var exhaustive = TestingEngine.Create(this.GetConfiguration().WithDFSStrategy()
                .WithTestingIterations(100), () => SchedulingPoint.Interleave());
            exhaustive.Run();
            Assert.True(exhaustive.TestReport.IsExplorationExhausted);
            int paths = exhaustive.TestReport.NumOfExploredFairPaths + exhaustive.TestReport.NumOfExploredUnfairPaths;
            using var engine = TestingEngine.Create(this.GetConfiguration().WithDFSStrategy()
                .WithTestingIterations((uint)paths), () => SchedulingPoint.Interleave());
            engine.Run();

            Assert.False(engine.TestReport.IsExplorationExhausted);
            Assert.Equal(paths, engine.TestReport.NumOfExploredFairPaths + engine.TestReport.NumOfExploredUnfairPaths);
            Assert.Equal(ExplorationStatus.Complete, engine.TestReport.GetVerdict().Status);
        }

        [Fact(Timeout = 10000)]
        public void TestDFSExhaustionBeforeTimeout()
        {
            using var engine = TestingEngine.Create(this.GetConfiguration().WithDFSStrategy()
                .WithTestingTimeout(30), () => SchedulingPoint.Interleave());
            engine.Run();

            Assert.True(engine.TestReport.IsExplorationExhausted);
            Assert.Equal(ExplorationStatus.Complete, engine.TestReport.GetVerdict().Status);
            Assert.Empty(engine.TestReport.GetVerdict().Warnings);
        }

        [Theory(Timeout = 5000)]
        [InlineData(false)]
        [InlineData(true)]
        public void TestMergeDoesNotHideUnobservedExhaustion(bool reverse)
        {
            var exhausted = this.CreateReport(3, true);
            var unfinished = this.CreateReport(1, false);
            var destination = reverse ? unfinished : exhausted;
            Assert.True(destination.Merge(reverse ? exhausted : unfinished));
            Assert.True(destination.Merge(this.CreateReport(0, true)));

            Assert.Equal(ExplorationStatus.Incomplete, destination.GetVerdict().Status);
            Assert.Equal(new[] { IncompleteExplorationReason.InsufficientExecutionPaths }, destination.GetVerdict().Reasons);
        }

        [Theory(Timeout = 5000)]
        [InlineData(false)]
        [InlineData(true)]
        public void TestMergeEmptyReportsAreNeutral(bool reverse)
        {
            var exhausted = this.CreateReport(3, true);
            var empty = this.CreateReport(0, false);
            var destination = reverse ? empty : exhausted;
            Assert.True(destination.Merge(reverse ? exhausted : empty));
            Assert.True(destination.Merge(this.CreateReport(0, false)));
            Assert.True(destination.Merge(this.CreateReport(2, true)));

            Assert.Equal(5, destination.NumOfExploredFairPaths);
            Assert.Equal(ExplorationStatus.Complete, destination.GetVerdict().Status);
            Assert.Empty(destination.GetVerdict().Reasons);
        }

        [Theory(Timeout = 5000)]
        [InlineData(0, 1, 2)]
        [InlineData(0, 2, 1)]
        [InlineData(1, 0, 2)]
        [InlineData(1, 2, 0)]
        [InlineData(2, 0, 1)]
        [InlineData(2, 1, 0)]
        public void TestMergeGroupingPreservesUnfinishedContributor(int first, int second, int third)
        {
            var reports = new[] { this.CreateReport(3, true), this.CreateReport(2, true), this.CreateReport(1, false) };
            var left = this.CreateReport(0, false);
            Assert.True(left.Merge(reports[first]));
            Assert.True(left.Merge(reports[second]));
            Assert.True(left.Merge(reports[third]));
            var right = this.CreateReport(0, false);
            var group = this.CreateReport(0, false);
            Assert.True(group.Merge(reports[second]));
            Assert.True(group.Merge(reports[third]));
            Assert.True(right.Merge(reports[first]));
            Assert.True(right.Merge(group));

            Assert.Equal(ExplorationStatus.Incomplete, left.GetVerdict().Status);
            Assert.Equal(new[] { IncompleteExplorationReason.InsufficientExecutionPaths }, left.GetVerdict().Reasons);
            Assert.Equal(left.GetVerdict().Reasons, right.GetVerdict().Reasons);
        }

        [Fact(Timeout = 5000)]
        public void TestMergeCompletedIterationBudgetNeedsNoExhaustionEvidence()
        {
            var destination = this.CreateReport(3, true);
            Assert.True(destination.Merge(this.CreateReport(100, false)));

            Assert.False(destination.IsExplorationExhausted);
            Assert.Equal(ExplorationStatus.Complete, destination.GetVerdict().Status);
        }

        [Fact(Timeout = 10000)]
        public void TestMergeDFSWithSilentlyInterruptedExploration()
        {
            using var exhausted = TestingEngine.Create(this.GetConfiguration().WithDFSStrategy()
                .WithTestingIterations(100), () => SchedulingPoint.Interleave());
            exhausted.Run();
            using var interrupted = TestingEngine.Create(this.GetConfiguration().WithTestingIterations(100),
                () => SchedulingPoint.Interleave());
            interrupted.RegisterStartIterationCallBack(iteration =>
            {
                if (iteration is 1)
                {
                    throw new TaskCanceledException();
                }
            });
            interrupted.Run();

            Assert.True(exhausted.TestReport.IsExplorationExhausted);
            Assert.Equal(1, interrupted.TestReport.NumOfExploredFairPaths + interrupted.TestReport.NumOfExploredUnfairPaths);
            Assert.False(interrupted.TestReport.IsExplorationExhausted);
            Assert.False(interrupted.TestReport.IsExplorationCanceled);
            Assert.False(interrupted.TestReport.IsExplorationTimedOut);
            Assert.True(exhausted.TestReport.Merge(interrupted.TestReport));
            Assert.Equal(ExplorationStatus.Incomplete, exhausted.TestReport.GetVerdict().Status);
            Assert.Contains(IncompleteExplorationReason.InsufficientExecutionPaths, exhausted.TestReport.GetVerdict().Reasons);
        }

        [Fact(Timeout = 5000)]
        public void TestExhaustionSurvivesClone()
        {
            var cloned = this.CreateReport(3, true).Clone();

            Assert.True(cloned.IsExplorationExhausted);
            Assert.Equal(ExplorationStatus.Complete, cloned.GetVerdict().Status);
        }

        [Fact(Timeout = 5000)]
        public void TestLegacyReportDoesNotInventExhaustionEvidence()
        {
            var serializer = new DataContractSerializer(typeof(TestReport));
            using var stream = new MemoryStream();
            serializer.WriteObject(stream, this.CreateReport(3, true));
            stream.Position = 0;
            var document = XDocument.Load(stream);
            document.Descendants().Single(element => element.Name.LocalName == "IsExplorationExhausted").Remove();
            stream.SetLength(0);
            document.Save(stream);
            stream.Position = 0;
            var legacy = (TestReport)serializer.ReadObject(stream);

            Assert.False(legacy.IsExplorationExhausted);
            Assert.Equal(ExplorationStatus.Incomplete, legacy.GetVerdict().Status);
            Assert.Contains(IncompleteExplorationReason.InsufficientExecutionPaths, legacy.GetVerdict().Reasons);
        }

        [Theory(Timeout = 5000)]
        [InlineData(IncompleteExplorationReason.TestingCanceled)]
        [InlineData(IncompleteExplorationReason.TestAssemblyNotRewritten)]
        [InlineData(IncompleteExplorationReason.UncontrolledInvocations)]
        [InlineData(IncompleteExplorationReason.TruncatedExecutionPaths)]
        [InlineData(IncompleteExplorationReason.ExceededUnfairStepsBound)]
        [InlineData(IncompleteExplorationReason.NoSchedulingDecisions)]
        [InlineData(IncompleteExplorationReason.NoExecutionPaths)]
        public void TestExhaustionPreservesOtherIncompleteReasons(IncompleteExplorationReason reason)
        {
            var report = this.CreateReport(3, true);
            switch (reason)
            {
                case IncompleteExplorationReason.TestingCanceled:
                    var emptyCanceled = this.CreateReport(0, false);
                    emptyCanceled.RecordExplorationTermination(isCanceled: true);
                    Assert.True(report.Merge(emptyCanceled));
                    break;
                case IncompleteExplorationReason.TestAssemblyNotRewritten:
                    var emptyUnrewritten = this.CreateReport(0, false);
                    emptyUnrewritten.RecordUnrewrittenTestAssembly();
                    Assert.True(report.Merge(emptyUnrewritten));
                    break;
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
                case IncompleteExplorationReason.NoExecutionPaths:
                    report.NumOfExploredFairPaths = 0;
                    break;
            }

            Assert.Equal(ExplorationStatus.Incomplete, report.GetVerdict().Status);
            Assert.Contains(reason, report.GetVerdict().Reasons);
            Assert.DoesNotContain(IncompleteExplorationReason.InsufficientExecutionPaths, report.GetVerdict().Reasons);
        }

        [Fact(Timeout = 5000)]
        public void TestExhaustionPreservesWarningsAndVerdictPriority()
        {
            var report = this.CreateReport(3, true);
            report.Configuration.WithStrictBoundCheckingEnabled(false);
            report.MaxUnfairStepsHitInFairTests = 1;
            report.RecordExplorationTermination(isCanceled: false);

            Assert.Equal(ExplorationStatus.Complete, report.GetVerdict().Status);
            Assert.Contains(ExplorationWarning.ExceededUnfairStepsBound, report.GetVerdict().Warnings);
            Assert.Contains(ExplorationWarning.TestingTimedOut, report.GetVerdict().Warnings);
            report.NumOfFoundBugs = 1;
            Assert.Equal(ExplorationStatus.BugFound, report.GetVerdict().Status);
            report.InternalErrors.Add("Fixture internal error.");
            Assert.Equal(ExplorationStatus.InternalError, report.GetVerdict().Status);
        }

        private TestReport CreateReport(int paths, bool exhausted)
        {
            var report = new TestReport(this.GetConfiguration().WithTestingIterations(100).WithStrictBoundCheckingEnabled())
            {
                NumOfExploredFairPaths = paths,
                MaxExploredFairSteps = paths > 0 ? 1 : 0
            };
            if (exhausted)
            {
                report.RecordExplorationExhaustion();
            }

            return report;
        }
    }
}
