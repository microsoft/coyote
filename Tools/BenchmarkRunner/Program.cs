// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using BenchmarkDotNet.Running;

namespace Microsoft.Coyote.Benchmarking
{
    /// <summary>
    /// The Coyote performance benchmark runner.
    /// </summary>
    internal class Program
    {
#pragma warning disable CA1801 // Parameter not used
        private static void Main(string[] args)
        {
            // BenchmarkSwitcher.FromAssembly(typeof(Program).GetTypeInfo().Assembly).Run(args);
            BenchmarkRunner.Run<Actors.StateMachines.CreationThroughputBenchmark>();
            BenchmarkRunner.Run<Actors.StateMachines.ExchangeEventLatencyBenchmark>();
            BenchmarkRunner.Run<Actors.StateMachines.SendEventThroughputBenchmark>();
            BenchmarkRunner.Run<Actors.StateMachines.DequeueEventThroughputBenchmark>();
        }
#pragma warning restore CA1801 // Parameter not used
    }
}
