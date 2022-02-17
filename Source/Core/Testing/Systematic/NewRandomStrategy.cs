// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Coyote.Runtime;

namespace Microsoft.Coyote.Testing.Systematic
{
    /// <summary>
    /// A simple (but effective) randomized scheduling strategy.
    /// </summary>
    internal class NewRandomStrategy : SystematicStrategy
    {
        /// <summary>
        /// Random value generator.
        /// </summary>
        protected IRandomValueGenerator RandomValueGenerator;

        /// <summary>
        /// The maximum number of steps to explore.
        /// </summary>
        protected readonly int MaxSteps;

        /// <summary>
        /// The number of exploration steps.
        /// </summary>
        protected int StepCount;

        private readonly HashSet<string> KnownSchedules;

        private string CurrentSchedule;

        private List<(string name, int enabledOpsCount, SchedulingPointType spType, OperationGroup group, string debug, int phase, bool isForced)> ExecutionPath;

        private Dictionary<string, string> OperationDebugInfo;

        private Dictionary<int, HashSet<OperationGroup>> Groups;

        private Dictionary<int, HashSet<ControlledOperation>> DisabledOps;

        private int Phase = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="NewRandomStrategy"/> class.
        /// </summary>
        internal NewRandomStrategy(int maxSteps, IRandomValueGenerator generator)
        {
            this.RandomValueGenerator = generator;
            this.MaxSteps = maxSteps;
            this.KnownSchedules = new HashSet<string>();
            this.CurrentSchedule = string.Empty;
            this.ExecutionPath = new List<(string, int, SchedulingPointType, OperationGroup, string, int, bool)>();
            this.OperationDebugInfo = new Dictionary<string, string>();
            this.Groups = new Dictionary<int, HashSet<OperationGroup>>();
            this.DisabledOps = new Dictionary<int, HashSet<ControlledOperation>>();
        }

        /// <inheritdoc/>
        internal override bool InitializeNextIteration(uint iteration)
        {
            // The random strategy just needs to reset the number of scheduled steps during
            // the current iretation.
            this.StepCount = 0;
            this.CurrentSchedule = string.Empty;
            this.ExecutionPath.Clear();
            this.OperationDebugInfo.Clear();
            this.Groups.Clear();
            this.DisabledOps.Clear();
            this.Phase = 0;
            return true;
        }

        /// <inheritdoc/>
        internal override bool GetNextOperation(IEnumerable<ControlledOperation> ops, ControlledOperation current,
            bool isYielding, out ControlledOperation next)
        {
            var enabledOps = ops.Where(op => op.Status is OperationStatus.Enabled).ToList();
            if (enabledOps.Count is 0)
            {
                next = null;
                return false;
            }

            // Find operations with schedule that has not been seen yet.
            // var newOps = enabledOps.Where(op => !this.KnownSchedules.Contains(op.Schedule)).ToList();
            // if (enabledOps.Count > 1)
            // {
            //     var newOps = new List<ControlledOperation>();
            //     foreach (var op in enabledOps)
            //     {
            //         var potentialSchedule = this.CurrentSchedule + $" | {current.Id}";
            //         System.Console.WriteLine($">>>>> Potential schedule for '{op.Id}': {potentialSchedule}");
            //         if (!this.KnownSchedules.Contains(potentialSchedule))
            //         {
            //             newOps.Add(op);
            //         }
            //     }
            //     if (newOps.Count > 0)
            //     {
            //         System.Console.WriteLine($">>>>> Filtering ops to '{newOps.Count}' from '{enabledOps.Count}'.");
            //         enabledOps = newOps;
            //     }
            // }

            bool isForced = false;
            // If explicit, and there is an operation group, then choose a random operation in the same group.
            if (current.SchedulingPoint != SchedulingPointType.Interleave &&
                current.SchedulingPoint != SchedulingPointType.Yield)
            {
                // Choose an operation that has the same group as the operation that just completed.
                if (current.Group != null)
                {
                    System.Console.WriteLine($">>>>> SCHEDULE NEXT: {current.Name} (o: {current.Group.Owner}, g: {current.Group})");
                    var groupOps = enabledOps.Where(op => current.Group.IsMember(op)).ToList();
                    System.Console.WriteLine($">>>>> groupOps: {groupOps.Count}");
                    foreach (var op in groupOps)
                    {
                        System.Console.WriteLine($">>>>>>>> groupOp: {op.Name} (o: {op.Group.Owner}, g: {op.Group})");
                    }

                    if (groupOps.Count > 0)
                    {
                        enabledOps = groupOps;
                        isForced = true;
                    }
                }
            }

            int idx = this.RandomValueGenerator.Next(enabledOps.Count);
            next = enabledOps[idx];

            this.ProcessSchedule(current.SchedulingPoint, next, ops, isForced);
            this.StepCount++;
            return true;
        }

