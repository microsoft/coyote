// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.Specifications;
using Microsoft.Coyote.SystematicTesting;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Tools.Tests
{
    public class UnrewrittenAssemblyTests : BaseToolsTest
    {
        public UnrewrittenAssemblyTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Theory(Timeout = 5000)]
        [InlineData(false)]
        [InlineData(true)]
        public void TestUnrewrittenEntryAssemblyVerdict(bool isStrict)
        {
            using var engine = TestingEngine.Create(this.GetConfiguration().WithTestingIterations(3)
                .WithStrictExplorationEnabled(isStrict), NativeTaskWithSchedulingPoint);
            engine.Run();

            Assert.False(engine.IsTestRewritten());
            Assert.Equal(3, engine.TestReport.NumOfExploredFairPaths + engine.TestReport.NumOfExploredUnfairPaths);
            Assert.True(engine.TestReport.MaxExploredFairSteps > 0 || engine.TestReport.MaxExploredUnfairSteps > 0);
            Assert.Equal(ExplorationStatus.Incomplete, engine.TestReport.GetVerdict().Status);
            using var document = JsonDocument.Parse(engine.GetJsonReport());
            var root = document.RootElement;
            Assert.False(root.GetProperty("assembly").GetProperty("isRewritten").GetBoolean());
            Assert.Equal("Incomplete", root.GetProperty("verdict").GetProperty("status").GetString());
            Assert.Contains("TestAssemblyNotRewritten",
                root.GetProperty("verdict").GetProperty("reasons").EnumerateArray().Select(element => element.GetString()));
        }

        [Fact(Timeout = 5000)]
        public void TestUnrewrittenVerdictSurvivesClone()
        {
            using var engine = TestingEngine.Create(this.GetConfiguration().WithTestingIterations(1),
                NativeTaskWithSchedulingPoint);
            engine.Run();

            var clone = engine.TestReport.Clone();
            Assert.Equal(ExplorationStatus.Incomplete, clone.GetVerdict().Status);
            Assert.Contains(IncompleteExplorationReason.TestAssemblyNotRewritten, clone.GetVerdict().Reasons);
            Assert.Equal(1, clone.NumOfExploredFairPaths + clone.NumOfExploredUnfairPaths);
        }

        [Theory(Timeout = 5000)]
        [InlineData(false)]
        [InlineData(true)]
        public void TestUnrewrittenVerdictSurvivesMerge(bool isUnrewrittenSource)
        {
            var configuration = this.GetConfiguration().WithTestingIterations(1);
            using var engine = TestingEngine.Create(configuration, NativeTaskWithSchedulingPoint);
            engine.Run();
            var unknownReport = new TestReport(configuration);
            var destination = isUnrewrittenSource ? unknownReport : engine.TestReport;
            var source = isUnrewrittenSource ? engine.TestReport : unknownReport;
            Assert.True(destination.Merge(source));
            Assert.True(destination.Merge(new TestReport(configuration)));

            Assert.Equal(ExplorationStatus.Incomplete, destination.GetVerdict().Status);
            Assert.Contains(IncompleteExplorationReason.TestAssemblyNotRewritten, destination.GetVerdict().Reasons);
        }

        [Fact(Timeout = 5000)]
        public void TestUnrewrittenAssemblyIsRecordedBeforeExecution()
        {
            using var engine = TestingEngine.Create(this.GetConfiguration().WithTestingIterations(0),
                NativeTaskWithSchedulingPoint);
            engine.Run();

            Assert.Equal(0, engine.TestReport.NumOfExploredFairPaths + engine.TestReport.NumOfExploredUnfairPaths);
            Assert.Contains(IncompleteExplorationReason.TestAssemblyNotRewritten, engine.TestReport.GetVerdict().Reasons);
            Assert.Contains(IncompleteExplorationReason.NoExecutionPaths, engine.TestReport.GetVerdict().Reasons);
        }

        [Fact(Timeout = 5000)]
        public void TestReportWithoutRewriteStateIsNeutral()
        {
            using var engine = TestingEngine.Create(this.GetConfiguration().WithTestingIterations(1),
                NativeTaskWithSchedulingPoint);
            engine.Run();
            var serializer = new DataContractSerializer(typeof(TestReport));
            using var stream = new MemoryStream();
            serializer.WriteObject(stream, engine.TestReport);
            stream.Position = 0;
            var document = XDocument.Load(stream);
            document.Descendants().Single(element => element.Name.LocalName == "HasUnrewrittenTestAssembly").Remove();
            stream.SetLength(0);
            document.Save(stream);
            stream.Position = 0;
            var legacyReport = (TestReport)serializer.ReadObject(stream);

            Assert.Equal(ExplorationStatus.Complete, legacyReport.GetVerdict().Status);
            Assert.Empty(legacyReport.GetVerdict().Reasons);
        }

        [Fact(Timeout = 5000)]
        public void TestUnrewrittenVerdictPreservesBugAndErrorPriority()
        {
            using var engine = TestingEngine.Create(this.GetConfiguration().WithTestingIterations(1), () =>
            {
                SchedulingPoint.Interleave();
                Specification.Assert(false, "Fixture bug.");
            });
            engine.Run();

            Assert.Equal(ExplorationStatus.BugFound, engine.TestReport.GetVerdict().Status);
            Assert.Contains(IncompleteExplorationReason.TestAssemblyNotRewritten, engine.TestReport.GetVerdict().Reasons);
            engine.TestReport.InternalErrors.Add("Fixture internal error.");
            Assert.Equal(ExplorationStatus.InternalError, engine.TestReport.GetVerdict().Status);
            using var document = JsonDocument.Parse(engine.GetJsonReport());
            Assert.Equal("InternalError", document.RootElement.GetProperty("verdict").GetProperty("status").GetString());
        }

        [Fact(Timeout = 5000)]
        public void TestUnrewrittenActorEntryAssemblyVerdict()
        {
            using var engine = TestingEngine.Create(this.GetConfiguration().WithTestingIterations(1),
                runtime => runtime.CreateActor(typeof(EmptyActor)));
            engine.Run();

            Assert.Equal(0, engine.TestReport.NumOfFoundBugs);
            Assert.Equal(ExplorationStatus.Incomplete, engine.TestReport.GetVerdict().Status);
            Assert.Contains(IncompleteExplorationReason.TestAssemblyNotRewritten, engine.TestReport.GetVerdict().Reasons);
        }

        private class EmptyActor : Actor
        {
        }

        private static void NativeTaskWithSchedulingPoint()
        {
            // Preserve the original regression case: native tasks plus an explicit scheduling point.
            Task.Run(() => { }).Wait();
            SchedulingPoint.Interleave();
        }
    }
}
