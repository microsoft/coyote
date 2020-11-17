// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Threading.Tasks;

namespace Microsoft.Coyote.Specifications
{
    /// <summary>
    /// A monitor that checks if a task eventually completes execution successfully.
    /// </summary>
    internal sealed class TaskLivenessMonitor
    {
        /// <summary>
        /// The task being monitored.
        /// </summary>
        private readonly Task Task;

        /// <summary>
        /// A counter that increases in each step of the execution, as long as the property
        /// has not been satisfied. If the temperature reaches the specified limit, then
        /// a potential liveness bug has been found.
        /// </summary>
        private int LivenessTemperature;

        /// <summary>
        /// True if the liveness property is satisfied, else false.
        /// </summary>
        internal bool IsSatisfied => this.Task.Status is TaskStatus.RanToCompletion;

        /// <summary>
        /// Trace used for debugging purposes.
        /// </summary>
        internal StackTrace StackTrace { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskLivenessMonitor"/> class.
        /// </summary>
        internal TaskLivenessMonitor(Task task)
        {
            this.Task = task;
            this.LivenessTemperature = 0;
            this.StackTrace = new StackTrace();
        }

        /// <summary>
        /// Checks the liveness temperature of the monitor and report a potential liveness bug if the
        /// the value exceeded the specified threshold.
        /// </summary>
        internal bool IsLivenessThresholdExceeded(int threshold)
        {
            if (!this.IsSatisfied && threshold > 0)
            {
                this.LivenessTemperature++;
                if (this.LivenessTemperature > threshold)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
