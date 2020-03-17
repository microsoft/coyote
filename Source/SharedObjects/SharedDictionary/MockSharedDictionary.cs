// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.SystematicTesting;

namespace Microsoft.Coyote.SharedObjects
{
    /// <summary>
    /// A wrapper for a shared dictionary modeled using an actor for testing.
    /// </summary>
    internal sealed class MockSharedDictionary<TKey, TValue> : ISharedDictionary<TKey, TValue>
    {
        /// <summary>
        /// Actor modeling the shared dictionary.
        /// </summary>
        private readonly ActorId DictionaryActor;

        /// <summary>
        /// The controlled runtime hosting this shared dictionary.
        /// </summary>
        private readonly ControlledRuntime Runtime;

        /// <summary>
        /// Initializes a new instance of the <see cref="MockSharedDictionary{TKey, TValue}"/> class.
        /// </summary>
        public MockSharedDictionary(IEqualityComparer<TKey> comparer, ControlledRuntime runtime)
        {
            this.Runtime = runtime;
            if (comparer != null)
            {
                this.DictionaryActor = this.Runtime.CreateActor(
                    typeof(SharedDictionaryActor<TKey, TValue>),
                    SharedDictionaryEvent.InitializeEvent(comparer));
            }
            else
            {
                this.DictionaryActor = this.Runtime.CreateActor(typeof(SharedDictionaryActor<TKey, TValue>));
            }
        }

        /// <summary>
        /// Adds a new key to the dictionary, if it doesn't already exist in the dictionary.
        /// </summary>
        public bool TryAdd(TKey key, TValue value)
        {
            var op = this.Runtime.Scheduler.GetExecutingOperation<ActorOperation>();
            this.Runtime.SendEvent(this.DictionaryActor, SharedDictionaryEvent.TryAddEvent(key, value, op.Actor.Id));
            var e = op.Actor.ReceiveEventAsync(typeof(SharedDictionaryResponseEvent<bool>)).Result as SharedDictionaryResponseEvent<bool>;
            return e.Value;
        }

        /// <summary>
        /// Updates the value for an existing key in the dictionary, if that key has a specific value.
        /// </summary>
        public bool TryUpdate(TKey key, TValue newValue, TValue comparisonValue)
        {
            var op = this.Runtime.Scheduler.GetExecutingOperation<ActorOperation>();
            this.Runtime.SendEvent(this.DictionaryActor, SharedDictionaryEvent.TryUpdateEvent(key, newValue, comparisonValue, op.Actor.Id));
            var e = op.Actor.ReceiveEventAsync(typeof(SharedDictionaryResponseEvent<bool>)).Result as SharedDictionaryResponseEvent<bool>;
            return e.Value;
        }

        /// <summary>
        /// Attempts to get the value associated with the specified key.
        /// </summary>
        public bool TryGetValue(TKey key, out TValue value)
        {
            var op = this.Runtime.Scheduler.GetExecutingOperation<ActorOperation>();
            this.Runtime.SendEvent(this.DictionaryActor, SharedDictionaryEvent.TryGetEvent(key, op.Actor.Id));
            var e = op.Actor.ReceiveEventAsync(typeof(SharedDictionaryResponseEvent<Tuple<bool, TValue>>)).Result
                as SharedDictionaryResponseEvent<Tuple<bool, TValue>>;
            value = e.Value.Item2;
            return e.Value.Item1;
        }

        /// <summary>
        /// Gets or sets the value associated with the specified key.
        /// </summary>
        public TValue this[TKey key]
        {
            get
            {
                var op = this.Runtime.Scheduler.GetExecutingOperation<ActorOperation>();
                this.Runtime.SendEvent(this.DictionaryActor, SharedDictionaryEvent.GetEvent(key, op.Actor.Id));
                var e = op.Actor.ReceiveEventAsync(typeof(SharedDictionaryResponseEvent<TValue>)).Result as SharedDictionaryResponseEvent<TValue>;
                return e.Value;
            }

            set
            {
                this.Runtime.SendEvent(this.DictionaryActor, SharedDictionaryEvent.SetEvent(key, value));
            }
        }

        /// <summary>
        /// Removes the specified key from the dictionary.
        /// </summary>
        public bool TryRemove(TKey key, out TValue value)
        {
            var op = this.Runtime.Scheduler.GetExecutingOperation<ActorOperation>();
            this.Runtime.SendEvent(this.DictionaryActor, SharedDictionaryEvent.TryRemoveEvent(key, op.Actor.Id));
            var e = op.Actor.ReceiveEventAsync(typeof(SharedDictionaryResponseEvent<Tuple<bool, TValue>>)).Result
                as SharedDictionaryResponseEvent<Tuple<bool, TValue>>;
            value = e.Value.Item2;
            return e.Value.Item1;
        }

        /// <summary>
        /// Gets the number of elements in the dictionary.
        /// </summary>
        public int Count
        {
            get
            {
                var op = this.Runtime.Scheduler.GetExecutingOperation<ActorOperation>();
                this.Runtime.SendEvent(this.DictionaryActor, SharedDictionaryEvent.CountEvent(op.Actor.Id));
                var e = op.Actor.ReceiveEventAsync(typeof(SharedDictionaryResponseEvent<int>)).Result as SharedDictionaryResponseEvent<int>;
                return e.Value;
            }
        }
    }
}
