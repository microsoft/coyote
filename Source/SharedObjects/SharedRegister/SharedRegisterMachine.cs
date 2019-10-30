// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Coyote.Machines;

namespace Microsoft.Coyote.SharedObjects
{
    /// <summary>
    /// A shared register modeled using a state-machine for testing.
    /// </summary>
    internal sealed class SharedRegisterMachine<T> : StateMachine
        where T : struct
    {
        /// <summary>
        /// The value of the shared register.
        /// </summary>
        private T Value;

        /// <summary>
        /// The start state of this machine.
        /// </summary>
        [Start]
        [OnEntry(nameof(Initialize))]
        [OnEventDoAction(typeof(SharedRegisterEvent), nameof(ProcessEvent))]
        private class Init : MachineState
        {
        }

        /// <summary>
        /// Initializes the machine.
        /// </summary>
        private void Initialize()
        {
            this.Value = default;
        }

        /// <summary>
        /// Processes the next dequeued event.
        /// </summary>
        private void ProcessEvent()
        {
            var e = this.ReceivedEvent as SharedRegisterEvent;
            switch (e.Operation)
            {
                case SharedRegisterEvent.SharedRegisterOperation.SET:
                    this.Value = (T)e.Value;
                    break;

                case SharedRegisterEvent.SharedRegisterOperation.GET:
                    this.Send(e.Sender, new SharedRegisterResponseEvent<T>(this.Value));
                    break;

                case SharedRegisterEvent.SharedRegisterOperation.UPDATE:
                    var func = (Func<T, T>)e.Func;
                    this.Value = func(this.Value);
                    this.Send(e.Sender, new SharedRegisterResponseEvent<T>(this.Value));
                    break;
            }
        }
    }
}
