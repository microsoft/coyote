// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using Microsoft.Coyote.Machines;

namespace Microsoft.Coyote.Core.Tests.LogMessages
{
    internal class Configure : Event
    {
        public TaskCompletionSource<bool> Tcs;

        public Configure(TaskCompletionSource<bool> tcs)
        {
            this.Tcs = tcs;
        }
    }

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
        private TaskCompletionSource<bool> Tcs;

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        [OnEventDoAction(typeof(E), nameof(Act))]
        private class Init : MachineState
        {
        }

        private async Task InitOnEntry()
        {
            this.Tcs = (this.ReceivedEvent as Configure).Tcs;
            var nTcs = new TaskCompletionSource<bool>();
            var n = this.CreateMachine(typeof(N), new Configure(nTcs));
            await nTcs.Task;
            this.Send(n, new E(this.Id));
        }

        private void Act()
        {
            this.Tcs.SetResult(true);
        }
    }

    internal class N : Machine
    {
        [Start]
        [OnEntry(nameof(InitOnEntry))]
        [OnEventDoAction(typeof(E), nameof(Act))]
        private class Init : MachineState
        {
        }

        private void InitOnEntry()
        {
            var tcs = (this.ReceivedEvent as Configure).Tcs;
            tcs.SetResult(true);
        }

        private void Act()
        {
            MachineId m = (this.ReceivedEvent as E).Id;
            this.Send(m, new E(this.Id));
        }
    }
}
