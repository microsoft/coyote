// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Microsoft.Coyote.SharedObjects
{
    /// <summary>
    /// Implements a shared register to be used in production.
    /// </summary>
    internal sealed class ProductionSharedRegister<T> : ISharedRegister<T>
        where T : struct
    {
        /// <summary>
        /// Current value of the register.
        /// </summary>
        private T Value;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProductionSharedRegister{T}"/> class.
        /// </summary>
        public ProductionSharedRegister(T value)
        {
            this.Value = value;
        }

        /// <summary>
        /// Reads and updates the register.
        /// </summary>
        public T Update(Func<T, T> func)
        {
            T oldValue, newValue;
            bool done = false;

            do
            {
                oldValue = this.Value;
                newValue = func(oldValue);

                lock (this)
                {
                    if (oldValue.Equals(this.Value))
                    {
                        this.Value = newValue;
                        done = true;
                    }
                }
            }
            while (!done);

            return newValue;
        }

        /// <summary>
        /// Gets current value of the register.
        /// </summary>
        public T GetValue()
        {
            T currentValue;
            lock (this)
            {
                currentValue = this.Value;
            }

            return currentValue;
        }

        /// <summary>
        /// Sets current value of the register.
        /// </summary>
        public void SetValue(T value)
        {
            lock (this)
            {
                this.Value = value;
            }
        }
    }
}
