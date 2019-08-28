// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Threading;

namespace Microsoft.Coyote.SharedObjects
{
    /// <summary>
    /// Implements a shared counter to be used in production.
    /// </summary>
    internal sealed class ProductionSharedCounter : ISharedCounter
    {
        /// <summary>
        /// The value of the shared counter.
        /// </summary>
        private volatile int Counter;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProductionSharedCounter"/> class.
        /// </summary>
        public ProductionSharedCounter(int value)
        {
            this.Counter = value;
        }

        /// <summary>
        /// Increments the shared counter.
        /// </summary>
        public void Increment()
        {
            Interlocked.Increment(ref this.Counter);
        }

        /// <summary>
        /// Decrements the shared counter.
        /// </summary>
        public void Decrement()
        {
            Interlocked.Decrement(ref this.Counter);
        }

        /// <summary>
        /// Gets the current value of the shared counter.
        /// </summary>
        public int GetValue() => this.Counter;

        /// <summary>
        /// Adds a value to the counter atomically.
        /// </summary>
        public int Add(int value) => Interlocked.Add(ref this.Counter, value);

        /// <summary>
        /// Sets the counter to a value atomically.
        /// </summary>
        public int Exchange(int value) => Interlocked.Exchange(ref this.Counter, value);

        /// <summary>
        /// Sets the counter to a value atomically if it is equal to a given value.
        /// </summary>
        public int CompareExchange(int value, int comparand) =>
            Interlocked.CompareExchange(ref this.Counter, value, comparand);
    }
}
