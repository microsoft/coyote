// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.Coyote.TestingServices.Scheduling.Strategies.DPOR;

namespace Microsoft.Coyote.TestingServices.Scheduling.Strategies
{
    /// <summary>
    /// Dynamic partial-order reduction (DPOR) scheduling strategy.
    /// In fact, this uses the Source DPOR algorithm.
    /// </summary>
    public class DPORStrategy : ISchedulingStrategy
    {
        /// <summary>
        /// The stack datastructure used to perform the
        /// depth-first search.
        /// </summary>
        private readonly Stack Stack;

        /// <summary>
        /// The actual DPOR algorithm implementation.
        /// </summary>
        private readonly DPORAlgorithm Dpor;

        /// <summary>
        /// Whether to use sleep sets.
        /// See <see cref="SleepSets"/>.
        /// </summary>
        private readonly bool UseSleepSets;

        /// <summary>
        /// If non-null, we perform random DPOR
        /// using this RNG.
        /// </summary>
        private readonly IRandomNumberGenerator Rand;

        /// <summary>
        /// The step limit.
        /// TODO: implement the step limit.
        /// </summary>
        private readonly int StepLimit;

        /// <summary>
        /// When doing random DPOR, we do an initial execution
        /// and then try to reverse races.
        /// This int specifies how many iterations of race reversing to perform
        /// before performing a new initial iteration.
        /// </summary>
        private readonly int RaceReversalIterationsLimit;

        /// <summary>
        /// Counter for <see cref="RaceReversalIterationsLimit"/>.
        /// </summary>
        private int NumRaceReversalIterationsCounter;

        /// <summary>
        /// Initializes a new instance of the <see cref="DPORStrategy"/> class.
        /// </summary>
        public DPORStrategy(
            IRandomNumberGenerator rand = null,
            int raceReversalIterationsLimit = -1,
            int stepLimit = -1,
            bool useSleepSets = true,
            bool dpor = true)
        {
            this.Rand = rand;
            this.StepLimit = stepLimit;
            this.Stack = new Stack(rand);
            this.Dpor = dpor ? new DPORAlgorithm() : null;
            this.UseSleepSets = rand is null && useSleepSets;
            this.RaceReversalIterationsLimit = raceReversalIterationsLimit;
            this.Reset();
        }

