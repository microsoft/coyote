// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Coyote.IO;
using Microsoft.Coyote.Runtime;

namespace Microsoft.Coyote.Testing.Systematic
{
    /// <summary>
    /// A depth-first search scheduling strategy.
    /// </summary>
    internal class DFSStrategy : SchedulingStrategy
    {
        /// <summary>
        /// The maximum number of steps to schedule.
        /// </summary>
        protected int MaxScheduledSteps;

        /// <summary>
        /// The number of scheduled steps.
        /// </summary>
        protected int ScheduledSteps;

        /// <summary>
        /// Stack of scheduling choices.
        /// </summary>
        private readonly List<List<SChoice>> ScheduleStack;

        /// <summary>
        /// Stack of nondeterministic choices.
        /// </summary>
        private readonly List<List<NondetBooleanChoice>> BoolNondetStack;

        /// <summary>
        /// Stack of nondeterministic choices.
        /// </summary>
        private readonly List<List<NondetIntegerChoice>> IntNondetStack;

        /// <summary>
        /// Current schedule index.
        /// </summary>
        private int SchIndex;

        /// <summary>
        /// Current nondeterministic index.
        /// </summary>
        private int NondetIndex;

        /// <summary>
        /// Initializes a new instance of the <see cref="DFSStrategy"/> class.
        /// </summary>
        internal DFSStrategy(int maxSteps)
        {
            this.MaxScheduledSteps = maxSteps;
            this.ScheduledSteps = 0;
            this.SchIndex = 0;
            this.NondetIndex = 0;
            this.ScheduleStack = new List<List<SChoice>>();
            this.BoolNondetStack = new List<List<NondetBooleanChoice>>();
            this.IntNondetStack = new List<List<NondetIntegerChoice>>();
        }

        /// <inheritdoc/>
        internal override bool InitializeNextIteration(uint iteration)
        {
            if (iteration is 0)
            {
                // The first iteration has already been initialized.
                return true;
            }

            if (this.ScheduleStack.All(scs => scs.All(val => val.IsDone)))
            {
                return false;
            }

            // DebugPrintSchedule();
            this.ScheduledSteps = 0;

            this.SchIndex = 0;
            this.NondetIndex = 0;

            for (int idx = this.BoolNondetStack.Count - 1; idx > 0; idx--)
            {
                if (!this.BoolNondetStack[idx].All(val => val.IsDone))
                {
                    break;
                }

                var previousChoice = this.BoolNondetStack[idx - 1].First(val => !val.IsDone);
                previousChoice.IsDone = true;

                this.BoolNondetStack.RemoveAt(idx);
            }

            for (int idx = this.IntNondetStack.Count - 1; idx > 0; idx--)
            {
                if (!this.IntNondetStack[idx].All(val => val.IsDone))
                {
                    break;
                }

                var previousChoice = this.IntNondetStack[idx - 1].First(val => !val.IsDone);
                previousChoice.IsDone = true;

                this.IntNondetStack.RemoveAt(idx);
            }

            if (this.BoolNondetStack.Count > 0 &&
                this.BoolNondetStack.All(ns => ns.All(nsc => nsc.IsDone)))
            {
                this.BoolNondetStack.Clear();
            }

            if (this.IntNondetStack.Count > 0 &&
                this.IntNondetStack.All(ns => ns.All(nsc => nsc.IsDone)))
            {
                this.IntNondetStack.Clear();
            }

            if (this.BoolNondetStack.Count is 0 &&
                this.IntNondetStack.Count is 0)
            {
                for (int idx = this.ScheduleStack.Count - 1; idx > 0; idx--)
                {
                    if (!this.ScheduleStack[idx].All(val => val.IsDone))
                    {
                        break;
                    }

                    var previousChoice = this.ScheduleStack[idx - 1].First(val => !val.IsDone);
                    previousChoice.IsDone = true;

                    this.ScheduleStack.RemoveAt(idx);
                }
            }
            else
            {
                var previousChoice = this.ScheduleStack.Last().LastOrDefault(val => val.IsDone);
                if (previousChoice != null)
                {
                    previousChoice.IsDone = false;
                }
            }

            return true;
        }

