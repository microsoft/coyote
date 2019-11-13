// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Coyote.Actors;

namespace Microsoft.Coyote.SharedObjects
{
    /// <summary>
    /// A shared register modeled using an actor for testing.
    /// </summary>
    [OnEventDoAction(typeof(SharedRegisterEvent), nameof(ProcessEvent))]
    internal sealed class SharedRegisterActor<T> : Actor
        where T : struct
    {
        /// <summary>
        /// The value of the shared register.
        /// </summary>
        private T Value;

        /// <summary>
        /// Initializes the actor.
        /// </summary>
        protected override Task OnInitializeAsync(Event initialEvent)
        {
            this.Value = default;
            return Task.CompletedTask;
        }

        /// <summary>
        /// Processes the next dequeued event.
        /// </summary>
        private void ProcessEvent()
        {
            var e = this.ReceivedEvent as SharedRegisterEvent;
            switch (e.Operation)
            {
                case SharedRegisterEvent.OperationType.Set:
                    this.Value = (T)e.Value;
                    break;

                case SharedRegisterEvent.OperationType.Get:
                    this.SendEvent(e.Sender, new SharedRegisterResponseEvent<T>(this.Value));
                    break;

                case SharedRegisterEvent.OperationType.Update:
                    var func = (Func<T, T>)e.Func;
                    this.Value = func(this.Value);
                    this.SendEvent(e.Sender, new SharedRegisterResponseEvent<T>(this.Value));
                    break;
            }
        }
    }
}
