// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Actors.SystematicTesting.Tests
{
    public class EventInheritanceTests : BaseActorSystematicTest
    {
        public EventInheritanceTests(ITestOutputHelper output)
            : base(output)
        {
        }

        public sealed class MultiPayloadMultiLevelTester
        {
            internal class E10 : Event
            {
                public short A10;
                public ushort B10;

                public E10(short a10, ushort b10)
                    : base()
                {
                    Assert.True(a10 is 1);
                    Assert.True(b10 is 2);
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
                    Assert.True(a1 is 30);
                    Assert.True(b1 is true);
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
                    Assert.True(a2 is 100);
                    Assert.True(b2 is 101);
                    this.A2 = a2;
                    this.B2 = b2;
                }
            }

            public static void Test()
            {
                Assert.True(new E2(1, 2, 30, true, 100, 101) is E1);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestMultiPayloadMultiLevel()
        {
            MultiPayloadMultiLevelTester.Test();
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
                    Assert.True(a10 is 1);
                    Assert.True(b10 is 2);
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
                    Assert.True(a1 is 30);
                    Assert.True(b1 is true);
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
                    Assert.True(a2 is 100);
                    Assert.True(b2 is 101);
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

        [Fact(Timeout = 5000)]
        public void TestMultiPayloadMultiLevelGeneric()
        {
            MultiPayloadMultiLevelGenericTester.Test();
        }

        private class A : StateMachine
        {
            internal class SetupEvent : Event
            {
                public TaskCompletionSource<bool> TCS;

                public SetupEvent(TaskCompletionSource<bool> tcs)
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

            private void InitOnEntry(Event e)
            {
                this.TCS = (e as SetupEvent).TCS;
            }

            private void E1_handler(Event e)
            {
                ++E1count;
                Xunit.Assert.True(e is E1);
                this.CheckComplete();
            }

            private void E2_handler(Event e)
            {
                ++E2count;
                Xunit.Assert.True(e is E1);
                Xunit.Assert.True(e is E2);
                this.CheckComplete();
            }

            private void E3_handler(Event e)
            {
                ++E3count;
                Xunit.Assert.True(e is E1);
                Xunit.Assert.True(e is E2);
                Xunit.Assert.True(e is E3);
                this.CheckComplete();
            }

            private void CheckComplete()
            {
                if (E1count is 1 && E2count is 1 && E3count is 1)
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

        [Fact(Timeout = 5000)]
        public void TestEventInheritanceInStateMachine()
        {
            var tcs = new TaskCompletionSource<bool>();
            var configuration = Configuration.Create();
            var runtime = RuntimeFactory.Create(configuration);
            var a = runtime.CreateActor(typeof(A), null, new A.SetupEvent(tcs));
            runtime.SendEvent(a, new A.E3());
            runtime.SendEvent(a, new E1());
            runtime.SendEvent(a, new E2());
            Assert.True(tcs.Task.Wait(3000), "Test timed out");
        }
    }
}
