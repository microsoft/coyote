// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.Coyote.Actors;

namespace Microsoft.Coyote.Performance.Tests.Actors.StateMachines
{
    [ClrJob(baseline: true), CoreJob]
    [MemoryDiagnoser]
    [MinColumn, MaxColumn, MeanColumn, Q1Column, Q3Column, RankColumn]
    [MarkdownExporter, HtmlExporter, CsvExporter, CsvMeasurementsExporter, RPlotExporter]
    public class PushTransitionThroughputBenchmark
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

            [OnEntry(nameof(DoBottomTransition))]
            [OnEventPushState(typeof(Trigger), typeof(Middle))]
            private class Bottom : State
            {
            }

            [OnEntry(nameof(DoMiddleTransition))]
            private class Middle : State
            {
            }

            [OnEntry(nameof(DoTopTransition))]
            [OnEventDoAction(typeof(Trigger), nameof(RaisePopStateEvent))]
            private class Top : State
            {
            }

            private void InitOnEntry(Event e)
            {
                this.Tcs = (e as SetupEvent).Tcs;
                this.NumTransitions = (e as SetupEvent).NumTransitions;
                this.RaiseGotoStateEvent(typeof(Bottom));
            }

            private void DoBottomTransition() => this.DoTransitionFromState(typeof(Bottom));

            private void DoMiddleTransition() => this.DoTransitionFromState(typeof(Middle));

            private void DoTopTransition() => this.DoTransitionFromState(typeof(Top));

            private void DoTransitionFromState(Type fromState)
            {
                if (this.NumTransitions > 0 && fromState == typeof(Bottom))
                {
                    this.RaiseEvent(Trigger.Instance);
                }
                else if (this.NumTransitions > 0 && fromState == typeof(Middle))
                {
                    this.RaisePushStateEvent(typeof(Top));
                }
                else if (this.NumTransitions > 0 && fromState == typeof(Top))
                {
                    this.SendEvent(this.Id, Trigger.Instance);
                    this.RaisePopStateEvent();
                }
                else if (this.NumTransitions == 0)
                {
                    this.RaiseHaltEvent();
                    this.Tcs.TrySetResult(true);
                }

                this.NumTransitions--;
            }
        }

        [Params(1000, 10000, 100000)]
        public int NumTransitions { get; set; }

        [Benchmark]
        public void MeasurePushTransitionThroughput()
        {
            var configuration = Configuration.Create();
            var runtime = RuntimeFactory.CreateProductionRuntime(configuration);

            var tcs = new TaskCompletionSource<bool>();
            var e = new SetupEvent(tcs, this.NumTransitions);
            runtime.CreateActor(typeof(M), null, e);

            tcs.Task.Wait();
        }
    }
}
