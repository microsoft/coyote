// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

using Microsoft.Coyote.Runtime;

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
        /// <param name="runtime">The actor runtime.</param>
        public static ISharedDictionary<TKey, TValue> Create<TKey, TValue>(IActorRuntime runtime)
        {
            if (runtime is ProductionRuntime)
            {
                return new ProductionSharedDictionary<TKey, TValue>();
            }
            else if (runtime is ControlledRuntime controlledRuntime)
            {
                return new MockSharedDictionary<TKey, TValue>(null, controlledRuntime);
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
        /// <param name="runtime">The actor runtime.</param>
        public static ISharedDictionary<TKey, TValue> Create<TKey, TValue>(IEqualityComparer<TKey> comparer, IActorRuntime runtime)
        {
            if (runtime is ProductionRuntime)
            {
                return new ProductionSharedDictionary<TKey, TValue>(comparer);
            }
            else if (runtime is ControlledRuntime controlledRuntime)
            {
                return new MockSharedDictionary<TKey, TValue>(comparer, controlledRuntime);
            }
            else
            {
                throw new RuntimeException("Unknown runtime object of type: " + runtime.GetType().Name + ".");
            }
        }
    }
}
