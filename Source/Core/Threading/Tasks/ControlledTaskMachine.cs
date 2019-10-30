// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Runtime;

namespace Microsoft.Coyote.Threading.Tasks
{
    /// <summary>
    /// Abstract machine that can execute a <see cref="ControlledTask"/> asynchronously.
    /// </summary>
    internal abstract class ControlledTaskMachine : Actor
    {
        /// <summary>
        /// The id of the task that provides access to the completed work.
        /// </summary>
        internal abstract int AwaiterTaskId { get; }

        /// <summary>
        /// Id used to identify subsequent operations performed by this machine.
        /// </summary>
        protected internal override Guid OperationGroupId { get; set; } = Guid.Empty;

        /// <summary>
        /// Initializes a new instance of the <see cref="ControlledTaskMachine"/> class.
        /// </summary>
        [DebuggerStepThrough]
        internal ControlledTaskMachine(CoyoteRuntime runtime)
        {
            var id = new ActorId(this.GetType(), "ControlledTask", runtime);
            this.Initialize(runtime, id);
        }

        /// <summary>
        /// Executes the work asynchronously.
        /// </summary>
        internal abstract Task ExecuteAsync();

        /// <summary>
        /// Tries to complete the machine with the specified exception.
        /// </summary>
        internal abstract void TryCompleteWithException(Exception exception);
    }
}
