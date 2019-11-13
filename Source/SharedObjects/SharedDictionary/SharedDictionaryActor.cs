// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Coyote.Actors;

namespace Microsoft.Coyote.SharedObjects
{
    /// <summary>
    /// A shared dictionary modeled using an actor for testing.
    /// </summary>
    [OnEventDoAction(typeof(SharedDictionaryEvent), nameof(ProcessEvent))]
    internal sealed class SharedDictionaryActor<TKey, TValue> : Actor
    {
        /// <summary>
        /// The internal shared dictionary.
        /// </summary>
        private Dictionary<TKey, TValue> Dictionary;

        /// <summary>
        /// Initializes the actor.
        /// </summary>
        protected override Task OnInitializeAsync(Event initialEvent)
        {
            if (initialEvent is SharedDictionaryEvent e)
            {
                if (e.Operation == SharedDictionaryEvent.OperationType.Initialize && e.Comparer != null)
                {
                    this.Dictionary = new Dictionary<TKey, TValue>(e.Comparer as IEqualityComparer<TKey>);
                }
                else
                {
                    throw new ArgumentException("Incorrect arguments provided to SharedDictionary.");
                }
            }
            else
            {
                this.Dictionary = new Dictionary<TKey, TValue>();
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Processes the next dequeued event.
        /// </summary>
        private void ProcessEvent()
        {
            var e = this.ReceivedEvent as SharedDictionaryEvent;
            switch (e.Operation)
            {
                case SharedDictionaryEvent.OperationType.TryAdd:
                    if (this.Dictionary.ContainsKey((TKey)e.Key))
                    {
                        this.SendEvent(e.Sender, new SharedDictionaryResponseEvent<bool>(false));
                    }
                    else
                    {
                        this.Dictionary[(TKey)e.Key] = (TValue)e.Value;
                        this.SendEvent(e.Sender, new SharedDictionaryResponseEvent<bool>(true));
                    }

                    break;

                case SharedDictionaryEvent.OperationType.TryUpdate:
                    if (!this.Dictionary.ContainsKey((TKey)e.Key))
                    {
                        this.SendEvent(e.Sender, new SharedDictionaryResponseEvent<bool>(false));
                    }
                    else
                    {
                        var currentValue = this.Dictionary[(TKey)e.Key];
                        if (currentValue.Equals((TValue)e.ComparisonValue))
                        {
                            this.Dictionary[(TKey)e.Key] = (TValue)e.Value;
                            this.SendEvent(e.Sender, new SharedDictionaryResponseEvent<bool>(true));
                        }
                        else
                        {
                            this.SendEvent(e.Sender, new SharedDictionaryResponseEvent<bool>(false));
                        }
                    }

                    break;

                case SharedDictionaryEvent.OperationType.TryGet:
                    if (!this.Dictionary.ContainsKey((TKey)e.Key))
                    {
                        this.SendEvent(e.Sender, new SharedDictionaryResponseEvent<Tuple<bool, TValue>>(Tuple.Create(false, default(TValue))));
                    }
                    else
                    {
                        this.SendEvent(e.Sender, new SharedDictionaryResponseEvent<Tuple<bool, TValue>>(Tuple.Create(true, this.Dictionary[(TKey)e.Key])));
                    }

                    break;

                case SharedDictionaryEvent.OperationType.Get:
                    this.SendEvent(e.Sender, new SharedDictionaryResponseEvent<TValue>(this.Dictionary[(TKey)e.Key]));
                    break;

                case SharedDictionaryEvent.OperationType.Set:
                    this.Dictionary[(TKey)e.Key] = (TValue)e.Value;
                    break;

                case SharedDictionaryEvent.OperationType.Count:
                    this.SendEvent(e.Sender, new SharedDictionaryResponseEvent<int>(this.Dictionary.Count));
                    break;

                case SharedDictionaryEvent.OperationType.TryRemove:
                    if (this.Dictionary.ContainsKey((TKey)e.Key))
                    {
                        var value = this.Dictionary[(TKey)e.Key];
                        this.Dictionary.Remove((TKey)e.Key);
                        this.SendEvent(e.Sender, new SharedDictionaryResponseEvent<Tuple<bool, TValue>>(Tuple.Create(true, value)));
                    }
                    else
                    {
                        this.SendEvent(e.Sender, new SharedDictionaryResponseEvent<Tuple<bool, TValue>>(Tuple.Create(false, default(TValue))));
                    }

                    break;
            }
        }
    }
}
