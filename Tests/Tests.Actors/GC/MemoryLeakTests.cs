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
            public bool HaltTest;

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

                    if (setup.HaltTest)
                    {
                        this.SendEvent(n, HaltEvent.Instance);
                    }
                }

                if (setup.HaltTest)
                {
                    this.RaiseHaltEvent();
                }

                setup.Tcs.SetResult(true);
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

        private static void AssertNoLeaks(SetupEvent e)
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
            Assert.True(count <= 1);
        }

        [Fact(Timeout = 10000)]
        public void TestNoMemoryLeakInEventSending()
        {
            this.Test(r =>
            {
                var setup = new SetupEvent();
                r.CreateActor(typeof(M), setup);

                setup.Tcs.Task.Wait(10000);
                Assert.True(setup.Tcs.Task.Status is TaskStatus.RanToCompletion);

                r.Stop();
                AssertNoLeaks(setup);
            });
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

                var setup = new SetupEvent() { HaltTest = true };
                r.CreateActor(typeof(M), setup);

                setup.Tcs.Task.Wait(10000);
                Assert.True(setup.Tcs.Task.Status is TaskStatus.RanToCompletion);

                tcs.Task.Wait(10000);
                Assert.True(tcs.Task.Status is TaskStatus.RanToCompletion);

                r.Stop();
                AssertNoLeaks(setup);
                Assert.True(count is 101);
            });
        }
    }
}
