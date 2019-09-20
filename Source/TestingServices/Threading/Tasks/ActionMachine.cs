// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Coyote.TestingServices.Runtime;
using Microsoft.Coyote.Threading.Tasks;

namespace Microsoft.Coyote.TestingServices.Threading.Tasks
{
    /// <summary>
    /// Implements a machine that can execute an <see cref="Action"/> asynchronously.
    /// </summary>
    internal sealed class ActionMachine : ControlledTaskMachine
    {
        /// <summary>
        /// Work to be executed asynchronously.
        /// </summary>
        private readonly Action Work;

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
        /// Initializes a new instance of the <see cref="ActionMachine"/> class.
        /// </summary>
        [DebuggerStepThrough]
        internal ActionMachine(SystematicTestingRuntime runtime, Action work)
            : base(runtime)
        {
            this.Work = work;
            this.Awaiter = new TaskCompletionSource<object>();
        }

        /// <summary>
        /// Executes the work asynchronously.
        /// </summary>
        [DebuggerHidden]
        internal override Task ExecuteAsync()
        {
            IO.Debug.WriteLine($"Machine '{this.Id}' is executing action on task '{ControlledTask.CurrentId}' (tcs: {this.Awaiter.Task.Id})");
            this.Work();
            IO.Debug.WriteLine($"Machine '{this.Id}' executed action on task '{ControlledTask.CurrentId}' (tcs: {this.Awaiter.Task.Id})");
            this.Awaiter.SetResult(default);
            IO.Debug.WriteLine($"Machine '{this.Id}' completed action on task '{ControlledTask.CurrentId}' (tcs: {this.Awaiter.Task.Id})");
            return Task.CompletedTask;
        }

        /// <summary>
        /// Tries to complete the machine with the specified exception.
        /// </summary>
        [DebuggerStepThrough]
        internal override void TryCompleteWithException(Exception exception)
        {
            this.Awaiter.SetException(exception);
        }
    }
}
