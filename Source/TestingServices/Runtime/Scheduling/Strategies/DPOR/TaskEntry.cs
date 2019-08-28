// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Collections.Generic;

namespace Microsoft.Coyote.TestingServices.Scheduling.Strategies.DPOR
{
    /// <summary>
    /// Task entry stored on the stack of a depth-first search to track which tasks existed
    /// and whether they have been executed already, etc.
    /// </summary>
    internal class TaskEntry
    {
        /// <summary>
        /// The id/index of this task in the original task creation order list of tasks.
        /// </summary>
        public int Id;

        /// <summary>
        /// Is the task enabled?
        /// </summary>
        public bool Enabled;

        /// <summary>
        /// Skip exploring this task from here.
        /// </summary>
        public bool Sleep;

        /// <summary>
        /// Backtrack to this transition?
        /// </summary>
        public bool Backtrack;

        /// <summary>
        /// Operation type.
        /// </summary>
        public AsyncOperationType OpType;

        /// <summary>
        /// Target type, e.g. task, queue, mutex, variable.
        /// </summary>
        public AsyncOperationTarget OpTarget;

        /// <summary>
        /// Target of the operation.
        /// </summary>
        public int TargetId;

        /// <summary>
        /// For a receive operation: the step of the corresponding send.
        /// </summary>
        public int SendStepIndex;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskEntry"/> class.
        /// </summary>
        public TaskEntry(int id, bool enabled, AsyncOperationType opType, AsyncOperationTarget target, int targetId, int sendStepIndex)
        {
            this.Id = id;
            this.Enabled = enabled;
            this.Sleep = false;
            this.Backtrack = false;
            this.OpType = opType;
            this.OpTarget = target;
            this.TargetId = targetId;
            this.SendStepIndex = sendStepIndex;
        }

        internal static readonly Comparer ComparerSingleton = new Comparer();

        internal class Comparer : IEqualityComparer<TaskEntry>
        {
            public bool Equals(TaskEntry x, TaskEntry y) =>
                x.OpType == y.OpType &&
                (x.OpType == AsyncOperationType.Yield || x.Enabled == y.Enabled) &&
                x.Id == y.Id &&
                x.TargetId == y.TargetId &&
                x.OpTarget == y.OpTarget;

            public int GetHashCode(TaskEntry obj)
            {
                unchecked
                {
                    int hash = 17;
                    hash = (hash * 23) + obj.Id.GetHashCode();
                    hash = (hash * 23) + obj.OpType.GetHashCode();
                    hash = (hash * 23) + obj.TargetId.GetHashCode();
                    hash = (hash * 23) + obj.OpTarget.GetHashCode();
                    hash = (hash * 23) + (obj.OpType == AsyncOperationType.Yield ? true.GetHashCode() : obj.Enabled.GetHashCode());
                    return hash;
                }
            }
        }
    }
}
