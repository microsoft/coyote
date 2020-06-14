// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
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
            public TaskCompletionSource<bool> Tcs = TaskCompletionSource.Create<bool>();
            public List<WeakReference<int[]>> Buffers = new List<WeakReference<int[]>>();
        }

        internal class E : Event
        {
            public ActorId Id;
            public readonly int[] Buffer;

            public E(ActorId id)
                : base()
            {
                this.Id = id;
                this.Buffer = new int[1000];
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
                var setup = e as SetupEvent;

                try
                {
                    for (int i = 0; i < 10; i++)
                    {
                        var n = this.CreateActor(typeof(N));

                        for (int j = 0; j < 100; j++)
                        {
                            var send = new E(this.Id);
                            setup.Buffers.Add(new WeakReference<int[]>(send.Buffer));
                            this.SendEvent(n, send);
                            E received = (E)await this.ReceiveEventAsync(typeof(E));
                        }
                    }
                }
                finally
                {
                    setup.Tcs.SetResult(true);
                }

                setup.Tcs.TrySetResult(true);
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
                var setup = new SetupEvent();
                r.CreateActor(typeof(M), setup);

                await this.WaitAsync(setup.Tcs.Task, 20000);

                (r as ActorRuntime).Stop();

                int retries = 10;
                int count = 0;
                do
                {
                    GC.Collect(3);
                    count = 0;
                    foreach (WeakReference<int[]> item in setup.Buffers)
                    {
                        if (item.TryGetTarget(out int[] buffer))
                        {
                            count++;
                        }
                    }
                }
                while (retries-- > 0 && count > 0);

                Assert.Equal(0, count);
            });
        }
    }
}
