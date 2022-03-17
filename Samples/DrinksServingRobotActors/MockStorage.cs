// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Specifications;

namespace Microsoft.Coyote.Samples.DrinksServingRobot
{
    /// <summary>
    /// A read operation.
    /// </summary>
    internal class ReadKeyEvent : Event
    {
        public readonly ActorId RequestorId;
        public readonly string Key;

        public ReadKeyEvent(ActorId requestorId, string key)
        {
            this.RequestorId = requestorId;
            this.Key = key;
        }
    }

    /// <summary>
    /// A write operation.
    /// </summary>
    internal class KeyValueEvent : Event
    {
        public readonly ActorId RequestorId;
        public readonly string Key;
        public readonly object Value;

        public KeyValueEvent(ActorId requestorId, string key, object value)
        {
            this.RequestorId = requestorId;
            this.Key = key;
            this.Value = value;
        }
    }

    /// <summary>
    /// Write operations are followed by a confirmation.
    /// </summary>
    internal class ConfirmedEvent : Event
    {
        public readonly string Key;
        public readonly object Value;
        public readonly bool Existing;

        public ConfirmedEvent(string key, object value, bool result)
        {
            this.Key = key;
            this.Value = value;
            this.Existing = result;
        }
    }

    internal class DeleteKeyEvent : Event
    {
        public readonly ActorId RequestorId;
        public readonly string Key;

        public DeleteKeyEvent(ActorId requestorId, string key)
        {
            this.RequestorId = requestorId;
            this.Key = key;
        }
    }

    /// <summary>
    /// MockStorage is a Coyote Actor that models the asynchronous nature of a typical
    /// cloud based storage service.  It supports simple read, write, delete operations
    /// where write operations are confirmed with a ConfirmedEvent, which is an Actor
    /// model of pseudo-transactional storage.
    /// </summary>
    [OnEventDoAction(typeof(ReadKeyEvent), nameof(ReadKey))]
    [OnEventDoAction(typeof(KeyValueEvent), nameof(WriteKey))]
    [OnEventDoAction(typeof(DeleteKeyEvent), nameof(DeleteKey))]
    internal class MockStorage : Actor
    {
        private readonly Dictionary<string, object> KeyValueStore = new Dictionary<string, object>();

        private void ReadKey(Event e)
        {
            if (e is ReadKeyEvent rke)
            {
                var requestorId = rke.RequestorId;
                var key = rke.Key;

                ValidateArguments(requestorId, key, nameof(ReadKeyEvent));

                var keyExists = this.KeyValueStore.TryGetValue(key, out object value);

                this.SendEvent(requestorId, new KeyValueEvent(requestorId, key, value));
            }
        }

        private void WriteKey(Event e)
        {
            if (e is KeyValueEvent kve)
            {
                var requestorId = kve.RequestorId;
                var key = kve.Key;
                ValidateArguments(requestorId, key, nameof(KeyValueEvent));

                bool existing = this.KeyValueStore.ContainsKey(key);
                this.KeyValueStore[key] = kve.Value;

                // send back a confirmation, this is like the commit of a transaction in the storage layer.
                this.SendEvent(requestorId, new ConfirmedEvent(key, kve.Value, existing));
            }
        }

        private void DeleteKey(Event e)
        {
            if (e is DeleteKeyEvent dke)
            {
                var requestorId = dke.RequestorId;
                var key = dke.Key;
                ValidateArguments(requestorId, key, nameof(DeleteKeyEvent));

                this.KeyValueStore.Remove(key);
            }
        }

        private static void ValidateArguments(ActorId requestorId, string key, string eventName)
        {
            Specification.Assert(requestorId != null, $"Error: The RequestorId in the {eventName} received by MockStorage is null");
            Specification.Assert(key != null, $"Error: The Key in the {eventName} received by MockStorage is null");
        }
    }
}