        private void ProcessSchedule(SchedulingPointType spType, ControlledOperation op,
            IEnumerable<ControlledOperation> ops, bool isForced)
        {
            string msg = op.Msg;
            if (!string.IsNullOrEmpty(msg) && !this.OperationDebugInfo.TryGetValue(op.Msg ?? string.Empty, out msg))
            {
                if (op.Msg.Length > 25)
                {
                    msg = op.Msg.Substring(0, 25);
                }
                else
                {
                    msg = op.Msg;
                }

                msg = $"REQ{this.OperationDebugInfo.Count} ({msg})";
                this.OperationDebugInfo.Add(op.Msg, msg);
            }

            var count = ops.Count(op => op.Status is OperationStatus.Enabled || op.Status is OperationStatus.Disabled);
            foreach (var xop in ops.Where(op => op.Status is OperationStatus.Enabled || op.Status is OperationStatus.Disabled))
            {
                if (xop.Group != null)
                {
                    if (!this.Groups.TryGetValue(this.Phase, out HashSet<OperationGroup> gSet))
                    {
                        gSet = new HashSet<OperationGroup>();
                        this.Groups.Add(this.Phase, gSet);
                    }

                    gSet.Add(xop.Group);
                }

                if (xop.Status is OperationStatus.Disabled)
                {
                    if (!this.DisabledOps.TryGetValue(this.Phase, out HashSet<ControlledOperation> dSet))
                    {
                        dSet = new HashSet<ControlledOperation>();
                        this.DisabledOps.Add(this.Phase, dSet);
                    }

                    dSet.Add(xop);
                }
            }

            this.ExecutionPath.Add((op.Name, count, spType, op.Group, msg, this.Phase, isForced));

            var groups = new HashSet<OperationGroup>();
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < this.ExecutionPath.Count; i++)
            {
                var step = this.ExecutionPath[i];
                if (i > 0 && step.phase != this.ExecutionPath[i - 1].phase)
                {
                    int phase = step.phase - 1;
                    sb.AppendLine($"===== CHANGE PHASE =====");
                    sb.AppendLine($"Phase: {phase}");
                    sb.AppendLine($"{groups.Count} scheduled groups:");
                    foreach (var g in groups)
                    {
                        sb.AppendLine($"  |_ {g}({g.Owner})");
                    }

                    if (this.Groups.TryGetValue(phase, out HashSet<OperationGroup> gSet))
                    {
                        var set = gSet.Where(g => !groups.Contains(g)).ToList();
                        if (set.Count > 0)
                        {
                            sb.AppendLine($"{set.Count} remaining groups:");
                            foreach (var g in set)
                            {
                                sb.AppendLine($"  |_ {g}({g.Owner})");
                            }
                        }
                    }

                    if (this.DisabledOps.TryGetValue(phase, out HashSet<ControlledOperation> dSet))
                    {
                        sb.AppendLine($"{dSet.Count} disabled ops:");
                        foreach (var d in dSet)
                        {
                            sb.AppendLine($"  |_ {d} : g {d.Group} + o {d.Group.Owner} ({d.Group.Owner.Msg}))");
                        }
                    }

                    sb.AppendLine($"Length: {i}");
                    sb.AppendLine($"======================");
                    groups.Clear();
                }

                string group = string.Empty;
                if (step.group != null)
                {
                    group = $" - GRP[{step.group}({step.group.Owner})]";
                    groups.Add(step.group);
                }

                string debug = string.Empty;
                if (!string.IsNullOrEmpty(step.debug))
                {
                    debug = $" - DBG[{step.debug}]";
                }

                string isStepForced = string.Empty;
                if (step.isForced)
                {
                    isStepForced = $" - FORCED";
                }

                sb.AppendLine($"{step.name} - OPS[{step.enabledOpsCount}] - SP[{step.spType}]{group}{debug}{isStepForced}");
            }

            this.CurrentSchedule = sb.ToString();
            System.Console.WriteLine($">>> Schedule so far: {this.CurrentSchedule}");
            this.KnownSchedules.Add(this.CurrentSchedule);
            RuntimeStats.NumVisitedSchedules = this.KnownSchedules.Count;
            System.Console.WriteLine($">>> Visited '{this.KnownSchedules.Count}' schedules so far.");
        }

        internal void MoveNextPhase(int phase)
        {
            System.Console.WriteLine($">>> Moved to phase '{this.Phase}'.");
            this.Phase = phase;
        }

        /// <inheritdoc/>
        internal override bool GetNextBooleanChoice(ControlledOperation current, int maxValue, out bool next)
        {
            next = false;
            if (this.RandomValueGenerator.Next(maxValue) is 0)
            {
                next = true;
            }

            this.StepCount++;
            return true;
        }

        /// <inheritdoc/>
        internal override bool GetNextIntegerChoice(ControlledOperation current, int maxValue, out int next)
        {
            next = this.RandomValueGenerator.Next(maxValue);
            this.StepCount++;
            return true;
        }

        /// <inheritdoc/>
        internal override int GetStepCount() => this.StepCount;

        /// <inheritdoc/>
        internal override bool IsMaxStepsReached()
        {
            if (this.MaxSteps is 0)
            {
                return false;
            }

            return this.StepCount >= this.MaxSteps;
        }

        /// <inheritdoc/>
        internal override bool IsFair() => true;

        /// <inheritdoc/>
        internal override string GetDescription() => $"new-random[seed '{this.RandomValueGenerator.Seed}']";

        /// <inheritdoc/>
        internal override void Reset()
        {
            this.StepCount = 0;
        }
    }
}
