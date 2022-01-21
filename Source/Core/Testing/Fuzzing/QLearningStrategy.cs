// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Coyote.Runtime;

namespace Microsoft.Coyote.Testing.Fuzzing
{
    /// <summary>
    /// A probabilistic fuzzing strategy that uses Q-learning.
    /// </summary>
    internal class QLearningStrategy : RandomStrategy
    {
        /// <summary>
        /// Map from program states to a map from next delays to their quality values.
        /// </summary>
        private readonly Dictionary<int, Dictionary<int, double>> OperationQTable;

        private readonly HashSet<int> UniqueStates;

        /// <summary>
        /// The path that is being executed during the current iteration. Each step
        /// of the execution is represented by an operation requesting a delay, the
        /// chosen delay value and a value representing the program state after the
        /// delay happened.
        /// </summary>
        private readonly LinkedList<(string op, int delay, int state)> ExecutionPath;

        /// <summary>
        /// Map from operations to number of steps they are currently delayed.
        /// </summary>
        private readonly Dictionary<string, ulong> DelayedOperationSteps;

        /// <summary>
        /// The last delayed operation.
        /// </summary>
        private (string op, int delay) LastDelayedOperation;

        /// <summary>
        /// The value of the learning rate.
        /// </summary>
        private readonly double LearningRate;

        /// <summary>
        /// The value of the discount factor.
        /// </summary>
        private readonly double Gamma;

        /// <summary>
        /// The basic action reward.
        /// </summary>
        private readonly double BasicActionReward;

        /// <summary>
        /// The number of explored executions.
        /// </summary>
        private int Epochs;

        /// <summary>
        /// Initializes a new instance of the <see cref="QLearningStrategy"/> class.
        /// It uses the specified random number generator.
        /// </summary>
        internal QLearningStrategy(int maxSteps, IRandomValueGenerator random)
            : base(maxSteps, random)
        {
            this.OperationQTable = new Dictionary<int, Dictionary<int, double>>();
            this.UniqueStates = new HashSet<int>();
            this.ExecutionPath = new LinkedList<(string, int, int)>();
            this.DelayedOperationSteps = new Dictionary<string, ulong>();
            this.LastDelayedOperation = ("Task(0)", 0);
            this.LearningRate = 0.3;
            this.Gamma = 0.7;
            this.BasicActionReward = -1;
            this.Epochs = 0;
        }

        /// <inheritdoc/>
        internal override bool GetNextDelay(IEnumerable<AsyncOperation> ops, AsyncOperation current,
            int maxValue, out int next)
        {
            int state = this.CaptureExecutionStep(ops, current);
            this.InitializeDelayQValues(state, maxValue);

            next = this.GetNextDelayByPolicy(state);
            this.LastDelayedOperation = (current.Name, next);

            this.StepCount++;
            return true;
        }

        /// <summary>
        /// Returns the next delay by drawing from the probability distribution
        /// over the specified state and range of delays.
        /// </summary>
        private int GetNextDelayByPolicy(int state)
        {
            var delays = new List<int>();
            var qValues = new List<double>();
            foreach (var pair in this.OperationQTable[state])
            {
                delays.Add(pair.Key);
                qValues.Add(pair.Value);
            }

            int idx = this.ChooseQValueIndexFromDistribution(qValues);
            return delays[idx];
        }

        /// <summary>
        /// Returns an index of a Q value by drawing from the probability distribution
        /// over the specified Q values.
        /// </summary>
        private int ChooseQValueIndexFromDistribution(List<double> qValues)
        {
            double sum = 0;
            for (int i = 0; i < qValues.Count; i++)
            {
                qValues[i] = Math.Exp(qValues[i]);
                sum += qValues[i];
            }

            for (int i = 0; i < qValues.Count; i++)
            {
                qValues[i] /= sum;
            }

            sum = 0;

            // First, change the shape of the distribution probability array to be cumulative.
            // For example, instead of [0.1, 0.2, 0.3, 0.4], we get [0.1, 0.3, 0.6, 1.0].
            var cumulative = qValues.Select(c =>
            {
                var result = c + sum;
                sum += c;
                return result;
            }).ToList();

            // Generate a random double value between 0.0 to 1.0.
            var rvalue = this.RandomValueGenerator.NextDouble();

            // Find the first index in the cumulative array that is greater
            // or equal than the generated random value.
            var idx = cumulative.BinarySearch(rvalue);

            if (idx < 0)
            {
                // If an exact match is not found, List.BinarySearch will return the index
                // of the first items greater than the passed value, but in a specific form
                // (negative) we need to apply ~ to this negative value to get real index.
                idx = ~idx;
            }

            if (idx > cumulative.Count - 1)
            {
                // Very rare case when probabilities do not sum to 1 because of
                // double precision issues (so sum is 0.999943 and so on).
                idx = cumulative.Count - 1;
            }

            return idx;
        }

