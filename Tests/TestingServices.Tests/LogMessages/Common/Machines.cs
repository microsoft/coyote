// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Machines;

namespace Microsoft.Coyote.TestingServices.Tests.LogMessages
{
    internal class E : Event
    {
        public ActorId Id;

        public E(ActorId id)
        {
            this.Id = id;
        }
    }

    internal class Unit : Event
    {
    }

    internal class M : StateMachine
    {
        [Start]
        [OnEntry(nameof(InitOnEntry))]
        [OnEventDoAction(typeof(E), nameof(Act))]
        private class Init : MachineState
        {
        }

        private void InitOnEntry()
        {
            var n = this.CreateMachine(typeof(N));
            this.Send(n, new E(this.Id));
        }

        private void Act()
        {
            this.Assert(false, "Bug found!");
        }
    }

    internal class N : StateMachine
    {
        [Start]
        [OnEventDoAction(typeof(E), nameof(Act))]
        private class Init : MachineState
        {
        }

        private void Act()
        {
            ActorId m = (this.ReceivedEvent as E).Id;
            this.Send(m, new E(this.Id));
        }
    }
}
