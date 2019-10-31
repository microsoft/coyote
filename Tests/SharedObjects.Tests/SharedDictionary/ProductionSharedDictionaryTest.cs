// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Runtime;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.SharedObjects.Tests
{
    public class ProductionSharedDictionaryTest : BaseTest
    {
        public ProductionSharedDictionaryTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class E : Event
        {
            public ISharedDictionary<int, string> Counter;
            public TaskCompletionSource<bool> Tcs;

            public E(ISharedDictionary<int, string> counter, TaskCompletionSource<bool> tcs)
            {
                this.Counter = counter;
                this.Tcs = tcs;
            }
        }

        private class M1 : StateMachine
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
                var n = this.CreateMachine(typeof(N1), this.ReceivedEvent);

                string v;
                while (counter.TryRemove(1, out v) == false)
                {
                }

                this.Assert(v == "N");

                tcs.SetResult(true);
            }
        }

        private class N1 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                var counter = (this.ReceivedEvent as E).Counter;

                var b = counter.TryAdd(1, "N");
                this.Assert(b == true);
            }
        }

        private class M2 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.CreateMachine(typeof(N2), this.ReceivedEvent);
            }
        }

        private class N2 : StateMachine
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

                counter.TryAdd(1, "N");

                // Key doesn't exist.
                var v = counter[2];
                tcs.SetResult(true);
            }
        }

        private class M3 : StateMachine
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

                for (int i = 0; i < 100; i++)
                {
                    counter.TryAdd(1, "M");
                    counter[1] = "M";
                }

                var c = counter.Count;
                this.Assert(c == 1);
                tcs.SetResult(true);
            }
        }

        private class N3 : StateMachine
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
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.CreateMachine(typeof(N4), this.ReceivedEvent);
            }
        }

        private class N4 : StateMachine
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

                counter.TryAdd(1, "N");

                var b = counter.TryGetValue(2, out string v);
                this.Assert(!b);

                b = counter.TryGetValue(1, out v);

                this.Assert(b);
                this.Assert(v == "N");

                tcs.SetResult(true);
            }
        }

        private class M5 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                var counter = (this.ReceivedEvent as E).Counter;
                var n = this.CreateMachine(typeof(N5), this.ReceivedEvent);

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
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                var counter = (this.ReceivedEvent as E).Counter;
                var tcs = (this.ReceivedEvent as E).Tcs;
                string v;

                while (!counter.TryGetValue(100000, out v))
                {
                }

                for (int i = 100000; i >= 0; i--)
                {
                    var b = counter.TryGetValue(i, out v);
                    this.Assert(b && v == i.ToString());
                }

                tcs.SetResult(true);
            }
        }

        [Fact(Timeout=5000)]
        public void TestProductionSharedDictionary1()
        {
            var runtime = ActorRuntimeFactory.Create();
            var counter = SharedDictionary.Create<int, string>(runtime);
            var tcs1 = new TaskCompletionSource<bool>();
            var failed = false;

            runtime.OnFailure += (ex) =>
            {
                failed = true;
                tcs1.SetResult(true);
            };

            var m1 = runtime.CreateMachine(typeof(M1), new E(counter, tcs1));

            Task.WaitAll(tcs1.Task);
            Assert.False(failed);
        }

        [Fact(Timeout=5000)]
        public void TestProductionSharedDictionary2()
        {
            var runtime = ActorRuntimeFactory.Create();
            var counter = SharedDictionary.Create<int, string>(runtime);
            var tcs1 = new TaskCompletionSource<bool>();
            var failed = false;

            runtime.OnFailure += (ex) =>
            {
                failed = true;
                tcs1.SetResult(true);
            };

            var m1 = runtime.CreateMachine(typeof(M2), new E(counter, tcs1));

            Task.WaitAll(tcs1.Task);
            Assert.True(failed);
        }

        [Fact(Timeout=5000)]
        public void TestProductionSharedDictionary3()
        {
            var runtime = ActorRuntimeFactory.Create();
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

            var m1 = runtime.CreateMachine(typeof(M3), new E(counter, tcs1));
            var m2 = runtime.CreateMachine(typeof(N3), new E(counter, tcs2));

            Task.WaitAll(tcs1.Task, tcs2.Task);
            Assert.False(failed);
        }

        [Fact(Timeout=5000)]
        public void TestProductionSharedDictionary4()
        {
            var runtime = ActorRuntimeFactory.Create();
            var counter = SharedDictionary.Create<int, string>(runtime);
            var tcs1 = new TaskCompletionSource<bool>();
            var failed = false;

            runtime.OnFailure += (ex) =>
            {
                failed = true;
                tcs1.SetResult(true);
            };

            var m1 = runtime.CreateMachine(typeof(M4), new E(counter, tcs1));

            Task.WaitAll(tcs1.Task);
            Assert.False(failed);
        }

        [Fact(Timeout=5000)]
        public void TestProductionSharedDictionary5()
        {
            var runtime = ActorRuntimeFactory.Create();
            var counter = SharedDictionary.Create<int, string>(runtime);
            var tcs1 = new TaskCompletionSource<bool>();
            var failed = false;

            runtime.OnFailure += (ex) =>
            {
                failed = true;
                tcs1.SetResult(true);
            };

            var m1 = runtime.CreateMachine(typeof(M5), new E(counter, tcs1));

            Task.WaitAll(tcs1.Task);
            Assert.False(failed);
        }
    }
}
