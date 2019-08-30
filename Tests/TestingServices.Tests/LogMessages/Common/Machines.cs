// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

namespace Microsoft.Coyote.TestingServices.Tests.LogMessages
{
    internal class E : Event
    {
        public MachineId Id;

        public E(MachineId id)
        {
            this.Id = id;
        }
    }

    internal class Unit : Event
    {
    }

    internal class M : Machine
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

    internal class N : Machine
    {
        [Start]
        [OnEventDoAction(typeof(E), nameof(Act))]
        private class Init : MachineState
        {
        }

        private void Act()
        {
            MachineId m = (this.ReceivedEvent as E).Id;
            this.Send(m, new E(this.Id));
        }
    }
}
