// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Coyote.IO;
using Microsoft.Coyote.Runtime;

namespace Microsoft.Coyote.Testing.Fuzzing
{
    /// <summary>
    /// A probabilistic priority-based scheduling strategy.
    /// </summary>
    /// <remarks>
    /// This strategy is described in the following paper:
    /// https://www.microsoft.com/en-us/research/wp-content/uploads/2016/02/asplos277-pct.pdf.
    /// </remarks>
    internal sealed class PCTStrategy : FuzzingStrategy
    {
        /// <summary>
        /// Random value generator.
        /// </summary>
        private readonly IRandomValueGenerator RandomValueGenerator;

        /// <summary>
        /// The maximum number of steps to explore.
        /// </summary>
        private readonly int MaxSteps;

        /// <summary>
        /// The number of exploration steps.
        /// </summary>
        private int StepCount;

        /// <summary>
        /// Max number of priority switch points.
        /// </summary>
        private readonly int MaxPrioritySwitchPoints;

        /// <summary>
        /// Approximate length of the schedule across all iterations.
        /// </summary>
        private int ScheduleLength;

        /// <summary>
        /// List of prioritized operations.
        /// </summary>
        private List<AsyncOperation> PrioritizedOperations;

        /// <summary>
        /// List of operations that have made critical delay requests post creation.
        /// </summary>
        private Dictionary<AsyncOperation, bool> CriticalDelayRequest;

        /// <summary>
        /// Scheduling points in the current execution where a priority change should occur.
        /// </summary>
        private readonly HashSet<int> PriorityChangePoints;

        /// <summary>
        /// Initializes a new instance of the <see cref="PCTStrategy"/> class.
        /// </summary>
        internal PCTStrategy(int maxSteps, IRandomValueGenerator generator, int maxPrioritySwitchPoints)
        {
            this.RandomValueGenerator = generator;
            this.MaxSteps = maxSteps;
            this.StepCount = 0;
            this.ScheduleLength = 0;
            this.MaxPrioritySwitchPoints = maxPrioritySwitchPoints;
            this.PrioritizedOperations = new List<AsyncOperation>();
            this.PriorityChangePoints = new HashSet<int>();
            this.CriticalDelayRequest = new Dictionary<AsyncOperation, bool>();
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
                this.ScheduleLength = Math.Max(this.ScheduleLength, this.StepCount);
                this.StepCount = 0;

                this.PrioritizedOperations.Clear();
                this.PriorityChangePoints.Clear();
                // Remove the operations that do not make critical delay requests
                this.CriticalDelayRequest = this.CriticalDelayRequest.Where(kvp => kvp.Value).ToDictionary(op => op.Key, op => op.Value);

                var range = Enumerable.Range(0, this.ScheduleLength);
                foreach (int point in this.Shuffle(range).Take(this.MaxPrioritySwitchPoints))
                {
                    this.PriorityChangePoints.Add(point);
                }

                this.DebugPrintPriorityChangePoints();
            }

            return true;
        }

