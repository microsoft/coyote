// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Runtime;

namespace Microsoft.Coyote.Benchmarking.Actors.StateMachines
{
    [ClrJob(baseline: true), CoreJob]
    [MemoryDiagnoser]
    [MinColumn, MaxColumn, MeanColumn, Q1Column, Q3Column, RankColumn]
    [MarkdownExporter, HtmlExporter, CsvExporter, CsvMeasurementsExporter, RPlotExporter]
    public class DequeueEventThroughputBenchmark
    {
        private class SetupProducerEvent : Event
        {
            public TaskCompletionSource<bool> TcsSetup;
            public ActorId Consumer;
            public long NumMessages;

            public SetupProducerEvent(TaskCompletionSource<bool> tcsSetup, ActorId consumer, long numMessages)
            {
                this.TcsSetup = tcsSetup;
                this.Consumer = consumer;
                this.NumMessages = numMessages;
            }
        }

        private class SetupConsumerEvent : Event
        {
            public TaskCompletionSource<bool> TcsExperiment;
            public long NumMessages;

            internal SetupConsumerEvent(TaskCompletionSource<bool> tcsExperiment, long numMessages)
            {
                this.TcsExperiment = tcsExperiment;
                this.NumMessages = numMessages;
            }
        }

        private class StartExperiment : Event
        {
        }

        private class Message : Event
        {
        }

        private class Ack : Event
        {
        }

        private class Producer : StateMachine
        {
            private TaskCompletionSource<bool> TcsSetup;
            private ActorId Consumer;
            private long NumMessages;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private void InitOnEntry(Event e)
            {
                this.TcsSetup = (e as SetupProducerEvent).TcsSetup;
                this.Consumer = (e as SetupProducerEvent).Consumer;
                this.NumMessages = (e as SetupProducerEvent).NumMessages;

                this.TcsSetup.SetResult(true);
                this.RaiseGotoStateEvent<Experiment>();
            }

            [OnEventDoAction(typeof(StartExperiment), nameof(Run))]
            private class Experiment : State
            {
            }

            private void Run()
            {
                for (int i = 0; i < this.NumMessages; i++)
                {
                    this.SendEvent(this.Consumer, new Message());
                }
            }
        }

        private class Consumer : StateMachine
        {
            private TaskCompletionSource<bool> TcsExperiment;
            private long NumMessages;
            private long Counter = 0;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(Message), nameof(HandleMessage))]
            private class Init : State
            {
            }

            private void InitOnEntry(Event e)
            {
                this.TcsExperiment = (e as SetupConsumerEvent).TcsExperiment;
                this.NumMessages = (e as SetupConsumerEvent).NumMessages;
            }

            private void HandleMessage()
            {
                this.Counter++;
                if (this.Counter == this.NumMessages)
                {
                    this.TcsExperiment.SetResult(true);
                }
            }
        }

        [Params(10, 100, 1000, 10000)]
        public int NumProducers { get; set; }

        private static int NumMessages => 1000000;

        private ProductionRuntime Runtime;
        private ActorId[] ProducerMachines;
        private TaskCompletionSource<bool> ExperimentAwaiter;

        [IterationSetup]
        public void IterationSetup()
        {
            var configuration = Configuration.Create();
            this.Runtime = ActorRuntimeFactory.CreateProductionRuntime(configuration);
            this.ExperimentAwaiter = new TaskCompletionSource<bool>();

            var consumer = this.Runtime.CreateActor(typeof(Consumer), null,
                new SetupConsumerEvent(this.ExperimentAwaiter, NumMessages));

            var tasks = new Task[this.NumProducers];
            this.ProducerMachines = new ActorId[this.NumProducers];
            for (int i = 0; i < this.NumProducers; i++)
            {
                var tcs = new TaskCompletionSource<bool>();
                this.ProducerMachines[i] = this.Runtime.CreateActor(typeof(Producer), null,
                    new SetupProducerEvent(tcs, consumer, NumMessages / this.NumProducers));
                tasks[i] = tcs.Task;
            }

            Task.WaitAll(tasks);
        }

        [Benchmark]
        public void MeasureDequeueEventThroughput()
        {
            for (int i = 0; i < this.NumProducers; i++)
            {
                this.Runtime.SendEvent(this.ProducerMachines[i], new StartExperiment());
            }

            this.ExperimentAwaiter.Task.Wait();
        }

        [IterationCleanup]
        public void IterationCleanup()
        {
            this.Runtime = null;
            this.ProducerMachines = null;
            this.ExperimentAwaiter = null;
        }
    }
}