        /// <summary>
        /// Captures metadata related to the current execution step, and returns
        /// a value representing the current program state.
        /// </summary>
        private int CaptureExecutionStep(IEnumerable<AsyncOperation> ops, AsyncOperation current)
        {
            this.UpdateDelayedOperationSteps(ops);

            int state = this.ComputeProgramState(ops, current);
            Console.WriteLine($">---> {current.Name}: state: {state}");

            // Update the execution path with the current state.
            this.ExecutionPath.AddLast((this.LastDelayedOperation.op, this.LastDelayedOperation.delay, state));
            return state;
        }

        /// <summary>
        /// Computes the number of steps that each operation has been delayed.
        /// </summary>
        private void UpdateDelayedOperationSteps(IEnumerable<AsyncOperation> ops)
        {
            foreach (var op in ops.OrderBy(op => op.Name))
            {
                if (!this.DelayedOperationSteps.ContainsKey(op.Name))
                {
                    this.DelayedOperationSteps.Add(op.Name, 0);
                }

                if (op.Status is AsyncOperationStatus.Delayed)
                {
                    this.DelayedOperationSteps[op.Name]++;
                }
                else
                {
                    this.DelayedOperationSteps[op.Name] = 0;
                }

                Console.WriteLine($"  |---> {op.Name}: delayed: {this.DelayedOperationSteps[op.Name]}");
            }
        }

        /// <summary>
        /// Computes the current program state.
        /// </summary>
        private int ComputeProgramState(IEnumerable<AsyncOperation> ops, AsyncOperation current)
        {
            unchecked
            {
                int hash = 19;

                // Add the hash of the current operation.
                var pre = hash;
                hash = (hash * 31) + current.Name.GetHashCode();
                hash = pre;

                // Add the hash of each operation.
                foreach (var op in ops.OrderBy(op => op.Name))
                {
                    Console.WriteLine($"  |---> {op.Name}: status: {op.Status}");
                    int operationHash = 31 + op.GetHashedState(SchedulingPolicy.Fuzzing);
                    this.UniqueStates.Add(op.GetHashedState(SchedulingPolicy.Fuzzing));
                    operationHash = (operationHash * 31) + this.DelayedOperationSteps[op.Name].GetHashCode();
                    hash *= operationHash;
                }

                return hash;
            }
        }

        /// <summary>
        /// Initializes the Q values of all delays that can be chosen at the specified state
        /// that have not been previously encountered.
        /// </summary>
        private void InitializeDelayQValues(int state, int maxValue)
        {
            if (!this.OperationQTable.TryGetValue(state, out Dictionary<int, double> qValues))
            {
                qValues = new Dictionary<int, double>();
                this.OperationQTable.Add(state, qValues);
            }

            for (int i = 0; i <= maxValue; i += maxValue)
            {
                if (!qValues.ContainsKey(i))
                {
                    qValues.Add(i, 0);
                }
            }
        }

        /// <inheritdoc/>
        internal override bool InitializeNextIteration(uint iteration)
        {
            this.LearnQValues();
            this.ExecutionPath.Clear();
            this.DelayedOperationSteps.Clear();
            this.LastDelayedOperation = ("Task(0)", 0);
            this.Epochs++;

            return base.InitializeNextIteration(iteration);
        }

        /// <summary>
        /// Learn Q values using data from the current execution.
        /// </summary>
        private void LearnQValues()
        {
            // var pathBuilder = new System.Text.StringBuilder();

            int idx = 0;
            var node = this.ExecutionPath.First;
            while (node?.Next != null)
            {
                // pathBuilder.Append($"({node.Value.delay},{node.Value.state}), ");

                var (_, _, state) = node.Value;
                var (nextOp, nextDelay, nextState) = node.Next.Value;

                // Compute the max Q value.
                double maxQ = double.MinValue;
                foreach (var nextOpQValuePair in this.OperationQTable[nextState])
                {
                    if (nextOpQValuePair.Value > maxQ)
                    {
                        maxQ = nextOpQValuePair.Value;
                    }
                }

                // Compute the reward.
                double reward = this.BasicActionReward;
                if (reward > 0)
                {
                    // The reward has underflowed.
                    reward = double.MinValue;
                }

                // Get the delays that are available from the current execution step.
                var currOpQValues = this.OperationQTable[state];
                if (!currOpQValues.ContainsKey(nextDelay))
                {
                    currOpQValues.Add(nextDelay, 0);
                }

                // Update the Q value of the next delay.
                // Q = [(1-a) * Q]  +  [a * (rt + (g * maxQ))]
                currOpQValues[nextDelay] = ((1 - this.LearningRate) * currOpQValues[nextDelay]) +
                    (this.LearningRate * (reward + (this.Gamma * maxQ)));

                node = node.Next;
                idx++;
            }

            Console.WriteLine($"Visited {this.OperationQTable.Count} states.");
            Console.WriteLine($"Found {this.UniqueStates.Count} unique custom states.");
            // Console.WriteLine(pathBuilder.ToString());
        }

        /// <inheritdoc/>
        internal override string GetDescription() => $"RL[seed '{this.RandomValueGenerator.Seed}']";
    }
}
