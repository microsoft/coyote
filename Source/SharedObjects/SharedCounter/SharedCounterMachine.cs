// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Machines;

namespace Microsoft.Coyote.SharedObjects
{
    /// <summary>
    /// A shared counter modeled using a state-machine for testing.
    /// </summary>
    internal sealed class SharedCounterMachine : StateMachine
    {
        /// <summary>
        /// The value of the shared counter.
        /// </summary>
        private int Counter;

        /// <summary>
        /// The start state of this machine.
        /// </summary>
        [Start]
        [OnEntry(nameof(Initialize))]
        [OnEventDoAction(typeof(SharedCounterEvent), nameof(ProcessEvent))]
        private class Init : MachineState
        {
        }

        /// <summary>
        /// Initializes the machine.
        /// </summary>
        private void Initialize()
        {
            this.Counter = 0;
        }

        /// <summary>
        /// Processes the next dequeued event.
        /// </summary>
        private void ProcessEvent()
        {
            var e = this.ReceivedEvent as SharedCounterEvent;
            switch (e.Operation)
            {
                case SharedCounterEvent.SharedCounterOperation.SET:
                    this.Send(e.Sender, new SharedCounterResponseEvent(this.Counter));
                    this.Counter = e.Value;
                    break;

                case SharedCounterEvent.SharedCounterOperation.GET:
                    this.Send(e.Sender, new SharedCounterResponseEvent(this.Counter));
                    break;

                case SharedCounterEvent.SharedCounterOperation.INC:
                    this.Counter++;
                    break;

                case SharedCounterEvent.SharedCounterOperation.DEC:
                    this.Counter--;
                    break;

                case SharedCounterEvent.SharedCounterOperation.ADD:
                    this.Counter += e.Value;
                    this.Send(e.Sender, new SharedCounterResponseEvent(this.Counter));
                    break;

                case SharedCounterEvent.SharedCounterOperation.CAS:
                    this.Send(e.Sender, new SharedCounterResponseEvent(this.Counter));
                    if (this.Counter == e.Comparand)
                    {
                        this.Counter = e.Value;
                    }

                    break;

                default:
                    throw new System.ArgumentOutOfRangeException("Unsupported SharedCounter operation: " + e.Operation);
            }
        }
    }
}
