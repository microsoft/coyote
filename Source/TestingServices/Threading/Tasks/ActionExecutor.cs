// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Coyote.TestingServices.Runtime;
using Microsoft.Coyote.Threading.Tasks;

namespace Microsoft.Coyote.TestingServices.Threading.Tasks
{
    /// <summary>
    /// Provides the capability to control and execute an asynchronous
    /// <see cref="Action"/> during testing.
    /// </summary>
    internal sealed class ActionExecutor : ControlledTaskExecutor
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
        /// Initializes a new instance of the <see cref="ActionExecutor"/> class.
        /// </summary>
        [DebuggerStepThrough]
        internal ActionExecutor(SystematicTestingRuntime runtime, Action work)
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
            IO.Debug.WriteLine($"'{this.Id}' is executing action on task '{ControlledTask.CurrentId}' (tcs: {this.Awaiter.Task.Id})");
            this.Work();
            IO.Debug.WriteLine($"'{this.Id}' executed action on task '{ControlledTask.CurrentId}' (tcs: {this.Awaiter.Task.Id})");
            this.Awaiter.SetResult(default);
            IO.Debug.WriteLine($"'{this.Id}' completed action on task '{ControlledTask.CurrentId}' (tcs: {this.Awaiter.Task.Id})");
            return Task.CompletedTask;
        }

        /// <summary>
        /// Tries to complete the work with the specified exception.
        /// </summary>
        [DebuggerStepThrough]
        internal override void TryCompleteWithException(Exception exception)
        {
            this.Awaiter.SetException(exception);
        }
    }
}
