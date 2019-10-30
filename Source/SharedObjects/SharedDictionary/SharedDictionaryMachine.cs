// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Coyote.Machines;

namespace Microsoft.Coyote.SharedObjects
{
    /// <summary>
    /// A shared dictionary modeled using a state-machine for testing.
    /// </summary>
    internal sealed class SharedDictionaryMachine<TKey, TValue> : StateMachine
    {
        /// <summary>
        /// The internal shared dictionary.
        /// </summary>
        private Dictionary<TKey, TValue> Dictionary;

        /// <summary>
        /// The start state of this machine.
        /// </summary>
        [Start]
        [OnEntry(nameof(Initialize))]
        [OnEventDoAction(typeof(SharedDictionaryEvent), nameof(ProcessEvent))]
        private class Init : MachineState
        {
        }

        /// <summary>
        /// Initializes the machine.
        /// </summary>
        private void Initialize()
        {
            if (this.ReceivedEvent is SharedDictionaryEvent e)
            {
                if (e.Operation == SharedDictionaryEvent.SharedDictionaryOperation.INIT && e.Comparer != null)
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
        }

        /// <summary>
        /// Processes the next dequeued event.
        /// </summary>
        private void ProcessEvent()
        {
            var e = this.ReceivedEvent as SharedDictionaryEvent;
            switch (e.Operation)
            {
                case SharedDictionaryEvent.SharedDictionaryOperation.TRYADD:
                    if (this.Dictionary.ContainsKey((TKey)e.Key))
                    {
                        this.Send(e.Sender, new SharedDictionaryResponseEvent<bool>(false));
                    }
                    else
                    {
                        this.Dictionary[(TKey)e.Key] = (TValue)e.Value;
                        this.Send(e.Sender, new SharedDictionaryResponseEvent<bool>(true));
                    }

                    break;

                case SharedDictionaryEvent.SharedDictionaryOperation.TRYUPDATE:
                    if (!this.Dictionary.ContainsKey((TKey)e.Key))
                    {
                        this.Send(e.Sender, new SharedDictionaryResponseEvent<bool>(false));
                    }
                    else
                    {
                        var currentValue = this.Dictionary[(TKey)e.Key];
                        if (currentValue.Equals((TValue)e.ComparisonValue))
                        {
                            this.Dictionary[(TKey)e.Key] = (TValue)e.Value;
                            this.Send(e.Sender, new SharedDictionaryResponseEvent<bool>(true));
                        }
                        else
                        {
                            this.Send(e.Sender, new SharedDictionaryResponseEvent<bool>(false));
                        }
                    }

                    break;

                case SharedDictionaryEvent.SharedDictionaryOperation.TRYGET:
                    if (!this.Dictionary.ContainsKey((TKey)e.Key))
                    {
                        this.Send(e.Sender, new SharedDictionaryResponseEvent<Tuple<bool, TValue>>(Tuple.Create(false, default(TValue))));
                    }
                    else
                    {
                        this.Send(e.Sender, new SharedDictionaryResponseEvent<Tuple<bool, TValue>>(Tuple.Create(true, this.Dictionary[(TKey)e.Key])));
                    }

                    break;

                case SharedDictionaryEvent.SharedDictionaryOperation.GET:
                    this.Send(e.Sender, new SharedDictionaryResponseEvent<TValue>(this.Dictionary[(TKey)e.Key]));
                    break;

                case SharedDictionaryEvent.SharedDictionaryOperation.SET:
                    this.Dictionary[(TKey)e.Key] = (TValue)e.Value;
                    break;

                case SharedDictionaryEvent.SharedDictionaryOperation.COUNT:
                    this.Send(e.Sender, new SharedDictionaryResponseEvent<int>(this.Dictionary.Count));
                    break;

                case SharedDictionaryEvent.SharedDictionaryOperation.TRYREMOVE:
                    if (this.Dictionary.ContainsKey((TKey)e.Key))
                    {
                        var value = this.Dictionary[(TKey)e.Key];
                        this.Dictionary.Remove((TKey)e.Key);
                        this.Send(e.Sender, new SharedDictionaryResponseEvent<Tuple<bool, TValue>>(Tuple.Create(true, value)));
                    }
                    else
                    {
                        this.Send(e.Sender, new SharedDictionaryResponseEvent<Tuple<bool, TValue>>(Tuple.Create(false, default(TValue))));
                    }

                    break;
            }
        }
    }
}
