// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Runtime;

namespace Microsoft.Coyote.Testing.Fuzzing
{
    internal class QLearningStrategy : RandomStrategy
    {
        /// <summary>
        /// Previous choice of delay value.
        /// </summary>
        private int PreviousDelayValue;

        /// <summary>
        /// Map from program states to a map from next delays to their quality values.
        /// </summary>
        private readonly Dictionary<int, Dictionary<int, double>> OperationQTable;

        private readonly LinkedList<(int, AsyncOperationType?, int)> ExecutionPath;

        private readonly ConcurrentDictionary<string, ActorStatus> OperationStatuses;

        private Dictionary<string, int> OperationActivationTimes;

        private Dictionary<int, int> RelativeDelays;

        private readonly double LearningRate;

        private readonly double Gamma;

        protected readonly new int MaxSteps;

        private readonly double FailureInjectionReward;

        private int Epochs;

        private readonly int BasicActionReward;

        private Stopwatch sw;

        internal QLearningStrategy(int maxSteps, IRandomValueGenerator random)
            : base(maxSteps, random)
        {
            this.OperationQTable = new Dictionary<int, Dictionary<int, double>>();
            this.ExecutionPath = new LinkedList<(int, AsyncOperationType?, int)>();
            this.OperationStatuses = new ConcurrentDictionary<string, ActorStatus>();
            this.OperationActivationTimes = new Dictionary<string, int>();
            this.RelativeDelays = new Dictionary<int, int>();
            this.PreviousDelayValue = 0;
            this.LearningRate = 0.3;
            this.Gamma = 0.7;
            this.BasicActionReward = -1;
            this.Epochs = 0;
            this.MaxSteps = maxSteps;
            this.FailureInjectionReward = -1000;
            this.sw = Stopwatch.StartNew();
        }

        internal override bool GetNextDelay(int maxValue, out int next, AsyncOperation operation = null)
        {
            // Console.WriteLine($">>> GetNextDelay for {operation?.Id} from task {Task.CurrentId}");

            int state = this.CaptureExecutionStep(operation);
            this.InitializeDelayQValues(state, 44);

            // implementation for microseconds.
            next = this.GetNextDelayByPolicy(state, 44) - 39;

            if (operation != null)
            {
                var activationTime = (int)((this.sw.ElapsedTicks * 40000) / Stopwatch.Frequency) + (next <= 0 ? (next / -1) : next * 40);
                var actor = (operation as ActorOperation).Actor;
                this.OperationActivationTimes[actor.Id.RLId] = activationTime;
            }

            this.PreviousDelayValue = next + 39;
            this.StepCount++;
            return true;
        }

        internal override void NotifyActorStatus(AsyncOperation operation, ActorStatus status)
        {
            if (operation != null)
            {
                var actor = (operation as ActorOperation).Actor;
                // Console.WriteLine($">>> NotifyActorStatus: {actor.Id} (rl: {actor.Id.RLId}) is {state}");
                if (this.OperationStatuses.ContainsKey(actor.Id.RLId))
                {
                    this.OperationStatuses[actor.Id.RLId] = status;
                }
                else
                {
                    this.OperationStatuses.TryAdd(actor.Id.RLId, status);
                }
            }
        }

        internal int CaptureExecutionStep(AsyncOperation operation)
        {
            // Console.WriteLine($">>> CaptureExecutionStep:");
            // Console.WriteLine($">>>>> OperationStatuses: {this.OperationStatuses.Count}");

            int state = this.ComputeStateHash(operation);
            // Console.WriteLine($">>>>> check operation type -> before");
            if (operation != null)
            {
                // Console.WriteLine($">>>>> operation: null -> before");
                this.ExecutionPath.AddLast((this.PreviousDelayValue, operation.Type, state));
                // Console.WriteLine($">>>>> operation: null -> after");
            }
            else
            {
                // Console.WriteLine($">>>>> operation: non-null -> before");
                this.ExecutionPath.AddLast((this.PreviousDelayValue, null, state));
                // Console.WriteLine($">>>>> operation: non-null -> after");
            }

            // Console.WriteLine($">>>>> State: {state}");
            return state;
        }

        private int ComputeStateHash(AsyncOperation operation)
        {
            unchecked
            {
                int hash = 19;

                foreach (var kvp in this.OperationStatuses)
                {
                    // Console.WriteLine($">>>>>>> Hashing: id {kvp.Key} - status {kvp.Value}");
                    // int operationHash = 31 + kvp.Key.GetHashCode();
                    // removing inactive actors from statehash
                    // operationHash = !(kvp.Value is ActorStatus.Inactive) ? (operationHash * 31) + kvp.Value.GetHashCode() : operationHash;
                    // including inactive actors in statehash
                    // operationHash = (operationHash * 31) + kvp.Value.GetHashCode();
                    // hash *= operationHash;
                }

                // int operationHash = 31;

                // Include the order of activation in the statehash
                // foreach (var kvp in this.OperationActivationTimes.OrderBy(x => x.Value))
                // {
                //     operationHash = (operationHash * 31) + kvp.Key.GetHashCode();
                //     Console.WriteLine(kvp.Value);
                // }

                // Include the first actor to activate in the statehash)
                // if (this.OperationActivationTimes.Count() > 1)
                // {
                //     hash = (hash * 31) + this.OperationActivationTimes.OrderBy(x => x.Value).Skip(1).First().Key.GetHashCode();
                // }

                // hash *= operationHash;

                // Changed the access of HashedState.
                if (operation != null)
                {
                    hash = (hash * 31) + (operation as ActorOperation).Actor.GetHashedState(); // Local
                    // hash = (hash * 31) + (operation as ActorOperation).Actor.Context.Runtime.CurrentHashedState; // Global
                    if ((operation as ActorOperation).Actor.HashedState != 0)
                    {
                        // hash = (hash * 31) + (operation as ActorOperation).Actor.HashedState; // Custom-only.
                    }
                }

                return hash;
            }
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
            // Reset the Root ID counter for actors created with no parents.
            ActorId.RootIdCounter = 0;
            this.sw.Restart();

            // var sb = new StringBuilder();
            // sb.AppendLine("OperationQTable: ");
            Console.WriteLine($"OperationQTable size: {this.OperationQTable.Count()}");
            // foreach (KeyValuePair<int, Dictionary<int, double>> kvp1 in this.OperationQTable)
            // {
            //     foreach (KeyValuePair<int, double> kvp2 in kvp1.Value)
            //     {
            //         sb.AppendLine($"{kvp1.Key}: {kvp2.Key}, {kvp2.Value}");
            //     }
            // }

            // sb.Append("ExecutionPath: ");
            // var node = this.ExecutionPath.First;
            // while (node != null)
            // {
            //     var value = node.Value;
            //     sb.Append($"({value.Item1}, {value.Item3}), ");
            // }

            // Console.WriteLine(sb.ToString());

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
                if (this.OperationQTable.TryGetValue(nextState, out Dictionary<int, double> qValues))
                {
                    foreach (var nextOpQValuePair in qValues)
                    {
                        if (nextOpQValuePair.Value > maxQ)
                        {
                            maxQ = nextOpQValuePair.Value;
                        }
                    }
                }

                // Compute the reward.
                double reward = nextType == AsyncOperationType.InjectFailure ?
                    this.FailureInjectionReward : this.BasicActionReward;
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
