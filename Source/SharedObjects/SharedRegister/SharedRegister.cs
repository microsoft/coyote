// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Actors;
using Microsoft.Coyote.SystematicTesting;

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
        /// <param name="runtime">The actor runtime.</param>
        /// <param name="value">The initial value.</param>
        public static ISharedRegister<T> Create<T>(IActorRuntime runtime, T value = default)
            where T : struct
        {
            if (runtime is ActorRuntime)
            {
                return new ProductionSharedRegister<T>(value);
            }
            else if (runtime is ControlledRuntime controlledRuntime)
            {
                return new MockSharedRegister<T>(value, controlledRuntime);
            }
            else
            {
                throw new RuntimeException("Unknown runtime object of type: " + runtime.GetType().Name + ".");
            }
        }
    }
}
