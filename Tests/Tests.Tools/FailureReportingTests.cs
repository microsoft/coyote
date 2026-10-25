// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using Microsoft.Coyote.Rewriting;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.SystematicTesting;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Tools.Tests
{
    [SkipRewriting("CLI regression tests manage native child processes.")]
    public class FailureReportingTests : BaseToolsTest
    {
        public FailureReportingTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Theory(Timeout = 20000)]
        [InlineData(false)]
        [InlineData(true)]
        public void TestInternalInitializationFailureEmitsJson(bool enableXml)
        {
            this.RunCli("throw-init", 1, false, (exitCode, output, report) =>
            {
                Assert.Equal(1, exitCode);
                Assert.Contains("Fixture initialization failed.", output);
                Assert.Equal("InternalError", report.GetProperty("verdict").GetProperty("status").GetString());
                Assert.Contains("Fixture initialization failed.", report.GetProperty("internalErrors")[0].GetString());
            }, enableXml);
        }

        [Theory(Timeout = 20000)]
        [InlineData(false)]
        [InlineData(true)]
        public void TestZeroExecutionCliIsIncomplete(bool initializePastTimeout)
        {
            this.RunCli(initializePastTimeout ? "slow-init" : string.Empty,
                initializePastTimeout ? 1 : 0, initializePastTimeout, (exitCode, output, report) =>
                {
                    Assert.Equal(4, exitCode);
                    Assert.True(report.GetProperty("assembly").GetProperty("isRewritten").GetBoolean());
                    Assert.Equal(0, report.GetProperty("exploration").GetProperty("fairPaths").GetInt32() +
                        report.GetProperty("exploration").GetProperty("unfairPaths").GetInt32());
                    Assert.Equal("Incomplete", report.GetProperty("verdict").GetProperty("status").GetString());
                    Assert.Contains("NoExecutionPaths", report.GetProperty("verdict").GetProperty("reasons").ToString());
                    Assert.Contains("Exploration verdict: Incomplete", output);
                    Assert.Equal(initializePastTimeout,
                        report.GetProperty("verdict").GetProperty("reasons").ToString().Contains("TestingTimedOut"));
                    if (initializePastTimeout)
                    {
                        Assert.Contains("INIT_STARTED", output);
                        Assert.DoesNotContain("INIT_FINISHED", output);
                    }
                });
        }

        private void RunCli(string mode, int iterations, bool timeout,
            Action<int, string, JsonElement> assertResult, bool enableXml = false)
        {
            string directory = Path.Combine(Path.GetTempPath(), "CoyoteFailureCli-" + Guid.NewGuid().ToString("N"));
            string fixtureDirectory = Path.Combine(directory, "fixture");
            try
            {
                Directory.CreateDirectory(fixtureDirectory);
                foreach (string file in Directory.GetFiles(AppContext.BaseDirectory))
                {
                    File.Copy(file, Path.Combine(fixtureDirectory, Path.GetFileName(file)));
                }

                string assembly = Path.Combine(fixtureDirectory, Path.GetFileName(Assembly.GetExecutingAssembly().Location));
                var rewrite = this.InvokeCli("rewrite " + Quote(assembly), string.Empty);
                Assert.True(rewrite.ExitCode is 0, rewrite.Output);
                string arguments = "test " + Quote(assembly) + " --method " + typeof(ReportFailureFixture).FullName +
                    ".Run --iterations " + iterations + " --strict --json-report --console --outdir " + Quote(directory);
                if (timeout)
                {
                    arguments += " --timeout 1";
                }

                if (enableXml)
                {
                    arguments += " --xml-trace";
                }

                var result = this.InvokeCli(arguments, mode);
                string[] reports = Directory.GetFiles(directory, "*.report.json", SearchOption.AllDirectories);
                Assert.True(reports.Length is 1, result.Output + "\nExpected exactly one JSON report, found " + reports.Length);
                using var document = JsonDocument.Parse(File.ReadAllText(reports[0]));
                assertResult(result.ExitCode, result.Output, document.RootElement);
            }
            finally
            {
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, true);
                }
            }
        }

        private (int ExitCode, string Output) InvokeCli(string arguments, string mode)
        {
            string framework = new DirectoryInfo(AppContext.BaseDirectory).Name;
#if NETFRAMEWORK
            string cli = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../bin", framework, "coyote.exe"));
            var start = new ProcessStartInfo(cli, arguments);
#else
            string cli = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../bin", framework, "coyote.dll"));
            string host = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet";
            var start = new ProcessStartInfo(host, Quote(cli) + " " + arguments);
#endif
            start.UseShellExecute = false;
            start.CreateNoWindow = true;
            start.RedirectStandardOutput = true;
            start.RedirectStandardError = true;
            start.EnvironmentVariables["COYOTE_REPORT_FAILURE_MODE"] = mode;
            start.EnvironmentVariables["COYOTE_CLI_TELEMETRY_OPTOUT"] = "1";
            using var process = Process.Start(start);
            try
            {
                var stdout = process.StandardOutput.ReadToEndAsync();
                var stderr = process.StandardError.ReadToEndAsync();
                Assert.True(process.WaitForExit(10000), "Coyote CLI did not terminate within ten seconds.");
                string output = stdout.GetAwaiter().GetResult() + stderr.GetAwaiter().GetResult();
                this.TestOutput.WriteLine(output);
                return (process.ExitCode, output);
            }
            finally
            {
                if (!process.HasExited)
                {
                    process.Kill();
                    process.WaitForExit(5000);
                }
            }
        }

        private static string Quote(string value) => "\"" + value + "\"";
    }

    public static class ReportFailureFixture
    {
        [TestInit]
        public static void Initialize()
        {
            string mode = Environment.GetEnvironmentVariable("COYOTE_REPORT_FAILURE_MODE");
            if (mode == "throw-init")
            {
                throw new InvalidOperationException("Fixture initialization failed.");
            }

            if (mode == "slow-init")
            {
                NativeInitialization.Delay();
            }
        }

        [Test]
        public static void Run() => SchedulingPoint.Interleave();

        [SkipRewriting("Preserve native TestInit timing for the timeout regression.")]
        private static class NativeInitialization
        {
            internal static void Delay()
            {
                Console.WriteLine("INIT_STARTED");
                Thread.Sleep(3000);
                Console.WriteLine("INIT_FINISHED");
            }
        }
    }
}
