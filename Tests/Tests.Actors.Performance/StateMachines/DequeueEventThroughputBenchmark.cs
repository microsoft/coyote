// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace Microsoft.Coyote.Actors.Tests.Performance.StateMachines
{
    [SimpleJob(RuntimeMoniker.NetCoreApp31)]
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
            private readonly Message MessageInstance = new Message();

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
                    this.SendEvent(this.Consumer, this.MessageInstance);
                }
            }
        }

        private class Consumer : StateMachine
        {
            private TaskCompletionSource<bool> TcsExperiment;
            private long NumMessages;
            private long Counter = 0;

            [Start]
            [OnEventDoAction(typeof(SetupConsumerEvent), nameof(OnSetup))]
            [OnEventDoAction(typeof(Message), nameof(HandleMessage))]
            private class Init : State
            {
            }

            private void OnSetup(Event e)
            {
                var se = (SetupConsumerEvent)e;
                this.TcsExperiment = se.TcsExperiment;
                this.NumMessages = se.NumMessages;
            }

            private void HandleMessage()
            {
                this.Counter++;
                if (this.Counter == this.NumMessages)
                {
                    this.TcsExperiment.SetResult(true);
                    this.Counter = 0;
                }
            }
        }

        public static int NumProducers => 1;

        private static int NumMessages => 100000;

        private IActorRuntime Runtime;
        private ActorId[] ProducerMachines;
        private ActorId ConsumerId;

        [IterationSetup]
        public void IterationSetup()
        {
            if (this.Runtime is null)
            {
                var configuration = Configuration.Create();
                this.Runtime = RuntimeFactory.Create(configuration);

                this.ConsumerId = this.Runtime.CreateActor(typeof(Consumer));

                var tasks = new Task[NumProducers];
                this.ProducerMachines = new ActorId[NumProducers];
                for (int i = 0; i < NumProducers; i++)
                {
                    var tcs = new TaskCompletionSource<bool>();
                    this.ProducerMachines[i] = this.Runtime.CreateActor(typeof(Producer), null,
                        new SetupProducerEvent(tcs, this.ConsumerId, NumMessages));
                    tasks[i] = tcs.Task;
                }

                Task.WaitAll(tasks);
            }
        }

        [Benchmark]
        public async Task MeasureDequeueEventThroughput()
        {
            var tcs = new TaskCompletionSource<bool>();

            this.Runtime.SendEvent(this.ConsumerId, new SetupConsumerEvent(tcs, NumMessages));

            for (int i = 0; i < NumProducers; i++)
            {
                this.Runtime.SendEvent(this.ProducerMachines[i], new StartExperiment());
            }

            await tcs.Task;
        }
    }
}
