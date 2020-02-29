// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.TestingServices.Runtime;

namespace Microsoft.Coyote.SharedObjects
{
    /// <summary>
    /// Shared dictionary that can be safely shared by multiple Coyote machines.
    /// </summary>
    public static class SharedDictionary
    {
        /// <summary>
        /// Creates a new shared dictionary.
        /// </summary>
        /// <param name="runtime">The machine runtime.</param>
        public static ISharedDictionary<TKey, TValue> Create<TKey, TValue>(IMachineRuntime runtime)
        {
            if (runtime is ProductionRuntime)
            {
                return new ProductionSharedDictionary<TKey, TValue>();
            }
            else if (runtime is SystematicTestingRuntime testingRuntime)
            {
                return new MockSharedDictionary<TKey, TValue>(null, testingRuntime);
            }
            else
            {
                throw new RuntimeException("Unknown runtime object of type: " + runtime.GetType().Name + ".");
            }
        }

        /// <summary>
        /// Creates a new shared dictionary.
        /// </summary>
        /// <param name="comparer">The key comparer.</param>
        /// <param name="runtime">The machine runtime.</param>
        public static ISharedDictionary<TKey, TValue> Create<TKey, TValue>(IEqualityComparer<TKey> comparer, IMachineRuntime runtime)
        {
            if (runtime is ProductionRuntime)
            {
                return new ProductionSharedDictionary<TKey, TValue>(comparer);
            }
            else if (runtime is SystematicTestingRuntime testingRuntime)
            {
                return new MockSharedDictionary<TKey, TValue>(comparer, testingRuntime);
            }
            else
            {
                throw new RuntimeException("Unknown runtime object of type: " + runtime.GetType().Name + ".");
            }
        }
    }
}
