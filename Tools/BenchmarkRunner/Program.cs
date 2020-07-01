// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using StateMachineTests = Microsoft.Coyote.Performance.Tests.Actors.StateMachines;

#pragma warning disable SA1005 // Single line comments should begin with single space

namespace Microsoft.Coyote.Benchmarking
{
    /// <summary>
    /// The Coyote performance benchmark runner.
    /// </summary>
    internal class Program
    {
        private struct BenchmarkTest
        {
            public string Name;
            public Type Test;
            public BenchmarkTest(string name, Type test)
            {
                this.Name = name;
                this.Test = test;
            }
        }

        private static readonly List<BenchmarkTest> Benchmarks = new List<BenchmarkTest>()
        {
            new BenchmarkTest("CreationThroughputBenchmark", typeof(StateMachineTests.CreationThroughputBenchmark)),
            new BenchmarkTest("ExchangeEventLatencyBenchmark", typeof(StateMachineTests.ExchangeEventLatencyBenchmark)),
            new BenchmarkTest("SendEventThroughputBenchmark", typeof(StateMachineTests.SendEventThroughputBenchmark)),
            new BenchmarkTest("DequeueEventThroughputBenchmark", typeof(StateMachineTests.DequeueEventThroughputBenchmark)),
            new BenchmarkTest("GotoTransitionThroughputBenchmark", typeof(StateMachineTests.GotoTransitionThroughputBenchmark)),
            new BenchmarkTest("PushTransitionThroughputBenchmark", typeof(StateMachineTests.PushTransitionThroughputBenchmark)),
            new BenchmarkTest("MathBenchmark", typeof(StateMachineTests.MathBenchmark)),
            new BenchmarkTest("MemoryBenchmark", typeof(StateMachineTests.MemoryBenchmark))
        };

        private string CommitId;
        private string OutputDir;
        private string DownloadPartition;
        private bool Cosmos;
        private readonly List<string> Filters = new List<string>();
        private readonly string RuntimeVersion;
        private readonly string MachineName;

        public Program()
        {
            this.MachineName = Environment.GetEnvironmentVariable("COMPUTERNAME");
            this.RuntimeVersion = GetRuntimeVersion();
        }

        private bool ParseCommandLine(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (arg[0] == '-')
                {
                    switch (arg.Trim('-'))
                    {
                        case "?":
                        case "help":
                        case "h":
                            return false;
                        case "download":
                            if (i == args.Length - 1)
                            {
                                Console.Error.WriteLine("Missing download partition key");
                                return false;
                            }

                            this.DownloadPartition = args[++i];
                            break;
                        case "cosmos":
                            this.Cosmos = true;
                            break;
                        case "commit":
                            if (i == args.Length - 1)
                            {
                                Console.Error.WriteLine("Missing commit id value");
                                return false;
                            }

                            this.CommitId = args[++i];
                            break;
                        case "outdir":
                            if (i == args.Length - 1)
                            {
                                Console.Error.WriteLine("Missing outdir value");
                                return false;
                            }

                            this.OutputDir = args[++i];
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    this.Filters.Add(arg);
                }
            }

            if (this.Cosmos && string.IsNullOrEmpty(this.CommitId))
            {
                Console.Error.WriteLine("Missing commit id argument");
                return false;
            }

            if (string.IsNullOrEmpty(this.OutputDir))
            {
                this.OutputDir = Directory.GetCurrentDirectory();
            }
            else if (!Directory.Exists(this.OutputDir))
            {
                Directory.CreateDirectory(this.OutputDir);
            }

            return true;
        }

        private static async Task<int> Main(string[] args)
        {
            Program p = new Program();
            if (!p.ParseCommandLine(args))
            {
                PrintUsage();
                return 1;
            }

            // This is how you can manually debug a test.
            // var t = new StateMachineTests.SendEventThroughputBenchmark();
            // t.MeasureSendEventThroughput();

            try
            {
                return await p.Run();
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.ToString());
                return 1;
            }
        }

        private async Task DowwnloadResults()
        {
            foreach (var file in Directory.GetFiles(this.OutputDir))
            {
                File.Delete(file);
            }

            Storage storage = new Storage();

            foreach (var b in Benchmarks)
            {
                var metadata = new TestMetadata(b.Test);
                object target = metadata.InstantiateTest();
                List<string> rowKeys = new List<string>();
                foreach (var comboList in metadata.EnumerateParamCombinations(0, new Stack<ParamInfo>()))
                {
                    foreach (var test in metadata.TestMethods)
                    {
                        string name = test.ApplyParams(target, comboList);
                        rowKeys.Add(this.CommitId + "." + b.Test.Name + "." + name);
                    }
                }

                Console.WriteLine("Downloading results for test {0}...", b.Name);

                string summaryFile = Path.Combine(this.OutputDir, "summary.csv");
                bool writeHeaders = !File.Exists(summaryFile);
                using (var file = new StreamWriter(summaryFile, true, Encoding.UTF8))
                {
                    if (writeHeaders)
                    {
                        PerfSummary.WriteHeaders(file);
                    }

                    foreach (var summary in await storage.DownloadAsync(this.DownloadPartition, rowKeys))
                    {
                        if (summary == null)
                        {
                            Console.WriteLine("Summary missing for {0}", b.Name);
                        }
                        else
                        {
                            summary.CommitId = this.CommitId;
                            summary.SetPartitionKey(this.DownloadPartition);
                            summary.WriteCsv(file);
                        }
                    }
                }
            }
        }

