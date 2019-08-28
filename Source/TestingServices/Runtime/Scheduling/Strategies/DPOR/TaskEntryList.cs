// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Microsoft.Coyote.TestingServices.Scheduling.Strategies.DPOR
{
    /// <summary>
    /// The elements of the <see cref="Stack"/> used by <see cref="DPORStrategy"/>. Stores
    /// a list of <see cref="TaskEntry"/> (one for each <see cref="IAsyncOperation"/>).
    /// </summary>
    internal class TaskEntryList
    {
        /// <summary>
        /// The actual list.
        /// </summary>
        public readonly List<TaskEntry> List;

        private int SelectedEntry;

        /// <summary>
        /// A list of random choices made by the <see cref="SelectedEntry"/> task
        /// as part of its visible operation. Can be null.
        /// </summary>
        public List<NonDetChoice> NondetChoices;

        /// <summary>
        /// When replaying/adding nondet choices, this is the index of the next nondet choice.
        /// </summary>
        private int NextNondetChoiceIndex;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskEntryList"/> class.
        /// </summary>
        public TaskEntryList(List<TaskEntry> list)
        {
            this.List = list;
            this.SelectedEntry = -1;
            this.NondetChoices = null;
            this.NextNondetChoiceIndex = 0;
        }

        /// <summary>
        /// Get a nondet choice. This may replay a nondet choice or make (and record) a new nondet choice.
        /// </summary>
        public int MakeOrReplayNondetChoice(bool isBoolChoice, IRandomNumberGenerator rand)
        {
            Debug.Assert(rand != null || isBoolChoice,
                "A DFS DPOR exploration of int nondeterminstic choices " +
                "is not currently supported because this won't scale.");

            if (this.NondetChoices is null)
            {
                this.NondetChoices = new List<NonDetChoice>();
            }

            if (this.NextNondetChoiceIndex < this.NondetChoices.Count)
            {
                // Replay:
                NonDetChoice choice = this.NondetChoices[this.NextNondetChoiceIndex];
                ++this.NextNondetChoiceIndex;
                Debug.Assert(choice.IsBoolChoice == isBoolChoice, "choice.IsBoolChoice != isBoolChoice");
                return choice.Choice;
            }

            // Adding a choice.
            Debug.Assert(this.NextNondetChoiceIndex == this.NondetChoices.Count, "this.NextNondetChoiceIndex != this.NondetChoices.Count");
            NonDetChoice ndc = new NonDetChoice
            {
                IsBoolChoice = isBoolChoice,
                Choice = rand is null ? 0 : (isBoolChoice ? rand.Next(2) : rand.Next())
            };

            this.NondetChoices.Add(ndc);
            ++this.NextNondetChoiceIndex;
            return ndc.Choice;
        }

        /// <summary>
        /// This method is used in a DFS exploration of nondet choice. It will pop off bool
        /// choices that are 1 until it reaches a 0 that will then be changed to a 1. The
        /// NextNondetChoiceIndex will be reset ready for replay.
        /// </summary>
        public bool BacktrackNondetChoices()
        {
            if (this.NondetChoices is null)
            {
                return false;
            }

            Debug.Assert(this.NextNondetChoiceIndex == this.NondetChoices.Count, "this.NextNondetChoiceIndex != this.NondetChoices.Count");

            this.NextNondetChoiceIndex = 0;

            while (this.NondetChoices.Count > 0)
            {
                NonDetChoice choice = this.NondetChoices[this.NondetChoices.Count - 1];
                Debug.Assert(choice.IsBoolChoice, "DFS DPOR only supports bool choices.");
                if (choice.Choice == 0)
                {
                    choice.Choice = 1;
                    this.NondetChoices[this.NondetChoices.Count - 1] = choice;
                    return true;
                }

                Debug.Assert(choice.Choice == 1, "Unexpected choice value.");
                this.NondetChoices.RemoveAt(this.NondetChoices.Count - 1);
            }

            return false;
        }

        /// <summary>
        /// Prepares the list of nondet choices for replay. This is used by random DPOR, which does not need to
        /// backtrack individual nondet choices, but may need to replay all of them.
        /// </summary>
        public void RewindNondetChoicesForReplay()
        {
            this.NextNondetChoiceIndex = 0;
        }

        /// <summary>
        /// Clears the list of nondet choices for replay from the next nondet choice onwards.
        /// That is, nondet choices that have already been replayed remain in the list.
        /// </summary>
        public void ClearNondetChoicesFromNext()
        {
            if (this.NondetChoices != null && this.NextNondetChoiceIndex < this.NondetChoices.Count)
            {
                this.NondetChoices.RemoveRange(this.NextNondetChoiceIndex, this.NondetChoices.Count - this.NextNondetChoiceIndex);
            }
        }

        /// <summary>
        /// Add all enabled tasks to the backtrack set.
        /// </summary>
        public void SetAllEnabledToBeBacktracked()
        {
            foreach (var tidEntry in this.List)
            {
                if (tidEntry.Enabled)
                {
                    tidEntry.Backtrack = true;

                    // TODO: Remove?
                    Debug.Assert(tidEntry.Enabled, "Not enabled.");
                }
            }
        }

        /// <summary>
        /// Utility method to show the enabled tasks.
        /// </summary>
        public string ShowEnabled()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("[");
            foreach (var tidEntry in this.List)
            {
                if (!tidEntry.Enabled)
                {
                    continue;
                }

                if (tidEntry.Id == this.SelectedEntry)
                {
                    sb.Append("*");
                }

                sb.Append("(");
                sb.Append(tidEntry.Id);
                sb.Append(", ");
                sb.Append(tidEntry.OpType);
                sb.Append(", ");
                sb.Append(tidEntry.OpTarget);
                sb.Append("-");
                sb.Append(tidEntry.TargetId);
                sb.Append(") ");
            }

            sb.Append("]");
            return sb.ToString();
        }

        /// <summary>
        /// Utility method to show the selected task.
        /// </summary>
        public string ShowSelected()
        {
            int selectedIndex = this.TryGetSelected();
            if (selectedIndex < 0)
            {
                return "-";
            }

            TaskEntry selected = this.List[selectedIndex];
            int priorSend = selected.OpType == AsyncOperationType.Receive
                ? selected.SendStepIndex
                : -1;

            return $"({selected.Id}, {selected.OpType}, {selected.OpTarget}, {selected.TargetId}, {priorSend})";
        }

        /// <summary>
        /// Utility method to show the tasks in the backtrack set.
        /// </summary>
        /// <returns>string</returns>
        public string ShowBacktrack()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("[");
            foreach (var tidEntry in this.List)
            {
                if (!tidEntry.Backtrack)
                {
                    continue;
                }

                if (tidEntry.Id == this.SelectedEntry)
                {
                    sb.Append("*");
                }

                sb.Append("(");
                sb.Append(tidEntry.Id);
                sb.Append(", ");
                sb.Append(tidEntry.OpType);
                sb.Append(", ");
                sb.Append(tidEntry.OpTarget);
                sb.Append("-");
                sb.Append(tidEntry.TargetId);
                sb.Append(") ");
            }

            sb.Append("]");
            return sb.ToString();
        }

        /// <summary>
        /// Gets the first task in backtrack that is not slept.
        /// </summary>
        public int GetFirstBacktrackNotSlept(int startingFrom)
        {
            int size = this.List.Count;
            int i = startingFrom;
            bool foundSlept = false;
            for (int count = 0; count < size; ++count)
            {
                if (this.List[i].Backtrack &&
                    !this.List[i].Sleep)
                {
                    return i;
                }

                if (this.List[i].Sleep)
                {
                    foundSlept = true;
                }

                ++i;
                if (i >= size)
                {
                    i = 0;
                }
            }

            return foundSlept ? Stack.SleepSetBlocked : -1;
        }

        /// <summary>
        /// Gets all tasks in backtrack that are not slept and not selected.
        /// </summary>
        public List<int> GetAllBacktrackNotSleptNotSelected()
        {
            List<int> res = new List<int>();
            for (int i = 0; i < this.List.Count; ++i)
            {
                if (this.List[i].Backtrack &&
                    !this.List[i].Sleep &&
                    this.List[i].Id != this.SelectedEntry)
                {
                    Debug.Assert(this.List[i].Enabled, "Not enabled.");
                    res.Add(i);
                }
            }

            return res;
        }

        /// <summary>
        /// Returns true if some tasks are in backtrack, and are not slept nor selected.
        /// </summary>
        public bool HasBacktrackNotSleptNotSelected()
        {
            foreach (TaskEntry t in this.List)
            {
                if (t.Backtrack &&
                    !t.Sleep &&
                    t.Id != this.SelectedEntry)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Sets the selected task to be slept.
        /// </summary>
        public void SetSelectedToSleep()
        {
            this.List[this.GetSelected()].Sleep = true;
        }

        /// <summary>
        /// Returns true if all tasks are done or slept.
        /// </summary>
        public bool AllDoneOrSlept()
        {
            return this.GetFirstBacktrackNotSlept(0) < 0;
        }

        /// <summary>
        /// Tries to get the single selected task. Returns -1 if no task is selected.
        /// </summary>
        public int TryGetSelected()
        {
            return this.SelectedEntry;
        }

        /// <summary>
        /// Checks if no tasks are selected.
        /// </summary>
        public bool IsNoneSelected()
        {
            return this.TryGetSelected() < 0;
        }

        /// <summary>
        /// Gets the selected task. Asserts that there is a selected task.
        /// </summary>
        public int GetSelected()
        {
            int res = this.TryGetSelected();
            Debug.Assert(res != -1, "DFS Strategy: No selected tid entry!");
            return res;
        }

        /// <summary>
        /// Deselect the selected task.
        /// </summary>
        public void ClearSelected()
        {
            this.SelectedEntry = -1;
        }

        /// <summary>
        /// Add the first enabled and not slept task to the backtrack set.
        /// </summary>
        public void AddFirstEnabledNotSleptToBacktrack(int startingFrom)
        {
            int size = this.List.Count;
            int i = startingFrom;
            for (int count = 0; count < size; ++count)
            {
                if (this.List[i].Enabled &&
                    !this.List[i].Sleep)
                {
                    this.List[i].Backtrack = true;

                    // TODO: Remove?
                    Debug.Assert(this.List[i].Enabled, "Not enabled.");
                    return;
                }

                ++i;
                if (i >= size)
                {
                    i = 0;
                }
            }
        }

        /// <summary>
        /// Add a task to the backtrack set.
        /// </summary>
        public void AddToBacktrack(int tid)
        {
            this.List[tid].Backtrack = true;
            Debug.Assert(this.List[tid].Enabled, "Not enabled.");
        }

        /// <summary>
        /// Add a random enabled and not slept task to the backtrack set.
        /// </summary>
        public void AddRandomEnabledNotSleptToBacktrack(IRandomNumberGenerator rand)
        {
            var enabledNotSlept = this.List.Where(e => e.Enabled && !e.Sleep).ToList();
            if (enabledNotSlept.Count > 0)
            {
                int choice = rand.Next(enabledNotSlept.Count);
                enabledNotSlept[choice].Backtrack = true;
            }
        }

        /// <summary>
        /// Sets the selected task id. There must not already be a selected task id.
        /// </summary>
        public void SetSelected(int tid)
        {
            Debug.Assert(this.SelectedEntry < 0, "this.SelectedEntry >= 0");
            Debug.Assert(tid >= 0 && tid < this.List.Count, "!(tid >= 0 && tid < this.List.Count)");
            this.SelectedEntry = tid;
        }
    }
}
