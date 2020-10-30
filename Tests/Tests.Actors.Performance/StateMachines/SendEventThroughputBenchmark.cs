// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace Microsoft.Coyote.Actors.Tests.Performance.StateMachines
{
    [SimpleJob(RuntimeMoniker.NetCoreApp31)]
    // [MemoryDiagnoser]
    [MinColumn, MaxColumn, MeanColumn, Q1Column, Q3Column, RankColumn]
    [MarkdownExporter, HtmlExporter, CsvExporter, CsvMeasurementsExporter, RPlotExporter]
    public class SendEventThroughputBenchmark
    {
        private class SetupProducerEvent : Event
        {
            public TaskCompletionSource<bool> TcsSetup;
            public long NumConsumers;
            public long NumMessages;

            public SetupProducerEvent(TaskCompletionSource<bool> tcsSetup, long numConsumers, long numMessages)
            {
                this.TcsSetup = tcsSetup;
                this.NumConsumers = numConsumers;
                this.NumMessages = numMessages;
            }
        }

        private class SetupConsumerEvent : Event
        {
            public ActorId Producer;
            public long NumMessages;

            internal SetupConsumerEvent(ActorId producer, long numMessages)
            {
                this.Producer = producer;
                this.NumMessages = numMessages;
            }
        }

        private class StartExperiment : Event
        {
            public TaskCompletionSource<bool> TcsExperiment;
            public StartExperiment(TaskCompletionSource<bool> tcs)
            {
                this.TcsExperiment = tcs;
            }
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
            private TaskCompletionSource<bool> TcsExperiment;
            private ActorId[] Consumers;
            private long NumConsumers;
            private long NumMessages;
            private long Counter;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(Ack), nameof(HandleCreationAck))]
            private class Init : State
            {
            }

            private void InitOnEntry(Event e)
            {
                var se = (SetupProducerEvent)e;
                this.TcsSetup = se.TcsSetup;
                this.NumConsumers = se.NumConsumers;
                this.NumMessages = se.NumMessages;

                this.Consumers = new ActorId[this.NumConsumers];
                this.Counter = 0;

                for (int i = 0; i < this.NumConsumers; i++)
                {
                    this.Consumers[i] = this.CreateActor(
                        typeof(Consumer),
                        new SetupConsumerEvent(this.Id, this.NumMessages / this.NumConsumers));
                }
            }

            private void HandleCreationAck()
            {
                this.Counter++;

                if (this.Counter == this.NumConsumers)
                {
                    this.TcsSetup.SetResult(true);
                    this.RaiseGotoStateEvent<Experiment>();
                }
            }

            [OnEventDoAction(typeof(StartExperiment), nameof(Run))]
            [OnEventDoAction(typeof(Ack), nameof(HandleMessageAck))]
            private class Experiment : State
            {
            }

            private void Run(Event e)
            {
                this.TcsExperiment = (e as StartExperiment).TcsExperiment;
                var m = new Message(); // no need to stress the garbage collector.
                this.Counter = 0;
                for (int i = 0; i < this.NumMessages; i++)
                {
                    this.SendEvent(this.Consumers[i % this.NumConsumers], m);
                }
            }

            private void HandleMessageAck()
            {
                this.Counter++;
                if (this.Counter == this.NumConsumers)
                {
                    this.TcsExperiment.SetResult(true);
                }
            }
        }

        private class Consumer : StateMachine
        {
            private ActorId Producer;
            private long NumMessages;
            private long Counter = 0;
            private readonly Ack AckInstance = new Ack(); // no need to stress the garbage collector.

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(Message), nameof(HandleMessage))]
            private class Init : State
            {
            }

            private void InitOnEntry(Event e)
            {
                this.Producer = (e as SetupConsumerEvent).Producer;
                this.NumMessages = (e as SetupConsumerEvent).NumMessages;
                this.SendEvent(this.Producer, this.AckInstance);
            }

            private void HandleMessage()
            {
                this.Counter++;
                if (this.Counter == this.NumMessages)
                {
                    this.SendEvent(this.Producer, this.AckInstance);
                    this.Counter = 0; // reset for next iteration.
                }
            }
        }

        public static int NumConsumers => 1;

        private static int NumMessages => 100000;

        private IActorRuntime Runtime;
        private ActorId ProducerMachine;

        [IterationSetup]
        public void IterationSetup()
        {
            if (this.ProducerMachine is null)
            {
                this.Runtime = RuntimeFactory.Create(Configuration.Create());
                var setuptcs = new TaskCompletionSource<bool>();
                this.ProducerMachine = this.Runtime.CreateActor(typeof(Producer), null,
                    new SetupProducerEvent(setuptcs, NumConsumers, NumMessages));
                setuptcs.Task.Wait();
            }
        }

        [Benchmark]
        public async Task MeasureSendEventThroughput()
        {
            var tcs = new TaskCompletionSource<bool>();
            this.Runtime.SendEvent(this.ProducerMachine, new StartExperiment(tcs));
            await tcs.Task;
        }
    }
}
