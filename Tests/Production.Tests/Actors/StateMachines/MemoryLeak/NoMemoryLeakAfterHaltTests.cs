// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Runtime;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Production.Tests.Actors.StateMachines
{
    public class NoMemoryLeakAfterHaltTests : BaseProductionTest
    {
        public NoMemoryLeakAfterHaltTests(ITestOutputHelper output)
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

            public E(ActorId id)
                : base()
            {
                this.Id = id;
            }
        }

        private class M : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private async Task InitOnEntry(Event e)
            {
                var tcs = (e as SetupEvent).Tcs;

                try
                {
                    int counter = 0;
                    while (counter < 100)
                    {
                        var n = this.CreateActor(typeof(N));
                        this.SendEvent(n, new E(this.Id));
                        await this.ReceiveEventAsync(typeof(E));
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
            private int[] LargeArray;

            [Start]
            [OnEntry(nameof(Configure))]
            [OnEventDoAction(typeof(E), nameof(Act))]
            private class Init : State
            {
            }

            private void Configure()
            {
                this.LargeArray = new int[10000000];
                this.LargeArray[this.LargeArray.Length - 1] = 1;
            }

            private void Act(Event e)
            {
                var sender = (e as E).Id;
                this.SendEvent(sender, new E(this.Id));
                this.RaiseHaltEvent();
            }
        }

        [Fact(Timeout = 15000)]
        public async Task TestNoMemoryLeakAfterHalt()
        {
            await this.RunAsync(async r =>
            {
                var tcs = new TaskCompletionSource<bool>();
                r.CreateActor(typeof(M), new SetupEvent(tcs));

                await WaitAsync(tcs.Task, 15000);

                (r as ProductionRuntime).Stop();
            });
        }
    }
}
