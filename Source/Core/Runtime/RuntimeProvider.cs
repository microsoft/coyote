// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Microsoft.Coyote.Runtime
{
    /// <summary>
    /// Provides methods for creating or accessing a <see cref="ICoyoteRuntime"/> runtime.
    /// </summary>
    public static class RuntimeProvider
    {
        /// <summary>
        /// Map from runtime identifiers to runtime instances.
        /// </summary>
        private static ConcurrentDictionary<Guid, CoyoteRuntime> Runtimes =
            new ConcurrentDictionary<Guid, CoyoteRuntime>();

        /// <summary>
        /// The default installed runtime instance.
        /// </summary>
        internal static CoyoteRuntime DefaultRuntime { get; private set; } = CreateWithConfiguration(default);

        /// <summary>
        /// Protects access to the default installed runtime.
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
        /// it as the default installed runtime, or returns the runtime if it already exists.
        /// </summary>
        /// <remarks>
        /// This is a thread-safe operation.
        /// </remarks>
        internal static CoyoteRuntime CreateAndInstall(Configuration configuration)
        {
            lock (SyncObject)
            {
                // Assign the newly created runtime as the default installed runtime.
                return DefaultRuntime = CreateWithConfiguration(configuration);
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

        /// <summary>
        /// Registers the specified runtime with the provider and returns a
        /// unique identifier that can be used to retrieve the runtime.
        /// </summary>
        internal static Guid Register(CoyoteRuntime runtime)
        {
            var id = Guid.NewGuid();
            Runtimes.TryAdd(id, runtime);
            return id;
        }

        /// <summary>
        /// Deregisters the runtime with the specified identifier from the provider.
        /// </summary>
        internal static void Deregister(Guid id)
        {
            Runtimes.TryRemove(id, out CoyoteRuntime _);
        }
    }
}
