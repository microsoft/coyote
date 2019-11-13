// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Coyote.IO;
using Microsoft.Coyote.TestingServices.StateCaching;
using Microsoft.Coyote.TestingServices.Tracing.Schedule;

using Monitor = Microsoft.Coyote.Specifications.Monitor;

namespace Microsoft.Coyote.TestingServices.Scheduling.Strategies
{
    /// <summary>
    /// Strategy for detecting liveness property violations using partial state-caching
    /// and cycle-replaying. It contains a nested <see cref="ISchedulingStrategy"/> that
    /// is used for scheduling decisions. Note that liveness property violations are
    /// checked only if the nested strategy is fair.
    /// </summary>
    internal sealed class CycleDetectionStrategy : LivenessCheckingStrategy
    {
        /// <summary>
        /// The state cache of the program.
        /// </summary>
        private readonly StateCache StateCache;

        /// <summary>
        /// The schedule trace of the program.
        /// </summary>
        private readonly ScheduleTrace ScheduleTrace;

        /// <summary>
        /// Monitors that are stuck in the hot state
        /// for the duration of the latest found
        /// potential cycle.
        /// </summary>
        private HashSet<Monitor> HotMonitors;

        /// <summary>
        /// The latest found potential cycle.
        /// </summary>
        private readonly List<ScheduleStep> PotentialCycle;

        /// <summary>
        /// Fingerprints captured in the latest potential cycle.
        /// </summary>
        private readonly HashSet<Fingerprint> PotentialCycleFingerprints;

        /// <summary>
        /// Is strategy trying to replay a potential cycle.
        /// </summary>
        private bool IsReplayingCycle;

        /// <summary>
        /// A counter that increases in each step of the execution,
        /// as long as the Coyote program remains in the same cycle,
        /// with the liveness monitors at the hot state.
        /// </summary>
        private int LivenessTemperature;

        /// <summary>
        /// The index of the last scheduling step in
        /// the currently detected cycle.
        /// </summary>
        private int EndOfCycleIndex;

        /// <summary>
        /// The current cycle index.
        /// </summary>
        private int CurrentCycleIndex;

        /// <summary>
        /// Nondeterminitic seed.
        /// </summary>
        private readonly int Seed;

        /// <summary>
        /// Randomizer.
        /// </summary>
        private readonly IRandomNumberGenerator Random;

        /// <summary>
        /// Map of fingerprints to schedule step indexes.
        /// </summary>
        private readonly Dictionary<Fingerprint, List<int>> FingerprintIndexMap;

        /// <summary>
        /// Initializes a new instance of the <see cref="CycleDetectionStrategy"/> class.
        /// </summary>
        internal CycleDetectionStrategy(Configuration configuration, StateCache cache, ScheduleTrace trace,
            List<Monitor> monitors, ISchedulingStrategy strategy)
            : base(configuration, monitors, strategy)
        {
            this.StateCache = cache;
            this.ScheduleTrace = trace;

            this.HotMonitors = new HashSet<Monitor>();
            this.PotentialCycle = new List<ScheduleStep>();
            this.PotentialCycleFingerprints = new HashSet<Fingerprint>();

            this.LivenessTemperature = 0;
            this.EndOfCycleIndex = 0;
            this.CurrentCycleIndex = 0;

            this.Seed = this.Configuration.RandomSchedulingSeed ?? DateTime.Now.Millisecond;
            this.Random = new DefaultRandomNumberGenerator(this.Seed);

            this.FingerprintIndexMap = new Dictionary<Fingerprint, List<int>>();
        }

