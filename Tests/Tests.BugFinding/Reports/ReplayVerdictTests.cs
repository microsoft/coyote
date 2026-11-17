// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.Specifications;
using Microsoft.Coyote.SystematicTesting;
using Microsoft.Coyote.Tests.Common.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.BugFinding.Tests.Reports
{
    public class ReplayVerdictTests : BaseBugFindingTest
    {
        public ReplayVerdictTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Theory(Timeout = 10000)]
        [InlineData(1, false, 0)]
        [InlineData(2, true, 0)]
        [InlineData(100, false, 0)]
        [InlineData(100, true, 0)]
        [InlineData(100, true, 30)]
        public void TestCompleteReplayUsesOnePath(int iterations, bool isStrict, int timeout)
        {
            var configuration = this.GetConfiguration()
                .WithTestingIterations((uint)iterations)
                .WithStrictExplorationEnabled(isStrict)
                .WithTestingTimeout(timeout)
                .WithReproducibleTrace(this.RecordTrace());
            using var engine = TestingEngine.Create(configuration, () => RunScenario(false));
            Assert.True(engine.IsTestRewritten());
            engine.Run();

            Assert.Equal(1, GetTotalPaths(engine.TestReport));
            Assert.Equal(0, engine.TestReport.NumOfFoundBugs);
            Assert.Empty(engine.TestReport.InternalErrors);
            Assert.Equal(ExplorationStatus.Complete, engine.TestReport.GetVerdict().Status);
            Assert.Empty(engine.TestReport.GetVerdict().Reasons);

            using var document = JsonDocument.Parse(engine.GetJsonReport());
            var root = document.RootElement;
            Assert.Equal("Complete", root.GetProperty("verdict").GetProperty("status").GetString());
            Assert.Empty(root.GetProperty("verdict").GetProperty("reasons").EnumerateArray());
            Assert.Equal(iterations, root.GetProperty("settings").GetProperty("iterations").GetInt32());
            Assert.Equal(isStrict, root.GetProperty("settings").GetProperty("isStrictExplorationEnabled").GetBoolean());
        }

        [Theory(Timeout = 10000)]
        [InlineData(1)]
        [InlineData(100)]
        public void TestReplayCanContinueAfterRecordedPrefix(int iterations)
        {
            string trace = this.RecordTrace();
            int recordedSteps = JsonSerializer.Deserialize<TraceReport>(trace).Steps.Count;
            using var engine = TestingEngine.Create(this.GetConfiguration().WithTestingIterations((uint)iterations)
                .WithReproducibleTrace(trace), () =>
                {
                    RunScenario(false);
                    SchedulingPoint.Interleave();
                });
            engine.Run();

            Assert.Equal(1, GetTotalPaths(engine.TestReport));
            Assert.Equal(0, engine.TestReport.NumOfFoundBugs);
            Assert.Empty(engine.TestReport.InternalErrors);
            Assert.True(engine.TestReport.MaxExploredFairSteps > recordedSteps ||
                engine.TestReport.MaxExploredUnfairSteps > recordedSteps);
            var verdict = engine.TestReport.GetVerdict();
            Assert.Equal(ExplorationStatus.Complete, verdict.Status);
            Assert.Empty(verdict.Reasons);
        }

        [Fact(Timeout = 10000)]
        public void TestReplayCanStillReproduceBug()
        {
            using var engine = TestingEngine.Create(this.GetConfiguration().WithTestingIterations(100)
                .WithReproducibleTrace(this.RecordTrace()), () => RunScenario(true));
            engine.Run();

            Assert.Equal(1, GetTotalPaths(engine.TestReport));
            Assert.Equal(1, engine.TestReport.NumOfFoundBugs);
            Assert.Empty(engine.TestReport.InternalErrors);
            Assert.Equal(ExplorationStatus.BugFound, engine.TestReport.GetVerdict().Status);
        }

        [Fact(Timeout = 10000)]
        public void TestReplayRejectsUnconsumedTrace()
        {
            using var engine = TestingEngine.Create(this.GetConfiguration().WithTestingIterations(100)
                .WithReproducibleTrace(this.RecordTrace()), () => { });
            engine.Run();

            Assert.Contains(engine.TestReport.InternalErrors, error => error.Contains("before consuming all recorded steps"));
            Assert.Equal(ExplorationStatus.InternalError, engine.TestReport.GetVerdict().Status);
            Assert.Contains(IncompleteExplorationReason.InsufficientExecutionPaths, engine.TestReport.GetVerdict().Reasons);
        }

        [Fact(Timeout = 10000)]
        public void TestReplayRetainsIterationCheckAfterMidTraceMismatch()
        {
            using var engine = TestingEngine.Create(this.GetConfiguration().WithTestingIterations(100)
                .WithReproducibleTrace(this.RecordTrace()), () =>
                {
                    SchedulingPoint.Interleave();
                    Microsoft.Coyote.Random.Generator.Create().NextBoolean();
                });
            engine.Run();

            Assert.Equal(1, GetTotalPaths(engine.TestReport));
            Assert.Contains(engine.TestReport.InternalErrors, error => error.Contains("next step is not a nondeterministic choice"));
            var verdict = engine.TestReport.GetVerdict();
            Assert.Equal(ExplorationStatus.InternalError, verdict.Status);
            Assert.Contains(IncompleteExplorationReason.InsufficientExecutionPaths, verdict.Reasons);
        }

        [Fact(Timeout = 10000)]
        public void TestTruncatedReplayRetainsIterationCheck()
        {
            var trace = JsonSerializer.Deserialize<TraceReport>(this.RecordTrace());
            trace.Settings.MaxFairSchedulingSteps = 1;
            trace.Settings.MaxUnfairSchedulingSteps = 1;
            using var engine = TestingEngine.Create(this.GetConfiguration().WithTestingIterations(100)
                .WithReproducibleTrace(trace.ToJson()), () => RunScenario(false));
            engine.Run();

            Assert.Equal(1, GetTotalPaths(engine.TestReport));
            Assert.Equal(1, engine.TestReport.NumOfTruncatedPaths);
            Assert.Empty(engine.TestReport.InternalErrors);
            var verdict = engine.TestReport.GetVerdict();
            Assert.Equal(ExplorationStatus.Incomplete, verdict.Status);
            Assert.Contains(IncompleteExplorationReason.TruncatedExecutionPaths, verdict.Reasons);
            Assert.Contains(IncompleteExplorationReason.InsufficientExecutionPaths, verdict.Reasons);
        }

        [Fact(Timeout = 10000)]
        public void TestStoppingReplayPreservesCancellationAfterCompletedPath()
        {
            using var engine = TestingEngine.Create(this.GetConfiguration().WithTestingIterations(100)
                .WithReproducibleTrace(this.RecordTrace()), () => RunScenario(false));
            engine.RegisterEndIterationCallBack(_ => engine.Stop());
            engine.Run();

            // Run can return before the canceled iteration publishes its report.
            Assert.True(SpinWait.SpinUntil(() => GetTotalPaths(engine.TestReport.CreateSnapshot()) > 0, 5000));
            Assert.Equal(1, GetTotalPaths(engine.TestReport));
            Assert.Empty(engine.TestReport.InternalErrors);
            var verdict = engine.TestReport.GetVerdict();
            Assert.Equal(ExplorationStatus.Incomplete, verdict.Status);
            Assert.Equal(new[] { IncompleteExplorationReason.TestingCanceled }, verdict.Reasons);
        }

        [Theory(Timeout = 10000)]
        [InlineData(false)]
        [InlineData(true)]
        public void TestExplorationWithoutRecordedStepsRetainsIterationCheck(bool withEmptyTrace)
        {
            var configuration = this.GetConfiguration().WithTestingIterations(100);
            if (withEmptyTrace)
            {
                var trace = JsonSerializer.Deserialize<TraceReport>(this.RecordTrace());
                trace.Steps.Clear();
                configuration.WithReproducibleTrace(JsonSerializer.Serialize(trace));
            }

            using var engine = TestingEngine.Create(configuration, () => RunScenario(false));
            InterruptSecondIteration(engine);
            engine.Run();

            Assert.Equal(1, GetTotalPaths(engine.TestReport));
            Assert.Empty(engine.TestReport.InternalErrors);
            Assert.Equal(ExplorationStatus.Incomplete, engine.TestReport.GetVerdict().Status);
            Assert.Contains(IncompleteExplorationReason.InsufficientExecutionPaths, engine.TestReport.GetVerdict().Reasons);
        }

        [Fact(Timeout = 10000)]
        public void TestInterruptedReplayFallbackRetainsIterationCheck()
        {
            using var engine = TestingEngine.Create(this.GetConfiguration().WithTestingIterations(100)
                .WithSystematicFuzzingFallbackEnabled(true).WithReproducibleTrace(this.RecordTrace()),
                () =>
                {
                    RunScenario(false);
                    AsyncProvider.GetPendingTasks().Single().Wait(0);
                });
            InterruptSecondIteration(engine);
            engine.Run();

            Assert.Equal(1, GetTotalPaths(engine.TestReport));
            Assert.Equal(SchedulingPolicy.Fuzzing, engine.Scheduler.SchedulingPolicy);
            Assert.Empty(engine.TestReport.InternalErrors);
            Assert.Equal(ExplorationStatus.Incomplete, engine.TestReport.GetVerdict().Status);
            Assert.Contains(IncompleteExplorationReason.UncontrolledInvocations, engine.TestReport.GetVerdict().Reasons);
            Assert.Contains(IncompleteExplorationReason.InsufficientExecutionPaths, engine.TestReport.GetVerdict().Reasons);
        }

        [Theory(Timeout = 10000)]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public void TestMergedReplayDoesNotExemptOrdinaryExploration(bool ordinaryExhausted, bool replayFirst)
        {
            using var replay = this.RunCompleteReplay();
            var configuration = this.GetConfiguration().WithTestingIterations(100);
            if (ordinaryExhausted)
            {
                configuration.WithDFSStrategy();
            }

            using var ordinary = TestingEngine.Create(configuration, () => RunScenario(false));
            if (!ordinaryExhausted)
            {
                InterruptSecondIteration(ordinary);
            }

            ordinary.Run();
            Assert.Equal(ordinaryExhausted, ordinary.TestReport.IsExplorationExhausted);
            int paths = GetTotalPaths(replay.TestReport) + GetTotalPaths(ordinary.TestReport);
            Assert.InRange(paths, 2, 99);

            var destination = replayFirst ? replay.TestReport : ordinary.TestReport;
            Assert.True(destination.Merge(replayFirst ? ordinary.TestReport : replay.TestReport));
            Assert.Equal(paths, GetTotalPaths(destination));
            Assert.Equal(ExplorationStatus.Incomplete, destination.GetVerdict().Status);
            Assert.Contains(IncompleteExplorationReason.InsufficientExecutionPaths, destination.GetVerdict().Reasons);
        }

        [Fact(Timeout = 10000)]
        public void TestReplayMergeAndClonePreserveCompletionAndOtherReasons()
        {
            using var replay = this.RunCompleteReplay();
            using var anotherReplay = this.RunCompleteReplay();
            Assert.Equal(ExplorationStatus.Complete, replay.TestReport.Clone().GetVerdict().Status);
            var aggregate = new TestReport(this.GetConfiguration().WithTestingIterations(100));
            Assert.True(aggregate.Merge(replay.TestReport));
            Assert.True(aggregate.Merge(anotherReplay.TestReport));
            Assert.Equal(2, GetTotalPaths(aggregate));
            Assert.Equal(ExplorationStatus.Complete, aggregate.Clone().GetVerdict().Status);

            var cancellation = new TestReport(this.GetConfiguration());
            cancellation.RecordExplorationTermination(isCanceled: true);
            Assert.True(aggregate.Merge(cancellation));
            var verdict = aggregate.GetVerdict();
            Assert.Equal(ExplorationStatus.Incomplete, verdict.Status);
            Assert.Equal(new[] { IncompleteExplorationReason.TestingCanceled }, verdict.Reasons);
        }

        [Fact(Timeout = 10000)]
        public void TestLegacyReportDoesNotInventCompletedReplayEvidence()
        {
            using var replay = this.RunCompleteReplay();
            var serializer = new DataContractSerializer(typeof(TestReport));
            using var stream = new MemoryStream();
            serializer.WriteObject(stream, replay.TestReport);
            stream.Position = 0;
            var document = XDocument.Load(stream);
            document.Descendants().Single(element => element.Name.LocalName == "NumOfCompletedReplayPaths").Remove();
            stream.SetLength(0);
            document.Save(stream);
            stream.Position = 0;
            var legacy = (TestReport)serializer.ReadObject(stream);

            Assert.Equal(1, GetTotalPaths(legacy));
            Assert.Equal(ExplorationStatus.Incomplete, legacy.GetVerdict().Status);
            Assert.Contains(IncompleteExplorationReason.InsufficientExecutionPaths, legacy.GetVerdict().Reasons);
        }

        private static void RunScenario(bool reportBug)
        {
            SchedulingPoint.Interleave();
            SchedulingPoint.Interleave();
            Specification.Assert(!reportBug, "Replay verdict fixture.");
        }

        private static int GetTotalPaths(TestReport report) =>
            report.NumOfExploredFairPaths + report.NumOfExploredUnfairPaths;

        private static void InterruptSecondIteration(TestingEngine engine) =>
            engine.RegisterStartIterationCallBack(iteration =>
            {
                if (iteration is 1)
                {
                    throw new TaskCanceledException();
                }
            });

        private string RecordTrace()
        {
            using var engine = TestingEngine.Create(this.GetConfiguration().WithTestingIterations(1)
                .WithRandomGeneratorSeed(20), () => RunScenario(true));
            engine.Run();
            Assert.Equal(1, engine.TestReport.NumOfFoundBugs);
            Assert.False(string.IsNullOrEmpty(engine.ReproducibleTrace));
            return engine.ReproducibleTrace;
        }

        private TestingEngine RunCompleteReplay()
        {
            var configuration = this.GetConfiguration().WithTestingIterations(100).WithReproducibleTrace(this.RecordTrace());
            var engine = TestingEngine.Create(configuration, () => RunScenario(false));
            engine.Run();
            Assert.Equal(ExplorationStatus.Complete, engine.TestReport.GetVerdict().Status);
            return engine;
        }
    }
}
