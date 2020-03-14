// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Actors;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Production.Tests.Actors.StateMachines
{
    public class ReceiveEventStressTests : BaseProductionTest
    {
        public ReceiveEventStressTests(ITestOutputHelper output)
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
            public ActorId Id;

            public int NumMessages;

            public SetupIdEvent(ActorId id, int numMessages)
            {
                this.Id = id;
                this.NumMessages = numMessages;
            }
        }

        private class Message : Event
        {
        }

        private class M1 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private void InitOnEntry(Event e)
            {
                var tcs = (e as SetupTcsEvent).Tcs;
                var numMessages = (e as SetupTcsEvent).NumMessages;

                var id = this.CreateActor(typeof(M2), new SetupTcsEvent(tcs, numMessages));

                var counter = 0;
                while (counter < numMessages)
                {
                    counter++;
                    this.SendEvent(id, new Message());
                }
            }
        }

        private class M2 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private async Task InitOnEntry(Event e)
            {
                var tcs = (e as SetupTcsEvent).Tcs;
                var numMessages = (e as SetupTcsEvent).NumMessages;

                var counter = 0;
                while (counter < numMessages)
                {
                    counter++;
                    await this.ReceiveEventAsync(typeof(Message));
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
                    r.CreateActor(typeof(M1), new SetupTcsEvent(tcs, 18000));

                    var result = await GetResultAsync(tcs.Task);
                    Assert.True(result);
                });
            }
        }

        private class M3 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private void InitOnEntry(Event e)
            {
                var tcs = (e as SetupTcsEvent).Tcs;
                var numMessages = (e as SetupTcsEvent).NumMessages;

                var id = this.CreateActor(typeof(M4), new SetupTcsEvent(tcs, numMessages));

                var counter = 0;
                while (counter < numMessages)
                {
                    counter++;
                    this.SendEvent(id, new Message());
                }
            }
        }

        private class M4 : StateMachine
        {
            private TaskCompletionSource<bool> Tcs;

            private int NumMessages;

            private int Counter;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(Message), nameof(HandleMessage))]
            private class Init : State
            {
            }

            private void InitOnEntry(Event e)
            {
                this.Tcs = (e as SetupTcsEvent).Tcs;
                this.NumMessages = (e as SetupTcsEvent).NumMessages;
                this.Counter = 0;
            }

            private async Task HandleMessage()
            {
                await this.ReceiveEventAsync(typeof(Message));
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
                    r.CreateActor(typeof(M3), new SetupTcsEvent(tcs, 18000));

                    var result = await GetResultAsync(tcs.Task);
                    Assert.True(result);
                });
            }
        }

        private class M5 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private async Task InitOnEntry(Event e)
            {
                var tcs = (e as SetupTcsEvent).Tcs;
                var numMessages = (e as SetupTcsEvent).NumMessages;

                var id = this.CreateActor(typeof(M6), new SetupIdEvent(this.Id, numMessages));

                var counter = 0;
                while (counter < numMessages)
                {
                    counter++;
                    this.SendEvent(id, new Message());
                    await this.ReceiveEventAsync(typeof(Message));
                }

                tcs.SetResult(true);
            }
        }

        private class M6 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private async Task InitOnEntry(Event e)
            {
                var id = (e as SetupIdEvent).Id;
                var numMessages = (e as SetupIdEvent).NumMessages;

                var counter = 0;
                while (counter < numMessages)
                {
                    counter++;
                    await this.ReceiveEventAsync(typeof(Message));
                    this.SendEvent(id, new Message());
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
                    r.CreateActor(typeof(M5), new SetupTcsEvent(tcs, 18000));

                    var result = await GetResultAsync(tcs.Task);
                    Assert.True(result);
                });
            }
        }
    }
}
