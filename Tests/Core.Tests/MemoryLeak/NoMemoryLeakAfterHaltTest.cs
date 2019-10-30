// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Machines;
using Microsoft.Coyote.Runtime;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Core.Tests
{
    public class NoMemoryLeakAfterHaltTest : BaseTest
    {
        public NoMemoryLeakAfterHaltTest(ITestOutputHelper output)
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

            public E(MachineId id)
                : base()
            {
                this.Id = id;
            }
        }

        internal class Unit : Event
        {
        }

        private class M : StateMachine
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
                    while (counter < 100)
                    {
                        var n = this.CreateMachine(typeof(N));
                        this.Send(n, new E(this.Id));
                        await this.Receive(typeof(E));
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
            private class Init : MachineState
            {
            }

            private void Configure()
            {
                this.LargeArray = new int[10000000];
                this.LargeArray[this.LargeArray.Length - 1] = 1;
            }

            private void Act()
            {
                var sender = (this.ReceivedEvent as E).Id;
                this.Send(sender, new E(this.Id));
                this.Raise(new Halt());
            }
        }

        [Fact(Timeout=15000)]
        public async Task TestNoMemoryLeakAfterHalt()
        {
            await this.RunAsync(async r =>
            {
                var tcs = new TaskCompletionSource<bool>();
                r.CreateMachine(typeof(M), new Configure(tcs));

                await WaitAsync(tcs.Task, 15000);

                (r as ProductionRuntime).Stop();
            });
        }
    }
}
