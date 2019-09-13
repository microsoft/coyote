// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.Coyote.Machines;
using Microsoft.Coyote.TestingServices.Runtime;

namespace Microsoft.Coyote.SharedObjects
{
    /// <summary>
    /// A wrapper for a shared counter modeled using a state-machine for testing.
    /// </summary>
    internal sealed class MockSharedCounter : ISharedCounter
    {
        /// <summary>
        /// Machine modeling the shared counter.
        /// </summary>
        private readonly MachineId CounterMachine;

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
            this.CounterMachine = this.Runtime.CreateMachine(typeof(SharedCounterMachine));
            var currentMachine = this.Runtime.GetExecutingMachine<Machine>();
            this.Runtime.SendEvent(this.CounterMachine, SharedCounterEvent.SetEvent(currentMachine.Id, value));
            currentMachine.Receive(typeof(SharedCounterResponseEvent)).Wait();
        }

        /// <summary>
        /// Increments the shared counter.
        /// </summary>
        public void Increment()
        {
            this.Runtime.SendEvent(this.CounterMachine, SharedCounterEvent.IncrementEvent());
        }

        /// <summary>
        /// Decrements the shared counter.
        /// </summary>
        public void Decrement()
        {
            this.Runtime.SendEvent(this.CounterMachine, SharedCounterEvent.DecrementEvent());
        }

        /// <summary>
        /// Gets the current value of the shared counter.
        /// </summary>
        public int GetValue()
        {
            var currentMachine = this.Runtime.GetExecutingMachine<Machine>();
            this.Runtime.SendEvent(this.CounterMachine, SharedCounterEvent.GetEvent(currentMachine.Id));
            var response = currentMachine.Receive(typeof(SharedCounterResponseEvent)).Result;
            return (response as SharedCounterResponseEvent).Value;
        }

        /// <summary>
        /// Adds a value to the counter atomically.
        /// </summary>
        public int Add(int value)
        {
            var currentMachine = this.Runtime.GetExecutingMachine<Machine>();
            this.Runtime.SendEvent(this.CounterMachine, SharedCounterEvent.AddEvent(currentMachine.Id, value));
            var response = currentMachine.Receive(typeof(SharedCounterResponseEvent)).Result;
            return (response as SharedCounterResponseEvent).Value;
        }

        /// <summary>
        /// Sets the counter to a value atomically.
        /// </summary>
        public int Exchange(int value)
        {
            var currentMachine = this.Runtime.GetExecutingMachine<Machine>();
            this.Runtime.SendEvent(this.CounterMachine, SharedCounterEvent.SetEvent(currentMachine.Id, value));
            var response = currentMachine.Receive(typeof(SharedCounterResponseEvent)).Result;
            return (response as SharedCounterResponseEvent).Value;
        }

        /// <summary>
        /// Sets the counter to a value atomically if it is equal to a given value.
        /// </summary>
        public int CompareExchange(int value, int comparand)
        {
            var currentMachine = this.Runtime.GetExecutingMachine<Machine>();
            this.Runtime.SendEvent(this.CounterMachine, SharedCounterEvent.CasEvent(currentMachine.Id, value, comparand));
            var response = currentMachine.Receive(typeof(SharedCounterResponseEvent)).Result;
            return (response as SharedCounterResponseEvent).Value;
        }
    }
}