        /// <summary>
        /// Returns the next asynchronous operation to schedule.
        /// </summary>
        public override bool GetNext(out IAsyncOperation next, List<IAsyncOperation> ops, IAsyncOperation current)
        {
            this.CaptureAndCheckProgramState();

            if (this.IsReplayingCycle)
            {
                var enabledOperations = ops.Where(op => op.Status is AsyncOperationStatus.Enabled).ToList();
                if (enabledOperations.Count == 0)
                {
                    next = null;
                    return false;
                }

                ScheduleStep nextStep = this.PotentialCycle[this.CurrentCycleIndex];
                if (nextStep.Type != ScheduleStepType.SchedulingChoice)
                {
                    Debug.WriteLine("<LivenessDebug> Trace is not reproducible: next step is not an operation.");
                    this.EscapeUnfairCycle();
                    return this.SchedulingStrategy.GetNext(out next, ops, current);
                }

                Debug.WriteLine("<LivenessDebug> Replaying '{0}' '{1}'.", nextStep.Index, nextStep.ScheduledOperationId);

                next = enabledOperations.FirstOrDefault(choice => choice.Id == nextStep.ScheduledOperationId);
                if (next is null)
                {
                    Debug.WriteLine("<LivenessDebug> Trace is not reproducible: cannot detect actor with id '{0}'.", nextStep.ScheduledOperationId);
                    this.EscapeUnfairCycle();
                    return this.SchedulingStrategy.GetNext(out next, ops, current);
                }

                this.SchedulingStrategy.ForceNext(next, ops, current);

                this.CurrentCycleIndex++;
                if (this.CurrentCycleIndex == this.PotentialCycle.Count)
                {
                    this.CurrentCycleIndex = 0;
                }

                return true;
            }
            else
            {
                return this.SchedulingStrategy.GetNext(out next, ops, current);
            }
        }

        /// <summary>
        /// Returns the next boolean choice.
        /// </summary>
        public override bool GetNextBooleanChoice(int maxValue, out bool next)
        {
            this.CaptureAndCheckProgramState();

            if (this.IsReplayingCycle)
            {
                ScheduleStep nextStep = this.PotentialCycle[this.CurrentCycleIndex];
                if ((nextStep.Type == ScheduleStepType.SchedulingChoice) || nextStep.BooleanChoice is null)
                {
                    Debug.WriteLine("<LivenessDebug> Trace is not reproducible: next step is not a nondeterministic boolean choice.");
                    this.EscapeUnfairCycle();
                    return this.SchedulingStrategy.GetNextBooleanChoice(maxValue, out next);
                }

                Debug.WriteLine("<LivenessDebug> Replaying '{0}' '{1}'.", nextStep.Index, nextStep.BooleanChoice.Value);

                next = nextStep.BooleanChoice.Value;

                this.SchedulingStrategy.ForceNextBooleanChoice(maxValue, next);

                this.CurrentCycleIndex++;
                if (this.CurrentCycleIndex == this.PotentialCycle.Count)
                {
                    this.CurrentCycleIndex = 0;
                }

                return true;
            }
            else
            {
                return this.SchedulingStrategy.GetNextBooleanChoice(maxValue, out next);
            }
        }

        /// <summary>
        /// Returns the next integer choice.
        /// </summary>
        public override bool GetNextIntegerChoice(int maxValue, out int next)
        {
            this.CaptureAndCheckProgramState();

            if (this.IsReplayingCycle)
            {
                ScheduleStep nextStep = this.PotentialCycle[this.CurrentCycleIndex];
                if (nextStep.Type != ScheduleStepType.NondeterministicChoice ||
                    nextStep.IntegerChoice is null)
                {
                    Debug.WriteLine("<LivenessDebug> Trace is not reproducible: next step is not a nondeterministic integer choice.");
                    this.EscapeUnfairCycle();
                    return this.SchedulingStrategy.GetNextIntegerChoice(maxValue, out next);
                }

                Debug.WriteLine("<LivenessDebug> Replaying '{0}' '{1}'.", nextStep.Index, nextStep.IntegerChoice.Value);

                next = nextStep.IntegerChoice.Value;

                this.SchedulingStrategy.ForceNextIntegerChoice(maxValue, next);

                this.CurrentCycleIndex++;
                if (this.CurrentCycleIndex == this.PotentialCycle.Count)
                {
                    this.CurrentCycleIndex = 0;
                }

                return true;
            }
            else
            {
                return this.SchedulingStrategy.GetNextIntegerChoice(maxValue, out next);
            }
        }

        /// <summary>
        /// Prepares for the next scheduling iteration. This is invoked
        /// at the end of a scheduling iteration. It must return false
        /// if the scheduling strategy should stop exploring.
        /// </summary>
        /// <returns>True to start the next iteration.</returns>
        public override bool PrepareForNextIteration()
        {
            if (this.IsReplayingCycle)
            {
                this.CurrentCycleIndex = 0;
                return true;
            }
            else
            {
                return base.PrepareForNextIteration();
            }
        }

        /// <summary>
        /// Resets the scheduling strategy. This is typically invoked by
        /// parent strategies to reset child strategies.
        /// </summary>
        public override void Reset()
        {
            if (this.IsReplayingCycle)
            {
                this.CurrentCycleIndex = 0;
            }
            else
            {
                base.Reset();
            }
        }

