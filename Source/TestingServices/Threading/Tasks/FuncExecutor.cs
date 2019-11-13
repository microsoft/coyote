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
    /// <see cref="Func{ControlledTask}"/> during testing.
    /// </summary>
    internal sealed class FuncExecutor : ControlledTaskExecutor
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
        /// Initializes a new instance of the <see cref="FuncExecutor"/> class.
        /// </summary>
        [DebuggerStepThrough]
        internal FuncExecutor(SystematicTestingRuntime runtime, Func<ControlledTask> work)
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
            IO.Debug.WriteLine($"'{this.Id}' is executing function on task '{ControlledTask.CurrentId}' (tcs: {this.Awaiter.Task.Id})");
            ControlledTask task = this.Work();
            this.Runtime.NotifyWaitTask(this, task.AwaiterTask);
            await task;
            IO.Debug.WriteLine($"'{this.Id}' executed function on task '{ControlledTask.CurrentId}' (tcs: {this.Awaiter.Task.Id})");
            this.Awaiter.SetResult(default);
            IO.Debug.WriteLine($"'{this.Id}' completed function on task '{ControlledTask.CurrentId}' (tcs: {this.Awaiter.Task.Id})");
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

    /// <summary>
    /// Provides the capability to control and execute an asynchronous
    /// <see cref="Func{TResult}"/> during testing.
    /// </summary>
    internal sealed class FuncExecutor<TResult> : ControlledTaskExecutor
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
        /// Initializes a new instance of the <see cref="FuncExecutor{TResult}"/> class.
        /// </summary>
        [DebuggerStepThrough]
        internal FuncExecutor(SystematicTestingRuntime runtime, Func<TResult> work)
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
            IO.Debug.WriteLine($"'{this.Id}' is executing function on task '{ControlledTask.CurrentId}' (tcs: {this.Awaiter.Task.Id})");

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

            IO.Debug.WriteLine($"'{this.Id}' executed function on task '{ControlledTask.CurrentId}' (tcs: {this.Awaiter.Task.Id})");
            this.Awaiter.SetResult(result);
            IO.Debug.WriteLine($"'{this.Id}' completed function on task '{ControlledTask.CurrentId}' (tcs: {this.Awaiter.Task.Id})");
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

    /// <summary>
    /// Provides the capability to control and execute an asynchronous
    /// <see cref="Func{TResult}"/> during testing.
    /// </summary>
    internal sealed class FuncTaskExecutor<TResult> : ControlledTaskExecutor
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
        /// Initializes a new instance of the <see cref="FuncTaskExecutor{TResult}"/> class.
        /// </summary>
        [DebuggerStepThrough]
        internal FuncTaskExecutor(SystematicTestingRuntime runtime, Func<ControlledTask<TResult>> work)
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
            IO.Debug.WriteLine($"'{this.Id}' is executing function on task '{ControlledTask.CurrentId}' (tcs: {this.Awaiter.Task.Id})");
            ControlledTask<TResult> task = this.Work();
            IO.Debug.WriteLine($"'{this.Id}' is getting result on task '{ControlledTask.CurrentId}' (tcs: {this.Awaiter.Task.Id})");
            this.Runtime.NotifyWaitTask(this, task.AwaiterTask);
            TResult result = await task;
            IO.Debug.WriteLine($"'{this.Id}' executed function on task '{ControlledTask.CurrentId}' (tcs: {this.Awaiter.Task.Id})");
            this.Awaiter.SetResult(result);
            IO.Debug.WriteLine($"'{this.Id}' completed function on task '{ControlledTask.CurrentId}' (tcs: {this.Awaiter.Task.Id})");
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
