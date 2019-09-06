// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests
{
    public class MustHandleEventTest : BaseTest
    {
        public MustHandleEventTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class E : Event
        {
            public MachineId Id;

            public E()
            {
            }

            public E(MachineId id)
            {
                this.Id = id;
            }
        }

        private class E1 : Event
        {
        }

        private class M1 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [IgnoreEvents(typeof(E))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Raise(new Halt());
            }
        }

        private class M2 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [IgnoreEvents(typeof(E))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Send(this.Id, new Halt());
                this.Send(this.Id, new Halt());
            }
        }

        private class M3 : Machine
        {
            [Start]
            [IgnoreEvents(typeof(E))]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
            }
        }

        private class M4 : Machine
        {
            [Start]
            [DeferEvents(typeof(E))]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
            }
        }

        private class M5 : Machine
        {
            [Start]
            [DeferEvents(typeof(E), typeof(Halt))]
            [OnEventGotoState(typeof(E1), typeof(Next))]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private class Next : MachineState
            {
            }

            private void InitOnEntry()
            {
            }
        }

        [Fact(Timeout=5000)]
        public void TestMustHandleFail1()
        {
            this.TestWithError(r =>
            {
                var m = r.CreateMachine(typeof(M1));
                r.SendEvent(m, new E(), options: new SendOptions(mustHandle: true));
            },
            configuration: Configuration.Create().WithNumberOfIterations(500),
            expectedErrors: new string[]
                {
                    string.Empty, // TODO: sometimes this test doesn't fail! (workitem #675)
                    "A must-handle event 'E' was sent to the halted machine 'M1()'.",
                    "Machine 'M1()' halted before dequeueing must-handle event 'E'."
                },
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestMustHandleFail2()
        {
            this.TestWithError(r =>
            {
                var m = r.CreateMachine(typeof(M2));
                r.SendEvent(m, new E());
                r.SendEvent(m, new E(), options: new SendOptions(mustHandle: true));
            },
            configuration: Configuration.Create().WithNumberOfIterations(500),
            expectedErrors: new string[]
                {
                    "A must-handle event 'E' was sent to the halted machine 'M2()'.",
                    "Machine 'M2()' halted before dequeueing must-handle event 'E'."
                },
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestMustHandleFail3()
        {
            this.TestWithError(r =>
            {
                var m = r.CreateMachine(typeof(M5));
                r.SendEvent(m, new Halt());
                r.SendEvent(m, new E(), options: new SendOptions(mustHandle: true));
                r.SendEvent(m, new E1());
            },
            configuration: Configuration.Create().WithNumberOfIterations(1),
            expectedError: "Machine 'M5()' halted before dequeueing must-handle event 'E'.",
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestMustHandleSuccess()
        {
            this.Test(r =>
            {
                var m = r.CreateMachine(typeof(M3));
                r.SendEvent(m, new E(), options: new SendOptions(mustHandle: true));
                r.SendEvent(m, new Halt());
            },
            configuration: Configuration.Create().WithNumberOfIterations(100));
        }

        [Fact(Timeout=5000)]
        public void TestMustHandleDeferFail()
        {
            this.TestWithError(r =>
            {
                var m = r.CreateMachine(typeof(M4));
                r.SendEvent(m, new E(), options: new SendOptions(mustHandle: true));
                r.SendEvent(m, new Halt());
            },
            configuration: Configuration.Create().WithNumberOfIterations(1),
            expectedError: "Machine 'M4()' halted before dequeueing must-handle event 'E'.",
            replay: true);
        }
    }
}
