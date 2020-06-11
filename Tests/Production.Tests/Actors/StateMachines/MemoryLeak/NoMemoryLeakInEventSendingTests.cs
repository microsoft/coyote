// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Tasks;
using Xunit;
using Xunit.Abstractions;
using SystemTasks = System.Threading.Tasks;

namespace Microsoft.Coyote.Production.Tests.Actors.StateMachines
{
    public class NoMemoryLeakInEventSendingTests : BaseProductionTest
    {
        public NoMemoryLeakInEventSendingTests(ITestOutputHelper output)
            : base(output)
        {
        }

        internal class SetupEvent : Event
        {
            public TaskCompletionSource<bool> Tcs;

            public SetupEvent(TaskCompletionSource<bool> tcs)
            {
                this.Tcs = tcs;
            }
        }

        internal class E : Event
        {
            public ActorId Id;
            public readonly int[] LargeArray;

            public E(ActorId id)
                : base()
            {
                this.Id = id;
                this.LargeArray = new int[10000000];
            }
        }

        private class M : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private async SystemTasks.Task InitOnEntry(Event e)
            {
                var tcs = (e as SetupEvent).Tcs;

                try
                {
                    int counter = 0;
                    var n = this.CreateActor(typeof(N));

                    while (counter < 1000)
                    {
                        this.SendEvent(n, new E(this.Id));
                        E received = (E)await this.ReceiveEventAsync(typeof(E));
                        received.LargeArray[10] = 7;
                        counter++;
                    }
                }
                finally
                {
                    tcs.SetResult(true);
                }

                tcs.SetResult(true);
            }
        }

        private class N : StateMachine
        {
            [Start]
            [OnEventDoAction(typeof(E), nameof(Act))]
            private class Init : State
            {
            }

            private void Act(Event e)
            {
                var sender = (e as E).Id;
                this.SendEvent(sender, new E(this.Id));
            }
        }

        [Fact(Timeout = 22000)]
        public async SystemTasks.Task TestNoMemoryLeakInEventSending()
        {
            await this.RunAsync(async r =>
            {
                var tcs = TaskCompletionSource.Create<bool>();
                r.CreateActor(typeof(M), new SetupEvent(tcs));

                await this.WaitAsync(tcs.Task, 20000);

                (r as ActorRuntime).Stop();
            });
        }
    }
}
