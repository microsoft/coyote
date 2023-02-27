// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Threading;
using Microsoft.Coyote.Logging;

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
        private static ConcurrentDictionary<Guid, CoyoteRuntime> RuntimeMap = new ConcurrentDictionary<Guid, CoyoteRuntime>();

        /// <summary>
        /// The default installed runtime instance.
        /// </summary>
        internal static CoyoteRuntime Default { get; private set; } = CreateWithConfiguration(default, default, default, default);

        /// <summary>
        /// The runtime installed in the current execution context.
        /// </summary>
        public static ICoyoteRuntime Current => CoyoteRuntime.Current;

        /// <summary>
        /// Protects access to the default installed runtime.
        /// </summary>
        private static readonly object SyncObject = new object();

        /// <summary>
        /// Creates a new Coyote runtime with the specified <see cref="Configuration"/> and sets it
        /// as the default installed runtime, or returns the runtime if it already exists.
        /// </summary>
        /// <remarks>
        /// Only one Coyote runtime can be used per process. If you create a new Coyote runtime
        /// it replaces the previously installed one. This is a thread-safe operation.
        /// </remarks>
        internal static CoyoteRuntime CreateAndInstall(Configuration configuration, LogWriter logWriter,
            LogManager logManager, IRuntimeExtension extension)
        {
            lock (SyncObject)
            {
                // Assign the newly created runtime as the default installed runtime.
                return Default = CreateWithConfiguration(configuration, logWriter, logManager, extension);
            }
        }

        /// <summary>
        /// Creates a new Coyote runtime with the specified <see cref="Configuration"/>.
        /// </summary>
        internal static CoyoteRuntime CreateWithConfiguration(Configuration configuration, LogWriter logWriter,
            LogManager logManager, IRuntimeExtension extension)
        {
            configuration ??= Configuration.Create();
            var valueGenerator = new RandomValueGenerator(configuration);
            logWriter ??= new LogWriter(configuration);
            if (logManager is null)
            {
                logManager = new LogManager();
                logManager.RegisterLog(new RuntimeLogTextFormatter(), logWriter);
            }

            return CoyoteRuntime.Create(configuration, valueGenerator, logWriter, logManager, extension);
        }

        /// <summary>
        /// Registers the specified runtime with the provider and returns a
        /// unique identifier that can be used to retrieve the runtime.
        /// </summary>
        internal static Guid Register(CoyoteRuntime runtime)
        {
            var id = Guid.NewGuid();
            RuntimeMap.TryAdd(id, runtime);
            return id;
        }

        /// <summary>
        /// Deregisters the runtime with the specified identifier from the provider.
        /// </summary>
        internal static void Deregister(Guid id)
        {
            RuntimeMap.TryRemove(id, out CoyoteRuntime _);
        }

        /// <summary>
        /// Tries to get the runtime from the current synchronization context, if there is one available.
        /// </summary>
        /// <returns>True if the runtime was found, else false.</returns>
        internal static bool TryGetFromSynchronizationContext(out CoyoteRuntime runtime)
        {
            if (SynchronizationContext.Current is ControlledSynchronizationContext controlledContext &&
                controlledContext.SchedulingPolicy != SchedulingPolicy.None)
            {
                runtime = controlledContext.Runtime;
                return true;
            }

            runtime = null;
            return false;
        }

        /// <summary>
        /// Tries to get the runtime with the specified identifier, if there is one available.
        /// </summary>
        /// <returns>True if the runtime was found, else false.</returns>
        internal static bool TryGetFromId(Guid runtimeId, out CoyoteRuntime runtime) =>
            RuntimeMap.TryGetValue(runtimeId, out runtime) ? true : false;
    }
}
