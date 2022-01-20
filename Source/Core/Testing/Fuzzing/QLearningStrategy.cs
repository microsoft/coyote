// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
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

        /// <summary>
        /// The path that is being executed during the current iteration. Each
        /// step of the execution is represented by a delay value and a value
        /// representing the program state after the delay happened.
        /// </summary>
        private readonly LinkedList<(int delay, int state)> ExecutionPath;

        /// <summary>
        /// Map of operation ids to their current activity status.
        /// </summary>
        private readonly ConcurrentDictionary<string, ActivityStatus> ActivityStatusMap;

        /// <summary>
        /// The previously chosen delay.
        /// </summary>
        private int PreviousDelay;

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
            this.ExecutionPath = new LinkedList<(int, int)>();
            this.ActivityStatusMap = new ConcurrentDictionary<string, ActivityStatus>();
            this.PreviousDelay = 0;
            this.LearningRate = 0.3;
            this.Gamma = 0.7;
            this.BasicActionReward = -1;
            this.Epochs = 0;
        }

        /// <inheritdoc/>
        internal override bool GetNextDelay(AsyncOperation current, int maxValue, out int next)
        {
            int state = this.CaptureExecutionStep(current);
            this.InitializeDelayQValues(state, maxValue);

            next = this.GetNextDelayByPolicy(state, maxValue);
            this.PreviousDelay = next;

            this.StepCount++;
            return true;
        }

        // /// <summary>
        // /// Notifies the activity status of the current operation.
        // /// </summary>
        // // internal override void NotifyActivityStatus(AsyncOperation current, ActivityStatus status)
        // // {
        // //     this.ActivityStatusMap.AddOrUpdate(current.Name, status, (id, old) => status);
        // // }

        /// <summary>
        /// Returns the next delay by drawing from the probability distribution
        /// over the specified state and range of delays.
        /// </summary>
        private int GetNextDelayByPolicy(int state, int maxValue)
        {
            var qValues = new List<double>(maxValue);
            for (int i = 0; i < maxValue; i++)
            {
                qValues.Add(this.OperationQTable[state][i]);
            }

            return this.ChooseQValueIndexFromDistribution(qValues);
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
        private int CaptureExecutionStep(AsyncOperation operation)
        {
            int state = ComputeStateHash(operation);

            // Update the list of chosen delays with the current state.
            this.ExecutionPath.AddLast((this.PreviousDelay, state));
            return state;
        }

        /// <summary>
        /// Computes a hash representing the current program state.
        /// </summary>
        private static int ComputeStateHash(AsyncOperation operation)
        {
            unchecked
            {
                int hash = 19;

                // Add the hash of the current operation.
                hash = (hash * 31) + operation.Name.GetHashCode();

                // foreach (var kvp in this.ActivityStatusMap)
                // {
                //     // Console.WriteLine($">>>>>>> Hashing: id {kvp.Key} - status {kvp.Value}");
                //     // int operationHash = 31 + kvp.Key.GetHashCode();
                //     // removing inactive actors from statehash
                //     // operationHash = !(kvp.Value is ActorStatus.Inactive) ? (operationHash * 31) + kvp.Value.GetHashCode() : operationHash;
                //     // including inactive actors in statehash
                //     // operationHash = (operationHash * 31) + kvp.Value.GetHashCode();
                //     // hash *= operationHash;
                // }

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

            for (int i = 0; i < maxValue; i++)
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
            this.PreviousDelay = 0;
            this.Epochs++;

            return base.InitializeNextIteration(iteration);
        }

        /// <summary>
        /// Learn Q values using data from the current execution.
        /// </summary>
        private void LearnQValues()
        {
            var pathBuilder = new System.Text.StringBuilder();

            int idx = 0;
            var node = this.ExecutionPath.First;
            while (node?.Next != null)
            {
                pathBuilder.Append($"{node.Value.delay},");

                var (_, state) = node.Value;
                var (nextDelay, nextState) = node.Next.Value;

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
        }

        /// <inheritdoc/>
        internal override string GetDescription() => $"RL[seed '{this.RandomValueGenerator.Seed}']";

        private enum ActivityStatus
        {
            ActiveAwake,
            ActiveSleeping,
            Inactive
        }
    }
}
