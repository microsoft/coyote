// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Coyote.Runtime;

namespace Microsoft.Coyote.Testing.Systematic
{
    /// <summary>
    /// Abstract scheduling strategy used during systematic testing.
    /// </summary>
    internal abstract class SystematicStrategy : ExplorationStrategy
    {
        protected string CurrentSchedule;

        protected List<(string name, int enabledOpsCount, SchedulingPointType spType, OperationGroup group, int phase, string filter)> Path;

        protected Dictionary<int, Dictionary<OperationGroup, (bool scheduled, bool disabled, bool completed)>> Groups;

        protected int Phase = 0;

        internal List<int> MustChangeCount = new List<int>();

        internal List<int> ChangeCount = new List<int>();

        internal HashSet<string> ReadAccessSet = new HashSet<string>();

        internal HashSet<string> WriteAccessSet = new HashSet<string>();

        /// <summary>
        /// Initializes a new instance of the <see cref="SystematicStrategy"/> class.
        /// </summary>
        protected SystematicStrategy(Configuration configuration, IRandomValueGenerator generator, bool isFair)
            : base(configuration, generator, isFair)
        {
        }

        /// <summary>
        /// Creates a <see cref="SystematicStrategy"/> from the specified configuration.
        /// </summary>
        internal static SystematicStrategy Create(Configuration configuration, IRandomValueGenerator generator)
        {
            SystematicStrategy strategy = null;
            if (configuration.SchedulingStrategy is "replay")
            {
                var trace = ScheduleTrace.Deserialize(configuration, out bool isFair);
                strategy = new ReplayStrategy(configuration, generator, trace, isFair);
            }
            else if (configuration.SchedulingStrategy is "random")
            {
                strategy = new RandomStrategy(configuration, generator);
            }
            else if (configuration.SchedulingStrategy is "pct")
            {
                strategy = new PriorityBasedStrategy(configuration, generator);
                // strategy = new PCTStrategy(configuration, generator);
            }
            else if (configuration.SchedulingStrategy is "fairpct")
            {
                var prefixStrategy = new PCTStrategy(configuration, generator);
                var suffixStrategy = new RandomStrategy(configuration, generator);
                strategy = new ComboStrategy(configuration, generator, prefixStrategy, suffixStrategy);
            }
            else if (configuration.SchedulingStrategy is "probabilistic")
            {
                strategy = new ProbabilisticRandomStrategy(configuration, generator);
            }
            else if (configuration.SchedulingStrategy is "rl")
            {
                strategy = new QLearningStrategy(configuration, generator);
            }
            else if (configuration.SchedulingStrategy is "dfs")
            {
                strategy = new DFSStrategy(configuration, generator);
            }

            return strategy;
        }

        /// <summary>
        /// Returns the next controlled operation to schedule.
        /// </summary>
        /// <param name="ops">Operations that can be scheduled.</param>
        /// <param name="current">The currently scheduled operation.</param>
        /// <param name="isYielding">True if the current operation is yielding, else false.</param>
        /// <param name="next">The next operation to schedule.</param>
        /// <returns>True if there is a next choice, else false.</returns>
        internal abstract bool GetNextOperation(IEnumerable<ControlledOperation> ops, ControlledOperation current,
            bool isYielding, out ControlledOperation next);

        /// <summary>
        /// Returns the next boolean choice.
        /// </summary>
        /// <param name="current">The currently scheduled operation.</param>
        /// <param name="maxValue">The max value.</param>
        /// <param name="next">The next boolean choice.</param>
        /// <returns>True if there is a next choice, else false.</returns>
        internal abstract bool GetNextBooleanChoice(ControlledOperation current, int maxValue, out bool next);

        /// <summary>
        /// Returns the next integer choice.
        /// </summary>
        /// <param name="current">The currently scheduled operation.</param>
        /// <param name="maxValue">The max value.</param>
        /// <param name="next">The next integer choice.</param>
        /// <returns>True if there is a next choice, else false.</returns>
        internal abstract bool GetNextIntegerChoice(ControlledOperation current, int maxValue, out int next);

        /// <summary>
        /// Resets the strategy.
        /// </summary>
        /// <remarks>
        /// This is typically invoked by parent strategies to reset child strategies.
        /// </remarks>
        internal abstract void Reset();

        protected void ProcessSchedule(SchedulingPointType spType, ControlledOperation op,
            IEnumerable<ControlledOperation> ops, string filter)
        {
            var group = op.Group;
            if (!this.Groups.TryGetValue(this.Phase, out var groupMap))
            {
                groupMap = new Dictionary<OperationGroup, (bool, bool, bool)>();
                this.Groups.Add(this.Phase, groupMap);
            }

            groupMap[group] = (true, false, false);

            var count = ops.Count(op => op.Status is OperationStatus.Enabled);
            this.Path.Add((op.Name, count, spType, group, this.Phase, filter));
        }

        internal void PrintSchedule()
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < this.Path.Count; i++)
            {
                var step = this.Path[i];
                if ((i > 0 && step.phase != this.Path[i - 1].phase) || i == this.Path.Count - 1)
                {
                    int phase = i == this.Path.Count - 1 ? step.phase : step.phase - 1;
                    sb.AppendLine($"===== PHASE DONE =====");
                    sb.AppendLine($"Phase: {phase}");
                    if (this.Groups.TryGetValue(phase, out var groupMap))
                    {
                        sb.AppendLine($"{groupMap.Count} scheduled groups:");
                        foreach (var kvp in groupMap)
                        {
                            if (kvp.Value.scheduled)
                            {
                                if (kvp.Value.completed)
                                {
                                    sb.AppendLine($"  |_ {kvp.Key}({kvp.Key.Owner} - COMPLETED!");
                                }
                                else
                                {
                                    sb.AppendLine($"  |_ {kvp.Key}({kvp.Key.Owner}");
                                }
                            }
                        }

                        var gd = groupMap.Where(g => g.Value.disabled).Select(g => g.Key).ToList();
                        if (gd.Count > 0)
                        {
                            sb.AppendLine($"{gd.Count} disabled groups:");
                            foreach (var g in gd)
                            {
                                sb.AppendLine($"  |_ {g}({g.Owner}");
                            }
                        }

                        var groupOld = this.Groups.Where(p => p.Key < phase).SelectMany(p => p.Value.Keys).Distinct();
                        var groupAll = this.Groups.Where(p => p.Key <= phase).SelectMany(p => p.Value.Keys).Distinct();
                        var groupDelta = groupMap.Where(g => !groupOld.Contains(g.Key));
                        sb.AppendLine($"Groups (old): {groupOld.Count()}");
                        sb.AppendLine($"Groups (all): {groupAll.Count()}");
                        sb.AppendLine($"Groups (new): {groupDelta.Count()}");
                    }

                    sb.AppendLine($"Length: {i}");
                    sb.AppendLine($"======================");
                }

                string group = step.group is null ? string.Empty : $" - GRP[{step.group}({step.group.Owner})]";
                string filter = string.IsNullOrEmpty(step.filter) ? string.Empty : $" - {step.filter}";
                sb.AppendLine($"{step.name} - OPS[{step.enabledOpsCount}] - SP[{step.spType}]{group}{filter}");
            }

            this.CurrentSchedule = sb.ToString();
            System.Console.WriteLine($">>> Schedule so far: {this.CurrentSchedule}");
        }
    }
}
