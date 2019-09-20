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
    /// Implements a machine that can execute a <see cref="Func{ControlledTask}"/> asynchronously.
    /// </summary>
    internal sealed class FuncMachine : ControlledTaskMachine
    {
        /// <summary>
        /// Work to be executed asynchronously.
        /// </summary>
        private readonly Func<ControlledTask> Work;

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
        /// Initializes a new instance of the <see cref="FuncMachine"/> class.
        /// </summary>
        [DebuggerStepThrough]
        internal FuncMachine(SystematicTestingRuntime runtime, Func<ControlledTask> work)
            : base(runtime)
        {
            this.Work = work;
            this.Awaiter = new TaskCompletionSource<object>();
        }

        /// <summary>
        /// Executes the work asynchronously.
        /// </summary>
        [DebuggerHidden]
        internal override async Task ExecuteAsync()
        {
            IO.Debug.WriteLine($"Machine '{this.Id}' is executing function on task '{ControlledTask.CurrentId}' (tcs: {this.Awaiter.Task.Id})");
            ControlledTask task = this.Work();
            this.Runtime.NotifyWaitTask(this, task.AwaiterTask);
            await task;
            IO.Debug.WriteLine($"Machine '{this.Id}' executed function on task '{ControlledTask.CurrentId}' (tcs: {this.Awaiter.Task.Id})");
            this.Awaiter.SetResult(default);
            IO.Debug.WriteLine($"Machine '{this.Id}' completed function on task '{ControlledTask.CurrentId}' (tcs: {this.Awaiter.Task.Id})");
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

    /// <summary>
    /// Implements a machine that can execute a <see cref="Func{TResult}"/> asynchronously.
    /// </summary>
    internal sealed class FuncMachine<TResult> : ControlledTaskMachine
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
        /// Initializes a new instance of the <see cref="FuncMachine{TResult}"/> class.
        /// </summary>
        [DebuggerStepThrough]
        internal FuncMachine(SystematicTestingRuntime runtime, Func<TResult> work)
            : base(runtime)
        {
            this.Work = work;
            this.Awaiter = new TaskCompletionSource<TResult>();
        }

        /// <summary>
        /// Executes the work asynchronously.
        /// </summary>
        [DebuggerHidden]
        internal override async Task ExecuteAsync()
        {
            IO.Debug.WriteLine($"Machine '{this.Id}' is executing function on task '{ControlledTask.CurrentId}' (tcs: {this.Awaiter.Task.Id})");

            TResult result = this.Work();
            if (this.Work is Func<Task> taskFunc)
            {
                var task = taskFunc();
                this.Runtime.NotifyWaitTask(this, task);
                await task;
                if (task is TResult resultTask)
                {
                    result = resultTask;
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
        [DebuggerStepThrough]
        internal override void TryCompleteWithException(Exception exception)
        {
            this.Awaiter.SetException(exception);
        }
    }

    /// <summary>
    /// Implements a machine that can execute a <see cref="Func{TResult}"/> asynchronously.
    /// </summary>
    internal sealed class FuncTaskMachine<TResult> : ControlledTaskMachine
    {
        /// <summary>
        /// Work to be executed asynchronously.
        /// </summary>
        private readonly Func<ControlledTask<TResult>> Work;

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
        /// Initializes a new instance of the <see cref="FuncTaskMachine{TResult}"/> class.
        /// </summary>
        [DebuggerStepThrough]
        internal FuncTaskMachine(SystematicTestingRuntime runtime, Func<ControlledTask<TResult>> work)
            : base(runtime)
        {
            this.Work = work;
            this.Awaiter = new TaskCompletionSource<TResult>();
        }

        /// <summary>
        /// Executes the work asynchronously.
        /// </summary>
        [DebuggerHidden]
        internal override async Task ExecuteAsync()
        {
            IO.Debug.WriteLine($"Machine '{this.Id}' is executing function on task '{ControlledTask.CurrentId}' (tcs: {this.Awaiter.Task.Id})");
            ControlledTask<TResult> task = this.Work();
            IO.Debug.WriteLine($"Machine '{this.Id}' is getting result on task '{ControlledTask.CurrentId}' (tcs: {this.Awaiter.Task.Id})");
            this.Runtime.NotifyWaitTask(this, task.AwaiterTask);
            TResult result = await task;
            IO.Debug.WriteLine($"Machine '{this.Id}' executed function on task '{ControlledTask.CurrentId}' (tcs: {this.Awaiter.Task.Id})");
            this.Awaiter.SetResult(result);
            IO.Debug.WriteLine($"Machine '{this.Id}' completed function on task '{ControlledTask.CurrentId}' (tcs: {this.Awaiter.Task.Id})");
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
