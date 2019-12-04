// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Runtime;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.SharedObjects.Tests
{
    public class MockSharedCounterTests : BaseTest
    {
        public MockSharedCounterTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private class SetupEvent : Event
        {
            public int Flag;

            public SetupEvent(int flag)
            {
                this.Flag = flag;
            }
        }

        private class E : Event
        {
            public ISharedCounter Counter;

            public E(ISharedCounter counter)
            {
                this.Counter = counter;
            }
        }

        private class Done : Event
        {
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
                var counter = SharedCounter.Create(this.Id.Runtime, 0);
                this.CreateActor(typeof(N1), new E(counter));

                counter.Increment();
                var v = counter.GetValue();
                this.Assert(v == 1);
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
                counter.Decrement();
            }
        }

        [Fact(Timeout=5000)]
        public void TestMockSharedCounter1()
        {
            var config = Configuration.Create().WithNumberOfIterations(50);
            var test = new Action<IActorRuntime>((r) =>
            {
                r.CreateActor(typeof(M1));
            });

            this.AssertFailed(config, test, "Detected an assertion failure.");
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
                var flag = (e as SetupEvent).Flag;

                var counter = SharedCounter.Create(this.Id.Runtime, 0);
                var n = this.CreateActor(typeof(N2), new E(counter));

                int v1 = counter.CompareExchange(10, 0); // if 0 then 10
                int v2 = counter.GetValue();

                if (flag == 0)
                {
                    this.Assert((v1 == 5 && v2 == 5) ||
                        (v1 == 0 && v2 == 10) ||
                        (v1 == 0 && v2 == 15));
                }
                else if (flag == 1)
                {
                    this.Assert((v1 == 0 && v2 == 10) ||
                        (v1 == 0 && v2 == 15));
                }
                else if (flag == 2)
                {
                    this.Assert((v1 == 5 && v2 == 5) ||
                        (v1 == 0 && v2 == 15));
                }
                else if (flag == 3)
                {
                    this.Assert((v1 == 5 && v2 == 5) ||
                        (v1 == 0 && v2 == 10));
                }
            }
        }

        private class N2 : StateMachine
        {
            private ISharedCounter Counter;

            [Start]
            [OnEventDoAction(typeof(Done), nameof(Check))]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private void InitOnEntry(Event e)
            {
                this.Counter = (e as E).Counter;
                this.Counter.Add(5);
            }

            private void Check()
            {
                var v = this.Counter.GetValue();
                this.Assert(v == 0);
            }
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
                var counter = SharedCounter.Create(this.Id.Runtime, 0);
                var n = this.CreateActor(typeof(N3), new E(counter));

                counter.Add(4);
                counter.Increment();
                counter.Add(5);

                this.SendEvent(n, new Done());
            }
        }

        private class N3 : StateMachine
        {
            private ISharedCounter Counter;

            [Start]
            [OnEventDoAction(typeof(Done), nameof(Check))]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private void InitOnEntry(Event e)
            {
                this.Counter = (e as E).Counter;
                this.Counter.Add(-4);
                this.Counter.Decrement();

                var v = this.Counter.Exchange(100);
                this.Counter.Add(-5);
                this.Counter.Add(v - 100);
            }

            private void Check()
            {
                var v = this.Counter.GetValue();
                this.Assert(v == 0);
            }
        }

        [Fact(Timeout=5000)]
        public void TestMockSharedCounter2()
        {
            var config = Configuration.Create().WithNumberOfIterations(100);
            var test = new Action<IActorRuntime>((r) =>
            {
                r.CreateActor(typeof(M2), new SetupEvent(0));
            });

            this.AssertSucceeded(config, test);
        }

        [Fact(Timeout=5000)]
        public void TestMockSharedCounter3()
        {
            var config = Configuration.Create().WithNumberOfIterations(100);
            var test = new Action<IActorRuntime>((r) =>
            {
                r.CreateActor(typeof(M2), new SetupEvent(1));
            });

            this.AssertFailed(config, test, "Detected an assertion failure.");
        }

        [Fact(Timeout=5000)]
        public void TestMockSharedCounter4()
        {
            var config = Configuration.Create().WithNumberOfIterations(100);
            var test = new Action<IActorRuntime>((r) =>
            {
                r.CreateActor(typeof(M2), new SetupEvent(2));
            });

            this.AssertFailed(config, test, "Detected an assertion failure.");
        }

        [Fact(Timeout=5000)]
        public void TestMockSharedCounter5()
        {
            var config = Configuration.Create().WithNumberOfIterations(100);
            var test = new Action<IActorRuntime>((r) =>
            {
                r.CreateActor(typeof(M2), new SetupEvent(3));
            });

            this.AssertFailed(config, test, "Detected an assertion failure.");
        }

        [Fact(Timeout=5000)]
        public void TestMockSharedCounter6()
        {
            var config = Configuration.Create().WithNumberOfIterations(50);
            var test = new Action<IActorRuntime>((r) =>
            {
                r.CreateActor(typeof(M3));
            });

            this.AssertSucceeded(config, test);
        }
    }
}
