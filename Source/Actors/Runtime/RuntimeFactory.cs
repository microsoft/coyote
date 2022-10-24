// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Logging;
using Microsoft.Coyote.Runtime;

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
        public static IActorRuntime Create(Configuration configuration)
        {
            configuration ??= Configuration.Create();
            var logWriter = new LogWriter(configuration);
            var logManager = CreateLogManager(logWriter);

            var actorRuntime = new ActorExecutionContext(configuration, logManager);
            var runtime = Runtime.RuntimeProvider.CreateAndInstall(configuration, logWriter, logManager, actorRuntime);
            return actorRuntime.WithRuntime(runtime);
        }

        /// <summary>
        /// Creates a new actor runtime with the specified <see cref="Configuration"/> for the specified <see cref="SchedulingPolicy"/>.
        /// </summary>
        internal static ActorExecutionContext Create(Configuration configuration, ActorLogManager logManager, SchedulingPolicy schedulingPolicy) =>
            schedulingPolicy is SchedulingPolicy.Interleaving ?
                new ActorExecutionContext.Mock(configuration, logManager) :
                new ActorExecutionContext(configuration, logManager);

        /// <summary>
        /// Creates a new runtime log manager that writes to the specified log writer.
        /// </summary>
        internal static ActorLogManager CreateLogManager(LogWriter logWriter)
        {
            var logManager = new ActorLogManager();
            logManager.RegisterLog(new ActorRuntimeLogTextFormatter(), logWriter);
            return logManager;
        }
    }
}
