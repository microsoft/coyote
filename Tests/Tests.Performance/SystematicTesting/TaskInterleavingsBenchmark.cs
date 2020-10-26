// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Microsoft.Coyote.SystematicTesting;

namespace Microsoft.Coyote.Tests.Performance.SystematicTesting
{
    [SimpleJob(RuntimeMoniker.NetCoreApp31)]
    [MemoryDiagnoser]
    [MinColumn, MaxColumn, MeanColumn, Q1Column, Q3Column, RankColumn]
    [MarkdownExporter, HtmlExporter, CsvExporter, CsvMeasurementsExporter, RPlotExporter]
    public class TaskInterleavingsBenchmark
    {
        private class SetupEvent : Event
        {
            public TaskCompletionSource<bool> Tcs;
            public int NumMachines;
            public int Counter;
            public bool DoHalt;

            public SetupEvent(TaskCompletionSource<bool> tcs, int numMachines, bool doHalt)
            {
                this.Tcs = tcs;
                this.NumMachines = numMachines;
                this.Counter = 0;
                this.DoHalt = doHalt;
            }
        }

        private volatile int Counter = 1;

        [Params(10)]
        public int NumTasks { get; set; }

        public async Task RunTaskInterleavings()
        {
            var tasks = new Task[this.NumTasks];
            for (int idx = 0; idx < this.NumTasks; idx++)
            {
                var task = Task.Run(async () =>
                {
                    var counter = this.Counter;
                    await Task.Yield();
                    this.Counter += counter;
                });

                tasks[idx] = task;
            }

            await Task.WhenAll(tasks);
        }

        [Benchmark]
        public void MeasureTaskInterleavingsThroughput()
        {
            var configuration = Configuration.Create().WithTestingIterations(100);
            var testingEngine = TestingEngine.Create(configuration, this.RunTaskInterleavings);
        }
    }
}
