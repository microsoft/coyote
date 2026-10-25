// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
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
    public class ReportFailureRegressionTests : BaseBugFindingTest
    {
        public ReportFailureRegressionTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 10000)]
        public void TestIgnoredNativeDeadlockIsIncomplete()
        {
            using var release = new ManualResetEventSlim();
            using var completed = new ManualResetEventSlim();
            using var engine = TestingEngine.Create(this.GetConfiguration().WithTestingIterations(1)
                .WithDeadlockTimeout(50).WithPotentialDeadlocksReportedAsBugs(false), () =>
                {
                    SchedulingPoint.Interleave();
                    NativeWait.Wait(release, completed);
                });
            try
            {
                engine.Run();
                Assert.Equal(0, engine.TestReport.NumOfFoundBugs);
                Assert.False(engine.TestReport.IsUncontrolledConcurrencyDetected);
                Assert.True(engine.TestReport.MaxExploredFairSteps > 0 || engine.TestReport.MaxExploredUnfairSteps > 0);
                Assert.Equal(ExplorationStatus.Incomplete, engine.TestReport.GetVerdict().Status);
                Assert.Contains(engine.TestReport.GetVerdict().Reasons, reason => reason.ToString() == "ExecutionInterrupted");
                Assert.Equal(ExplorationStatus.Incomplete, engine.TestReport.Clone().GetVerdict().Status);
                var merged = new TestReport(engine.TestReport.Configuration);
                Assert.True(merged.Merge(engine.TestReport));
                Assert.Equal(ExplorationStatus.Incomplete, merged.GetVerdict().Status);
            }
            finally
            {
                release.Set();
                Assert.True(completed.Wait(5000));
            }
        }

        [Theory(Timeout = 10000)]
        [InlineData(false)]
        [InlineData(true)]
        public void TestCallbackCancellationWithoutEngineCancellationIsIncomplete(bool isStartCallback)
        {
            using var engine = TestingEngine.Create(this.GetConfiguration().WithTestingTimeout(10),
                () => SchedulingPoint.Interleave());
            Action<uint> callback = iteration =>
            {
                if (iteration > 0)
                {
                    throw new TaskCanceledException("Unexpected callback cancellation.");
                }
            };
            if (isStartCallback)
            {
                engine.RegisterStartIterationCallBack(callback);
            }
            else
            {
                engine.RegisterEndIterationCallBack(callback);
            }

            engine.Run();
            Assert.True(engine.TestReport.NumOfExploredFairPaths + engine.TestReport.NumOfExploredUnfairPaths > 0);
            Assert.False(engine.TestReport.IsExplorationTimedOut);
            Assert.False(engine.TestReport.IsExplorationCanceled);
            Assert.Equal(ExplorationStatus.Incomplete, engine.TestReport.GetVerdict().Status);
            Assert.Contains(engine.TestReport.GetVerdict().Reasons, reason => reason.ToString() == "ExecutionInterrupted");
            using var document = JsonDocument.Parse(engine.GetJsonReport());
            Assert.Equal("Incomplete", document.RootElement.GetProperty("verdict").GetProperty("status").GetString());
        }

        [Fact(Timeout = 10000)]
        public void TestReplayMismatchAfterSchedulingIsNotComplete()
        {
            Action original = () =>
            {
                SchedulingPoint.Interleave();
                Microsoft.Coyote.Random.Generator.Create().NextBoolean();
                Specification.Assert(false, "Replay fixture bug.");
            };
            using var recorded = TestingEngine.Create(this.GetConfiguration().WithTestingIterations(1), original);
            recorded.Run();
            Assert.Equal(ExplorationStatus.BugFound, recorded.TestReport.GetVerdict().Status);
            Assert.False(string.IsNullOrEmpty(recorded.ReproducibleTrace));
            this.TestOutput.WriteLine("RECORDED_TRACE\n" + recorded.ReproducibleTrace);

            using var replay = TestingEngine.Create(this.GetConfiguration().WithTestingIterations(1)
                .WithReproducibleTrace(recorded.ReproducibleTrace), () =>
                {
                    SchedulingPoint.Interleave();
                    Microsoft.Coyote.Random.Generator.Create().NextInteger(2);
                });
            replay.Run();
            Assert.True(replay.TestReport.MaxExploredFairSteps > 0 || replay.TestReport.MaxExploredUnfairSteps > 0);
            Assert.Equal(0, replay.TestReport.NumOfFoundBugs);
            Assert.NotEmpty(replay.TestReport.InternalErrors);
            Assert.Equal(ExplorationStatus.InternalError, replay.TestReport.GetVerdict().Status);
            using var document = JsonDocument.Parse(replay.GetJsonReport());
            Assert.Equal("InternalError", document.RootElement.GetProperty("verdict").GetProperty("status").GetString());
            Assert.NotEmpty(document.RootElement.GetProperty("internalErrors").EnumerateArray());
            this.TestOutput.WriteLine("MISMATCH_REPORT\n" + replay.GetJsonReport());

            using var matched = TestingEngine.Create(this.GetConfiguration().WithTestingIterations(1)
                .WithReproducibleTrace(recorded.ReproducibleTrace), original);
            matched.Run();
            Assert.Equal(ExplorationStatus.BugFound, matched.TestReport.GetVerdict().Status);
            Assert.Empty(matched.TestReport.InternalErrors);
            this.TestOutput.WriteLine("MATCHED_REPORT\n" + matched.GetJsonReport());
        }

        [Fact(Timeout = 10000)]
        public void TestDynamicAssemblyJsonAndEmission()
        {
            var name = new AssemblyName("DynamicReportFixture" + Guid.NewGuid().ToString("N"));
#if NETFRAMEWORK
            var assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(name, AssemblyBuilderAccess.Run);
#else
            var assembly = AssemblyBuilder.DefineDynamicAssembly(name, AssemblyBuilderAccess.Run);
#endif
            var builder = assembly.DefineDynamicModule(name.Name).DefineType("Fixture", TypeAttributes.Public);
            var method = builder.DefineMethod("Run", MethodAttributes.Public | MethodAttributes.Static,
                typeof(void), Type.EmptyTypes);
            method.GetILGenerator().Emit(OpCodes.Ret);
            var type = builder.CreateType();
            var action = (Action)Delegate.CreateDelegate(typeof(Action), type.GetMethod("Run"));
            using var engine = TestingEngine.Create(this.GetConfiguration().WithTestingIterations(1)
                .WithJsonReportEnabled(), action);
            engine.Run();
            using var document = JsonDocument.Parse(engine.GetJsonReport());
            var identity = document.RootElement.GetProperty("assembly");
            Assert.Equal(name.Name, identity.GetProperty("name").GetString());
            Assert.False(identity.GetProperty("isRewritten").GetBoolean());
            Assert.False(identity.TryGetProperty("path", out var path) && path.ValueKind != JsonValueKind.Null);
            Assert.False(identity.TryGetProperty("sha256", out var hash) && hash.ValueKind != JsonValueKind.Null);

            string directory = Path.Combine(Path.GetTempPath(), "CoyoteDynamicReport-" + Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(directory);
                Assert.True(engine.TryEmitReports(directory, "dynamic", out var reports));
                Assert.Contains(reports, report => report.EndsWith(".report.json", StringComparison.Ordinal));
            }
            finally
            {
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, true);
                }
            }
        }

        [Fact(Timeout = 5000)]
        public void TestDerivedReportAndConfigurationClone()
        {
            var configuration = new DerivedConfiguration { Marker = "configuration" };
            configuration.WithTestingIterations(1);
            var report = new DerivedReport(configuration)
            {
                Marker = "report",
                NumOfExploredFairPaths = 1,
                MaxExploredFairSteps = 1
            };
            var clone = Assert.IsType<DerivedReport>(report.Clone());
            Assert.Equal("report", clone.Marker);
            Assert.Equal("configuration", Assert.IsType<DerivedConfiguration>(clone.Configuration).Marker);
            Assert.Equal(ExplorationStatus.Complete, clone.GetVerdict().Status);
        }

        [Theory(Timeout = 10000)]
        [InlineData(false)]
        [InlineData(true)]
        public void TestDerivedConfigurationAllReportApis(bool enableJson)
        {
            var configuration = new DerivedConfiguration { Marker = "configuration" };
            configuration.WithTelemetryEnabled(false).WithTestingIterations(1).WithJsonReportEnabled(enableJson);
            using var engine = TestingEngine.Create(configuration, () => SchedulingPoint.Interleave());
            engine.Run();
            Assert.Contains("Complete", engine.GetReport());
            using var json = JsonDocument.Parse(engine.GetJsonReport());
            Assert.Equal("Complete", json.RootElement.GetProperty("verdict").GetProperty("status").GetString());
            string directory = Path.Combine(Path.GetTempPath(), "CoyoteDerivedReport-" + Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(directory);
                Assert.Equal(enableJson, engine.TryEmitReports(directory, "derived", out var reports));
                Assert.Equal(enableJson, reports.Any(report => report.EndsWith(".report.json", StringComparison.Ordinal)));
            }
            finally
            {
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, true);
                }
            }
        }

        [DataContract]
        private class DerivedConfiguration : Configuration
        {
            [DataMember]
            public string Marker { get; set; }
        }

        [DataContract]
        private class DerivedReport : TestReport
        {
            [DataMember]
            public string Marker { get; set; }

            public DerivedReport(Configuration configuration)
                : base(configuration)
            {
            }
        }

        [SkipRewriting("Exercise the periodic monitor without an intercepted native wait.")]
        private static class NativeWait
        {
            internal static void Wait(object release, object completed)
            {
                try
                {
                    ((ManualResetEventSlim)release).Wait();
                }
                finally
                {
                    ((ManualResetEventSlim)completed).Set();
                }
            }
        }
    }
}
