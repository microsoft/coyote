// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Actors.SystematicTesting.Tests.Runtime
{
    public class ReceivingExternalEventTests : BaseSystematicActorTest
    {
        public ReceivingExternalEventTests(ITestOutputHelper output)
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
            public static void Send(IActorRuntime runtime, ActorId target)
            {
                runtime.SendEvent(target, new E(2));
            }
        }

        private class M : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E), nameof(HandleEvent))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                Engine.Send(this.Context, this.Id);
            }

            private void HandleEvent(Event e)
            {
                this.Assert((e as E).Value == 2);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestReceivingExternalEvents()
        {
            this.Test(r =>
            {
                r.CreateActor(typeof(M));
            });
        }
    }
}
