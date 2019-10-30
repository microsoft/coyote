// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Actors;

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

    internal class N : StateMachine
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
            ActorId m = (this.ReceivedEvent as E).Id;
            this.Send(m, new E(this.Id));
        }
    }
}
