// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Runtime;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.SharedObjects.Tests
{
    public class ProductionSharedObjectsTests : BaseTest
    {
        public ProductionSharedObjectsTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private class E : Event
        {
            public ISharedDictionary<int, string> Dictionary;
            public ISharedCounter Counter;
            public TaskCompletionSource<bool> Tcs;

            public E(ISharedDictionary<int, string> dictionary, ISharedCounter counter, TaskCompletionSource<bool> tcs)
            {
                this.Dictionary = dictionary;
                this.Counter = counter;
                this.Tcs = tcs;
            }
        }

        private class M : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private void InitOnEntry(Event e)
            {
                var dictionary = (e as E).Dictionary;
                var counter = (e as E).Counter;
                var tcs = (e as E).Tcs;

                for (int i = 0; i < 100; i++)
                {
                    dictionary.TryAdd(i, i.ToString());
                }

                for (int i = 0; i < 100; i++)
                {
                    var b = dictionary.TryRemove(i, out string v);
                    this.Assert(b == false || v == i.ToString());

                    if (b)
                    {
                        counter.Increment();
                    }
                }

                var c = dictionary.Count;
                this.Assert(c == 0);
                tcs.TrySetResult(true);
            }
        }

        private class N : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private void InitOnEntry(Event e)
            {
                var dictionary = (e as E).Dictionary;
                var counter = (e as E).Counter;
                var tcs = (e as E).Tcs;

                for (int i = 0; i < 100; i++)
                {
                    var b = dictionary.TryRemove(i, out string v);
                    this.Assert(b == false || v == i.ToString());

                    if (b)
                    {
                        counter.Increment();
                    }
                }

                tcs.TrySetResult(true);
            }
        }

        [Fact(Timeout=5000)]
        public void TestProductionSharedObjects()
        {
            var runtime = ActorRuntimeFactory.Create();
            var dictionary = SharedDictionary.Create<int, string>(runtime);
            var counter = SharedCounter.Create(runtime);
            var tcs1 = new TaskCompletionSource<bool>();
            var tcs2 = new TaskCompletionSource<bool>();
            var failed = false;

            runtime.OnFailure += (ex) =>
            {
                failed = true;
                tcs1.TrySetResult(true);
                tcs2.TrySetResult(true);
            };

            var m1 = runtime.CreateActor(typeof(M), new E(dictionary, counter, tcs1));
            var m2 = runtime.CreateActor(typeof(N), new E(dictionary, counter, tcs2));

            Task.WaitAll(tcs1.Task, tcs2.Task);
            Assert.False(failed);
            Assert.True(counter.GetValue() == 100);
        }
    }
}
