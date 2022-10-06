// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Actors.Tests
{
    public class MemoryLeakTests : BaseActorTest
    {
        public MemoryLeakTests(ITestOutputHelper output)
            : base(output)
        {
        }

        internal class SetupEvent : Event
        {
            public TaskCompletionSource<bool> Tcs = new TaskCompletionSource<bool>();
            public List<WeakReference<int[]>> Buffers = new List<WeakReference<int[]>>();

            internal void Add(int[] buffer)
            {
                lock (this.Buffers)
                {
                    this.Buffers.Add(new WeakReference<int[]>(buffer));
                }
            }
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

        private class M : Actor
        {
            protected override async Task OnInitializeAsync(Event e)
            {
                var setup = e as SetupEvent;
                for (int i = 0; i < 100; i++)
                {
                    var n = this.CreateActor(typeof(N), e);
                    for (int j = 0; j < 100; j++)
                    {
                        var send = new E(this.Id);
                        setup.Add(send.Buffer);
                        this.SendEvent(n, send);
                        await this.ReceiveEventAsync(typeof(E));
                    }

                    this.SendEvent(n, HaltEvent.Instance);
                }

                setup.Tcs.SetResult(true);
                this.RaiseHaltEvent();
            }
        }

        private class N : StateMachine
        {
            private int[] Buffer;
            private SetupEvent Setup;

            [Start]
            [OnEntry(nameof(Configure))]
            [OnEventDoAction(typeof(E), nameof(Act))]
            private class Init : State
            {
            }

            private void Configure(Event e)
            {
                this.Setup = e as SetupEvent;
                this.Buffer = new int[10000];
                this.Buffer[this.Buffer.Length - 1] = 1;
                this.Setup.Add(this.Buffer);
            }

            private void Act(Event e)
            {
                var sender = (e as E).Id;
                var send = new E(this.Id);
                this.Setup.Add(send.Buffer);
                this.SendEvent(sender, new E(this.Id));
            }
        }

        private static void AssertNoEventLeaks(SetupEvent e)
        {
            int retries = 10;
            int count = 0;
            do
            {
                GC.Collect(3);
                count = 0;
                foreach (WeakReference<int[]> item in e.Buffers)
                {
                    if (item.TryGetTarget(out int[] buffer))
                    {
                        count++;
                    }
                }
            }
            while (retries-- > 0 && count > 1);

            // MacOs really doesn't want to let go of the last one for some reason (perhaps
            // because we are also grabbing references in the above foreach statement).
            Assert.InRange(count, 0, 1);
        }

        [Fact(Timeout = 10000)]
        public void TestNoMemoryLeakAfterHalt()
        {
            this.Test(r =>
            {
                var tcs = new TaskCompletionSource<bool>();
                int count = 0;
                r.OnActorHalted += id =>
                {
                    count++;
                    if (count == 101)
                    {
                        tcs.SetResult(true);
                    }
                };

                var setup = new SetupEvent();
                r.CreateActor(typeof(M), setup);

                setup.Tcs.Task.Wait(10000);
                Assert.Equal(TaskStatus.RanToCompletion, setup.Tcs.Task.Status);

                tcs.Task.Wait(10000);
                Assert.Equal(TaskStatus.RanToCompletion, tcs.Task.Status);

                r.Stop();
                AssertNoEventLeaks(setup);
                Assert.Equal(101, count);
            });
        }
    }
}