        internal override bool GetNextDelay(IEnumerable<AsyncOperation> ops, AsyncOperation current,
            int maxValue, bool positiveDelay, bool isRecursive, out int next)
        {
            Console.WriteLine($"Current operation: {current.Name}");

            // if all operations except current have acquired statuses that are perpetually blocked, then assign zero delay
            if (ops.Where(op => !op.Equals(current) &&
                (op.Status is AsyncOperationStatus.Delayed || op.Status is AsyncOperationStatus.Enabled)).Count() is 0)
            {
                next = 0;
                // Count as step only if the operation belongs to the benchmark
                this.StepCount += (current.Name is "'Task(0)'") ? 0 : 1;
                return true;
            }

            // if the operation has not yet asked a critical delay request, include it as a potential operation
            // (critical request status: false). 
            // If it has been included already, then update its critical request status to true
            if (!isRecursive)
            {
                if (!this.CriticalDelayRequest.ContainsKey(current))
                {
                    this.CriticalDelayRequest[current] = false;
                }
                else
                {
                    this.CriticalDelayRequest[current] = true;
                }
            }

            // Console.WriteLine($"Freq: {this.CriticalDelayRequest[current].ToString()}");
            // Console.WriteLine($"OpId: {current.Id} Status: {current.Status}");

            // Update the priority list with new operations and return the highest priority operation
            this.UpdateAndGetLowestPriorityEnabledOperation(ops, current, out AsyncOperation HighestEnabledOperation);

            // Console.WriteLine($"{HighestEnabledOperation.Name}");

            // if the current operation is making a non-critical request (request for delay before creation) then assign zero delay. 
            // In a message passing system operation creation delay does not make a difference.
            if (!this.CriticalDelayRequest[current])
            {
                next = 0;
                this.StepCount += (current.Name is "'Task(0)'") ? 0 : 1;
                return true;
            }

            // Assign zero delay if the current operation is the highest enabled operation otherwise delay it.
            if (HighestEnabledOperation.Equals(current))
            {
                next = 0;
            }
            else
            {
                next = 1;
            }

            // if the current delay request is a primary one, then increase the step count indicating that the opn was scheduled.
            // The operation can get out of recursive loop only if it chooses a zero delay.
            // However, we increase step count even when the operation might never get out of the loop and hence get scheduled.
            // The only cost for this inaccuracy is redundant consumption of step count by at most #(total operations).
            if (!isRecursive)
            {
                this.StepCount += (current.Name is "'Task(0)'") ? 0 : 1;
            }

            return true;
        }

        internal override bool GetNextRecursiveDelayChoice(IEnumerable<AsyncOperation> ops, AsyncOperation current)
        {
            this.UpdateAndGetLowestPriorityEnabledOperation(ops, current, out AsyncOperation HighestEnabledOperation);

            if (HighestEnabledOperation.Equals(current))
            {
                return true;
            }

            return this.RandomValueGenerator.Next(2) is 0 ? true : false;
        }

        /// <inheritdoc/>
        internal bool UpdateAndGetLowestPriorityEnabledOperation(IEnumerable<AsyncOperation> ops, AsyncOperation current,
            out AsyncOperation HighestEnabledOperation)
        {
            HighestEnabledOperation = null;
            // enumerate all operations which are either sleeping or enabled and have made a critical delay request so far.
            var criticalOps = ops.Where(op
                => (op.Status is AsyncOperationStatus.Enabled || op.Status is AsyncOperationStatus.Delayed) && 
                    this.CriticalDelayRequest.ContainsKey(op) && this.CriticalDelayRequest[op]).ToList();

            if (criticalOps.Count is 0)
            {
                return false;
            }

            this.SetNewOperationPriorities(criticalOps, current);
            this.DeprioritizeEnabledOperationWithHighestPriority(criticalOps);
            this.DebugPrintOperationPriorityList();

            // HighestEnabledOperation = this.GetEnabledOperationWithLowestPriority(criticalOps); // Case: (n-1) PCT
            HighestEnabledOperation = this.GetEnabledOperationWithHighestPriority(criticalOps); // Case: (1) PCT
            return true;
        }

        /// <summary>
        /// Sets the priority of new operations, if there are any.
        /// </summary>
        private void SetNewOperationPriorities(List<AsyncOperation> ops, AsyncOperation current)
        {
            if (this.PrioritizedOperations.Count is 0)
            {
                this.PrioritizedOperations.Add(current);
            }

            // Randomize the priority of all new operations.
            foreach (var op in ops.Where(op => !this.PrioritizedOperations.Contains(op)))
            {
                // Randomly choose a priority for this operation between the lowest and the highest priority.
                int index = this.RandomValueGenerator.Next(this.PrioritizedOperations.Count - 1) + 1;
                this.PrioritizedOperations.Insert(index, op);
                Debug.WriteLine("<PCTLog> chose priority '{0}' for new operation '{1}'.", index, op.Name);
            }
        }

