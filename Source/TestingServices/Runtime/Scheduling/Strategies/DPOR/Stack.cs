// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.Coyote.TestingServices.Scheduling.Strategies.DPOR
{
    /// <summary>
    /// The stack datastructure used by <see cref="DPORStrategy"/> to perform the depth-first search.
    /// </summary>
    internal class Stack
    {
        /// <summary>
        /// The actual stack.
        /// </summary>
        public readonly List<TaskEntryList> StackInternal;

        private int NextStackPos;
        private readonly IRandomNumberGenerator Rand;

        /// <summary>
        /// If no task id can be chosen, a negative task id is returned.
        /// This indicates that some threads were enabled, but they were
        /// all slept (due to <see cref="SleepSets"/>).
        /// </summary>
        public const int SleepSetBlocked = -2;

        /// <summary>
        /// Initializes a new instance of the <see cref="Stack"/> class.
        /// If the <paramref name="rand"/> is non-null, then randomized
        /// DPOR is assumed.
        /// </summary>
        public Stack(IRandomNumberGenerator rand)
        {
            this.StackInternal = new List<TaskEntryList>();
            this.Rand = rand;
        }

        /// <summary>
        /// Push a list of tid entries onto the stack. If we are replaying, this
        /// will verify that the list is what we expected.
        /// </summary>
        public bool Push(List<IAsyncOperation> ops)
        {
            List<TaskEntry> list = new List<TaskEntry>();

            foreach (var op in ops)
            {
                list.Add(new TaskEntry(
                    (int)op.SourceId,
                    op.IsEnabled,
                    op.Type,
                    op.Target,
                    (int)op.TargetId,
                    (int)op.MatchingSendIndex));
            }

            Debug.Assert(this.NextStackPos <= this.StackInternal.Count, "DFS strategy unexpected stack state.");

            bool added = this.NextStackPos == this.StackInternal.Count;

            if (added)
            {
                this.StackInternal.Add(new TaskEntryList(list));
            }
            else
            {
                this.CheckMatches(list);
            }

            ++this.NextStackPos;

            return added;
        }

        /// <summary>
        /// Get the number of entries on the stack (not including those that are yet to be replayed).
        /// </summary>
        public int GetNumSteps()
        {
            return this.NextStackPos;
        }

        /// <summary>
        /// Get the real size of the stack (including entries that are yet to be replayed).
        /// </summary>
        public int GetInternalSize()
        {
            return this.StackInternal.Count;
        }

        /// <summary>
        /// Get the top entry of the stack.
        /// </summary>
        public TaskEntryList GetTop()
        {
            return this.StackInternal[this.NextStackPos - 1];
        }

        /// <summary>
        /// Get the second from top entry of the stack.
        /// </summary>
        public TaskEntryList GetSecondFromTop()
        {
            return this.StackInternal[this.NextStackPos - 2];
        }

        /// <summary>
        /// Gets the top of stack and also ensures that this is the real top of stack.
        /// </summary>
        public TaskEntryList GetTopAsRealTop()
        {
            Debug.Assert(this.NextStackPos == this.StackInternal.Count, "DFS Strategy: top of stack is not aligned.");
            return this.GetTop();
        }

        /// <summary>
        /// Get the next task to schedule: either the preselected task entry
        /// from the current schedule prefix that we are replaying or the first
        /// suitable task entry from the real top of the stack.
        /// </summary>
        public int GetSelectedOrFirstBacktrackNotSlept(int startingFrom)
        {
            var top = this.GetTop();

            if (this.StackInternal.Count > this.NextStackPos)
            {
                return top.GetSelected();
            }

            int res = top.TryGetSelected();
            return res >= 0 ? res : top.GetFirstBacktrackNotSlept(startingFrom);
        }

        /// <summary>
        /// Prepare for the next schedule by popping entries from the stack
        /// until we find some tid entries that are not slept.
        /// </summary>
        public void PrepareForNextSchedule()
        {
            if (this.Rand != null)
            {
                this.NextStackPos = 0;
                return;
            }

            {
                // Deadlock / sleep set blocked; no selected tid entry.
                TaskEntryList top = this.GetTopAsRealTop();
                if (top.IsNoneSelected())
                {
                    this.Pop();
                }
            }

            // Pop until there are some tid entries that are not done/slept OR stack is empty.
            while (this.StackInternal.Count > 0)
            {
                TaskEntryList top = this.GetTopAsRealTop();

                if (top.BacktrackNondetChoices())
                {
                    break;
                }

                top.SetSelectedToSleep();
                top.ClearSelected();

                if (!top.AllDoneOrSlept())
                {
                    break;
                }

                this.Pop();
            }

            this.NextStackPos = 0;
        }

        private void Pop()
        {
            Debug.Assert(this.NextStackPos == this.StackInternal.Count, "DFS Strategy: top of stack is not aligned.");
            this.StackInternal.RemoveAt(this.StackInternal.Count - 1);
            --this.NextStackPos;
        }

        private void CheckMatches(List<TaskEntry> list)
        {
            Debug.Assert(
                this.StackInternal[this.NextStackPos].List.SequenceEqual(list, TaskEntry.ComparerSingleton),
                "DFS strategy detected nondeterminism when replaying.");
        }

        /// <summary>
        /// Clear the stack.
        /// </summary>
        public void Clear()
        {
            this.StackInternal.Clear();
        }

        /// <summary>
        /// Clear all entries beyond the current top of stack.
        /// </summary>
        public void ClearAboveTop()
        {
            this.StackInternal.RemoveRange(this.NextStackPos, this.StackInternal.Count - this.NextStackPos);
        }
    }
}
