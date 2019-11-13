// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Coyote.TestingServices.Runtime;
using Microsoft.Coyote.TestingServices.Scheduling;
using Microsoft.Coyote.Threading;
using Microsoft.Coyote.Threading.Tasks;

namespace Microsoft.Coyote.TestingServices.Threading
{
    /// <summary>
    /// A <see cref="ControlledLock"/> that is controlled by the runtime scheduler.
    /// </summary>
    internal sealed class MachineLock : ControlledLock
    {
        /// <summary>
        /// The testing runtime controlling this lock.
        /// </summary>
        private readonly SystematicTestingRuntime Runtime;

        /// <summary>
        /// Queue of operations awaiting to acquire the lock.
        /// </summary>
        private readonly Queue<TaskOperation> Awaiters;

        /// <summary>
        /// Initializes a new instance of the <see cref="MachineLock"/> class.
        /// </summary>
        internal MachineLock(SystematicTestingRuntime runtime, ulong id)
            : base(id)
        {
            this.Runtime = runtime;
            this.Awaiters = new Queue<TaskOperation>();
        }

        /// <summary>
        /// Tries to acquire the lock asynchronously, and returns a task that completes
        /// when the lock has been acquired. The returned task contains a releaser that
        /// releases the lock when disposed.
        /// </summary>
        public override ControlledTask<Releaser> AcquireAsync()
        {
            var callerOp = this.Runtime.Scheduler.GetExecutingOperation<TaskOperation>();
            if (this.IsAcquired)
            {
                this.Runtime.Logger.WriteLine("<SyncLog> '{0}' is waiting to acquire lock '{1}'.",
                    callerOp.Name, this.Id);
                this.Awaiters.Enqueue(callerOp);
                callerOp.Status = AsyncOperationStatus.BlockedOnResource;
            }

            this.IsAcquired = true;
            this.Runtime.Scheduler.ScheduleNextEnabledOperation();
            this.Runtime.Logger.WriteLine("<SyncLog> '{0}' is acquiring lock '{1}'.", callerOp.Name, this.Id);

            return ControlledTask.FromResult(new Releaser(this));
        }

        /// <summary>
        /// Releases the lock.
        /// </summary>
        protected override void Release()
        {
            this.IsAcquired = false;
            if (this.Awaiters.Count > 0)
            {
                TaskOperation awaiterOp = this.Awaiters.Dequeue();
                awaiterOp.Status = AsyncOperationStatus.Enabled;
            }

            var callerOp = this.Runtime.Scheduler.GetExecutingOperation<TaskOperation>();
            this.Runtime.Logger.WriteLine("<SyncLog> '{0}' is releasing lock '{1}'.", callerOp.Name, this.Id);
            this.Runtime.Scheduler.ScheduleNextEnabledOperation();
        }
    }
}
