// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Runtime;

namespace Microsoft.Coyote.Testing.Fuzzing
{
    internal class QLearningStrategy : RandomStrategy
    {
        private int PreviousDelayValue;

        private readonly Dictionary<int, Dictionary<int, double>> OperationQTable;

        private readonly LinkedList<(int, AsyncOperationType, int)> ExecutionPath;

        private readonly double LearningRate;

        private readonly double Gamma;

        protected readonly new int MaxSteps;

        private readonly Dictionary<int, ulong> TransitionFrequencies;

        private readonly double FailureInjectionReward;

        private int Epochs;

        private readonly int BasicActionReward;

        internal QLearningStrategy(int maxSteps, IRandomValueGenerator random)
            : base(maxSteps, random)
        {
            this.OperationQTable = new Dictionary<int, Dictionary<int, double>>();
            this.ExecutionPath = new LinkedList<(int, AsyncOperationType, int)>();
            this.PreviousDelayValue = 0;
            this.LearningRate = 0.3;
            this.Gamma = 0.7;
            this.BasicActionReward = -1;
            this.Epochs = 0;
            this.MaxSteps = maxSteps;
            this.TransitionFrequencies = new Dictionary<int, ulong>();
            this.FailureInjectionReward = -1000;
        }

        internal override bool GetNextDelay(int maxValue, out int next, FuzzingState currentstate, AsyncOperation operation)
        {
            int state = this.CaptureExecutionStep(currentstate, operation);
            this.InitializeDelayQValues(state, maxValue);

            next = this.GetNextDelayByPolicy(state, maxValue);
            // TODO : workaround for this.
            int delay = next;

            if (next != 0)
            {
                currentstate.Snooze((operation as ActorOperation).Actor);
                Task.Run(async () =>
                {
                    await Task.Delay(delay);
                    // TODO : The fuzzingstate will wake up the actor few millisecs before the actor is really awake. This means that a getnxtdelay call during this sweet time will result in incorrect state computation.
                    currentstate.Wake((operation as ActorOperation).Actor);
                });
            }

            this.PreviousDelayValue = next;
            this.StepCount++;
            return true;
        }

        internal int CaptureExecutionStep(FuzzingState currentstate, AsyncOperation operation)
        {
            int state = currentstate.GetHashedState(operation);

            this.ExecutionPath.AddLast((this.PreviousDelayValue, operation.Type, state));

            return state;
        }

        internal void InitializeDelayQValues(int state, int maxValue)
        {
            if (!this.OperationQTable.TryGetValue(state, out Dictionary<int, double> qValues))
            {
                qValues = new Dictionary<int, double>();
                this.OperationQTable.Add(state, qValues);
            }

            for (int i = 0;  i < maxValue; i++)
            {
                if (!qValues.ContainsKey(i))
                {
                    qValues.Add(i, 0);
                }
            }
        }

        internal int GetNextDelayByPolicy(int state, int maxValue)
        {
            var qValues = new List<double>(maxValue);
            for (int i = 0; i < maxValue; i++)
            {
                qValues.Add(this.OperationQTable[state][i]);
            }

            return this.ChooseQValueIndexFromDistribution(qValues);
        }

        internal int ChooseQValueIndexFromDistribution(List<double> qValues)
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

        internal override bool InitializeNextIteration(uint iteration)
        {
            this.LearnQValues();
            this.ExecutionPath.Clear();
            this.PreviousDelayValue = 0;
            this.Epochs++;
            this.StepCount = 0;

            return true;
        }

        private void LearnQValues()
        {
            var pathBuilder = new System.Text.StringBuilder();

            int idx = 0;
            var node = this.ExecutionPath.First;
            while (node?.Next != null)
            {
                pathBuilder.Append($"{node.Value},");

                var (_, _, state) = node.Value;
                var (nextDelay, nextType, nextState) = node.Next.Value;

                // Compute the max Q value.
                double maxQ = double.MinValue;
                foreach (var nextOpQValuePair in this.OperationQTable[nextState])
                {
                    if (nextOpQValuePair.Value > maxQ)
                    {
                        maxQ = nextOpQValuePair.Value;
                    }
                }

                // Compute the reward. Program states that are visited with higher frequency result into lesser rewards.
                var freq = this.TransitionFrequencies[nextState];
                double reward = (nextType == AsyncOperationType.InjectFailure ?
                    this.FailureInjectionReward : this.BasicActionReward) * freq;
                if (reward > 0)
                {
                    // The reward has underflowed.
                    reward = double.MinValue;
                }

                // Get the operations that are available from the current execution step.
                var currOpQValues = this.OperationQTable[state];
                if (!currOpQValues.ContainsKey(nextDelay))
                {
                    currOpQValues.Add(nextDelay, 0);
                }

                // Update the Q value of the next operation.
                // Q = [(1-a) * Q]  +  [a * (rt + (g * maxQ))]
                currOpQValues[nextDelay] = ((1 - this.LearningRate) * currOpQValues[nextDelay]) +
                    (this.LearningRate * (reward + (this.Gamma * maxQ)));

                node = node.Next;
                idx++;
            }
        }
    }
}
