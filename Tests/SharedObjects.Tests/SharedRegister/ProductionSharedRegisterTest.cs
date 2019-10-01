// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Machines;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.SharedObjects.Tests
{
    public class ProductionSharedRegisterTest : BaseTest
    {
        public ProductionSharedRegisterTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class E : Event
        {
            public ISharedRegister<int> Counter;
            public TaskCompletionSource<bool> Tcs;

            public E(ISharedRegister<int> counter, TaskCompletionSource<bool> tcs)
            {
                this.Counter = counter;
                this.Tcs = tcs;
            }
        }

        private class M : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                var counter = (this.ReceivedEvent as E).Counter;
                var tcs = (this.ReceivedEvent as E).Tcs;

                for (int i = 0; i < 1000; i++)
                {
                    counter.Update(x => x + 5);

                    var v1 = counter.GetValue();
                    this.Assert(v1 == 10 || v1 == 15);

                    counter.Update(x => x - 5);

                    var v2 = counter.GetValue();
                    this.Assert(v2 == 5 || v2 == 10);
                }

                tcs.SetResult(true);
            }
        }

        [Fact(Timeout=5000)]
        public void TestProductionSharedRegister()
        {
            var runtime = MachineRuntimeFactory.Create();
            var counter = SharedRegister.Create<int>(runtime, 0);
            counter.SetValue(5);

            var tcs1 = new TaskCompletionSource<bool>();
            var tcs2 = new TaskCompletionSource<bool>();
            var failed = false;

            runtime.OnFailure += (ex) =>
            {
                failed = true;
                tcs1.SetResult(true);
                tcs2.SetResult(true);
            };

            var m1 = runtime.CreateMachine(typeof(M), new E(counter, tcs1));
            var m2 = runtime.CreateMachine(typeof(M), new E(counter, tcs2));

            Task.WaitAll(tcs1.Task, tcs2.Task);
            Assert.False(failed);
        }
    }
}
