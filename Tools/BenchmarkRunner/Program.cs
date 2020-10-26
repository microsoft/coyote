// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Running;
using StateMachineTests = Microsoft.Coyote.Actors.Tests.Performance.StateMachines;

namespace Microsoft.Coyote.Benchmarking
{
    /// <summary>
    /// The Coyote performance benchmark runner.
    /// </summary>
    internal class Program
    {
        private static readonly List<Benchmark> Benchmarks = new List<Benchmark>()
        {
            new Benchmark(nameof(StateMachineTests.CreationThroughputBenchmark), typeof(StateMachineTests.CreationThroughputBenchmark)),
            new Benchmark(nameof(StateMachineTests.ExchangeEventLatencyBenchmark), typeof(StateMachineTests.ExchangeEventLatencyBenchmark)),
            new Benchmark(nameof(StateMachineTests.SendEventThroughputBenchmark), typeof(StateMachineTests.SendEventThroughputBenchmark)),
            new Benchmark(nameof(StateMachineTests.DequeueEventThroughputBenchmark), typeof(StateMachineTests.DequeueEventThroughputBenchmark)),
            new Benchmark(nameof(StateMachineTests.GotoTransitionThroughputBenchmark), typeof(StateMachineTests.GotoTransitionThroughputBenchmark)),
            new Benchmark(nameof(StateMachineTests.PushTransitionThroughputBenchmark), typeof(StateMachineTests.PushTransitionThroughputBenchmark))
        };

        private static int Main(string[] args)
        {
            var filters = new List<string>();
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
                            PrintUsage();
                            return 1;
                        default:
                            break;
                    }
                }
                else
                {
                    filters.Add(arg);
                }
            }

            int matching = 0;
            foreach (var benchmark in Benchmarks)
            {
                if (FilterMatches(benchmark.Name, filters))
                {
                    matching++;
                    BenchmarkRunner.Run(benchmark.Type);
                }
            }

            if (matching is 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("No benchmarks matching filters: {0}", string.Join(",", filters));
                Console.ResetColor();
                PrintUsage();
                return 1;
            }

            return 0;
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Usage: BenchmarkRunner.exe [filter]");
            Console.WriteLine("Runs all benchmarks matching the optional filter.");
            Console.WriteLine("Writing output files to the specified outdir folder.");
            Console.WriteLine("Benchmark names are:");
            foreach (var item in Benchmarks)
            {
                Console.WriteLine("  {0}", item.Name);
            }
        }

        private static bool FilterMatches(string name, List<string> filters) =>
            filters.Count is 0 || filters.Any(f => name.IndexOf(f, StringComparison.OrdinalIgnoreCase) >= 0);
    }
}