        /// <inheritdoc/>
        internal override bool GetNextOperation(IEnumerable<AsyncOperation> ops, AsyncOperation current,
            bool isYielding, out AsyncOperation next)
        {
            var enabledOps = ops.Where(op => op.Status is AsyncOperationStatus.Enabled).ToList();
            if (enabledOps.Count is 0)
            {
                next = null;
                return false;
            }

            SChoice nextChoice = null;
            List<SChoice> scs = null;

            if (this.SchIndex < this.ScheduleStack.Count)
            {
                scs = this.ScheduleStack[this.SchIndex];
            }
            else
            {
                scs = new List<SChoice>();
                foreach (var task in enabledOps)
                {
                    scs.Add(new SChoice(task.Id));
                }

                this.ScheduleStack.Add(scs);
            }

            nextChoice = scs.FirstOrDefault(val => !val.IsDone);
            if (nextChoice is null)
            {
                next = null;
                return false;
            }

            if (this.SchIndex > 0)
            {
                var previousChoice = this.ScheduleStack[this.SchIndex - 1].Last(val => val.IsDone);
                previousChoice.IsDone = false;
            }

            next = enabledOps.Find(task => task.Id == nextChoice.Id);
            nextChoice.IsDone = true;
            this.SchIndex++;

            if (next is null)
            {
                return false;
            }

            this.ScheduledSteps++;

            return true;
        }

        /// <inheritdoc/>
        internal override bool GetNextBooleanChoice(AsyncOperation current, int maxValue, out bool next)
        {
            NondetBooleanChoice nextChoice = null;
            List<NondetBooleanChoice> ncs = null;

            if (this.NondetIndex < this.BoolNondetStack.Count)
            {
                ncs = this.BoolNondetStack[this.NondetIndex];
            }
            else
            {
                ncs = new List<NondetBooleanChoice>
                {
                    new NondetBooleanChoice(false),
                    new NondetBooleanChoice(true)
                };

                this.BoolNondetStack.Add(ncs);
            }

            nextChoice = ncs.FirstOrDefault(val => !val.IsDone);
            if (nextChoice is null)
            {
                next = false;
                return false;
            }

            if (this.NondetIndex > 0)
            {
                var previousChoice = this.BoolNondetStack[this.NondetIndex - 1].Last(val => val.IsDone);
                previousChoice.IsDone = false;
            }

            next = nextChoice.Value;
            nextChoice.IsDone = true;
            this.NondetIndex++;

            this.ScheduledSteps++;

            return true;
        }

        /// <inheritdoc/>
        internal override bool GetNextIntegerChoice(AsyncOperation current, int maxValue, out int next)
        {
            NondetIntegerChoice nextChoice = null;
            List<NondetIntegerChoice> ncs = null;

            if (this.NondetIndex < this.IntNondetStack.Count)
            {
                ncs = this.IntNondetStack[this.NondetIndex];
            }
            else
            {
                ncs = new List<NondetIntegerChoice>();
                for (int value = 0; value < maxValue; value++)
                {
                    ncs.Add(new NondetIntegerChoice(value));
                }

                this.IntNondetStack.Add(ncs);
            }

            nextChoice = ncs.FirstOrDefault(val => !val.IsDone);
            if (nextChoice is null)
            {
                next = 0;
                return false;
            }

            if (this.NondetIndex > 0)
            {
                var previousChoice = this.IntNondetStack[this.NondetIndex - 1].Last(val => val.IsDone);
                previousChoice.IsDone = false;
            }

            next = nextChoice.Value;
            nextChoice.IsDone = true;
            this.NondetIndex++;

            this.ScheduledSteps++;

            return true;
        }