        /// <summary>
        /// Returns or forces the next choice to schedule.
        /// </summary>
        private bool GetNextHelper(ref IAsyncOperation next, List<IAsyncOperation> ops, IAsyncOperation current)
        {
            int currentSchedulableId = (int)current.SourceId;

            // "Yield" and "Waiting for quiescence" hack.
            if (ops.TrueForAll(op => !op.IsEnabled))
            {
                if (ops.Exists(op => op.Type is AsyncOperationType.Yield))
                {
                    foreach (var op in ops)
                    {
                        if (op.Type is AsyncOperationType.Yield)
                        {
                            op.IsEnabled = true;
                        }
                    }
                }
                else if (ops.Exists(op => op.Type is AsyncOperationType.WaitForQuiescence))
                {
                    foreach (var op in ops)
                    {
                        if (op.Type is AsyncOperationType.WaitForQuiescence)
                        {
                            op.IsEnabled = true;
                        }
                    }
                }
            }

            // Forced choice.
            if (next != null)
            {
                this.AbdandonReplay(false);
            }

            bool added = this.Stack.Push(ops);
            TaskEntryList top = this.Stack.GetTop();

            Debug.Assert(next is null || added, "DPOR: Forced choice implies we should have added to stack.");

            if (added)
            {
                if (this.UseSleepSets)
                {
                    SleepSets.UpdateSleepSets(this.Stack);
                }

                if (this.Dpor is null)
                {
                    top.SetAllEnabledToBeBacktracked();
                }
                else if (this.Dpor.RaceReplaySuffix.Count > 0 && this.Dpor.ReplayRaceIndex < this.Dpor.RaceReplaySuffix.Count)
                {
                    // Replaying a race:
                    var tidReplay = this.Dpor.RaceReplaySuffix[this.Dpor.ReplayRaceIndex];

                    // Restore the nondet choices on the top of stack.
                    top.NondetChoices = tidReplay.NondetChoices;

                    // Add the replay tid to the backtrack set.
                    top.List[tidReplay.Id].Backtrack = true;
                    Debug.Assert(top.List[tidReplay.Id].Enabled || top.List[tidReplay.Id].OpType is AsyncOperationType.Yield, "Failed.");
                    ++this.Dpor.ReplayRaceIndex;
                }
                else
                {
                    // TODO: Here is where we can combine with another scheduler:
                    // For now, we just do round-robin when doing DPOR and random when doing random DPOR.

                    // If our choice is forced by parent scheduler:
                    if (next != null)
                    {
                        top.AddToBacktrack((int)next.SourceId);
                    }
                    else if (this.Rand is null)
                    {
                        top.AddFirstEnabledNotSleptToBacktrack(currentSchedulableId);
                    }
                    else
                    {
                        top.AddRandomEnabledNotSleptToBacktrack(this.Rand);
                    }
                }
            }
            else if (this.Rand != null)
            {
                // When doing random DPOR: we are replaying a schedule prefix so rewind the nondet choices now.
                top.RewindNondetChoicesForReplay();
            }

            int nextTid = this.Stack.GetSelectedOrFirstBacktrackNotSlept(currentSchedulableId);

            if (nextTid < 0)
            {
                next = null;

                // TODO: if nextTidIndex == DPORAlgorithm.SLEEP_SET_BLOCKED then let caller know that this is the case.
                // I.e. this is not deadlock.
                return false;
            }

            if (top.TryGetSelected() != nextTid)
            {
                top.SetSelected(nextTid);
            }

            Debug.Assert(nextTid < ops.Count, "nextTid >= choices.Count");
            next = ops[nextTid];

            // TODO: Part of yield hack.
            if (!next.IsEnabled && next.Type is AsyncOperationType.Yield)
            {
                // Uncomment to avoid waking a yielding task.
                // next = null;
                // TODO: let caller know that this is not deadlock.
                // return false;
                next.IsEnabled = true;
            }

            Debug.Assert(next.IsEnabled, "Not enabled.");
            return true;
        }

        /// <summary>
        /// Returns or forces the next boolean choice.
        /// </summary>
        private bool GetNextBooleanChoiceHelper(ref bool? next)
        {
            if (next != null)
            {
                this.AbdandonReplay(true);
                return true;
            }

            next = this.Stack.GetTop().MakeOrReplayNondetChoice(true, this.Rand) == 1;
            return true;
        }

        /// <summary>
        /// Returns or forces the next integer choice.
        /// </summary>
        private bool GetNextIntegerChoiceHelper(ref int? next)
        {
            if (next != null)
            {
                this.AbdandonReplay(true);
                return true;
            }

            next = this.Stack.GetTop().MakeOrReplayNondetChoice(false, this.Rand);
            return true;
        }

        /// <summary>
        /// Abandon the replay of a schedule prefix and/or a race suffice.
        /// </summary>
        private void AbdandonReplay(bool clearNonDet)
        {
            Debug.Assert(this.Rand != null, "DPOR: Forced choices are only supported with random DPOR.");

            // Abandon remaining stack entries and race replay.
            if (clearNonDet)
            {
                this.Stack.GetTop().ClearNondetChoicesFromNext();
            }

            this.Stack.ClearAboveTop();
            this.Dpor.ReplayRaceIndex = int.MaxValue;
        }

        /// <summary>
        /// Returns the next asynchronous operation to schedule.
        /// </summary>
        public bool GetNext(out IAsyncOperation next, List<IAsyncOperation> ops, IAsyncOperation current)
        {
            next = null;
            return this.GetNextHelper(ref next, ops, current);
        }

