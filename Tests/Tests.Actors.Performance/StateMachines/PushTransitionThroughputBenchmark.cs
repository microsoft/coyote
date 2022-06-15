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
                if (this.NumTransitions is 0)
                {
                    this.RaiseHaltEvent();
                    this.Tcs.TrySetResult(true);
                }
                else if (fromState == typeof(Bottom))
                {
                    this.RaiseEvent(Trigger.Instance);
                }
                else if (fromState == typeof(Middle))
                {
                    this.RaisePushStateEvent(typeof(Top));
                }
                else if (fromState == typeof(Top))
                {
                    this.SendEvent(this.Id, Trigger.Instance);
                    this.RaisePopStateEvent();
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
        public async Task MeasurePushTransitionThroughput()
        {
            var tcs = new TaskCompletionSource<bool>();
            var setup = new SetupEvent(tcs, NumTransitions);
            this.Runtime.CreateActor(typeof(M), null, setup);
            await tcs.Task;
        }
    }
}
