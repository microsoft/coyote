// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.Coyote.Machines;
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

        private class MustHandleEvent : Event
        {
            public MachineId Id;

            public MustHandleEvent()
            {
            }

            public MustHandleEvent(MachineId id)
            {
                this.Id = id;
            }
        }

        private class MoveEvent : Event
        {
        }

        private class M1 : Machine
        {
            [Start]
            [IgnoreEvents(typeof(MustHandleEvent))]
            private class Init : MachineState
            {
            }
        }

        [Fact(Timeout = 5000)]
        public void TestMustHandleEventNotTriggered()
        {
            this.Test(r =>
            {
                var m = r.CreateMachine(typeof(M1));
                r.SendEvent(m, new MustHandleEvent(), options: new SendOptions(mustHandle: true));
                r.SendEvent(m, new Halt());
            },
            configuration: Configuration.Create().WithNumberOfIterations(100));
        }

        private class M2 : Machine
        {
            [Start]
            [DeferEvents(typeof(MustHandleEvent))]
            private class Init : MachineState
            {
            }
        }

        [Fact(Timeout = 5000)]
        public void TestMustHandleDeferredEvent()
        {
            this.TestWithError(r =>
            {
                var m = r.CreateMachine(typeof(M2));
                r.SendEvent(m, new MustHandleEvent(), options: new SendOptions(mustHandle: true));
                r.SendEvent(m, new Halt());
            },
            configuration: Configuration.Create().WithNumberOfIterations(1),
            expectedError: "Machine 'M2()' halted before dequeueing must-handle event 'MustHandleEvent'.",
            replay: true);
        }

        private class M3 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [DeferEvents(typeof(MustHandleEvent))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Raise(new Halt());
            }
        }

        [Fact(Timeout = 5000)]
        public void TestMustHandleEventAfterRaisingHalt()
        {
            this.TestWithError(r =>
            {
                var m = r.CreateMachine(typeof(M3));
                r.SendEvent(m, new MustHandleEvent(), options: new SendOptions(mustHandle: true));
            },
            configuration: Configuration.Create().WithNumberOfIterations(500),
            expectedErrors: new string[]
                {
                    "A must-handle event 'MustHandleEvent' was sent to the halted machine 'M3()'.",
                    "Machine 'M3()' halted before dequeueing must-handle event 'MustHandleEvent'."
                },
            replay: true);
        }

        private class M4 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [DeferEvents(typeof(MustHandleEvent))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Send(this.Id, new Halt());
            }
        }

        [Fact(Timeout = 5000)]
        public void TestMustHandleEventAfterSendingHalt()
        {
            this.TestWithError(r =>
            {
                var m = r.CreateMachine(typeof(M4));
                r.SendEvent(m, new MustHandleEvent(), options: new SendOptions(mustHandle: true));
            },
            configuration: Configuration.Create().WithNumberOfIterations(500),
            expectedErrors: new string[]
                {
                    "A must-handle event 'MustHandleEvent' was sent to the halted machine 'M4()'.",
                    "Machine 'M4()' halted before dequeueing must-handle event 'MustHandleEvent'."
                },
            replay: true);
        }

        private class M5 : Machine
        {
            [Start]
            [DeferEvents(typeof(MustHandleEvent), typeof(Halt))]
            [OnEventGotoState(typeof(MoveEvent), typeof(Next))]
            private class Init : MachineState
            {
            }

            private class Next : MachineState
            {
            }
        }

        [Fact(Timeout = 5000)]
        public void TestMustHandleDeferredEventAfterStateTransition()
        {
            this.TestWithError(r =>
            {
                var m = r.CreateMachine(typeof(M5));
                r.SendEvent(m, new Halt());
                r.SendEvent(m, new MustHandleEvent(), options: new SendOptions(mustHandle: true));
                r.SendEvent(m, new MoveEvent());
            },
            configuration: Configuration.Create().WithNumberOfIterations(1),
            expectedError: "Machine 'M5()' halted before dequeueing must-handle event 'MustHandleEvent'.",
            replay: true);
        }
    }
}
