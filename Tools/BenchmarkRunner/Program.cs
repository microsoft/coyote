// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using BenchmarkDotNet.Running;
using StateMachineTests = Microsoft.Coyote.Performance.Tests.Actors.StateMachines;

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
            BenchmarkRunner.Run<StateMachineTests.CreationThroughputBenchmark>();
            BenchmarkRunner.Run<StateMachineTests.ExchangeEventLatencyBenchmark>();
            BenchmarkRunner.Run<StateMachineTests.SendEventThroughputBenchmark>();
            BenchmarkRunner.Run<StateMachineTests.DequeueEventThroughputBenchmark>();
            BenchmarkRunner.Run<StateMachineTests.GotoTransitionThroughputBenchmark>();
            BenchmarkRunner.Run<StateMachineTests.PushTransitionThroughputBenchmark>();
        }
#pragma warning restore CA1801 // Parameter not used
    }
}