        /// <summary>
        /// Returns the next boolean choice.
        /// </summary>
        public bool GetNextBooleanChoice(int maxValue, out bool next)
        {
            bool? nextTemp = null;
            this.GetNextBooleanChoiceHelper(ref nextTemp);
            Debug.Assert(nextTemp != null, "nextTemp is null");
            next = nextTemp.Value;
            return true;
        }

        /// <summary>
        /// Returns the next integer choice.
        /// </summary>
        public bool GetNextIntegerChoice(int maxValue, out int next)
        {
            int? nextTemp = null;
            this.GetNextIntegerChoiceHelper(ref nextTemp);
            Debug.Assert(nextTemp != null, "nextTemp is null");
            next = nextTemp.Value;
            return true;
        }

        /// <summary>
        /// Forces the next asynchronous operation to be scheduled.
        /// </summary>
        public void ForceNext(IAsyncOperation next, List<IAsyncOperation> ops, IAsyncOperation current)
        {
            IAsyncOperation temp = next;
            bool res = this.GetNextHelper(ref temp, ops, current);
            Debug.Assert(res, "DPOR scheduler refused to schedule a forced choice.");
            Debug.Assert(temp == next, "DPOR scheduler changed forced next choice.");
        }

        /// <summary>
        /// Forces the next boolean choice.
        /// </summary>
        public void ForceNextBooleanChoice(int maxValue, bool next)
        {
            bool? nextTemp = next;
            bool res = this.GetNextBooleanChoiceHelper(ref nextTemp);
            Debug.Assert(res, "DPOR scheduler refused to schedule a forced boolean choice.");
            Debug.Assert(nextTemp.HasValue && nextTemp.Value == next,
                "DPOR scheduler changed forced next boolean choice.");
        }

        /// <summary>
        /// Forces the next integer choice.
        /// </summary>
        public void ForceNextIntegerChoice(int maxValue, int next)
        {
            int? nextTemp = next;
            bool res = this.GetNextIntegerChoiceHelper(ref nextTemp);
            Debug.Assert(res, "DPOR scheduler refused to schedule a forced integer choice.");
            Debug.Assert(nextTemp.HasValue && nextTemp.Value == next,
                "DPOR scheduler changed forced next integer choice.");
        }

        /// <summary>
        /// Returns the explored steps.
        /// </summary>
        public int GetScheduledSteps() => this.Stack.GetNumSteps();

        /// <summary>
        /// True if the scheduling strategy has reached the max
        /// scheduling steps for the given scheduling iteration.
        /// </summary>
        public bool HasReachedMaxSchedulingSteps() => this.StepLimit > 0 && this.Stack.GetNumSteps() >= this.StepLimit;

        /// <summary>
        /// Checks if this a fair scheduling strategy.
        /// </summary>
        public bool IsFair() => false;

        /// <summary>
        /// Prepares the next scheduling iteration.
        /// </summary>
        public bool PrepareForNextIteration()
        {
            this.Dpor?.DoDPOR(this.Stack, this.Rand);

            this.Stack.PrepareForNextSchedule();

            if (this.Rand != null && this.RaceReversalIterationsLimit >= 0)
            {
                ++this.NumRaceReversalIterationsCounter;
                if (this.NumRaceReversalIterationsCounter >= this.RaceReversalIterationsLimit)
                {
                    this.NumRaceReversalIterationsCounter = 0;
                    this.AbdandonReplay(false);
                }
            }

            return this.Rand != null || this.Stack.GetInternalSize() != 0;
        }

        /// <summary>
        /// Resets the scheduling strategy.
        /// </summary>
        public void Reset()
        {
            this.Stack.Clear();
            this.NumRaceReversalIterationsCounter = 0;
        }

        /// <summary>
        /// Returns a textual description of the scheduling strategy.
        /// </summary>
        public string GetDescription() => "DPOR";
    }
}
