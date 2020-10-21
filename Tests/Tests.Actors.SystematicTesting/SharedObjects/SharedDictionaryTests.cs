// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Actors.SharedObjects;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Actors.SystematicTesting.Tests.SharedObjects
{
    public class SharedDictionaryTests : BaseActorSystematicTest
    {
        public SharedDictionaryTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private class E1 : Event
        {
            public SharedDictionary<int, string> Counter;

            public E1(SharedDictionary<int, string> counter)
            {
                this.Counter = counter;
            }
        }

        private class E2 : Event
        {
            public SharedDictionary<int, string> Counter;
            public bool Flag;

            public E2(SharedDictionary<int, string> counter, bool flag)
            {
                this.Counter = counter;
                this.Flag = flag;
            }
        }

        private class M1 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                var counter = SharedDictionary.Create<int, string>(this.Id.Runtime);
                this.CreateActor(typeof(N1), new E1(counter));
                counter.TryAdd(1, "M");

                var v = counter[1];
                this.Assert(v == "M", "Reached test assertion.");
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
                var counter = (e as E1).Counter;
                counter.TryUpdate(1, "N", "M");
            }
        }

        [Fact(Timeout = 5000)]
        public void TestSharedDictionary1()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M1));
            },
            configuration: Configuration.Create().WithTestingIterations(50),
            expectedError: "Reached test assertion.",
            replay: true);
        }

        private class M2 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                var counter = SharedDictionary.Create<int, string>(this.Id.Runtime);
                counter.TryAdd(1, "M");

                // Key not present; will throw an exception.
                _ = counter[2];
            }
        }

        [Fact(Timeout = 5000)]
        public void TestSharedDictionary2()
        {
            this.TestWithException<System.Collections.Generic.KeyNotFoundException>(r =>
            {
                r.CreateActor(typeof(M2));
            },
            configuration: Configuration.Create().WithTestingIterations(50),
            replay: true);
        }

        private class M3 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                var counter = SharedDictionary.Create<int, string>(this.Id.Runtime);
                this.CreateActor(typeof(N3), new E1(counter));

                counter.TryAdd(1, "M");

                _ = counter[1];
                var c = counter.Count;

                this.Assert(c == 1);
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
                var counter = (e as E1).Counter;
                counter.TryUpdate(1, "N", "M");
            }
        }

        [Fact(Timeout = 5000)]
        public void TestSharedDictionary3()
        {
            this.Test(r =>
            {
                r.CreateActor(typeof(M3));
            },
            configuration: Configuration.Create().WithTestingIterations(50));
        }

        private class M4 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                var counter = SharedDictionary.Create<int, string>(this.Id.Runtime);
                this.CreateActor(typeof(N4), new E1(counter));

                counter.TryAdd(1, "M");

                var b = counter.TryRemove(1, out string v);

                this.Assert(b == false || v == "M");
                this.Assert(counter.Count == 0);
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
                var counter = (e as E1).Counter;
                var b = counter.TryRemove(1, out string v);

                this.Assert(b == false || v == "M");
            }
        }

        [Fact(Timeout = 5000)]
        public void TestSharedDictionary4()
        {
            this.Test(r =>
            {
                r.CreateActor(typeof(M4));
            },
            configuration: Configuration.Create().WithTestingIterations(50));
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
                var counter = (e as E2).Counter;
                var flag = (e as E2).Flag;

                counter.TryAdd(1, "M");

                if (flag)
                {
                    this.CreateActor(typeof(N5), new E2(counter, false));
                }

                var b = counter.TryGetValue(2, out string v);

                if (!flag)
                {
                    this.Assert(!b);
                }

                if (b)
                {
                    this.Assert(v == "N");
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
                var counter = (e as E2).Counter;
                bool b = counter.TryGetValue(1, out string v);

                this.Assert(b);
                this.Assert(v == "M");

                counter.TryAdd(2, "N");
            }
        }

        [Fact(Timeout = 5000)]
        public void TestSharedDictionary5()
        {
            this.Test(r =>
            {
                var counter = SharedDictionary.Create<int, string>(r);
                r.CreateActor(typeof(M5), new E2(counter, true));
            },
            configuration: Configuration.Create().WithTestingIterations(50));
        }

        [Fact(Timeout = 5000)]
        public void TestSharedDictionary6()
        {
            this.Test(r =>
            {
                var counter = SharedDictionary.Create<int, string>(r);
                r.CreateActor(typeof(M5), new E2(counter, false));
            },
            configuration: Configuration.Create().WithTestingIterations(50));
        }

        private class M6 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private void InitOnEntry(Event e)
            {
                var counter = (e as E1).Counter;

                this.CreateActor(typeof(N6), new E1(counter));
                counter.TryAdd(1, "M");

                var b = counter.TryGetValue(2, out string _);
                this.Assert(!b, "Reached test assertion.");
            }
        }

        private class N6 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private void InitOnEntry(Event e)
            {
                var counter = (e as E1).Counter;
                counter.TryAdd(2, "N");
            }
        }

        [Fact(Timeout = 5000)]
        public void TestSharedDictionary7()
        {
            this.TestWithError(r =>
            {
                var counter = SharedDictionary.Create<int, string>(r);
                r.CreateActor(typeof(M6), new E1(counter));
            },
            configuration: Configuration.Create().WithTestingIterations(50),
            expectedError: "Reached test assertion.",
            replay: true);
        }
    }
}
