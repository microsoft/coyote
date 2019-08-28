// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.SharedObjects.Tests
{
    public class MockSharedCounterTest : BaseTest
    {
        public MockSharedCounterTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class Config : Event
        {
            public int Flag;

            public Config(int flag)
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

        private class M1 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                var counter = SharedCounter.Create(this.Id.Runtime, 0);
                this.CreateMachine(typeof(N1), new E(counter));

                counter.Increment();
                var v = counter.GetValue();
                this.Assert(v == 1);
            }
        }

        private class N1 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                var counter = (this.ReceivedEvent as E).Counter;
                counter.Decrement();
            }
        }

        [Fact(Timeout=5000)]
        public void TestMockSharedCounter1()
        {
            var config = Configuration.Create().WithNumberOfIterations(50);
            var test = new Action<ICoyoteRuntime>((r) =>
            {
                r.CreateMachine(typeof(M1));
            });

            this.AssertFailed(config, test, "Detected an assertion failure.");
        }

        private class M2 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                var flag = (this.ReceivedEvent as Config).Flag;

                var counter = SharedCounter.Create(this.Id.Runtime, 0);
                var n = this.CreateMachine(typeof(N2), new E(counter));

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

        private class N2 : Machine
        {
            private ISharedCounter Counter;

            [Start]
            [OnEventDoAction(typeof(Done), nameof(Check))]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Counter = (this.ReceivedEvent as E).Counter;
                this.Counter.Add(5);
            }

            private void Check()
            {
                var v = this.Counter.GetValue();
                this.Assert(v == 0);
            }
        }

        private class M3 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                var counter = SharedCounter.Create(this.Id.Runtime, 0);
                var n = this.CreateMachine(typeof(N3), new E(counter));

                counter.Add(4);
                counter.Increment();
                counter.Add(5);

                this.Send(n, new Done());
            }
        }

        private class N3 : Machine
        {
            private ISharedCounter Counter;

            [Start]
            [OnEventDoAction(typeof(Done), nameof(Check))]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Counter = (this.ReceivedEvent as E).Counter;
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
            var test = new Action<ICoyoteRuntime>((r) =>
            {
                r.CreateMachine(typeof(M2), new Config(0));
            });

            this.AssertSucceeded(config, test);
        }

        [Fact(Timeout=5000)]
        public void TestMockSharedCounter3()
        {
            var config = Configuration.Create().WithNumberOfIterations(100);
            var test = new Action<ICoyoteRuntime>((r) =>
            {
                r.CreateMachine(typeof(M2), new Config(1));
            });

            this.AssertFailed(config, test, "Detected an assertion failure.");
        }

        [Fact(Timeout=5000)]
        public void TestMockSharedCounter4()
        {
            var config = Configuration.Create().WithNumberOfIterations(100);
            var test = new Action<ICoyoteRuntime>((r) =>
            {
                r.CreateMachine(typeof(M2), new Config(2));
            });

            this.AssertFailed(config, test, "Detected an assertion failure.");
        }

        [Fact(Timeout=5000)]
        public void TestMockSharedCounter5()
        {
            var config = Configuration.Create().WithNumberOfIterations(100);
            var test = new Action<ICoyoteRuntime>((r) =>
            {
                r.CreateMachine(typeof(M2), new Config(3));
            });

            this.AssertFailed(config, test, "Detected an assertion failure.");
        }

        [Fact(Timeout=5000)]
        public void TestMockSharedCounter6()
        {
            var config = Configuration.Create().WithNumberOfIterations(50);
            var test = new Action<ICoyoteRuntime>((r) =>
            {
                r.CreateMachine(typeof(M3));
            });

            this.AssertSucceeded(config, test);
        }
    }
}
