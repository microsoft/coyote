// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Machines;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Core.Tests
{
    public class ReceiveEventStressTest : BaseTest
    {
        public ReceiveEventStressTest(ITestOutputHelper output)
            : base(output)
        {
        }

        internal class SetupTcsEvent : Event
        {
            public TaskCompletionSource<bool> Tcs;

            public int NumMessages;

            public SetupTcsEvent(TaskCompletionSource<bool> tcs, int numMessages)
            {
                this.Tcs = tcs;
                this.NumMessages = numMessages;
            }
        }

        internal class SetupIdEvent : Event
        {
            public MachineId Id;

            public int NumMessages;

            public SetupIdEvent(MachineId id, int numMessages)
            {
                this.Id = id;
                this.NumMessages = numMessages;
            }
        }

        private class Message : Event
        {
        }

        private class M1 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                var tcs = (this.ReceivedEvent as SetupTcsEvent).Tcs;
                var numMessages = (this.ReceivedEvent as SetupTcsEvent).NumMessages;

                var mid = this.CreateMachine(typeof(M2), new SetupTcsEvent(tcs, numMessages));

                var counter = 0;
                while (counter < numMessages)
                {
                    counter++;
                    this.Send(mid, new Message());
                }
            }
        }

        private class M2 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private async Task InitOnEntry()
            {
                var tcs = (this.ReceivedEvent as SetupTcsEvent).Tcs;
                var numMessages = (this.ReceivedEvent as SetupTcsEvent).NumMessages;

                var counter = 0;
                while (counter < numMessages)
                {
                    counter++;
                    await this.Receive(typeof(Message));
                }

                tcs.SetResult(true);
            }
        }

        [Fact(Timeout = 20000)]
        public async Task TestReceiveEvent()
        {
            for (int i = 0; i < 100; i++)
            {
                await this.RunAsync(async r =>
                {
                    r.Logger.WriteLine($"Iteration #{i}");

                    var tcs = new TaskCompletionSource<bool>();
                    r.CreateMachine(typeof(M1), new SetupTcsEvent(tcs, 18000));

                    var result = await GetResultAsync(tcs.Task);
                    Assert.True(result);
                });
            }
        }

        private class M3 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                var tcs = (this.ReceivedEvent as SetupTcsEvent).Tcs;
                var numMessages = (this.ReceivedEvent as SetupTcsEvent).NumMessages;

                var mid = this.CreateMachine(typeof(M4), new SetupTcsEvent(tcs, numMessages));

                var counter = 0;
                while (counter < numMessages)
                {
                    counter++;
                    this.Send(mid, new Message());
                }
            }
        }

        private class M4 : Machine
        {
            private TaskCompletionSource<bool> Tcs;

            private int NumMessages;

            private int Counter;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(Message), nameof(HandleMessage))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Tcs = (this.ReceivedEvent as SetupTcsEvent).Tcs;
                this.NumMessages = (this.ReceivedEvent as SetupTcsEvent).NumMessages;
                this.Counter = 0;
            }

            private async Task HandleMessage()
            {
                await this.Receive(typeof(Message));
                this.Counter += 2;

                if (this.Counter == this.NumMessages)
                {
                    this.Tcs.SetResult(true);
                }
            }
        }

        [Fact(Timeout = 20000)]
        public async Task TestReceiveEventAlternate()
        {
            for (int i = 0; i < 100; i++)
            {
                await this.RunAsync(async r =>
                {
                    r.Logger.WriteLine($"Iteration #{i}");

                    var tcs = new TaskCompletionSource<bool>();
                    r.CreateMachine(typeof(M3), new SetupTcsEvent(tcs, 18000));

                    var result = await GetResultAsync(tcs.Task);
                    Assert.True(result);
                });
            }
        }

        private class M5 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private async Task InitOnEntry()
            {
                var tcs = (this.ReceivedEvent as SetupTcsEvent).Tcs;
                var numMessages = (this.ReceivedEvent as SetupTcsEvent).NumMessages;

                var mid = this.CreateMachine(typeof(M6), new SetupIdEvent(this.Id, numMessages));

                var counter = 0;
                while (counter < numMessages)
                {
                    counter++;
                    this.Send(mid, new Message());
                    await this.Receive(typeof(Message));
                }

                tcs.SetResult(true);
            }
        }

        private class M6 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private async Task InitOnEntry()
            {
                var mid = (this.ReceivedEvent as SetupIdEvent).Id;
                var numMessages = (this.ReceivedEvent as SetupIdEvent).NumMessages;

                var counter = 0;
                while (counter < numMessages)
                {
                    counter++;
                    await this.Receive(typeof(Message));
                    this.Send(mid, new Message());
                }
            }
        }

        [Fact(Timeout = 20000)]
        public async Task TestReceiveEventExchange()
        {
            for (int i = 0; i < 100; i++)
            {
                await this.RunAsync(async r =>
                {
                    r.Logger.WriteLine($"Iteration #{i}");

                    var tcs = new TaskCompletionSource<bool>();
                    r.CreateMachine(typeof(M5), new SetupTcsEvent(tcs, 18000));

                    var result = await GetResultAsync(tcs.Task);
                    Assert.True(result);
                });
            }
        }
    }
}
