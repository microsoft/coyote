// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using Microsoft.Coyote.Rewriting;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.SystematicTesting;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Tools.Tests
{
    [SkipRewriting("CLI regression tests manage native child processes.")]
    public class FailureReportingTests : BaseToolsTest
    {
        private const string StrategyRotationPaused = "STRATEGY_ROTATION_PAUSED";

        public FailureReportingTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Theory(Timeout = 20000)]
        [InlineData(false)]
        [InlineData(true)]
        public void TestInternalInitializationFailureEmitsJson(bool enableXml)
        {
            this.RunCli("throw-init", 1, 0, (exitCode, output, report) =>
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
                initializePastTimeout ? 1 : 0, initializePastTimeout ? 1 : 0, (exitCode, output, report) =>
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

        [Theory(Timeout = 20000)]
        [InlineData(false, 0, "Complete")]
        [InlineData(true, 4, "Incomplete")]
        public void TestSingleStrategyTimeoutEmitsJsonAfterCompletedPath(bool omitSchedulingDecision,
            int expectedExitCode, string expectedVerdict)
        {
            this.RunCli(omitSchedulingDecision ? "timeout-after-path-without-decisions" : "timeout-after-path",
                2, 3, (exitCode, output, report) =>
                {
                    Assert.Equal(expectedExitCode, exitCode);
                    Assert.Equal("random", report.GetProperty("settings").GetProperty("strategy").GetString());
                    var exploration = report.GetProperty("exploration");
                    Assert.Equal(1, exploration.GetProperty("fairPaths").GetInt32() +
                        exploration.GetProperty("unfairPaths").GetInt32());
                    var verdict = report.GetProperty("verdict");
                    Assert.Equal(expectedVerdict, verdict.GetProperty("status").GetString());
                    Assert.Equal(omitSchedulingDecision,
                        verdict.GetProperty("reasons").ToString().Contains("NoSchedulingDecisions"));
                    Assert.Contains("TestingTimedOut", verdict.GetProperty("warnings").ToString());
                    Assert.Contains("Exploration verdict: " + expectedVerdict, output);
                }, pauseDuringStrategyRotation: true);
        }

        private void RunCli(string mode, int iterations, int timeoutSeconds,
            Action<int, string, JsonElement> assertResult, bool enableXml = false, bool pauseDuringStrategyRotation = false)
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
                if (timeoutSeconds > 0)
                {
                    arguments += " --timeout " + timeoutSeconds;
                }

                string cli = GetCliPath();
                if (pauseDuringStrategyRotation)
                {
                    arguments += " --strategy random";
                    cli = CreateCliWithPausedStrategyRotation(cli, directory);
                }

                if (enableXml)
                {
                    arguments += " --xml-trace";
                }

                var result = this.InvokeCli(arguments, mode, cli);
                if (pauseDuringStrategyRotation)
                {
                    int pause = result.Output.IndexOf(StrategyRotationPaused, StringComparison.Ordinal);
                    int reportStart = result.Output.IndexOf("... Emitting execution trace reports:", StringComparison.Ordinal);
                    Assert.True(pause >= 0 && reportStart > pause,
                        $"Expected strategy rotation to pause before report emission.\n{result.Output}");
                }

                string[] reports = Directory.GetFiles(directory, "*.report.json", SearchOption.AllDirectories);
                Assert.True(reports.Length is 1,
                    $"{result.Output}\nExpected exactly one JSON report, found {reports.Length} (exit {result.ExitCode}).");
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

        private static string CreateCliWithPausedStrategyRotation(string cli, string directory)
        {
            const string coreAssemblyName = "Microsoft.Coyote.dll";
            string cliDirectory = Path.GetDirectoryName(cli);
            string isolatedCli = Directory.CreateDirectory(Path.Combine(directory, "cli")).FullName;
            foreach (string file in Directory.GetFiles(cliDirectory))
            {
                File.Copy(file, Path.Combine(isolatedCli, Path.GetFileName(file)));
            }

            // Pause in the actual rotation, after removing the old head or before publishing the new cursor.
            // The CLI timeout can report while the iteration remains parked. Only this disposable copy is changed.
            using var core = AssemblyDefinition.ReadAssembly(Path.Combine(cliDirectory, coreAssemblyName));
            var scheduler = core.MainModule.GetType("Microsoft.Coyote.Runtime.OperationScheduler");
            var initialize = scheduler.Methods.Single(method => method.Name == "InitializeNextIteration");
            var checkpoint = Assert.Single(initialize.Body.Instructions.Where(instruction =>
                instruction.Operand is MethodReference method &&
                ((method.DeclaringType.Name == "LinkedList`1" && method.Name == "RemoveFirst") ||
                 (method.DeclaringType.Name == "LinkedListNode`1" && method.Name == "get_Next"))));

            // The cursor checkpoint is valid only when rotation leaves the strategy list intact.
            if (checkpoint.Operand is MethodReference { Name: "get_Next" })
            {
                Assert.DoesNotContain(initialize.Body.Instructions, instruction =>
                    instruction.Operand is MethodReference method && method.DeclaringType.Name == "LinkedList`1" &&
                    (method.Name.StartsWith("Add", StringComparison.Ordinal) ||
                     method.Name.StartsWith("Remove", StringComparison.Ordinal) || method.Name == "Clear"));
            }

            var processor = initialize.Body.GetILProcessor();
            var marker = processor.Create(OpCodes.Ldstr, StrategyRotationPaused);
            processor.InsertAfter(checkpoint, marker);
            var writeLine = processor.Create(OpCodes.Call,
                core.MainModule.ImportReference(typeof(Console).GetMethod(nameof(Console.WriteLine), new[] { typeof(string) })));
            processor.InsertAfter(marker, writeLine);
            var delay = processor.Create(OpCodes.Ldc_I4_M1);
            processor.InsertAfter(writeLine, delay);
            processor.InsertAfter(delay, processor.Create(OpCodes.Call,
                core.MainModule.ImportReference(typeof(Thread).GetMethod(nameof(Thread.Sleep), new[] { typeof(int) }))));

            string signingKey = Path.Combine(AppContext.BaseDirectory, "../../../../Common/Key.snk");
            core.Write(Path.Combine(isolatedCli, coreAssemblyName),
                new WriterParameters { StrongNameKeyBlob = File.ReadAllBytes(signingKey) });
            return Path.Combine(isolatedCli, Path.GetFileName(cli));
        }

        private static string GetCliPath()
        {
            string framework = new DirectoryInfo(AppContext.BaseDirectory).Name;
#if NETFRAMEWORK
            return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../bin", framework, "coyote.exe"));
#else
            return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../bin", framework, "coyote.dll"));
#endif
        }

        private (int ExitCode, string Output) InvokeCli(string arguments, string mode, string cli = null)
        {
            cli ??= GetCliPath();
#if NETFRAMEWORK
            var start = new ProcessStartInfo(cli, arguments);
#else
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
        public static void Run()
        {
            if (Environment.GetEnvironmentVariable("COYOTE_REPORT_FAILURE_MODE") != "timeout-after-path-without-decisions")
            {
                SchedulingPoint.Interleave();
            }
        }

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
