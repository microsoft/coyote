// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.Coyote.Actors;

namespace Microsoft.Coyote.Performance.Tests.Actors.StateMachines
{
    // [MemoryDiagnoser, ThreadingDiagnoser]
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

        public static int NumMachines => 10000;

        [Params(true, false)]
        public bool DoHalt { get; set; }

        private IActorRuntime Runtime;

        [IterationSetup]
        public void IterationSetup()
        {
            if (this.Runtime == null)
            {
                var configuration = Configuration.Create();
                this.Runtime = RuntimeFactory.Create(configuration);
            }
        }

        [Benchmark]
        public void MeasureCreationThroughput()
        {
            var tcs = new TaskCompletionSource<bool>();
            var setup = new SetupEvent(tcs, NumMachines, this.DoHalt);
            for (int idx = 0; idx < NumMachines; idx++)
            {
                this.Runtime.CreateActor(typeof(M), null, setup);
            }

            setup.Tcs.Task.Wait();
        }

        [IterationCleanup]
        public void IterationClean()
        {
            this.Runtime = null;
        }
    }
}
