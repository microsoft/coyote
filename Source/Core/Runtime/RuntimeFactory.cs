// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Actors;

namespace Microsoft.Coyote.Runtime
{
    /// <summary>
    /// Provides methods for creating a <see cref="ICoyoteRuntime"/> runtime.
    /// </summary>
    public static class RuntimeFactory
    {
        /// <summary>
        /// Creates a new Coyote runtime.
        /// </summary>
        /// <returns>The created task runtime.</returns>
        /// <remarks>
        /// Only one runtime can be created per async local context. This is not a thread-safe operation.
        /// </remarks>
        public static ICoyoteRuntime Create() => Create(default);

        /// <summary>
        /// Creates a new Coyote runtime with the specified <see cref="Configuration"/>.
        /// </summary>
        /// <param name="configuration">The runtime configuration to use.</param>
        /// <returns>The created task runtime.</returns>
        /// <remarks>
        /// Only one runtime can be created per async local context. This is not a thread-safe operation.
        /// </remarks>
        public static ICoyoteRuntime Create(Configuration configuration)
        {
            if (configuration is null)
            {
                configuration = Configuration.Create();
            }

            var valueGenerator = new RandomValueGenerator(configuration);
            var runtime = new ActorRuntime(configuration ?? Configuration.Create(), valueGenerator);

            // Assign the runtime to the currently executing asynchronous control flow.
            CoyoteRuntime.AssignGlobalRuntime(runtime);
            return runtime;
        }
    }
}