        /// <summary>
        /// True if the scheduling strategy has reached the max
        /// scheduling steps for the given scheduling iteration.
        /// </summary>
        public override bool HasReachedMaxSchedulingSteps()
        {
            if (this.IsReplayingCycle)
            {
                return false;
            }
            else
            {
                return base.HasReachedMaxSchedulingSteps();
            }
        }

        /// <summary>
        /// Checks if this is a fair scheduling strategy.
        /// </summary>
        public override bool IsFair()
        {
            if (this.IsReplayingCycle)
            {
                return true;
            }
            else
            {
                return base.IsFair();
            }
        }

        /// <summary>
        /// Captures the program state and checks for liveness violations.
        /// </summary>
        private void CaptureAndCheckProgramState()
        {
            if (this.ScheduleTrace.Count == 0)
            {
                return;
            }

            if (this.Configuration.SafetyPrefixBound <= this.GetScheduledSteps())
            {
                bool stateExists = this.StateCache.CaptureState(out State _, out Fingerprint fingerprint,
                    this.FingerprintIndexMap, this.ScheduleTrace.Peek(), this.Monitors);
                if (stateExists)
                {
                    Debug.WriteLine("<LivenessDebug> Detected potential infinite execution.");
                    this.CheckLivenessAtTraceCycle(this.FingerprintIndexMap[fingerprint]);
                }
            }

            if (this.PotentialCycle.Count > 0)
            {
                // Only check for a liveness property violation
                // if there is a potential cycle.
                this.CheckLivenessTemperature();
            }
        }

        /// <summary>
        /// Checks the liveness temperature of each monitor, and
        /// reports an error if one of the liveness monitors has
        /// passed the temperature threshold.
        /// </summary>
        private void CheckLivenessTemperature()
        {
            var coldMonitors = this.HotMonitors.Where(m => m.IsInColdState()).ToList();
            if (coldMonitors.Count > 0)
            {
                if (Debug.IsEnabled)
                {
                    foreach (var coldMonitor in coldMonitors)
                    {
                        Debug.WriteLine(
                            "<LivenessDebug> Trace is not reproducible: monitor {0} transitioned to a cold state.",
                            coldMonitor.Id);
                    }
                }

                this.EscapeUnfairCycle();
                return;
            }

            var randomWalkScheduleTrace = this.ScheduleTrace.Where(val => val.Index > this.EndOfCycleIndex);
            foreach (var step in randomWalkScheduleTrace)
            {
                State state = step.State;
                if (!this.PotentialCycleFingerprints.Contains(state.Fingerprint))
                {
                    if (Debug.IsEnabled)
                    {
                        state.PrettyPrint();
                        Debug.WriteLine("<LivenessDebug> Detected a state that does not belong to the potential cycle.");
                    }

                    this.EscapeUnfairCycle();
                    return;
                }
            }

            // Increments the temperature of each monitor.
            // foreach (var monitor in HotMonitors)
            // {
            //    string message = IO.Utilities.Format("Monitor '{0}' detected infinite execution that " +
            //        "violates a liveness property.", monitor.GetType().Name);
            //    Runtime.Scheduler.NotifyAssertionFailure(message, false);
            // }
            this.LivenessTemperature++;
            if (this.LivenessTemperature > this.Configuration.LivenessTemperatureThreshold)
            {
                foreach (var monitor in this.HotMonitors)
                {
                    monitor.CheckLivenessTemperature(this.LivenessTemperature);
                }

                // foreach (var monitor in HotMonitors)
                // {
                //    string message = IO.Utilities.Format("Monitor '{0}' detected infinite execution that " +
                //        "violates a liveness property.", monitor.GetType().Name);
                //    Runtime.Scheduler.NotifyAssertionFailure(message, false);
                // }

                // Runtime.Scheduler.Stop();
            }
        }

