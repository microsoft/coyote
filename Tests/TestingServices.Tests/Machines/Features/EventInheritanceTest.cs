// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Runtime;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests
{
    public sealed class MultiPayloadMultiLevelTester
    {
        internal class E10 : Event
        {
            public short A10;
            public ushort B10;

            public E10(short a10, ushort b10)
                : base()
            {
                Assert.True(a10 == 1);
                Assert.True(b10 == 2);
                this.A10 = a10;
                this.B10 = b10;
            }
        }

        internal class E1 : E10
        {
            public byte A1;
            public bool B1;

            public E1(short a10, ushort b10, byte a1, bool b1)
                : base(a10, b10)
            {
                Assert.True(a1 == 30);
                Assert.True(b1 == true);
                this.A1 = a1;
                this.B1 = b1;
            }
        }

        internal class E2 : E1
        {
            public int A2;
            public uint B2;

            public E2(short a10, ushort b10, byte a1, bool b1, int a2, uint b2)
                : base(a10, b10, a1, b1)
            {
                Assert.True(a2 == 100);
                Assert.True(b2 == 101);
                this.A2 = a2;
                this.B2 = b2;
            }
        }

        public static void Test()
        {
            Assert.True(new E2(1, 2, 30, true, 100, 101) is E1);
        }
    }

    public sealed class MultiPayloadMultiLevelGenericTester
    {
        internal class E10<Te10> : Event
        {
            public short A10;
            public ushort B10;

            public E10(short a10, ushort b10)
                : base()
            {
                Assert.True(a10 == 1);
                Assert.True(b10 == 2);
                this.A10 = a10;
                this.B10 = b10;
            }
        }

        internal class E1<Te10, Te1> : E10<Te10>
        {
            public byte A1;
            public bool B1;

            public E1(short a10, ushort b10, byte a1, bool b1)
                : base(a10, b10)
            {
                Assert.True(a1 == 30);
                Assert.True(b1 == true);
                this.A1 = a1;
                this.B1 = b1;
            }
        }

        internal class E2<Te2, Te1, Te10> : E1<Te10, Te1>
        {
            public int A2;
            public uint B2;

            public E2(short a10, ushort b10, byte a1, bool b1, int a2, uint b2)
                : base(a10, b10, a1, b1)
            {
                Assert.True(a2 == 100);
                Assert.True(b2 == 101);
                this.A2 = a2;
                this.B2 = b2;
            }
        }

        public static void Test()
        {
            var e2 = new E2<string, int, bool>(1, 2, 30, true, 100, 101);
            Assert.True(e2 is E1<bool, int>);
            Assert.True(e2 is E10<bool>);
        }
    }

    public class EventInheritanceTest : BaseTest
    {
        public EventInheritanceTest(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout=5000)]
        public void TestMultiPayloadMultiLevel()
        {
            MultiPayloadMultiLevelTester.Test();
        }

        [Fact(Timeout=5000)]
        public void TestMultiPayloadMultiLevelGeneric()
        {
            MultiPayloadMultiLevelGenericTester.Test();
        }

        private class A : StateMachine
        {
            internal class Configure : Event
            {
                public TaskCompletionSource<bool> TCS;

                public Configure(TaskCompletionSource<bool> tcs)
                {
                    this.TCS = tcs;
                }
            }

            public static int E1count;
            public static int E2count;
            public static int E3count;

            private TaskCompletionSource<bool> TCS;

            public class E3 : E2
            {
            }

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E1), nameof(E1_handler))]
            [OnEventDoAction(typeof(E2), nameof(E2_handler))]
            [OnEventDoAction(typeof(E3), nameof(E3_handler))]
            private class S0 : State
            {
            }

            private void InitOnEntry()
            {
                this.TCS = (this.ReceivedEvent as Configure).TCS;
            }

            private void E1_handler()
            {
                ++E1count;
                Xunit.Assert.True(this.ReceivedEvent is E1);
                this.CheckComplete();
            }

            private void E2_handler()
            {
                ++E2count;
                Xunit.Assert.True(this.ReceivedEvent is E1);
                Xunit.Assert.True(this.ReceivedEvent is E2);
                this.CheckComplete();
            }

            private void E3_handler()
            {
                ++E3count;
                Xunit.Assert.True(this.ReceivedEvent is E1);
                Xunit.Assert.True(this.ReceivedEvent is E2);
                Xunit.Assert.True(this.ReceivedEvent is E3);
                this.CheckComplete();
            }

            private void CheckComplete()
            {
                if (E1count == 1 && E2count == 1 && E3count == 1)
                {
                    this.TCS.SetResult(true);
                }
            }
        }

        private class E1 : Event
        {
        }

        private class E2 : E1
        {
        }

        [Fact(Timeout=5000)]
        public void TestEventInheritanceRun()
        {
            var tcs = new TaskCompletionSource<bool>();
            var configuration = Configuration.Create();
            var runtime = new ProductionRuntime(configuration);
            var a = runtime.CreateStateMachine(typeof(A), null, new A.Configure(tcs));
            runtime.SendEvent(a, new A.E3());
            runtime.SendEvent(a, new E1());
            runtime.SendEvent(a, new E2());
            Assert.True(tcs.Task.Wait(3000), "Test timed out");
        }
    }
}
