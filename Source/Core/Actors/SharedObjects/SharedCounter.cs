// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading;
using Microsoft.Coyote.Actors.Mocks;
using Microsoft.Coyote.SystematicTesting;

namespace Microsoft.Coyote.Actors.SharedObjects
{
    /// <summary>
    /// A thread-safe counter that can be shared in-memory by actors.
    /// </summary>
    /// <remarks>
    /// See also <see href="/coyote/learn/programming-models/actors/sharing-objects">Sharing Objects</see>.
    /// </remarks>
    public class SharedCounter
    {
        /// <summary>
        /// The value of the shared counter.
        /// </summary>
        private volatile int Counter;

        /// <summary>
        /// Initializes a new instance of the <see cref="SharedCounter"/> class.
        /// </summary>
        private SharedCounter(int value)
        {
            this.Counter = value;
        }

        /// <summary>
        /// Creates a new shared counter.
        /// </summary>
        /// <param name="runtime">The actor runtime.</param>
        /// <param name="value">The initial value.</param>
        public static SharedCounter Create(IActorRuntime runtime, int value = 0)
        {
            if (runtime is MockActorManager actorManager)
            {
                return new Mock(value, actorManager);
            }

            return new SharedCounter(value);
        }

        /// <summary>
        /// Increments the shared counter.
        /// </summary>
        public virtual void Increment()
        {
            Interlocked.Increment(ref this.Counter);
        }

        /// <summary>
        /// Decrements the shared counter.
        /// </summary>
        public virtual void Decrement()
        {
            Interlocked.Decrement(ref this.Counter);
        }

        /// <summary>
        /// Gets the current value of the shared counter.
        /// </summary>
        public virtual int GetValue() => this.Counter;

        /// <summary>
        /// Adds a value to the counter atomically.
        /// </summary>
        public virtual int Add(int value) => Interlocked.Add(ref this.Counter, value);

        /// <summary>
        /// Sets the counter to a value atomically.
        /// </summary>
        public virtual int Exchange(int value) => Interlocked.Exchange(ref this.Counter, value);

        /// <summary>
        /// Sets the counter to a value atomically if it is equal to a given value.
        /// </summary>
        public virtual int CompareExchange(int value, int comparand) =>
            Interlocked.CompareExchange(ref this.Counter, value, comparand);

        /// <summary>
        /// Mock implementation of <see cref="SharedCounter"/> that can be controlled during systematic testing.
        /// </summary>
        private sealed class Mock : SharedCounter
        {
            // TODO: port to the new resource API or controlled locks once we integrate actors with tasks.

            /// <summary>
            /// Actor modeling the shared counter.
            /// </summary>
            private readonly ActorId CounterActor;

            /// <summary>
            /// The actor manager hosting this shared counter.
            /// </summary>
            private readonly MockActorManager Manager;

            /// <summary>
            /// Initializes a new instance of the <see cref="Mock"/> class.
            /// </summary>
            internal Mock(int value, MockActorManager manager)
                : base(value)
            {
                this.Manager = manager;
                this.CounterActor = manager.CreateActor(typeof(SharedCounterActor));
                var op = manager.Scheduler.GetExecutingOperation<ActorOperation>();
                manager.SendEvent(this.CounterActor, SharedCounterEvent.SetEvent(op.Actor.Id, value));
                op.Actor.ReceiveEventAsync(typeof(SharedCounterResponseEvent)).Wait();
            }

            /// <summary>
            /// Increments the shared counter.
            /// </summary>
            public override void Increment() =>
                this.Manager.SendEvent(this.CounterActor, SharedCounterEvent.IncrementEvent());

            /// <summary>
            /// Decrements the shared counter.
            /// </summary>
            public override void Decrement() =>
                this.Manager.SendEvent(this.CounterActor, SharedCounterEvent.DecrementEvent());

            /// <summary>
            /// Gets the current value of the shared counter.
            /// </summary>
            public override int GetValue()
            {
                var op = this.Manager.Scheduler.GetExecutingOperation<ActorOperation>();
                this.Manager.SendEvent(this.CounterActor, SharedCounterEvent.GetEvent(op.Actor.Id));
                var response = op.Actor.ReceiveEventAsync(typeof(SharedCounterResponseEvent)).Result;
                return (response as SharedCounterResponseEvent).Value;
            }

            /// <summary>
            /// Adds a value to the counter atomically.
            /// </summary>
            public override int Add(int value)
            {
                var op = this.Manager.Scheduler.GetExecutingOperation<ActorOperation>();
                this.Manager.SendEvent(this.CounterActor, SharedCounterEvent.AddEvent(op.Actor.Id, value));
                var response = op.Actor.ReceiveEventAsync(typeof(SharedCounterResponseEvent)).Result;
                return (response as SharedCounterResponseEvent).Value;
            }

            /// <summary>
            /// Sets the counter to a value atomically.
            /// </summary>
            public override int Exchange(int value)
            {
                var op = this.Manager.Scheduler.GetExecutingOperation<ActorOperation>();
                this.Manager.SendEvent(this.CounterActor, SharedCounterEvent.SetEvent(op.Actor.Id, value));
                var response = op.Actor.ReceiveEventAsync(typeof(SharedCounterResponseEvent)).Result;
                return (response as SharedCounterResponseEvent).Value;
            }

            /// <summary>
            /// Sets the counter to a value atomically if it is equal to a given value.
            /// </summary>
            public override int CompareExchange(int value, int comparand)
            {
                var op = this.Manager.Scheduler.GetExecutingOperation<ActorOperation>();
                this.Manager.SendEvent(this.CounterActor, SharedCounterEvent.CompareExchangeEvent(op.Actor.Id, value, comparand));
                var response = op.Actor.ReceiveEventAsync(typeof(SharedCounterResponseEvent)).Result;
                return (response as SharedCounterResponseEvent).Value;
            }
        }
    }
}
