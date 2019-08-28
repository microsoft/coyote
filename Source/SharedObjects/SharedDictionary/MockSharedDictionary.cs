// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

using Microsoft.Coyote.TestingServices.Runtime;

namespace Microsoft.Coyote.SharedObjects
{
    /// <summary>
    /// A wrapper for a shared dictionary modeled using a state-machine for testing.
    /// </summary>
    internal sealed class MockSharedDictionary<TKey, TValue> : ISharedDictionary<TKey, TValue>
    {
        /// <summary>
        /// Machine modeling the shared dictionary.
        /// </summary>
        private readonly MachineId DictionaryMachine;

        /// <summary>
        /// The testing runtime hosting this shared dictionary.
        /// </summary>
        private readonly SystematicTestingRuntime Runtime;

        /// <summary>
        /// Initializes a new instance of the <see cref="MockSharedDictionary{TKey, TValue}"/> class.
        /// </summary>
        public MockSharedDictionary(IEqualityComparer<TKey> comparer, SystematicTestingRuntime runtime)
        {
            this.Runtime = runtime;
            if (comparer != null)
            {
                this.DictionaryMachine = this.Runtime.CreateMachine(
                    typeof(SharedDictionaryMachine<TKey, TValue>),
                    SharedDictionaryEvent.InitEvent(comparer));
            }
            else
            {
                this.DictionaryMachine = this.Runtime.CreateMachine(typeof(SharedDictionaryMachine<TKey, TValue>));
            }
        }

        /// <summary>
        /// Adds a new key to the dictionary, if it doesn’t already exist in the dictionary.
        /// </summary>
        public bool TryAdd(TKey key, TValue value)
        {
            var currentMachine = this.Runtime.GetExecutingMachine<Machine>();
            this.Runtime.SendEvent(this.DictionaryMachine, SharedDictionaryEvent.TryAddEvent(key, value, currentMachine.Id));
            var e = currentMachine.Receive(typeof(SharedDictionaryResponseEvent<bool>)).Result as SharedDictionaryResponseEvent<bool>;
            return e.Value;
        }

        /// <summary>
        /// Updates the value for an existing key in the dictionary, if that key has a specific value.
        /// </summary>
        public bool TryUpdate(TKey key, TValue newValue, TValue comparisonValue)
        {
            var currentMachine = this.Runtime.GetExecutingMachine<Machine>();
            this.Runtime.SendEvent(this.DictionaryMachine, SharedDictionaryEvent.TryUpdateEvent(key, newValue, comparisonValue, currentMachine.Id));
            var e = currentMachine.Receive(typeof(SharedDictionaryResponseEvent<bool>)).Result as SharedDictionaryResponseEvent<bool>;
            return e.Value;
        }

        /// <summary>
        /// Attempts to get the value associated with the specified key.
        /// </summary>
        public bool TryGetValue(TKey key, out TValue value)
        {
            var currentMachine = this.Runtime.GetExecutingMachine<Machine>();
            this.Runtime.SendEvent(this.DictionaryMachine, SharedDictionaryEvent.TryGetEvent(key, currentMachine.Id));
            var e = currentMachine.Receive(typeof(SharedDictionaryResponseEvent<Tuple<bool, TValue>>)).Result
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
                var currentMachine = this.Runtime.GetExecutingMachine<Machine>();
                this.Runtime.SendEvent(this.DictionaryMachine, SharedDictionaryEvent.GetEvent(key, currentMachine.Id));
                var e = currentMachine.Receive(typeof(SharedDictionaryResponseEvent<TValue>)).Result as SharedDictionaryResponseEvent<TValue>;
                return e.Value;
            }

            set
            {
                this.Runtime.SendEvent(this.DictionaryMachine, SharedDictionaryEvent.SetEvent(key, value));
            }
        }

        /// <summary>
        /// Removes the specified key from the dictionary.
        /// </summary>
        public bool TryRemove(TKey key, out TValue value)
        {
            var currentMachine = this.Runtime.GetExecutingMachine<Machine>();
            this.Runtime.SendEvent(this.DictionaryMachine, SharedDictionaryEvent.TryRemoveEvent(key, currentMachine.Id));
            var e = currentMachine.Receive(typeof(SharedDictionaryResponseEvent<Tuple<bool, TValue>>)).Result
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
                var currentMachine = this.Runtime.GetExecutingMachine<Machine>();
                this.Runtime.SendEvent(this.DictionaryMachine, SharedDictionaryEvent.CountEvent(currentMachine.Id));
                var e = currentMachine.Receive(typeof(SharedDictionaryResponseEvent<int>)).Result as SharedDictionaryResponseEvent<int>;
                return e.Value;
            }
        }
    }
}
