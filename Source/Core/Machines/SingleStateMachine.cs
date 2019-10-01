// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;

namespace Microsoft.Coyote.Machines
{
    /// <summary>
    /// Abstract class representing a single-state machine.
    /// </summary>
    public abstract class SingleStateMachine : Machine
    {
        [Start]
        [OnEntry(nameof(HandleInitOnEntry))]
        [OnEventGotoState(typeof(Halt), typeof(Terminating))]
        [OnEventDoAction(typeof(WildCardEvent), nameof(HandleProcessEvent))]
        private sealed class Init : MachineState
        {
        }

        [OnEntry(nameof(TerminatingOnEntry))]
        private sealed class Terminating : MachineState
        {
        }

        /// <summary>
        /// Initilizes the state machine on creation.
        /// </summary>
        private async Task HandleInitOnEntry()
        {
            await this.InitOnEntry(this.ReceivedEvent);
        }

        /// <summary>
        /// Initilizes the state machine on creation.
        /// </summary>
        /// <param name="e">Initial event provided on machine creation, or null otherwise.</param>
        protected virtual Task InitOnEntry(Event e) => Task.CompletedTask;

        /// <summary>
        /// Process incoming event.
        /// </summary>
        private async Task HandleProcessEvent()
        {
            await this.ProcessEvent(this.ReceivedEvent);
        }

        /// <summary>
        /// Process incoming event.
        /// </summary>
        /// <param name="e">Event.</param>
        protected abstract Task ProcessEvent(Event e);

        /// <summary>
        /// Halts the machine.
        /// </summary>
        private void TerminatingOnEntry()
        {
            this.Raise(new Halt());
        }
    }
}
