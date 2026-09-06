// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Coyote.Specifications;
using Microsoft.Coyote.SystematicTesting;
using Microsoft.Coyote.SystematicTesting.Frameworks.XUnit;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.BugFinding.Tests.Reports
{
    public class JsonReportTests : BaseBugFindingTest
    {
        public JsonReportTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestJsonReportForCompleteExploration()
        {
            using var logger = new TestOutputLogger(this.TestOutput);
            using var engine = RunEngine(
                async () =>
                {
                    var entry = new SharedEntry();
                    var task1 = entry.GetWriteResultWithDelayAsync(3);
                    var task2 = entry.GetWriteResultWithDelayAsync(5);
                    await Task.WhenAll(task1, task2);
                },
                this.GetConfiguration().WithTestingIterations(10),
                logger);

            using var document = JsonDocument.Parse(engine.GetJsonReport());
            var root = document.RootElement;
            Assert.Equal(typeof(TestingEngine).Assembly.GetName().Version.ToString(),
                root.GetProperty("coyoteVersion").GetString());
            Assert.Equal("Complete", root.GetProperty("verdict").GetProperty("status").GetString());
            Assert.Equal(0, root.GetProperty("verdict").GetProperty("reasons").GetArrayLength());
            Assert.Equal(0, root.GetProperty("bugs").GetProperty("count").GetInt32());
            Assert.Equal(0, root.GetProperty("uncontrolledInvocations").GetArrayLength());
            Assert.Equal(10, GetTotalPaths(root));
            Assert.True(root.GetProperty("assembly").GetProperty("isRewritten").GetBoolean());
            Assert.False(root.GetProperty("settings").GetProperty("isStrictExplorationEnabled").GetBoolean());
            Assert.Equal(10, root.GetProperty("settings").GetProperty("iterations").GetInt32());
            Assert.False(root.TryGetProperty("artifacts", out _));
        }

        [Fact(Timeout = 5000)]
        public void TestJsonReportForIncompleteExploration()
        {
            using var logger = new TestOutputLogger(this.TestOutput);
            using var engine = RunEngine(
                () =>
                {
                    var task = new Task(() => { });
                    task.ContinueWith(_ => { }, TaskScheduler.Current);
                },
                this.GetConfiguration()
                    .WithPartiallyControlledConcurrencyAllowed()
                    .WithStrictExplorationEnabled()
                    .WithTestingIterations(5),
                logger);

            using var document = JsonDocument.Parse(engine.GetJsonReport());
            var root = document.RootElement;
            var verdict = root.GetProperty("verdict");
            Assert.Equal("Incomplete", verdict.GetProperty("status").GetString());
            Assert.Contains("UncontrolledInvocations",
                verdict.GetProperty("reasons").EnumerateArray().Select(element => element.GetString()));
            Assert.Equal(0, verdict.GetProperty("warnings").GetArrayLength());
            Assert.True(root.GetProperty("uncontrolledInvocations").GetArrayLength() > 0);
            Assert.True(root.GetProperty("settings").GetProperty("isPartiallyControlledConcurrencyAllowed").GetBoolean());
            Assert.True(root.GetProperty("settings").GetProperty("isStrictExplorationEnabled").GetBoolean());
        }

        [Fact(Timeout = 5000)]
        public void TestJsonReportIsEmittedWithArtifacts()
        {
            using var logger = new TestOutputLogger(this.TestOutput);
            using var engine = RunEngine(
                () =>
                {
                    Specification.Assert(false, "Reachable.");
                },
                this.GetConfiguration().WithTestingIterations(10).WithJsonReportEnabled(),
                logger);

            string directory = Path.Combine(Path.GetTempPath(), "coyote-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                Assert.True(engine.TryEmitReports(directory, "test", out IEnumerable<string> paths));
                string jsonPath = paths.Single(path => path.EndsWith(".report.json", StringComparison.Ordinal));

                using var document = JsonDocument.Parse(File.ReadAllText(jsonPath));
                var root = document.RootElement;
                Assert.Equal("BugFound", root.GetProperty("verdict").GetProperty("status").GetString());
                Assert.Equal(1, root.GetProperty("bugs").GetProperty("count").GetInt32());
                Assert.Contains("Reachable.", root.GetProperty("bugs").GetProperty("reports")[0].GetString());
                Assert.Equal(1, GetTotalPaths(root));

                var artifacts = root.GetProperty("artifacts");
                Assert.True(File.Exists(artifacts.GetProperty("readableTrace").GetString()));
                Assert.True(File.Exists(artifacts.GetProperty("reproducibleTrace").GetString()));
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        private static TestingEngine RunEngine(Action test, Configuration configuration, TestOutputLogger logger)
        {
            var engine = TestingEngine.Create(configuration, test);
            engine.SetLogger(logger);
            engine.Run();
            return engine;
        }

        private static TestingEngine RunEngine(Func<Task> test, Configuration configuration, TestOutputLogger logger)
        {
            var engine = TestingEngine.Create(configuration, test);
            engine.SetLogger(logger);
            engine.Run();
            return engine;
        }

        private static int GetTotalPaths(JsonElement root) =>
            root.GetProperty("exploration").GetProperty("fairPaths").GetInt32() +
            root.GetProperty("exploration").GetProperty("unfairPaths").GetInt32();
    }
}
