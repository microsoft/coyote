// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Tasks;
using Xunit;
using Xunit.Abstractions;
using SystemTasks = System.Threading.Tasks;

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

            private async SystemTasks.Task InitOnEntry(Event e)
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
        public void TestReceiveEvent()
        {
            this.Test(async r =>
            {
                var tcs = TaskCompletionSource.Create<bool>();
                r.CreateActor(typeof(M1), new SetupTcsEvent(tcs, 18000));

                var result = await this.GetResultAsync(tcs);
                Assert.True(result);
            });
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

            private async SystemTasks.Task HandleMessage()
            {
                await this.ReceiveEventAsync(typeof(Message));
                this.Counter += 2; // +2 because we are handling a message and receiving another.

                if (this.Counter == this.NumMessages)
                {
                    this.Tcs.SetResult(true);
                }
            }
        }

        [Fact(Timeout = 20000)]
        public void TestReceiveEventAlternate()
        {
            this.Test(async r =>
            {
                var tcs = TaskCompletionSource.Create<bool>();
                r.CreateActor(typeof(M3), new SetupTcsEvent(tcs, 18000));

                var result = await this.GetResultAsync(tcs);
                Assert.True(result);
            });
        }

        private class M5 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private async SystemTasks.Task InitOnEntry(Event e)
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

            private async SystemTasks.Task InitOnEntry(Event e)
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
        public void TestReceiveEventExchange()
        {
            this.Test(async r =>
            {
                var tcs = TaskCompletionSource.Create<bool>();
                r.CreateActor(typeof(M5), new SetupTcsEvent(tcs, 18000));

                var result = await this.GetResultAsync(tcs);
                Assert.True(result);
            });
        }
    }
}