        /// <summary>
        /// Checks liveness at a schedule trace cycle.
        /// </summary>
        /// <param name="indices">Indices corresponding to the fingerprint of root.</param>
        private void CheckLivenessAtTraceCycle(List<int> indices)
        {
            // If there is a potential cycle found, do not create a new one until the
            // liveness checker has finished exploring the current cycle.
            if (this.PotentialCycle.Count > 0)
            {
                return;
            }

            var checkIndexRand = indices[indices.Count - 2];
            var index = this.ScheduleTrace.Count - 1;

            for (int i = checkIndexRand + 1; i <= index; i++)
            {
                var scheduleStep = this.ScheduleTrace[i];
                this.PotentialCycle.Add(scheduleStep);
                this.PotentialCycleFingerprints.Add(scheduleStep.State.Fingerprint);
                Debug.WriteLine(
                    "<LivenessDebug> Cycle contains {0} with {1}.",
                    scheduleStep.Type, scheduleStep.State.Fingerprint.ToString());
            }

            this.DebugPrintScheduleTrace();
            this.DebugPrintPotentialCycle();

            if (!IsSchedulingFair(this.PotentialCycle))
            {
                Debug.WriteLine("<LivenessDebug> Scheduling in cycle is unfair.");
                this.PotentialCycle.Clear();
                this.PotentialCycleFingerprints.Clear();
            }
            else if (!IsNondeterminismFair(this.PotentialCycle))
            {
                Debug.WriteLine("<LivenessDebug> Nondeterminism in cycle is unfair.");
                this.PotentialCycle.Clear();
                this.PotentialCycleFingerprints.Clear();
            }

            if (this.PotentialCycle.Count == 0)
            {
                bool isFairCycleFound = false;
                int counter = Math.Min(indices.Count - 1, 3);
                while (counter > 0)
                {
                    var randInd = this.Random.Next(indices.Count - 2);
                    checkIndexRand = indices[randInd];

                    index = this.ScheduleTrace.Count - 1;
                    for (int i = checkIndexRand + 1; i <= index; i++)
                    {
                        var scheduleStep = this.ScheduleTrace[i];
                        this.PotentialCycle.Add(scheduleStep);
                        this.PotentialCycleFingerprints.Add(scheduleStep.State.Fingerprint);
                        Debug.WriteLine(
                            "<LivenessDebug> Cycle contains {0} with {1}.",
                            scheduleStep.Type, scheduleStep.State.Fingerprint.ToString());
                    }

                    if (IsSchedulingFair(this.PotentialCycle) && IsNondeterminismFair(this.PotentialCycle))
                    {
                        isFairCycleFound = true;
                        break;
                    }
                    else
                    {
                        this.PotentialCycle.Clear();
                        this.PotentialCycleFingerprints.Clear();
                    }

                    counter--;
                }

                if (!isFairCycleFound)
                {
                    this.PotentialCycle.Clear();
                    this.PotentialCycleFingerprints.Clear();
                    return;
                }
            }

            Debug.WriteLine("<LivenessDebug> Cycle execution is fair.");

            this.HotMonitors = GetHotMonitors(this.PotentialCycle);
            if (this.HotMonitors.Count > 0)
            {
                this.EndOfCycleIndex = this.PotentialCycle.Select(val => val).Min(val => val.Index);
                this.Configuration.LivenessTemperatureThreshold = 10 * this.PotentialCycle.Count;
                this.IsReplayingCycle = true;
            }
            else
            {
                this.PotentialCycle.Clear();
                this.PotentialCycleFingerprints.Clear();
            }
        }

        /// <summary>
        /// Checks if the scheduling is fair in a schedule trace cycle.
        /// </summary>
        /// <param name="cycle">Cycle of states.</param>
        private static bool IsSchedulingFair(List<ScheduleStep> cycle)
        {
            var result = false;

            var enabledMachines = new HashSet<ulong>();
            var scheduledMachines = new HashSet<ulong>();

            var schedulingChoiceSteps = cycle.Where(
                val => val.Type == ScheduleStepType.SchedulingChoice);
            foreach (var step in schedulingChoiceSteps)
            {
                scheduledMachines.Add(step.ScheduledOperationId);
            }

            foreach (var step in cycle)
            {
                enabledMachines.UnionWith(step.State.EnabledActorIds);
            }

            if (Debug.IsEnabled)
            {
                foreach (var m in enabledMachines)
                {
                    Debug.WriteLine("<LivenessDebug> Enabled actor {0}.", m);
                }

                foreach (var m in scheduledMachines)
                {
                    Debug.WriteLine("<LivenessDebug> Scheduled actor {0}.", m);
                }
            }

            if (enabledMachines.Count == scheduledMachines.Count)
            {
                result = true;
            }

            return result;
        }

