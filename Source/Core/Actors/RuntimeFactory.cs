// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Coyote.Actors
{
    /// <summary>
    /// Provides methods for creating a <see cref="IActorRuntime"/> runtime.
    /// </summary>
    public static class RuntimeFactory
    {
        /// <summary>
        /// Creates a new actor runtime.
        /// </summary>
        /// <returns>The created actor runtime.</returns>
        /// <remarks>
        /// Only one actor runtime can be used per process. If you create a new actor runtime
        /// it replaces the previously installed one. This is a thread-safe operation.
        /// </remarks>
        public static IActorRuntime Create() => Create(default);

        /// <summary>
        /// Creates a new actor runtime with the specified <see cref="Configuration"/>.
        /// </summary>
        /// <param name="configuration">The runtime configuration to use.</param>
        /// <returns>The created actor runtime.</returns>
        /// <remarks>
        /// Only one actor runtime can be used per process. If you create a new actor runtime
        /// it replaces the previously installed one. This is a thread-safe operation.
        /// </remarks>
        public static IActorRuntime Create(Configuration configuration) =>
            Runtime.RuntimeFactory.CreateAndInstall(configuration).DefaultActorExecutionContext;
    }
}
