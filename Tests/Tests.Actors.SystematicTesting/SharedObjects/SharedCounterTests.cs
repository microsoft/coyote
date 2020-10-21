// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Actors.SharedObjects;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Actors.SystematicTesting.Tests.SharedObjects
{
    public class SharedCounterTests : BaseActorSystematicTest
    {
        public SharedCounterTests(ITestOutputHelper output)
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
            public SharedCounter Counter;

            public E(SharedCounter counter)
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
                this.Assert(v == 1, "Reached test assertion.");
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

        [Fact(Timeout = 5000)]
        public void TestSharedCounter1()
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

            private void InitOnEntry(Event e)
            {
                var flag = (e as SetupEvent).Flag;

                var counter = SharedCounter.Create(this.Id.Runtime, 0);
                this.CreateActor(typeof(N2), new E(counter));

                int v1 = counter.CompareExchange(10, 0); // if 0 then 10
                int v2 = counter.GetValue();

                if (flag == 0)
                {
                    this.Assert((v1 == 5 && v2 == 5) || (v1 == 0 && v2 == 10) ||
                        (v1 == 0 && v2 == 15));
                }
                else if (flag == 1)
                {
                    this.Assert((v1 == 0 && v2 == 10) || (v1 == 0 && v2 == 15),
                        "Reached test assertion.");
                }
                else if (flag == 2)
                {
                    this.Assert((v1 == 5 && v2 == 5) || (v1 == 0 && v2 == 15),
                        "Reached test assertion.");
                }
                else if (flag == 3)
                {
                    this.Assert((v1 == 5 && v2 == 5) || (v1 == 0 && v2 == 10),
                        "Reached test assertion.");
                }
            }
        }

        private class N2 : StateMachine
        {
            private SharedCounter Counter;

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

        [Fact(Timeout = 5000)]
        public void TestSharedCounter2()
        {
            this.Test(r =>
            {
                r.CreateActor(typeof(M2), new SetupEvent(0));
            },
            configuration: Configuration.Create().WithTestingIterations(50));
        }

        [Fact(Timeout = 5000)]
        public void TestSharedCounter3()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M2), new SetupEvent(1));
            },
            configuration: Configuration.Create().WithTestingIterations(100),
            expectedError: "Reached test assertion.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestSharedCounter4()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M2), new SetupEvent(2));
            },
            configuration: Configuration.Create().WithTestingIterations(100),
            expectedError: "Reached test assertion.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestSharedCounter5()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M2), new SetupEvent(3));
            },
            configuration: Configuration.Create().WithTestingIterations(100),
            expectedError: "Reached test assertion.",
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
            private SharedCounter Counter;

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

        [Fact(Timeout = 5000)]
        public void TestSharedCounter6()
        {
            this.Test(r =>
            {
                r.CreateActor(typeof(M3));
            },
            configuration: Configuration.Create().WithTestingIterations(50));
        }
    }
}
