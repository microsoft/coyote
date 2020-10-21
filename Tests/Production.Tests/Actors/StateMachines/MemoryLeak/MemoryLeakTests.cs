// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Tasks;
using Xunit;
using Xunit.Abstractions;
using SystemTasks = System.Threading.Tasks;

namespace Microsoft.Coyote.Production.Tests.Actors.StateMachines
{
    public class MemoryLeakTests : BaseProductionTest
    {
        public MemoryLeakTests(ITestOutputHelper output)
            : base(output)
        {
        }

        internal class SetupEvent : Event
        {
            public TaskCompletionSource<bool> Tcs = TaskCompletionSource.Create<bool>();
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

        private class M : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private async SystemTasks.Task InitOnEntry(Event e)
            {
                var setup = (SetupEvent)e;
                var tcs = setup.Tcs;

                try
                {
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
                    }
                }
                finally
                {
                    tcs.TrySetResult(true);
                }

                tcs.TrySetResult(true);
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
                this.Setup = (SetupEvent)e;
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
                if (this.Setup.HaltTest)
                {
                    this.RaiseHaltEvent();
                }
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
            this.Test(async r =>
            {
                var setup = new SetupEvent();
                r.CreateActor(typeof(M), setup);

                await this.WaitAsync(setup.Tcs.Task, 10000);

                r.Stop();

                AssertNoLeaks(setup);
            });
        }

        [Fact(Timeout = 10000)]
        public void TestNoMemoryLeakAfterHalt()
        {
            this.Test(async r =>
            {
                // test that actors don't leak after they've been halted and that
                // subsequent events that are dropped also don't leak.
                var setup = new SetupEvent() { HaltTest = true };
                r.CreateActor(typeof(M), setup);

                await this.WaitAsync(setup.Tcs.Task, 10000);

                r.Stop();

                AssertNoLeaks(setup);
            });
        }
    }
}
