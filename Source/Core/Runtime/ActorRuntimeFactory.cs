// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Coyote.Runtime
{
    /// <summary>
    /// The runtime for creating and executing actors.
    /// </summary>
    public static class ActorRuntimeFactory
    {
        /// <summary>
        /// Creates a new actor runtime.
        /// </summary>
        /// <returns>The created actor runtime.</returns>
        public static IActorRuntime Create()
        {
            return new ProductionRuntime(Configuration.Create());
        }

        /// <summary>
        /// Creates a new actor runtime with the specified <see cref="Configuration"/>.
        /// </summary>
        /// <param name="configuration">The runtime configuration to use.</param>
        /// <returns>The created actor runtime.</returns>
        public static IActorRuntime Create(Configuration configuration)
        {
            return new ProductionRuntime(configuration ?? Configuration.Create());
        }
    }
}
