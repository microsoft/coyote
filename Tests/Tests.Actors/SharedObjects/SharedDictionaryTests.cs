// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Actors.SharedObjects;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Actors.Tests.SharedObjects
{
    public class SharedDictionaryTests : BaseActorTest
    {
        public SharedDictionaryTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private class E : Event
        {
            public SharedDictionary<int, string> Counter;
            public TaskCompletionSource<bool> Tcs;

            public E(SharedDictionary<int, string> counter, TaskCompletionSource<bool> tcs)
            {
                this.Counter = counter;
                this.Tcs = tcs;
            }
        }

        private class M1 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private void InitOnEntry(Event e)
            {
                var counter = (e as E).Counter;
                var tcs = (e as E).Tcs;
                this.CreateActor(typeof(N1), e);

                string v;
                while (counter.TryRemove(1, out v) is false)
                {
                }

                this.Assert(v is "N");

                tcs.SetResult(true);
            }
        }

        private class N1 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private void InitOnEntry(Event e)
            {
                var counter = (e as E).Counter;
                var b = counter.TryAdd(1, "N");
                this.Assert(b is true);
            }
        }

        private class M2 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private void InitOnEntry(Event e)
            {
                this.CreateActor(typeof(N2), e);
            }
        }

        private class N2 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private void InitOnEntry(Event e)
            {
                var counter = (e as E).Counter;
                var tcs = (e as E).Tcs;

                counter.TryAdd(1, "N");

                // Key doesn't exist.
                _ = counter[2];
                tcs.SetResult(true);
            }
        }

        private class M3 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private void InitOnEntry(Event e)
            {
                var counter = (e as E).Counter;
                var tcs = (e as E).Tcs;

                for (int i = 0; i < 100; i++)
                {
                    counter.TryAdd(1, "M");
                    counter[1] = "M";
                }

                var c = counter.Count;
                this.Assert(c is 1);
                tcs.SetResult(true);
            }
        }

        private class N3 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private void InitOnEntry(Event e)
            {
                var counter = (e as E).Counter;
                var tcs = (e as E).Tcs;

                for (int i = 0; i < 100; i++)
                {
                    counter.TryAdd(1, "N");
                    counter[1] = "N";
                }

                tcs.SetResult(true);
            }
        }

        private class M4 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private void InitOnEntry(Event e)
            {
                this.CreateActor(typeof(N4), e);
            }
        }

        private class N4 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private void InitOnEntry(Event e)
            {
                var counter = (e as E).Counter;
                var tcs = (e as E).Tcs;

                counter.TryAdd(1, "N");

                var b = counter.TryGetValue(2, out string _);
                this.Assert(!b);

                b = counter.TryGetValue(1, out string v);

                this.Assert(b);
                this.Assert(v is "N");

                tcs.SetResult(true);
            }
        }

        private class M5 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private void InitOnEntry(Event e)
            {
                var counter = (e as E).Counter;
                this.CreateActor(typeof(N5), e);

                for (int i = 0; i <= 100000; i++)
                {
                    counter[i] = i.ToString();
                }
            }
        }

        private class N5 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private void InitOnEntry(Event e)
            {
                var counter = (e as E).Counter;
                var tcs = (e as E).Tcs;

                while (!counter.TryGetValue(100000, out _))
                {
                }

                for (int i = 100000; i >= 0; i--)
                {
                    var b = counter.TryGetValue(i, out string v);
                    this.Assert(b && v == i.ToString());
                }

                tcs.SetResult(true);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestProductionSharedDictionary1()
        {
            var runtime = RuntimeFactory.Create();
            var counter = SharedDictionary.Create<int, string>(runtime);
            var tcs1 = new TaskCompletionSource<bool>();
            var failed = false;

            runtime.OnFailure += (ex) =>
            {
                failed = true;
                tcs1.SetResult(true);
            };

            var m1 = runtime.CreateActor(typeof(M1), new E(counter, tcs1));

            Task.WaitAll(tcs1.Task);
            Assert.False(failed);
        }

        [Fact(Timeout = 5000)]
        public void TestProductionSharedDictionary2()
        {
            var runtime = RuntimeFactory.Create();
            var counter = SharedDictionary.Create<int, string>(runtime);
            var tcs1 = new TaskCompletionSource<bool>();
            var failed = false;

            runtime.OnFailure += (ex) =>
            {
                failed = true;
                tcs1.SetResult(true);
            };

            var m1 = runtime.CreateActor(typeof(M2), new E(counter, tcs1));

            Task.WaitAll(tcs1.Task);
            Assert.True(failed);
        }

        [Fact(Timeout = 5000)]
        public void TestProductionSharedDictionary3()
        {
            var runtime = RuntimeFactory.Create();
            var counter = SharedDictionary.Create<int, string>(runtime);
            var tcs1 = new TaskCompletionSource<bool>();
            var tcs2 = new TaskCompletionSource<bool>();
            var failed = false;

            runtime.OnFailure += (ex) =>
            {
                failed = true;
                tcs1.SetResult(true);
                tcs2.SetResult(true);
            };

            var m1 = runtime.CreateActor(typeof(M3), new E(counter, tcs1));
            var m2 = runtime.CreateActor(typeof(N3), new E(counter, tcs2));

            Task.WaitAll(tcs1.Task, tcs2.Task);
            Assert.False(failed);
        }

        [Fact(Timeout = 5000)]
        public void TestProductionSharedDictionary4()
        {
            var runtime = RuntimeFactory.Create();
            var counter = SharedDictionary.Create<int, string>(runtime);
            var tcs1 = new TaskCompletionSource<bool>();
            var failed = false;

            runtime.OnFailure += (ex) =>
            {
                failed = true;
                tcs1.SetResult(true);
            };

            var m1 = runtime.CreateActor(typeof(M4), new E(counter, tcs1));

            Task.WaitAll(tcs1.Task);
            Assert.False(failed);
        }

        [Fact(Timeout = 5000)]
        public void TestProductionSharedDictionary5()
        {
            var runtime = RuntimeFactory.Create();
            var counter = SharedDictionary.Create<int, string>(runtime);
            var tcs1 = new TaskCompletionSource<bool>();
            var failed = false;

            runtime.OnFailure += (ex) =>
            {
                failed = true;
                tcs1.SetResult(true);
            };

            var m1 = runtime.CreateActor(typeof(M5), new E(counter, tcs1));

            Task.WaitAll(tcs1.Task);
            Assert.False(failed);
        }
    }
}