        private async Task<int> Run()
        {
            if (!string.IsNullOrEmpty(this.DownloadPartition))
            {
                await this.DowwnloadResults();
                return 0;
            }

            if (string.IsNullOrEmpty(this.CommitId))
            {
                this.CommitId = Guid.NewGuid().ToString().Replace("-", string.Empty);
            }

            Storage storage = new Storage();
            List<PerfSummary> results = new List<PerfSummary>();
            int matching = 0;
            foreach (var b in Benchmarks)
            {
                if (FilterMatches(b.Name, this.Filters))
                {
                    matching++;
                    var config = DefaultConfig.Instance.WithArtifactsPath(this.OutputDir)
                        .WithOption(ConfigOptions.DisableOptimizationsValidator, true);
                    config.AddDiagnoser(new CpuDiagnoser());
                    config.AddDiagnoser(new TotalMemoryDiagnoser());

                    var summary = BenchmarkRunner.Run(b.Test, config);

                    foreach (var report in summary.Reports)
                    {
                        var data = this.GetEntities(report);
                        if (data.Count > 0)
                        {
                            results.Add(new PerfSummary(data));
                        }
                    }
                }
            }

            if (matching == 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("No benchmarks matching given filter: {0}", string.Join(",", this.Filters));
                Console.ResetColor();
                PrintUsage();
                return 1;
            }
            else if (this.Cosmos)
            {
                await storage.UploadAsync(results);
            }

            return 0;
        }

        private List<PerfEntity> GetEntities(BenchmarkReport report)
        {
            List<PerfEntity> results = new List<PerfEntity>();
            string testName = report.BenchmarkCase.Descriptor.DisplayInfo;
            foreach (var p in report.BenchmarkCase.Parameters.Items)
            {
                testName += string.Format(" {0}={1}", p.Name, p.Value);
            }

            // Right now we are choosing NOT to return each test result as
            // a separate entity, as this is too much information, so for now
            // we return the "Min" time from this run, based on the idea that the
            // minimum time has the least OS noise in it so it should be more stable.
            List<double> times = new List<double>();
            foreach (var row in report.GetResultRuns())
            {
                double msPerTest = row.Nanoseconds / 1000000.0 / row.Operations;
                times.Add(msPerTest);
            }

            var e = new PerfEntity(this.MachineName, this.RuntimeVersion, this.CommitId, testName, 0)
            {
                Time = times.Min(),
                TimeStdDev = MathHelpers.StandardDeviation(times),
            };

            if (report.Metrics.ContainsKey("CPU"))
            {
                e.Cpu = report.Metrics["CPU"].Value;
            }

            if (report.Metrics.ContainsKey("TotalMemory"))
            {
                e.Memory = report.Metrics["TotalMemory"].Value;
            }

            results.Add(e);
            return results;
        }

        private void ExportToCsv(List<PerfSummary> results)
        {
            this.SaveSummary(results);
            foreach (var item in results)
            {
                this.SaveReport(item.Data);
            }
        }

        private void SaveSummary(List<PerfSummary> report)
        {
            string filename = Path.Combine(this.OutputDir, "summary.csv");
            bool writeHeaders = !File.Exists(filename);
            using (StreamWriter writer = new StreamWriter(filename, true, Encoding.UTF8))
            {
                if (writeHeaders)
                {
                    PerfSummary.WriteHeaders(writer);
                }

                foreach (var item in report)
                {
                    item.WriteCsv(writer);
                }
            }
        }

        private void SaveReport(List<PerfEntity> data)
        {
            var testName = data[0].TestName.Split(' ')[0];
            string filename = Path.Combine(this.OutputDir, testName + ".csv");
            bool writeHeaders = !File.Exists(filename);
            using (StreamWriter writer = new StreamWriter(filename, true, Encoding.UTF8))
            {
                if (writeHeaders)
                {
                    PerfEntity.WriteHeaders(writer);
                }

                foreach (var item in data)
                {
                    item.WriteCsv(writer);
                }
            }
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Usage: BenchmarkRuner [-outdir name] [-commit id] [-cosmos] [filter]");
            Console.WriteLine("Runs all benchmarks matching optional filter");
            Console.WriteLine("Writing output csv files to the specified outdir folder");
            Console.WriteLine("Benchmark names are:");
            foreach (var item in Benchmarks)
            {
                Console.WriteLine("    {0}", item.Name);
            }
        }

        private static bool FilterMatches(string name, List<string> filters)
        {
            if (filters.Count == 0)
            {
                return true;
            }

            return (from f in filters where name.IndexOf(f, StringComparison.OrdinalIgnoreCase) >= 0 select f).Any();
        }

        public static string GetRuntimeVersion()
        {
#if NETSTANDARD2_1
            return "netstandard2.1";
#elif NETSTANDARD2_0
            return "netstandard2.0";
#elif NETSTANDARD
            return "netstandard";
#elif NETCOREAPP3_1
            return "netcoreapp3.1";
#elif NETCOREAPP
            return "netcoreapp";
#elif NET48
            return "net48";
#elif NET47
            return "net47";
#elif NETFRAMEWORK
            return "net";
#endif
        }
    }
}
