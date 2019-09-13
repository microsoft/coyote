// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using Microsoft.Coyote.Machines;
using Microsoft.Coyote.Runtime;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Core.Tests
{
    public class NoMemoryLeakInEventSendingTest : BaseTest
    {
        public NoMemoryLeakInEventSendingTest(ITestOutputHelper output)
            : base(output)
        {
        }

        internal class Configure : Event
        {
            public TaskCompletionSource<bool> Tcs;

            public Configure(TaskCompletionSource<bool> tcs)
            {
                this.Tcs = tcs;
            }
        }

        internal class E : Event
        {
            public MachineId Id;
            public readonly int[] LargeArray;

            public E(MachineId id)
                : base()
            {
                this.Id = id;
                this.LargeArray = new int[10000000];
            }
        }

        internal class Unit : Event
        {
        }

        private class M : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private async Task InitOnEntry()
            {
                var tcs = (this.ReceivedEvent as Configure).Tcs;

                try
                {
                    int counter = 0;
                    var n = this.CreateMachine(typeof(N));

                    while (counter < 1000)
                    {
                        this.Send(n, new E(this.Id));
                        E e = (E)await this.Receive(typeof(E));
                        e.LargeArray[10] = 7;
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

        private class N : Machine
        {
            [Start]
            [OnEventDoAction(typeof(E), nameof(Act))]
            private class Init : MachineState
            {
            }

            private void Act()
            {
                var sender = (this.ReceivedEvent as E).Id;
                this.Send(sender, new E(this.Id));
            }
        }

        [Fact(Timeout=22000)]
        public async Task TestNoMemoryLeakInEventSending()
        {
            await this.RunAsync(async r =>
            {
                var tcs = new TaskCompletionSource<bool>();
                r.CreateMachine(typeof(M), new Configure(tcs));

                await WaitAsync(tcs.Task, 20000);

                (r as ProductionRuntime).Stop();
            });
        }
    }
}
