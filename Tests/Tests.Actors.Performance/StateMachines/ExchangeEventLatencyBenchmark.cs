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
    public class ExchangeEventLatencyBenchmark
    {
        private class SetupTcsEvent : Event
        {
            public long NumMessages;

            public SetupTcsEvent(long numMessages)
            {
                this.NumMessages = numMessages;
            }
        }

        private class SetupTargetEvent : Event
        {
            public ActorId Target;
            public long NumMessages;

            internal SetupTargetEvent(ActorId target, long numMessages)
            {
                this.Target = target;
                this.NumMessages = numMessages;
            }
        }

        private class Message : Event
        {
        }

        private class StartExchangeEventLatencyEvent : Event
        {
            public TaskCompletionSource<bool> Tcs;
            public StartExchangeEventLatencyEvent(TaskCompletionSource<bool> tcs)
            {
                this.Tcs = tcs;
            }
        }

        private class StartLatencyExchangeEventViaReceiveEvent : Event
        {
            public TaskCompletionSource<bool> Tcs;
            public StartLatencyExchangeEventViaReceiveEvent(TaskCompletionSource<bool> tcs)
            {
                this.Tcs = tcs;
            }
        }

        private class M1 : StateMachine
        {
            private TaskCompletionSource<bool> Tcs;
            private ActorId Target;
            private long NumMessages;
            private long Counter = 0;
            private readonly Message MessageInstance = new Message();

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(Message), nameof(SendMessage))]
            [OnEventDoAction(typeof(StartExchangeEventLatencyEvent), nameof(StartExchangeEventLatency))]
            [OnEventDoAction(typeof(StartLatencyExchangeEventViaReceiveEvent), nameof(StartLatencyExchangeEventViaReceive))]
            private class Init : State
            {
            }

            private void InitOnEntry(Event e)
            {
                this.NumMessages = (e as SetupTcsEvent).NumMessages;
                this.Target = this.CreateActor(typeof(M2), new SetupTargetEvent(this.Id, this.NumMessages));
            }

            private void StartExchangeEventLatency(Event e)
            {
                this.Counter = 0;
                this.Tcs = (e as StartExchangeEventLatencyEvent).Tcs;
                this.SendMessage();
            }

            private void SendMessage()
            {
                if (this.Counter == this.NumMessages)
                {
                    this.Tcs.SetResult(true);
                }
                else
                {
                    this.Counter++;
                    this.SendEvent(this.Target, this.MessageInstance);
                }
            }

            private async Task StartLatencyExchangeEventViaReceive(Event e)
            {
                this.Tcs = (e as StartLatencyExchangeEventViaReceiveEvent).Tcs;
                this.SendEvent(this.Target, e);
                var counter = 0;
                while (counter < this.NumMessages)
                {
                    counter++;
                    await this.ReceiveEventAsync(typeof(Message));
                    this.SendEvent(this.Target, this.MessageInstance);
                }

                this.Tcs.SetResult(true);
            }
        }

        private class M2 : StateMachine
        {
            private ActorId Target;
            private long NumMessages;
            private readonly Message MessageInstance = new Message();

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(Message), nameof(SendMessage))]
            [OnEventDoAction(typeof(StartLatencyExchangeEventViaReceiveEvent), nameof(StartLatencyExchangeEventViaReceive))]
            private class Init : State
            {
            }

            private void InitOnEntry(Event e)
            {
                var s = (SetupTargetEvent)e;
                this.Target = s.Target;
                this.NumMessages = s.NumMessages;
            }

            private void SendMessage()
            {
                this.SendEvent(this.Target, this.MessageInstance);
            }

            private async Task StartLatencyExchangeEventViaReceive()
            {
                var counter = 0;
                while (counter < this.NumMessages)
                {
                    counter++;
                    this.SendEvent(this.Target, this.MessageInstance);
                    await this.ReceiveEventAsync(typeof(Message));
                }
            }
        }

        public static int NumMessages => 100000;

        private IActorRuntime Runtime;
        private ActorId Master;

        [IterationSetup]
        public void IterationSetup()
        {
            if (this.Runtime is null)
            {
                var configuration = Configuration.Create();
                this.Runtime = RuntimeFactory.Create(configuration);
                this.Master = this.Runtime.CreateActor(typeof(M1), null, new SetupTcsEvent(NumMessages));
            }
        }

        [Benchmark]
        public async Task MeasureExchangeEventLatency()
        {
            var tcs = new TaskCompletionSource<bool>();
            this.Runtime.SendEvent(this.Master, new StartExchangeEventLatencyEvent(tcs));
            await tcs.Task;
        }

        [Benchmark]
        public async Task MeasureLatencyExchangeEventViaReceive()
        {
            var tcs = new TaskCompletionSource<bool>();
            this.Runtime.SendEvent(this.Master, new StartLatencyExchangeEventViaReceiveEvent(tcs));
            await tcs.Task;
        }
    }
}
