// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace Microsoft.Coyote.Actors.Tests.Performance.StateMachines
{
    // [MemoryDiagnoser]
    [MinColumn, MaxColumn, MeanColumn, Q1Column, Q3Column, RankColumn]
    [MarkdownExporter, HtmlExporter, CsvExporter, CsvMeasurementsExporter, RPlotExporter]
    public class CreationThroughputBenchmark
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

        private class M : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private void InitOnEntry(Event e)
            {
                var tcs = (e as SetupEvent).Tcs;
                var numMachines = (e as SetupEvent).NumMachines;
                var doHalt = (e as SetupEvent).DoHalt;

                var counter = Interlocked.Increment(ref (e as SetupEvent).Counter);
                if (counter == numMachines)
                {
                    tcs.TrySetResult(true);
                }

                if (doHalt)
                {
                    this.RaiseHaltEvent();
                }
            }
        }

        private static int NumMachines => 10000;

        private IActorRuntime Runtime;

        [Params(true, false)]
        public bool DoHalt { get; set; }

        [IterationSetup]
        public void IterationSetup()
        {
            if (this.Runtime is null)
            {
                var configuration = Configuration.Create();
                this.Runtime = RuntimeFactory.Create(configuration);
            }
        }

        [Benchmark]
        public async Task MeasureCreationThroughput()
        {
            var tcs = new TaskCompletionSource<bool>();
            var setupEvent = new SetupEvent(tcs, NumMachines, this.DoHalt);

            for (int idx = 0; idx < NumMachines; idx++)
            {
                this.Runtime.CreateActor(typeof(M), null, setupEvent);
            }

            await tcs.Task;
        }

        [IterationCleanup]
        public void IterationClean()
        {
            this.Runtime = null;
        }
    }
}
