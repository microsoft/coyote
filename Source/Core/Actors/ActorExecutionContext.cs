// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Linq;
using Microsoft.Coyote.Coverage;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.Specifications;
using Microsoft.Coyote.SystematicTesting;

namespace Microsoft.Coyote.Actors
{
    /// <summary>
    /// The execution context for one or more actors.
    /// </summary>
    internal sealed class ActorExecutionContext : IDisposable
    {
        /// <summary>
        /// The configuration used by the runtime.
        /// </summary>
        internal readonly Configuration Configuration;

        /// <summary>
        /// The runtime associated with this context.
        /// </summary>
        internal readonly CoyoteRuntime Runtime;

        /// <summary>
        /// The asynchronous operation scheduler, if available.
        /// </summary>
        internal readonly OperationScheduler Scheduler;

        /// <summary>
        /// Responsible for checking specifications.
        /// </summary>
        internal readonly SpecificationEngine SpecificationEngine;

        /// <summary>
        /// Map from unique actor ids to actors.
        /// </summary>
        internal readonly ConcurrentDictionary<ActorId, Actor> ActorMap;

        /// <summary>
        /// Map that stores all unique names and their corresponding actor ids.
        /// </summary>
        internal readonly ConcurrentDictionary<string, ActorId> NameValueToActorId;

        /// <summary>
        /// Data structure containing information regarding testing coverage.
        /// </summary>
        internal readonly CoverageInfo CoverageInfo;

        /// <summary>
        /// Responsible for generating random values.
        /// </summary>
        internal readonly IRandomValueGenerator ValueGenerator;

        /// <summary>
        /// Responsible for writing to all registered <see cref="IActorRuntimeLog"/> objects.
        /// </summary>
        internal readonly LogWriter LogWriter;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActorExecutionContext"/> class.
        /// </summary>
        internal ActorExecutionContext(Configuration configuration, CoyoteRuntime runtime, OperationScheduler scheduler,
            SpecificationEngine specificationEngine, CoverageInfo coverageInfo, IRandomValueGenerator valueGenerator,
            LogWriter logWriter)
        {
            this.Configuration = configuration;
            this.Runtime = runtime;
            this.Scheduler = scheduler;
            this.SpecificationEngine = specificationEngine;
            this.ActorMap = new ConcurrentDictionary<ActorId, Actor>();
            this.NameValueToActorId = new ConcurrentDictionary<string, ActorId>();
            this.CoverageInfo = coverageInfo;
            this.ValueGenerator = valueGenerator;
            this.LogWriter = logWriter;
        }

        /// <summary>
        /// Gets the actor of type <typeparamref name="TActor"/> with the specified id,
        /// or null if no such actor exists.
        /// </summary>
        internal TActor GetActorWithId<TActor>(ActorId id)
            where TActor : Actor =>
            id != null && this.ActorMap.TryGetValue(id, out Actor value) &&
            value is TActor actor ? actor : null;

        /// <summary>
        /// Returns the next available unique operation id.
        /// </summary>
        /// <returns>Value representing the next available unique operation id.</returns>
        internal ulong GetNextOperationId() => this.Runtime.GetNextOperationId();

        /// <summary>
        /// Get the coverage graph information, if any. This information is only available
        /// when <see cref="Configuration.ReportActivityCoverage"/> is enabled.
        /// </summary>
        /// <returns>A new CoverageInfo object.</returns>
        internal CoverageInfo GetCoverageInfo()
        {
            var result = this.CoverageInfo;
            if (result != null)
            {
                var builder = this.LogWriter.GetLogsOfType<ActorRuntimeLogGraphBuilder>().FirstOrDefault();
                if (builder != null)
                {
                    result.CoverageGraph = builder.SnapshotGraph(this.Configuration.IsDgmlBugGraph);
                }

                var eventCoverage = this.LogWriter.GetLogsOfType<ActorRuntimeLogEventCoverage>().FirstOrDefault();
                if (eventCoverage != null)
                {
                    result.EventInfo = eventCoverage.EventCoverage;
                }
            }

            return result;
        }

        /// <summary>
        /// Disposes runtime resources.
        /// </summary>
        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.ActorMap.Clear();
            }
        }

        /// <summary>
        /// Disposes runtime resources.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
