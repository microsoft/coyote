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
    /// Implements a machine that can execute a <see cref="Func{Task}"/> asynchronously.
    /// </summary>
    internal sealed class FuncWorkMachine : WorkMachine
    {
        /// <summary>
        /// Work to be executed asynchronously.
        /// </summary>
        private readonly Func<Task> Work;

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
        /// Initializes a new instance of the <see cref="FuncWorkMachine"/> class.
        /// </summary>
        internal FuncWorkMachine(SystematicTestingRuntime runtime, Func<Task> work)
            : base(runtime)
        {
            this.Work = work;
            this.Awaiter = new TaskCompletionSource<object>();
        }

        /// <summary>
        /// Executes the work asynchronously.
        /// </summary>
        internal override async Task ExecuteAsync()
        {
            IO.Debug.WriteLine($"Machine '{this.Id}' is executing function on task '{ControlledTask.CurrentId}' (tcs: {this.Awaiter.Task.Id})");
            Task task = this.Work();
            this.Runtime.NotifyWaitTask(this, task);
            await task;
            IO.Debug.WriteLine($"Machine '{this.Id}' executed function on task '{ControlledTask.CurrentId}' (tcs: {this.Awaiter.Task.Id})");
            this.Awaiter.SetResult(default);
            IO.Debug.WriteLine($"Machine '{this.Id}' completed function on task '{ControlledTask.CurrentId}' (tcs: {this.Awaiter.Task.Id})");
        }

        /// <summary>
        /// Tries to complete the machine with the specified exception.
        /// </summary>
        internal override void TryCompleteWithException(Exception exception)
        {
            this.Awaiter.SetException(exception);
        }
    }

    /// <summary>
    /// Implements a machine that can execute a <see cref="Func{TResult}"/> asynchronously.
    /// </summary>
    internal sealed class FuncWorkMachine<TResult> : WorkMachine
    {
        /// <summary>
        /// Work to be executed asynchronously.
        /// </summary>
        private readonly Func<TResult> Work;

        /// <summary>
        /// Provides the capability to await for work completion.
        /// </summary>
        private readonly TaskCompletionSource<TResult> Awaiter;

        /// <summary>
        /// Task that provides access to the completed work.
        /// </summary>
        internal Task<TResult> AwaiterTask => this.Awaiter.Task;

        /// <summary>
        /// The id of the task that provides access to the completed work.
        /// </summary>
        internal override int AwaiterTaskId => this.AwaiterTask.Id;

        /// <summary>
        /// Initializes a new instance of the <see cref="FuncWorkMachine{TResult}"/> class.
        /// </summary>
        internal FuncWorkMachine(SystematicTestingRuntime runtime, Func<TResult> work)
            : base(runtime)
        {
            this.Work = work;
            this.Awaiter = new TaskCompletionSource<TResult>();
        }

        /// <summary>
        /// Executes the work asynchronously.
        /// </summary>
        internal override async Task ExecuteAsync()
        {
            IO.Debug.WriteLine($"Machine '{this.Id}' is executing function on task '{ControlledTask.CurrentId}' (tcs: {this.Awaiter.Task.Id})");

            TResult result = default;
            if (this.Work is Func<ControlledTask> taskFunc)
            {
                var task = taskFunc();
                this.Runtime.NotifyWaitTask(this, task.AwaiterTask);
                await task;
                if (task is TResult controlledTask)
                {
                    result = controlledTask;
                }
            }
            else
            {
                result = this.Work();
            }

            IO.Debug.WriteLine($"Machine '{this.Id}' executed function on task '{ControlledTask.CurrentId}' (tcs: {this.Awaiter.Task.Id})");
            this.Awaiter.SetResult(result);
            IO.Debug.WriteLine($"Machine '{this.Id}' completed function on task '{ControlledTask.CurrentId}' (tcs: {this.Awaiter.Task.Id})");
        }

        /// <summary>
        /// Tries to complete the machine with the specified exception.
        /// </summary>
        internal override void TryCompleteWithException(Exception exception)
        {
            this.Awaiter.SetException(exception);
        }
    }

    /// <summary>
    /// Implements a machine that can execute a <see cref="Func{TResult}"/> asynchronously.
    /// </summary>
    internal sealed class FuncTaskWorkMachine<TResult> : WorkMachine
    {
        /// <summary>
        /// Work to be executed asynchronously.
        /// </summary>
        private readonly Func<Task<TResult>> Work;

        /// <summary>
        /// Provides the capability to await for work completion.
        /// </summary>
        private readonly TaskCompletionSource<TResult> Awaiter;

        /// <summary>
        /// Task that provides access to the completed work.
        /// </summary>
        internal Task<TResult> AwaiterTask => this.Awaiter.Task;

        /// <summary>
        /// The id of the task that provides access to the completed work.
        /// </summary>
        internal override int AwaiterTaskId => this.AwaiterTask.Id;

        /// <summary>
        /// Initializes a new instance of the <see cref="FuncTaskWorkMachine{TResult}"/> class.
        /// </summary>
        internal FuncTaskWorkMachine(SystematicTestingRuntime runtime, Func<Task<TResult>> work)
            : base(runtime)
        {
            this.Work = work;
            this.Awaiter = new TaskCompletionSource<TResult>();
        }

        /// <summary>
        /// Executes the work asynchronously.
        /// </summary>
        internal override async Task ExecuteAsync()
        {
            IO.Debug.WriteLine($"Machine '{this.Id}' is executing function on task '{ControlledTask.CurrentId}' (tcs: {this.Awaiter.Task.Id})");
            Task<TResult> task = this.Work();
            IO.Debug.WriteLine($"Machine '{this.Id}' is getting result on task '{ControlledTask.CurrentId}' (tcs: {this.Awaiter.Task.Id})");
            this.Runtime.NotifyWaitTask(this, task);
            TResult result = await task;
            IO.Debug.WriteLine($"Machine '{this.Id}' executed function on task '{ControlledTask.CurrentId}' (tcs: {this.Awaiter.Task.Id})");
            this.Awaiter.SetResult(result);
            IO.Debug.WriteLine($"Machine '{this.Id}' completed function on task '{ControlledTask.CurrentId}' (tcs: {this.Awaiter.Task.Id})");
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
