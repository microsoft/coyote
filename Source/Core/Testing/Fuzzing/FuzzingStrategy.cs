// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Coyote.Testing.Fuzzing
{
    /// <summary>
    /// Abstract fuzzing strategy used during testing.
    /// </summary>
    internal abstract class FuzzingStrategy : ExplorationStrategy
    {
        /// <summary>
        /// Provides access to the operation id associated with each asynchronous control flow.
        /// </summary>
        private static readonly AsyncLocal<Guid> OperationId = new AsyncLocal<Guid>();

        /// <summary>
        /// Map from task ids to operation ids.
        /// </summary>
        private readonly ConcurrentDictionary<int, Guid> OperationIdMap;

        /// <summary>
        /// Initializes a new instance of the <see cref="FuzzingStrategy"/> class.
        /// </summary>
        internal FuzzingStrategy()
        {
            this.OperationIdMap = new ConcurrentDictionary<int, Guid>();
        }

        /// <summary>
        /// Creates a <see cref="FuzzingStrategy"/> from the specified configuration.
        /// </summary>
        internal static FuzzingStrategy Create(Configuration configuration, IRandomValueGenerator generator)
        {
            switch (configuration.SchedulingStrategy)
            {
                case "pct":
                    return new PCTStrategy(configuration.MaxUnfairSchedulingSteps, generator, configuration.StrategyBound);
                default:
                    // return new RandomStrategy(configuration.MaxFairSchedulingSteps, generator);
                    return new BoundedRandomStrategy(configuration.MaxUnfairSchedulingSteps, generator);
            }
        }

        /// <summary>
        /// Returns the next delay.
        /// </summary>
        /// <param name="maxValue">The max value.</param>
        /// <param name="next">The next delay.</param>
        /// <returns>True if there is a next delay, else false.</returns>
        internal abstract bool GetNextDelay(int maxValue, out int next);

        /// <summary>
        /// Returns the current operation id.
        /// </summary>
        protected Guid GetOperationId()
        {
            Guid id;
            if (Task.CurrentId is null)
            {
                id = OperationId.Value;
                if (id == Guid.Empty)
                {
                    id = Guid.NewGuid();
                    OperationId.Value = id;
                }
            }
            else
            {
                id = this.OperationIdMap.GetOrAdd(Task.CurrentId.Value, Guid.NewGuid());
                OperationId.Value = id;
            }

            return id;
        }
    }
}
