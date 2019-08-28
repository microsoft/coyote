﻿// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.TestingServices.Runtime;

namespace Microsoft.Coyote.SharedObjects
{
    /// <summary>
    /// Shared register that can be safely shared by multiple Coyote machines.
    /// </summary>
    public static class SharedRegister
    {
        /// <summary>
        /// Creates a new shared register.
        /// </summary>
        /// <param name="runtime">The machine runtime.</param>
        /// <param name="value">The initial value.</param>
        public static ISharedRegister<T> Create<T>(ICoyoteRuntime runtime, T value = default)
            where T : struct
        {
            if (runtime is ProductionRuntime)
            {
                return new ProductionSharedRegister<T>(value);
            }
            else if (runtime is SystematicTestingRuntime testingRuntime)
            {
                return new MockSharedRegister<T>(value, testingRuntime);
            }
            else
            {
                throw new RuntimeException("Unknown runtime object of type: " + runtime.GetType().Name + ".");
            }
        }
    }
}