        /// <inheritdoc/>
        internal override int GetScheduledSteps() => this.ScheduledSteps;

        /// <inheritdoc/>
        internal override bool HasReachedMaxSchedulingSteps()
        {
            if (this.MaxScheduledSteps is 0)
            {
                return false;
            }

            return this.ScheduledSteps >= this.MaxScheduledSteps;
        }

        /// <inheritdoc/>
        internal override bool IsFair() => false;

        /// <inheritdoc/>
        internal override string GetDescription() => "dfs";

        /// <summary>
        /// Prints the schedule, if debug is enabled.
        /// </summary>
        private void DebugPrintSchedule()
        {
            Debug.WriteLine("*******************");
            Debug.WriteLine("Schedule stack size: " + this.ScheduleStack.Count);
            for (int idx = 0; idx < this.ScheduleStack.Count; idx++)
            {
                Debug.WriteLine("Index: " + idx);
                foreach (var sc in this.ScheduleStack[idx])
                {
                    Debug.Write(sc.Id + " [" + sc.IsDone + "], ");
                }

                Debug.WriteLine(string.Empty);
            }

            Debug.WriteLine("*******************");
            Debug.WriteLine("Random bool stack size: " + this.BoolNondetStack.Count);
            for (int idx = 0; idx < this.BoolNondetStack.Count; idx++)
            {
                Debug.WriteLine("Index: " + idx);
                foreach (var nc in this.BoolNondetStack[idx])
                {
                    Debug.Write(nc.Value + " [" + nc.IsDone + "], ");
                }

                Debug.WriteLine(string.Empty);
            }

            Debug.WriteLine("*******************");
            Debug.WriteLine("Random int stack size: " + this.IntNondetStack.Count);
            for (int idx = 0; idx < this.IntNondetStack.Count; idx++)
            {
                Debug.WriteLine("Index: " + idx);
                foreach (var nc in this.IntNondetStack[idx])
                {
                    Debug.Write(nc.Value + " [" + nc.IsDone + "], ");
                }

                Debug.WriteLine(string.Empty);
            }

            Debug.WriteLine("*******************");
        }

        /// <inheritdoc/>
        internal override void Reset()
        {
            this.ScheduleStack.Clear();
            this.BoolNondetStack.Clear();
            this.IntNondetStack.Clear();
            this.SchIndex = 0;
            this.NondetIndex = 0;
            this.ScheduledSteps = 0;
        }

        /// <summary>
        /// A scheduling choice. Contains an id and a boolean that is
        /// true if the choice has been previously explored.
        /// </summary>
        private class SChoice
        {
            internal ulong Id;
            internal bool IsDone;

            /// <summary>
            /// Initializes a new instance of the <see cref="SChoice"/> class.
            /// </summary>
            internal SChoice(ulong id)
            {
                this.Id = id;
                this.IsDone = false;
            }
        }

        /// <summary>
        /// A nondeterministic choice. Contains a boolean value that
        /// corresponds to the choice and a boolean that is true if
        /// the choice has been previously explored.
        /// </summary>
        private class NondetBooleanChoice
        {
            internal bool Value;
            internal bool IsDone;

            /// <summary>
            /// Initializes a new instance of the <see cref="NondetBooleanChoice"/> class.
            /// </summary>
            internal NondetBooleanChoice(bool value)
            {
                this.Value = value;
                this.IsDone = false;
            }
        }

        /// <summary>
        /// A nondeterministic choice. Contains an integer value that
        /// corresponds to the choice and a boolean that is true if
        /// the choice has been previously explored.
        /// </summary>
        private class NondetIntegerChoice
        {
            internal int Value;
            internal bool IsDone;

            /// <summary>
            /// Initializes a new instance of the <see cref="NondetIntegerChoice"/> class.
            /// </summary>
            internal NondetIntegerChoice(int value)
            {
                this.Value = value;
                this.IsDone = false;
            }
        }
    }
}
