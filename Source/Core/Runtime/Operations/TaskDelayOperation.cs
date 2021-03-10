// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.SystematicTesting;

namespace Microsoft.Coyote.Runtime
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
        internal TaskDelayOperation(ulong operationId, string name, uint delay, OperationScheduler scheduler)
            : base(operationId, name, scheduler)
        {
            this.Timeout = delay > int.MaxValue ? int.MaxValue : (int)delay;
        }

        /// <summary>
        /// Delays the execution of the operation.
        /// </summary>
        internal void DelayUntilTimeout()
        {
            this.Timeout = this.Scheduler.GetNextNondeterministicIntegerChoice(this.Timeout);
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

                this.Status = AsyncOperationStatus.Enabled;
                return true;
            }

            return base.TryEnable();
        }
    }
}
