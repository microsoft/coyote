// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Machines;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests
{
    public class ReceivingExternalEventTest : BaseTest
    {
        public ReceivingExternalEventTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class E : Event
        {
            public int Value;

            public E(int value)
            {
                this.Value = value;
            }
        }

        private class Engine
        {
            public static void Send(IMachineRuntime runtime, MachineId target)
            {
                runtime.SendEvent(target, new E(2));
            }
        }

        private class M : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E), nameof(HandleEvent))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                Engine.Send(this.Runtime, this.Id);
            }

            private void HandleEvent()
            {
                this.Assert((this.ReceivedEvent as E).Value == 2);
            }
        }

        [Fact(Timeout=5000)]
        public void TestReceivingExternalEvents()
        {
            this.Test(r =>
            {
                r.CreateMachine(typeof(M));
            });
        }
    }
}