        /// <summary>
        /// Deprioritizes the enabled operation with the highest priority, if there is a
        /// priotity change point installed on the current execution step.
        /// </summary>
        private void DeprioritizeEnabledOperationWithHighestPriority(List<AsyncOperation> ops)
        {
            if (ops.Count <= 1)
            {
                // Nothing to do, there is only one enabled operation available.
                return;
            }

            AsyncOperation deprioritizedOperation = null;
            if (this.PriorityChangePoints.Contains(this.StepCount))
            {
                // This scheduling step was chosen as a priority switch point.
                deprioritizedOperation = this.GetEnabledOperationWithHighestPriority(ops);
                Debug.WriteLine("<PCTLog> operation '{0}' is deprioritized.", deprioritizedOperation.Name);
            }

            if (deprioritizedOperation != null)
            {
                // Deprioritize the operation by putting it in the end of the list.
                this.PrioritizedOperations.Remove(deprioritizedOperation);
                this.PrioritizedOperations.Add(deprioritizedOperation);
            }
        }

        /// <summary>
        /// Returns the enabled operation with the lowest priority.
        /// </summary>
        private AsyncOperation GetEnabledOperationWithLowestPriority(List<AsyncOperation> ops)
        {
            foreach (var entity in this.PrioritizedOperations.Reverse<AsyncOperation>())
            {
                if (ops.Any(m => m == entity))
                {
                    return entity;
                }
            }

            return null;
        }

        /// <summary>
        /// Returns the enabled operation with the highest priority.
        /// </summary>
        private AsyncOperation GetEnabledOperationWithHighestPriority(List<AsyncOperation> ops)
        {
            foreach (var entity in this.PrioritizedOperations)
            {
                if (ops.Any(m => m == entity))
                {
                    return entity;
                }
            }

            return null;
        }

        // /// <inheritdoc/>
        // internal override bool GetNextBooleanChoice(AsyncOperation current, int maxValue, out bool next)
        // {
        //     next = false;
        //     if (this.RandomValueGenerator.Next(maxValue) is 0)
        //     {
        //         next = true;
        //     }
        //     this.StepCount++;
        //     return true;
        // }

        // /// <inheritdoc/>
        // internal override bool GetNextIntegerChoice(AsyncOperation current, int maxValue, out int next)
        // {
        //     next = this.RandomValueGenerator.Next(maxValue);
        //     this.StepCount++;
        //     return true;
        // }

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
        internal override bool IsFair() => false;

        /// <inheritdoc/>
        internal override string GetDescription()
        {
            var text = $"pct[seed '" + this.RandomValueGenerator.Seed + "']";
            return text;
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

        // /// <inheritdoc/>
        // internal override void Reset()
        // {
        //     this.ScheduleLength = 0;
        //     this.StepCount = 0;
        //     this.PrioritizedOperations.Clear();
        //     this.PriorityChangePoints.Clear();
        // }

        /// <summary>
        /// Print the operation priority list, if debug is enabled.
        /// </summary>
        private void DebugPrintOperationPriorityList()
        {
            if (Debug.IsEnabled)
            {
                Debug.Write("<PCTLog> operation priority list: ");
                for (int idx = 0; idx < this.PrioritizedOperations.Count; idx++)
                {
                    if (idx < this.PrioritizedOperations.Count - 1)
                    {
                        Debug.Write("'{0}', ", this.PrioritizedOperations[idx].Name);
                    }
                    else
                    {
                        Debug.WriteLine("'{0}'.", this.PrioritizedOperations[idx].Name);
                    }
                }
            }
        }

        /// <summary>
        /// Print the priority change points, if debug is enabled.
        /// </summary>
        private void DebugPrintPriorityChangePoints()
        {
            if (Debug.IsEnabled)
            {
                // Sort them before printing for readability.
                var sortedChangePoints = this.PriorityChangePoints.ToArray();
                Array.Sort(sortedChangePoints);
                Debug.WriteLine("<PCTLog> next priority change points ('{0}' in total): {1}",
                    sortedChangePoints.Length, string.Join(", ", sortedChangePoints));
            }
        }
    }
}
