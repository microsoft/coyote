// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Actors;

namespace Microsoft.Coyote.SharedObjects
{
    /// <summary>
    /// A shared counter modeled using an actor for testing.
    /// </summary>
    [OnEventDoAction(typeof(SharedCounterEvent), nameof(ProcessEvent))]
    internal sealed class SharedCounterActor : Actor
    {
        /// <summary>
        /// The value of the shared counter.
        /// </summary>
        private int Counter;

        /// <summary>
        /// Initializes the actor.
        /// </summary>
        protected override Task OnInitializeAsync(Event initialEvent)
        {
            this.Counter = 0;
            return Task.CompletedTask;
        }

        /// <summary>
        /// Processes the next dequeued event.
        /// </summary>
        private void ProcessEvent()
        {
            var e = this.ReceivedEvent as SharedCounterEvent;
            switch (e.Operation)
            {
                case SharedCounterEvent.OperationType.Set:
                    this.SendEvent(e.Sender, new SharedCounterResponseEvent(this.Counter));
                    this.Counter = e.Value;
                    break;

                case SharedCounterEvent.OperationType.Get:
                    this.SendEvent(e.Sender, new SharedCounterResponseEvent(this.Counter));
                    break;

                case SharedCounterEvent.OperationType.Increment:
                    this.Counter++;
                    break;

                case SharedCounterEvent.OperationType.Decrement:
                    this.Counter--;
                    break;

                case SharedCounterEvent.OperationType.Add:
                    this.Counter += e.Value;
                    this.SendEvent(e.Sender, new SharedCounterResponseEvent(this.Counter));
                    break;

                case SharedCounterEvent.OperationType.CompareExchange:
                    this.SendEvent(e.Sender, new SharedCounterResponseEvent(this.Counter));
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
