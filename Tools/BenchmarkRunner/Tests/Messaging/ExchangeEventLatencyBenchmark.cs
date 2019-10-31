// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Runtime;

namespace Microsoft.Coyote.Benchmarking.Messaging
{
    [ClrJob(baseline: true), CoreJob]
    [MemoryDiagnoser]
    [MinColumn, MaxColumn, MeanColumn, Q1Column, Q3Column, RankColumn]
    [MarkdownExporter, HtmlExporter, CsvExporter, CsvMeasurementsExporter, RPlotExporter]
    public class ExchangeEventLatencyBenchmark
    {
        private class SetupTcsEvent : Event
        {
            public TaskCompletionSource<bool> Tcs;
            public long NumMessages;

            public SetupTcsEvent(TaskCompletionSource<bool> tcs, long numMessages)
            {
                this.Tcs = tcs;
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

        private class M1 : StateMachine
        {
            private TaskCompletionSource<bool> Tcs;
            private ActorId Target;
            private long NumMessages;
            private long Counter = 0;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(Message), nameof(SendMessage))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.Tcs = (this.ReceivedEvent as SetupTcsEvent).Tcs;
                this.NumMessages = (this.ReceivedEvent as SetupTcsEvent).NumMessages;
                this.Target = this.CreateStateMachine(typeof(M2), new SetupTargetEvent(this.Id, this.NumMessages));
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
                    this.SendEvent(this.Target, new Message());
                }
            }
        }

        private class M2 : StateMachine
        {
            private ActorId Target;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(Message), nameof(SendMessage))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.Target = (this.ReceivedEvent as SetupTargetEvent).Target;
            }

            private void SendMessage()
            {
                this.SendEvent(this.Target, new Message());
            }
        }

        private class M3 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private async Task InitOnEntry()
            {
                var tcs = (this.ReceivedEvent as SetupTcsEvent).Tcs;
                var numMessages = (this.ReceivedEvent as SetupTcsEvent).NumMessages;
                var target = this.CreateStateMachine(typeof(M4), new SetupTargetEvent(this.Id, numMessages));

                var counter = 0;
                while (counter < numMessages)
                {
                    counter++;
                    this.SendEvent(target, new Message());
                    await this.ReceiveEventAsync(typeof(Message));
                }

                tcs.SetResult(true);
            }
        }

        private class M4 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private async Task InitOnEntry()
            {
                var target = (this.ReceivedEvent as SetupTargetEvent).Target;
                var numMessages = (this.ReceivedEvent as SetupTargetEvent).NumMessages;

                var counter = 0;
                while (counter < numMessages)
                {
                    counter++;
                    await this.ReceiveEventAsync(typeof(Message));
                    this.SendEvent(target, new Message());
                }
            }
        }

        [Params(10000, 100000)]
        public int NumMessages { get; set; }

        [Benchmark]
        public void MeasureLatencyExchangeEvent()
        {
            var tcs = new TaskCompletionSource<bool>();

            var configuration = Configuration.Create();
            var runtime = new ProductionRuntime(configuration);
            runtime.CreateStateMachine(typeof(M1), null,
                new SetupTcsEvent(tcs, this.NumMessages));

            tcs.Task.Wait();
        }

        [Benchmark]
        public void MeasureLatencyExchangeEventViaReceive()
        {
            var tcs = new TaskCompletionSource<bool>();

            var configuration = Configuration.Create();
            var runtime = new ProductionRuntime(configuration);
            runtime.CreateStateMachine(typeof(M3), null,
                new SetupTcsEvent(tcs, this.NumMessages));

            tcs.Task.Wait();
        }
    }
}