        private static bool IsNondeterminismFair(List<ScheduleStep> cycle)
        {
            var fairNondeterministicChoiceSteps = cycle.Where(
                val => val.Type == ScheduleStepType.FairNondeterministicChoice &&
                val.BooleanChoice != null).ToList();
            foreach (var step in fairNondeterministicChoiceSteps)
            {
                var choices = fairNondeterministicChoiceSteps.Where(c => c.NondetId.Equals(step.NondetId)).ToList();
                var falseChoices = choices.Count(c => c.BooleanChoice == false);
                var trueChoices = choices.Count(c => c.BooleanChoice == true);
                if (trueChoices == 0 || falseChoices == 0)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Gets all monitors that are in hot state, but not in cold
        /// state during the schedule trace cycle.
        /// </summary>
        /// <param name="cycle">Cycle of states.</param>
        private static HashSet<Monitor> GetHotMonitors(List<ScheduleStep> cycle)
        {
            var hotMonitors = new HashSet<Monitor>();

            foreach (var step in cycle)
            {
                foreach (var kvp in step.State.MonitorStatus)
                {
                    if (kvp.Value == MonitorStatus.Hot)
                    {
                        hotMonitors.Add(kvp.Key);
                    }
                }
            }

            if (hotMonitors.Count > 0)
            {
                foreach (var step in cycle)
                {
                    foreach (var kvp in step.State.MonitorStatus)
                    {
                        if (kvp.Value == MonitorStatus.Cold &&
                            hotMonitors.Contains(kvp.Key))
                        {
                            hotMonitors.Remove(kvp.Key);
                        }
                    }
                }
            }

            return hotMonitors;
        }

        /// <summary>
        /// Escapes the unfair cycle and continues to explore the
        /// schedule with the original scheduling strategy.
        /// </summary>
        private void EscapeUnfairCycle()
        {
            Debug.WriteLine("<LivenessDebug> Escaped from unfair cycle.");

            this.HotMonitors.Clear();
            this.PotentialCycle.Clear();
            this.PotentialCycleFingerprints.Clear();

            this.LivenessTemperature = 0;
            this.EndOfCycleIndex = 0;
            this.CurrentCycleIndex = 0;

            this.IsReplayingCycle = false;
        }

        /// <summary>
        /// Prints the program schedule trace. Works only
        /// if debug mode is enabled.
        /// </summary>
        private void DebugPrintScheduleTrace()
        {
            if (Debug.IsEnabled)
            {
                Debug.WriteLine("<LivenessDebug> ------------ SCHEDULE ------------.");

                foreach (var step in this.ScheduleTrace)
                {
                    if (step.Type == ScheduleStepType.SchedulingChoice)
                    {
                        Debug.WriteLine($"{step.Index} :: {step.Type} :: {step.ScheduledOperationId} :: {step.State.Fingerprint}");
                    }
                    else if (step.BooleanChoice != null)
                    {
                        Debug.WriteLine($"{step.Index} :: {step.Type} :: {step.BooleanChoice.Value} :: {step.State.Fingerprint}");
                    }
                    else
                    {
                        Debug.WriteLine($"{step.Index} :: {step.Type} :: {step.IntegerChoice.Value} :: {step.State.Fingerprint}");
                    }
                }

                Debug.WriteLine("<LivenessDebug> ----------------------------------.");
            }
        }

        /// <summary>
        /// Prints the potential cycle. Works only if
        /// debug mode is enabled.
        /// </summary>
        private void DebugPrintPotentialCycle()
        {
            if (Debug.IsEnabled)
            {
                Debug.WriteLine("<LivenessDebug> ------------- CYCLE --------------.");

                foreach (var step in this.PotentialCycle)
                {
                    if (step.Type == ScheduleStepType.SchedulingChoice)
                    {
                        Debug.WriteLine($"{step.Index} :: {step.Type} :: {step.ScheduledOperationId}");
                    }
                    else if (step.BooleanChoice != null)
                    {
                        Debug.WriteLine($"{step.Index} :: {step.Type} :: {step.BooleanChoice.Value}");
                    }
                    else
                    {
                        Debug.WriteLine($"{step.Index} :: {step.Type} :: {step.IntegerChoice.Value}");
                    }

                    step.State.PrettyPrint();
                }

                Debug.WriteLine("<LivenessDebug> ----------------------------------.");
            }
        }
    }
}
