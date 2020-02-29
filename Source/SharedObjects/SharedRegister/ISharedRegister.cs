// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Microsoft.Coyote.SharedObjects
{
    /// <summary>
    /// Interface of a shared register.
    /// </summary>
    /// <typeparam name="T">Value type of the shared register</typeparam>
    public interface ISharedRegister<T>
        where T : struct
    {
        /// <summary>
        /// Reads and updates the register.
        /// </summary>
        /// <param name="func">Update function</param>
        /// <returns>Resulting value of the register</returns>
        T Update(Func<T, T> func);

        /// <summary>
        /// Gets current value of the register.
        /// </summary>
        /// <returns>Current value</returns>
        T GetValue();

        /// <summary>
        /// Sets current value of the register.
        /// </summary>
        /// <param name="value">Value</param>
        void SetValue(T value);
    }
}
