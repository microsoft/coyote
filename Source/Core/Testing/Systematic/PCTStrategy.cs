// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Coyote.IO;
using Microsoft.Coyote.Runtime;

namespace Microsoft.Coyote.Testing.Systematic
{
    /// <summary>
    /// A probabilistic priority-based scheduling strategy.
    /// </summary>
    /// <remarks>
    /// This strategy is described in the following paper:
    /// https://www.microsoft.com/en-us/research/wp-content/uploads/2016/02/asplos277-pct.pdf.
    /// </remarks>
    internal sealed class PCTStrategy : SystematicStrategy
    {
        /// <summary>
        /// List of prioritized operations.
        /// </summary>
        private readonly List<ControlledOperation> PrioritizedOperations;

        /// <summary>
        /// Scheduling points in the current execution where a priority change should occur.
        /// </summary>
        private readonly HashSet<int> PriorityChangePoints;

        /// <summary>
        /// Max number of potential priority change points across all iterations.
        /// </summary>
        private int MaxPriorityChangePoints;

        /// <summary>
        /// Max number of priority changes per iteration.
        /// </summary>
        private readonly int MaxPriorityChanges;

        /// <summary>
        /// Initializes a new instance of the <see cref="PCTStrategy"/> class.
        /// </summary>
        internal PCTStrategy(Configuration configuration, IRandomValueGenerator generator)
            : base(configuration, generator, false)
        {
            this.PrioritizedOperations = new List<ControlledOperation>();
            this.PriorityChangePoints = new HashSet<int>();
            this.MaxPriorityChangePoints = 0;
            this.MaxPriorityChanges = configuration.StrategyBound;
        }

        /// <inheritdoc/>
        internal override bool InitializeNextIteration(uint iteration)
        {
            // The first iteration has no knowledge of the execution, so only initialize from the second
            // iteration and onwards. Note that although we could initialize the first length based on a
            // heuristic, its not worth it, as the strategy will typically explore thousands of iterations,
            // plus its also interesting to explore a schedule with no forced priority switch points.
            if (iteration > 0)
            {
                this.MaxPriorityChangePoints = Math.Max(
                    this.MaxPriorityChangePoints, this.StepCount);
                this.StepCount = 0;

                this.PrioritizedOperations.Clear();
                this.PriorityChangePoints.Clear();

                var range = Enumerable.Range(0, this.MaxPriorityChangePoints);
                foreach (int point in this.Shuffle(range).Take(this.MaxPriorityChanges))
                {
                    this.PriorityChangePoints.Add(point);
                }
            }

            return true;
        }

        /// <inheritdoc/>
        internal override bool GetNextOperation(IEnumerable<ControlledOperation> ops, ControlledOperation current,
            bool isYielding, out ControlledOperation next)
        {
            this.SetNewPriorities(ops, current);
            if (ops.Skip(1).Any())
            {
                // There are at least two operations, so check if we should switch to a new priority.
                this.TryPrioritizeNextOperation(ops, current, isYielding);
            }

            ControlledOperation highestEnabledOperation = this.GetOperationWithHighestPriority(ops);
            next = ops.First(op => op.Equals(highestEnabledOperation));
            this.StepCount++;
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
        /// Sets the priority of new operations, if there are any.
        /// </summary>
        private void SetNewPriorities(IEnumerable<ControlledOperation> ops, ControlledOperation current)
        {
            if (this.PrioritizedOperations.Count is 0)
            {
                this.PrioritizedOperations.Add(current);
            }

            // Randomize the priority of all new operations.
            foreach (var op in ops.Where(op => !this.PrioritizedOperations.Contains(op)))
            {
                // Randomly choose a priority for this operation.
                int index = this.RandomValueGenerator.Next(this.PrioritizedOperations.Count) + 1;
                this.PrioritizedOperations.Insert(index, op);
                Debug.WriteLine("<ScheduleLog> Assigned priority '{0}' for operation '{1}'.", index, op.Name);
            }
        }

        /// <summary>
        /// Deprioritizes the enabled operation with the highest priority, if there is a
        /// priority change point installed on the current execution step.
        /// </summary>
        private void TryPrioritizeNextOperation(IEnumerable<ControlledOperation> ops,
            ControlledOperation current, bool isYielding)
        {
            ControlledOperation deprioritizedOperation = null;
            if (this.PriorityChangePoints.Contains(this.StepCount))
            {
                // This scheduling step was chosen as a priority change point.
                deprioritizedOperation = this.GetOperationWithHighestPriority(ops);
                Debug.WriteLine("<ScheduleLog> operation '{0}' is deprioritized.", deprioritizedOperation.Name);
            }
            else if (isYielding)
            {
                // The current operation is yielding its execution to the next prioritized operation.
                deprioritizedOperation = current;
                Debug.WriteLine("<ScheduleLog> operation '{0}' yields its priority.", deprioritizedOperation.Name);
            }

            if (deprioritizedOperation != null)
            {
                // Deprioritize the operation by putting it in the end of the list.
                this.PrioritizedOperations.Remove(deprioritizedOperation);
                this.PrioritizedOperations.Add(deprioritizedOperation);
            }
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
        internal override string GetDescription() =>
            $"pct[bound:{this.MaxPriorityChanges},seed:{this.RandomValueGenerator.Seed}]";

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
        internal override void Reset()
        {
            this.StepCount = 0;
            this.PrioritizedOperations.Clear();
            this.PriorityChangePoints.Clear();
        }
    }
}
