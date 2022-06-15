// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace Microsoft.Coyote.Actors.Tests.Performance.StateMachines
{
    // [MemoryDiagnoser]
    [MinColumn, MaxColumn, MeanColumn, Q1Column, Q3Column, RankColumn]
    [MarkdownExporter, HtmlExporter, CsvExporter, CsvMeasurementsExporter, RPlotExporter]
    public class GotoTransitionThroughputBenchmark
    {
        private class SetupEvent : Event
        {
            public TaskCompletionSource<bool> Tcs;
            public int NumTransitions;

            public SetupEvent(TaskCompletionSource<bool> tcs, int numTransitions)
            {
                this.Tcs = tcs;
                this.NumTransitions = numTransitions;
            }
        }

        private class Trigger : Event
        {
            public static Trigger Instance { get; } = new Trigger();

            private Trigger()
            {
            }
        }

        private class M : StateMachine
        {
            private TaskCompletionSource<bool> Tcs;
            private int NumTransitions;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            [OnEntry(nameof(PingOnEntry))]
            [OnEventGotoState(typeof(Trigger), typeof(Pong))]
            private class Ping : State
            {
            }

            [OnEntry(nameof(PongOnEntry))]
            private class Pong : State
            {
            }

            private void InitOnEntry(Event e)
            {
                this.Tcs = (e as SetupEvent).Tcs;
                this.NumTransitions = (e as SetupEvent).NumTransitions;
                this.RaiseGotoStateEvent(typeof(Ping));
            }

            private void PingOnEntry() => this.DoTransitionFromState(typeof(Ping));

            private void PongOnEntry() => this.DoTransitionFromState(typeof(Pong));

            private void DoTransitionFromState(Type fromState)
            {
                if (this.NumTransitions > 0 && fromState == typeof(Ping))
                {
                    this.RaiseEvent(Trigger.Instance);
                }
                else if (this.NumTransitions > 0 && fromState == typeof(Pong))
                {
                    this.RaiseGotoStateEvent(typeof(Ping));
                }
                else if (this.NumTransitions is 0)
                {
                    this.RaiseHaltEvent();
                    this.Tcs.TrySetResult(true);
                }

                this.NumTransitions--;
            }
        }

        public static int NumTransitions => 100000;

        private IActorRuntime Runtime;

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
        public async Task MeasureGotoTransitionThroughput()
        {
            var tcs = new TaskCompletionSource<bool>();
            var setup = new SetupEvent(tcs, NumTransitions);
            this.Runtime.CreateActor(typeof(M), null, setup);
            await tcs.Task;
        }
    }
}
