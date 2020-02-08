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
        public static IActorRuntime Create() => CreateProductionRuntime(default);

        /// <summary>
        /// Creates a new actor runtime with the specified <see cref="Configuration"/>.
        /// </summary>
        /// <param name="configuration">The runtime configuration to use.</param>
        /// <returns>The created actor runtime.</returns>
        public static IActorRuntime Create(Configuration configuration) => CreateProductionRuntime(configuration);

        /// <summary>
        /// Creates a new production actor runtime with the specified <see cref="Configuration"/>.
        /// </summary>
        /// <param name="configuration">The runtime configuration to use.</param>
        /// <returns>The created production actor runtime.</returns>
        internal static ProductionRuntime CreateProductionRuntime(Configuration configuration)
        {
            if (configuration is null)
            {
                configuration = Configuration.Create();
            }

            var valueGenerator = new RandomValueGenerator(configuration);
            return new ProductionRuntime(configuration ?? Configuration.Create(), valueGenerator);
        }
    }
}
