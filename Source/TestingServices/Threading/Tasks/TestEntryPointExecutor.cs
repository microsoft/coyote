// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.TestingServices.Runtime;
using Microsoft.Coyote.Threading.Tasks;

namespace Microsoft.Coyote.TestingServices.Threading.Tasks
{
    /// <summary>
    /// Provides the capability to control and execute an asynchronous test entry point.
    /// </summary>
    internal sealed class TestEntryPointExecutor : ControlledTaskExecutor
    {
        /// <summary>
        /// Test to be executed asynchronously.
        /// </summary>
        private readonly Delegate Test;

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
        /// Initializes a new instance of the <see cref="TestEntryPointExecutor"/> class.
        /// </summary>
        [DebuggerStepThrough]
        internal TestEntryPointExecutor(SystematicTestingRuntime runtime, Delegate test)
            : base(runtime)
        {
            this.Test = test;
            this.Awaiter = new TaskCompletionSource<object>();
        }

        /// <summary>
        /// Executes the work asynchronously.
        /// </summary>
        [DebuggerHidden]
        internal override async Task ExecuteAsync()
        {
            IO.Debug.WriteLine($"'{this.Id}' is executing test on task '{ControlledTask.CurrentId}' (tcs: {this.Awaiter.Task.Id})");

            if (this.Test is Action<IActorRuntime> actionWithRuntime)
            {
                actionWithRuntime(this.Runtime);
            }
            else if (this.Test is Action action)
            {
                action();
            }
            else if (this.Test is Func<IActorRuntime, ControlledTask> functionWithRuntime)
            {
                await functionWithRuntime(this.Runtime);
            }
            else if (this.Test is Func<ControlledTask> function)
            {
                await function();
            }
            else
            {
                throw new InvalidOperationException($"Unsupported test delegate of type '{this.Test?.GetType()}'.");
            }

            IO.Debug.WriteLine($"'{this.Id}' executed test on task '{ControlledTask.CurrentId}' (tcs: {this.Awaiter.Task.Id})");
            this.Awaiter.SetResult(default);
            IO.Debug.WriteLine($"'{this.Id}' completed test on task '{ControlledTask.CurrentId}' (tcs: {this.Awaiter.Task.Id})");
        }

        /// <summary>
        /// Tries to complete the work with the specified exception.
        /// </summary>
        [DebuggerStepThrough]
        internal override void TryCompleteWithException(Exception exception)
        {
            // The entry point of a test should always report
            // an unhandled exception as an error.
            throw exception;
        }
    }
}
