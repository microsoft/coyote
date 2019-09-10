// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;

using Microsoft.Coyote.TestingServices.Runtime;
using Microsoft.Coyote.Threading.Tasks;

namespace Microsoft.Coyote.TestingServices.Threading.Tasks
{
    /// <summary>
    /// Implements a machine that can execute a delay asynchronously.
    /// </summary>
    internal sealed class DelayWorkMachine : WorkMachine
    {
        /// <summary>
        /// Provides the capability to await for work completion.
        /// </summary>
        private readonly TaskCompletionSource<object> Awaiter;

        /// <summary>
        /// Task that provides access to the completed work.
        /// </summary>
        internal Task AwaiterTask => this.Awaiter.Task;

        /// <summary>
        /// The id of the task that provides access to the completed work.
        /// </summary>
        internal override int AwaiterTaskId => this.AwaiterTask.Id;

        /// <summary>
        /// Initializes a new instance of the <see cref="DelayWorkMachine"/> class.
        /// </summary>
        internal DelayWorkMachine(SystematicTestingRuntime runtime)
            : base(runtime)
        {
            this.Awaiter = new TaskCompletionSource<object>();
        }

        /// <summary>
        /// Executes the work asynchronously.
        /// </summary>
        internal override Task ExecuteAsync()
        {
            IO.Debug.WriteLine($"Machine '{this.Id}' is performing a delay on task '{ControlledTask.CurrentId}' (tcs: {this.Awaiter.Task.Id})");
            this.Awaiter.SetResult(default);
            IO.Debug.WriteLine($"Machine '{this.Id}' completed a delay on task '{ControlledTask.CurrentId}' (tcs: {this.Awaiter.Task.Id})");
            return Task.CompletedTask;
        }

        /// <summary>
        /// Tries to complete the machine with the specified exception.
        /// </summary>
        internal override void TryCompleteWithException(Exception exception)
        {
            this.Awaiter.SetException(exception);
        }
    }
}
