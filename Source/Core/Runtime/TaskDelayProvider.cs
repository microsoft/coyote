// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Coyote.Runtime
{
    /// <summary>
    /// Provides methods for creating controlled task delays.
    /// </summary>
    internal class TaskDelayProvider
    {
        /// <summary>
        /// Map from task ids to task completion sources modeling the delays.
        /// </summary>
        private readonly Dictionary<int, TaskCompletionSource<bool>> TaskDelayMap;

        /// <summary>
        /// Map from task ids to scheduling steps for completing the delays.
        /// </summary>
        private readonly Dictionary<int, uint> CompletionStepsMap;

        /// <summary>
        /// Protects access to the provider.
        /// </summary>
        private readonly object SyncObject;

        /// <summary>
        /// The number of task delays.
        /// </summary>
        internal int NumDelays
        {
            get
            {
                lock (this.SyncObject)
                {
                    return this.TaskDelayMap.Count;
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskDelayProvider"/> class.
        /// </summary>
        internal TaskDelayProvider()
        {
            this.TaskDelayMap = new Dictionary<int, TaskCompletionSource<bool>>();
            this.CompletionStepsMap = new Dictionary<int, uint>();
            this.SyncObject = new object();
        }

        /// <summary>
        /// Creates a new task delay.
        /// </summary>
        internal Task CreateDelay(uint steps)
        {
            if (steps is 0)
            {
                // If the number of steps to complete the delay are 0, then complete synchronously.
                IO.Debug.WriteLine("<AsyncBuilder> Creating completed task delay from thread '{0}'.",
                    Thread.CurrentThread.ManagedThreadId);
                return Task.CompletedTask;
            }

            // var tcs = new TaskCompletionSource<bool>();
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (this.SyncObject)
            {
                this.TaskDelayMap.Add(tcs.Task.Id, tcs);
                this.CompletionStepsMap.Add(tcs.Task.Id, steps);
            }

            IO.Debug.WriteLine("<AsyncBuilder> Creating task delay '{0}' for '{1}' steps from thread '{2}'.",
                tcs.Task.Id, steps, Thread.CurrentThread.ManagedThreadId);
            return tcs.Task;
        }

        /// <summary>
        /// Progress any pending delays.
        /// </summary>
        internal void ProgressDelays(bool force)
        {
            lock (this.SyncObject)
            {
                var keysToRemove = new List<int>();
                foreach (var kvp in this.TaskDelayMap)
                {
                    var steps = this.CompletionStepsMap[kvp.Key] - 1;
                    this.CompletionStepsMap[kvp.Key] = steps;
                    if (steps is 0 || force)
                    {
                        kvp.Value.SetResult(true);
                        keysToRemove.Add(kvp.Key);
                    }
                }

                foreach (var key in keysToRemove)
                {
                    this.TaskDelayMap.Remove(key);
                    this.CompletionStepsMap.Remove(key);
                }
            }
        }
    }
}
