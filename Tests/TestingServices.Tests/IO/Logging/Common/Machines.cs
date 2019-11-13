// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Actors;

namespace Microsoft.Coyote.TestingServices.Tests.IO
{
    internal class E : Event
    {
        public ActorId Id;

        public E(ActorId id)
        {
            this.Id = id;
        }
    }

    internal class M : StateMachine
    {
        [Start]
        [OnEntry(nameof(InitOnEntry))]
        [OnEventDoAction(typeof(E), nameof(Act))]
        private class Init : State
        {
        }

        private void InitOnEntry()
        {
            var n = this.CreateActor(typeof(N));
            this.SendEvent(n, new E(this.Id));
        }

        private void Act()
        {
            this.Assert(false, "Reached test assertion.");
        }
    }

    internal class N : StateMachine
    {
        [Start]
        [OnEventDoAction(typeof(E), nameof(Act))]
        private class Init : State
        {
        }

        private void Act()
        {
            ActorId m = (this.ReceivedEvent as E).Id;
            this.SendEvent(m, new E(this.Id));
        }
    }
}
