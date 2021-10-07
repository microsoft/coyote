// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Coyote.Runtime
{
    /// <summary>
    /// Provides methods for creating a <see cref="ICoyoteRuntime"/> runtime.
    /// </summary>
    public static class RuntimeFactory
    {
        /// <summary>
        /// The installed runtime instance.
        /// </summary>
        internal static CoyoteRuntime InstalledRuntime { get; private set; } = CreateWithConfiguration(default);

        /// <summary>
        /// Protects access to the installed runtime.
        /// </summary>
        private static readonly object SyncObject = new object();

        /// <summary>
        /// Creates a new Coyote runtime.
        /// </summary>
        /// <returns>The created Coyote runtime.</returns>
        /// <remarks>
        /// Only one Coyote runtime can be used per process. If you create a new Coyote runtime
        /// it replaces the previously installed one. This is a thread-safe operation.
        /// </remarks>
        public static ICoyoteRuntime Create() => CreateAndInstall(default).DefaultActorExecutionContext;

        /// <summary>
        /// Creates a new Coyote runtime with the specified <see cref="Configuration"/>.
        /// </summary>
        /// <param name="configuration">The runtime configuration to use.</param>
        /// <returns>The created Coyote runtime.</returns>
        /// <remarks>
        /// Only one Coyote runtime can be used per process. If you create a new Coyote runtime
        /// it replaces the previously installed one. This is a thread-safe operation.
        /// </remarks>
        public static ICoyoteRuntime Create(Configuration configuration) =>
            CreateAndInstall(configuration).DefaultActorExecutionContext;

        /// <summary>
        /// Creates a new Coyote runtime with the specified <see cref="Configuration"/> and sets
        /// it as the installed runtime, or returns the installed runtime if it already exists.
        /// </summary>
        /// <remarks>
        /// This is a thread-safe operation.
        /// </remarks>
        internal static CoyoteRuntime CreateAndInstall(Configuration configuration)
        {
            lock (SyncObject)
            {
                // Assign the newly created runtime as the installed runtime.
                return InstalledRuntime = CreateWithConfiguration(configuration);
            }
        }

        /// <summary>
        /// Creates a new Coyote runtime with the specified <see cref="Configuration"/>.
        /// </summary>
        private static CoyoteRuntime CreateWithConfiguration(Configuration configuration)
        {
            if (configuration is null)
            {
                configuration = Configuration.Create();
            }

            var valueGenerator = new RandomValueGenerator(configuration);
            return new CoyoteRuntime(configuration, valueGenerator);
        }
    }
}
