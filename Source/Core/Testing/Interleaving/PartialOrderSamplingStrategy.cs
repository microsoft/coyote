// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Coyote.IO;
using Microsoft.Coyote.Runtime;

namespace Microsoft.Coyote.Testing.Interleaving
{
    /// <summary>
    /// A partial-order sampling scheduling strategy.
    /// </summary>
    /// <remarks>
    /// This strategy implements partial-order sampling as described in the following paper:
    /// https://link.springer.com/chapter/10.1007/978-3-319-96142-2_20.
    /// </remarks>
    internal sealed class PartialOrderSamplingStrategy : RandomStrategy
    {
        /// <summary>
        /// List of prioritized operations.
        /// </summary>
        private readonly List<ControlledOperation> PrioritizedOperations;

        /// <summary>
        /// Initializes a new instance of the <see cref="PartialOrderSamplingStrategy"/> class.
        /// </summary>
        internal PartialOrderSamplingStrategy(Configuration configuration, IRandomValueGenerator generator, bool isFair)
            : base(configuration, generator, isFair)
        {
            this.PrioritizedOperations = new List<ControlledOperation>();
        }

        /// <inheritdoc/>
        internal override bool InitializeNextIteration(uint iteration)
        {
            this.PrioritizedOperations.Clear();
            return base.InitializeNextIteration(iteration);
        }

        /// <inheritdoc/>
        internal override bool NextOperation(IEnumerable<ControlledOperation> ops, ControlledOperation current,
            bool isYielding, out ControlledOperation next)
        {
            if (this.IsFair && this.StepCount >= this.Configuration.MaxUnfairSchedulingSteps)
            {
                return base.NextOperation(ops, current, isYielding, out next);
            }

            // Set the priority of any new operations.
            this.SetNewOperationPriorities(ops, current);

            // Get the highest priority operation.
            ControlledOperation prioritized = this.GetOperationWithHighestPriority(ops);

            // Reset the priority of all racing operations.
            foreach (var op in ops)
            {
                if (op != prioritized && ControlledOperation.IsRacing(prioritized, op))
                {
                    if (Debug.IsEnabled)
                    {
                        Debug.WriteLine($"<ScheduleLog> {prioritized} and {op} are racing.");
                        Debug.WriteLine($"<ScheduleLog> Resetting priority of {op}.");
                    }

                    this.PrioritizedOperations.Remove(op);
                }
            }

            this.PrioritizedOperations.Remove(prioritized);
            Debug.WriteLine($"<ScheduleLog> Resetting priority of {prioritized}.");

            next = ops.First(op => op == prioritized);
            return true;
        }

        /// <summary>
        /// Returns the operation with the highest priority.
        /// </summary>
        private ControlledOperation GetOperationWithHighestPriority(IEnumerable<ControlledOperation> ops)
        {
            foreach (var operation in this.PrioritizedOperations)
            {
                if (ops.Any(op => op == operation))
                {
                    return operation;
                }
            }

            return null;
        }

        /// <summary>
        /// Sets a random priority to any new operations.
        /// </summary>
        private void SetNewOperationPriorities(IEnumerable<ControlledOperation> ops, ControlledOperation current)
        {
            this.PrioritizedOperations.RemoveAll(op => op.Status is OperationStatus.Completed);

            int count = this.PrioritizedOperations.Count;
            if (count is 0)
            {
                this.PrioritizedOperations.Add(current);
            }

            // Randomize the priority of all new operations.
            foreach (var op in ops.Where(o => !this.PrioritizedOperations.Contains(o)))
            {
                // Randomly choose a priority for this operation.
                int index = this.RandomValueGenerator.Next(this.PrioritizedOperations.Count + 1);
                this.PrioritizedOperations.Insert(index, op);
                Debug.WriteLine("<ScheduleLog> Assigned priority '{0}' for operation '{1}'.", index, op);
            }

            if (this.PrioritizedOperations.Count > count)
            {
                this.DebugPrintOperationPriorityList();
            }
        }

        /// <summary>
        /// Shuffles the specified range using the Fisher-Yates algorithm.
        /// </summary>
        /// <remarks>
        /// See https://en.wikipedia.org/wiki/Fisher%E2%80%93Yates_shuffle.
        /// </remarks>
        private IList<int> Shuffle(IEnumerable<int> range)
        {
            var result = new List<int>(range);
            for (int idx = result.Count - 1; idx >= 1; idx--)
            {
                int point = this.RandomValueGenerator.Next(result.Count);
                int temp = result[idx];
                result[idx] = result[point];
                result[point] = temp;
            }

            return result;
        }

        /// <inheritdoc/>
        internal override string GetDescription() =>
            $"PartialOrderSampling[fair:{this.IsFair},seed:{this.RandomValueGenerator.Seed}]";

        /// <inheritdoc/>
        internal override void Reset()
        {
            this.PrioritizedOperations.Clear();
            base.Reset();
        }

        /// <summary>
        /// Print the operation priority list, if debug is enabled.
        /// </summary>
        private void DebugPrintOperationPriorityList()
        {
            if (Debug.IsEnabled)
            {
                Debug.WriteLine("<ScheduleLog> Updated operation priority list: ");
                for (int idx = 0; idx < this.PrioritizedOperations.Count; idx++)
                {
                    var op = this.PrioritizedOperations[idx];
                    if (op.Status is OperationStatus.Enabled)
                    {
                        Debug.WriteLine("  |_ [{0}] operation with id '{1}' [enabled]", idx, op);
                    }
                    else if (op.Status != OperationStatus.Completed)
                    {
                        Debug.WriteLine("  |_ [{0}] operation with id '{1}'", idx, op);
                    }
                }
            }
        }
    }
}
