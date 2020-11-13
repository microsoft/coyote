// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Microsoft.Coyote.SystematicTesting
{
    /// <summary>
    /// Represents an asynchronous task delay operation that can be controlled during systematic testing.
    /// </summary>
    internal class TaskDelayOperation : TaskOperation
    {
        /// <summary>
        /// The value until the operation may complete.
        /// </summary>
        internal int Timeout;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskDelayOperation"/> class.
        /// </summary>
        internal TaskDelayOperation(ulong operationId, string name, TimeSpan delay, OperationScheduler scheduler)
            : base(operationId, name, scheduler)
        {
            // TODO: do we need some normalization of delay here?
            this.Timeout = (int)delay.TotalMilliseconds;
        }

        /// <summary>
        /// Delays the execution of the operation.
        /// </summary>
        internal void DelayUntilTimeout()
        {
            IO.Debug.WriteLine("<ScheduleDebug> Try delay operation '{0}' for '{1}' delay.", this.Id, this.Timeout);
            this.Timeout = this.Scheduler.GetNextNondeterministicIntegerChoice(this.Timeout);
            IO.Debug.WriteLine("<ScheduleDebug> Try delay operation '{0}' for '{1}' delay.", this.Id, this.Timeout);
            this.Status = AsyncOperationStatus.Delayed;
            this.Scheduler.ScheduleNextOperation();
        }

        /// <inheritdoc/>
        internal override bool TryEnable()
        {
            if (this.Status is AsyncOperationStatus.Delayed)
            {
                if (this.Timeout > 0)
                {
                    this.Timeout--;
                    return false;
                }

                IO.Debug.WriteLine("<ScheduleDebug> Operation '{0}' is undelayed.", this.Id);
                this.Status = AsyncOperationStatus.Enabled;
                return true;
            }

            return base.TryEnable();
        }
    }
}
