// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Runtime;

namespace Microsoft.Coyote.SharedObjects
{
    /// <summary>
    /// A wrapper for a shared counter modeled using an actor for testing.
    /// </summary>
    internal sealed class MockSharedCounter : ISharedCounter
    {
        /// <summary>
        /// Actor modeling the shared counter.
        /// </summary>
        private readonly ActorId CounterActor;

        /// <summary>
        /// The testing runtime hosting this shared counter.
        /// </summary>
        private readonly SystematicTestingRuntime Runtime;

        /// <summary>
        /// Initializes a new instance of the <see cref="MockSharedCounter"/> class.
        /// </summary>
        public MockSharedCounter(int value, SystematicTestingRuntime runtime)
        {
            this.Runtime = runtime;
            this.CounterActor = this.Runtime.CreateActor(typeof(SharedCounterActor));
            var op = this.Runtime.Scheduler.GetExecutingOperation<ActorOperation>();
            this.Runtime.SendEvent(this.CounterActor, SharedCounterEvent.SetEvent(op.Actor.Id, value));
            op.Actor.ReceiveEventAsync(typeof(SharedCounterResponseEvent)).Wait();
        }

        /// <summary>
        /// Increments the shared counter.
        /// </summary>
        public void Increment()
        {
            this.Runtime.SendEvent(this.CounterActor, SharedCounterEvent.IncrementEvent());
        }

        /// <summary>
        /// Decrements the shared counter.
        /// </summary>
        public void Decrement()
        {
            this.Runtime.SendEvent(this.CounterActor, SharedCounterEvent.DecrementEvent());
        }

        /// <summary>
        /// Gets the current value of the shared counter.
        /// </summary>
        public int GetValue()
        {
            var op = this.Runtime.Scheduler.GetExecutingOperation<ActorOperation>();
            this.Runtime.SendEvent(this.CounterActor, SharedCounterEvent.GetEvent(op.Actor.Id));
            var response = op.Actor.ReceiveEventAsync(typeof(SharedCounterResponseEvent)).Result;
            return (response as SharedCounterResponseEvent).Value;
        }

        /// <summary>
        /// Adds a value to the counter atomically.
        /// </summary>
        public int Add(int value)
        {
            var op = this.Runtime.Scheduler.GetExecutingOperation<ActorOperation>();
            this.Runtime.SendEvent(this.CounterActor, SharedCounterEvent.AddEvent(op.Actor.Id, value));
            var response = op.Actor.ReceiveEventAsync(typeof(SharedCounterResponseEvent)).Result;
            return (response as SharedCounterResponseEvent).Value;
        }

        /// <summary>
        /// Sets the counter to a value atomically.
        /// </summary>
        public int Exchange(int value)
        {
            var op = this.Runtime.Scheduler.GetExecutingOperation<ActorOperation>();
            this.Runtime.SendEvent(this.CounterActor, SharedCounterEvent.SetEvent(op.Actor.Id, value));
            var response = op.Actor.ReceiveEventAsync(typeof(SharedCounterResponseEvent)).Result;
            return (response as SharedCounterResponseEvent).Value;
        }

        /// <summary>
        /// Sets the counter to a value atomically if it is equal to a given value.
        /// </summary>
        public int CompareExchange(int value, int comparand)
        {
            var op = this.Runtime.Scheduler.GetExecutingOperation<ActorOperation>();
            this.Runtime.SendEvent(this.CounterActor, SharedCounterEvent.CompareExchangeEvent(op.Actor.Id, value, comparand));
            var response = op.Actor.ReceiveEventAsync(typeof(SharedCounterResponseEvent)).Result;
            return (response as SharedCounterResponseEvent).Value;
        }
    }
}
