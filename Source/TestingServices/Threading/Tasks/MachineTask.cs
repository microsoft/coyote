// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.TestingServices.Runtime;
using Microsoft.Coyote.TestingServices.Scheduling;
using Microsoft.Coyote.Threading.Tasks;

namespace Microsoft.Coyote.TestingServices.Threading.Tasks
{
    /// <summary>
    /// A <see cref="ControlledTask"/> that is controlled by the runtime scheduler.
    /// </summary>
    internal sealed class MachineTask : ControlledTask
    {
        /// <summary>
        /// The testing runtime executing this task.
        /// </summary>
        private readonly SystematicTestingRuntime Runtime;

        /// <summary>
        /// Initializes a new instance of the <see cref="MachineTask"/> class.
        /// </summary>
        [DebuggerStepThrough]
        internal MachineTask(SystematicTestingRuntime runtime, Task task)
            : base(task)
        {
            IO.Debug.WriteLine("<ControlledTask> Creating task '{0}' from task '{1}'.", task.Id, Task.CurrentId);
            this.Runtime = runtime;
        }

        /// <summary>
        /// Waits for the task to complete execution.
        /// </summary>
        public override void Wait()
        {
            var callerOp = this.Runtime.Scheduler.GetExecutingOperation<TaskOperation>();
            IO.Debug.WriteLine("<ControlledTask> '{0}' is waiting task '{1}' to complete from task '{2}'.",
                callerOp.Name, this.Id, Task.CurrentId);
            callerOp.OnWaitTask(this.AwaiterTask);
        }

        /// <summary>
        /// Waits for the task to complete execution within a specified number of milliseconds.
        /// </summary>
        public override bool Wait(int millisecondsTimeout) => this.Wait(millisecondsTimeout, default);

        /// <summary>
        /// Waits for the task to complete execution. The wait terminates if a timeout interval
        /// elapses or a cancellation token is canceled before the task completes.
        /// </summary>
        public override bool Wait(int millisecondsTimeout, CancellationToken cancellationToken)
        {
            // TODO: support timeouts and cancellation tokens.
            this.Wait();
            return true;
        }

        /// <summary>
        /// Waits for the task to complete execution within a specified time interval.
        /// </summary>
        public override bool Wait(TimeSpan timeout) => this.Wait((int)timeout.TotalMilliseconds, default);

        /// <summary>
        /// Waits for the task to complete execution. The wait terminates if
        /// a cancellation token is canceled before the task completes.
        /// </summary>
        public override void Wait(CancellationToken cancellationToken) => this.Wait();

        /// <summary>
        /// Gets an awaiter for this awaitable.
        /// </summary>
        [DebuggerHidden]
        public override ControlledTaskAwaiter GetAwaiter()
        {
            var callerOp = this.Runtime.Scheduler.GetExecutingOperation<TaskOperation>();
            callerOp.OnGetControlledAwaiter();
            return new ControlledTaskAwaiter(this, this.AwaiterTask);
        }

        /// <summary>
        /// Ends the wait for the completion of the task.
        /// </summary>
        [DebuggerHidden]
        internal override void GetResult(TaskAwaiter awaiter)
        {
            var callerOp = this.Runtime.Scheduler.GetExecutingOperation<TaskOperation>();
            callerOp.OnWaitTask(this.AwaiterTask);
            awaiter.GetResult();
        }

        /// <summary>
        /// Sets the action to perform when the task completes.
        /// </summary>
        [DebuggerHidden]
        internal override void OnCompleted(Action continuation, TaskAwaiter awaiter) =>
            this.DispatchWork(continuation);

        /// <summary>
        /// Schedules the continuation action that is invoked when the task completes.
        /// </summary>
        [DebuggerHidden]
        internal override void UnsafeOnCompleted(Action continuation, TaskAwaiter awaiter) =>
            this.DispatchWork(continuation);

        /// <summary>
        /// Configures an awaiter used to await this task.
        /// </summary>
        [DebuggerHidden]
        public override ConfiguredControlledTaskAwaitable ConfigureAwait(bool continueOnCapturedContext)
        {
            var callerOp = this.Runtime.Scheduler.GetExecutingOperation<TaskOperation>();
            callerOp.OnGetControlledAwaiter();
            return new ConfiguredControlledTaskAwaitable(this, this.AwaiterTask, continueOnCapturedContext);
        }

        /// <summary>
        /// Ends the wait for the completion of the task.
        /// </summary>
        [DebuggerHidden]
        internal override void GetResult(ConfiguredTaskAwaitable.ConfiguredTaskAwaiter awaiter)
        {
            var callerOp = this.Runtime.Scheduler.GetExecutingOperation<TaskOperation>();
            IO.Debug.WriteLine("<ControlledTask> '{0}' is waiting task '{1}' to complete from task '{2}'.",
                callerOp.Name, this.Id, Task.CurrentId);
            callerOp.OnWaitTask(this.AwaiterTask);
            awaiter.GetResult();
        }

        /// <summary>
        /// Sets the action to perform when the task completes.
        /// </summary>
        [DebuggerHidden]
        internal override void OnCompleted(Action continuation, ConfiguredTaskAwaitable.ConfiguredTaskAwaiter awaiter) =>
            this.DispatchWork(continuation);

        /// <summary>
        /// Schedules the continuation action that is invoked when the task completes.
        /// </summary>
        [DebuggerHidden]
        internal override void UnsafeOnCompleted(Action continuation, ConfiguredTaskAwaitable.ConfiguredTaskAwaiter awaiter) =>
            this.DispatchWork(continuation);

        /// <summary>
        /// Dispatches the work.
        /// </summary>
        [DebuggerHidden]
        private void DispatchWork(Action continuation)
        {
            try
            {
                var callerOp = this.Runtime.Scheduler.GetExecutingOperation<TaskOperation>();
                this.Runtime.Assert(callerOp != null,
                    "Task with id '{0}' that is not controlled by the runtime is executing controlled task '{1}'.",
                    Task.CurrentId.HasValue ? Task.CurrentId.Value.ToString() : "<unknown>", this.Id);

                if (callerOp.IsExecutingInRootAsyncMethod())
                {
                    IO.Debug.WriteLine("<ControlledTask> '{0}' is executing continuation of task '{1}' on task '{2}'.",
                        callerOp.Name, this.Id, Task.CurrentId);
                    continuation();
                    IO.Debug.WriteLine("<ControlledTask> '{0}' resumed after continuation of task '{1}' on task '{2}'.",
                        callerOp.Name, this.Id, Task.CurrentId);
                }
                else
                {
                    IO.Debug.WriteLine("<ControlledTask> '{0}' is dispatching continuation of task '{1}'.", callerOp.Name, this.Id);
                    this.Runtime.DispatchWork(new ActionExecutor(this.Runtime, continuation), this.AwaiterTask);
                    IO.Debug.WriteLine("<ControlledTask> '{0}' dispatched continuation of task '{1}'.", callerOp.Name, this.Id);
                }
            }
            catch (ExecutionCanceledException)
            {
                IO.Debug.WriteLine($"<Exception> ExecutionCanceledException was thrown from task '{Task.CurrentId}'.");
            }
        }
    }

    /// <summary>
    /// A <see cref="ControlledTask{TResult}"/> that is controlled by the runtime scheduler.
    /// </summary>
    internal sealed class MachineTask<TResult> : ControlledTask<TResult>
    {
        /// <summary>
        /// The testing runtime executing this task.
        /// </summary>
        private readonly SystematicTestingRuntime Runtime;

        /// <summary>
        /// Gets the result value of this task.
        /// </summary>
        public override TResult Result
        {
            get
            {
                var callerOp = this.Runtime.Scheduler.GetExecutingOperation<TaskOperation>();
                IO.Debug.WriteLine("<ControlledTask> '{0}' is waiting task '{1}' with result type '{2}' to complete from task '{3}'.",
                    callerOp.Name, this.Id, typeof(TResult), Task.CurrentId);
                callerOp.OnWaitTask(this.AwaiterTask);
                return this.AwaiterTask.Result;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MachineTask{TResult}"/> class.
        /// </summary>
        [DebuggerStepThrough]
        internal MachineTask(SystematicTestingRuntime runtime, Task<TResult> task)
            : base(task)
        {
            IO.Debug.WriteLine("<ControlledTask> Creating task '{0}' with result type '{1}' from task '{2}'.",
                task.Id, typeof(TResult), Task.CurrentId);
            this.Runtime = runtime;
        }

        /// <summary>
        /// Gets an awaiter for this awaitable.
        /// </summary>
        [DebuggerHidden]
        public override ControlledTaskAwaiter<TResult> GetAwaiter()
        {
            var callerOp = this.Runtime.Scheduler.GetExecutingOperation<TaskOperation>();
            callerOp.OnGetControlledAwaiter();
            return new ControlledTaskAwaiter<TResult>(this, this.AwaiterTask);
        }

        /// <summary>
        /// Ends the wait for the completion of the task.
        /// </summary>
        [DebuggerHidden]
        internal override TResult GetResult(TaskAwaiter<TResult> awaiter)
        {
            var callerOp = this.Runtime.Scheduler.GetExecutingOperation<TaskOperation>();
            callerOp.OnWaitTask(this.AwaiterTask);
            return awaiter.GetResult();
        }

        /// <summary>
        /// Sets the action to perform when the task completes.
        /// </summary>
        [DebuggerHidden]
        internal override void OnCompleted(Action continuation, TaskAwaiter<TResult> awaiter) =>
            this.DispatchWork(continuation);

        /// <summary>
        /// Schedules the continuation action that is invoked when the task completes.
        /// </summary>
        [DebuggerHidden]
        internal override void UnsafeOnCompleted(Action continuation, TaskAwaiter<TResult> awaiter) =>
            this.DispatchWork(continuation);

        /// <summary>
        /// Configures an awaiter used to await this task.
        /// </summary>
        [DebuggerHidden]
        public override ConfiguredControlledTaskAwaitable<TResult> ConfigureAwait(bool continueOnCapturedContext)
        {
            var callerOp = this.Runtime.Scheduler.GetExecutingOperation<TaskOperation>();
            callerOp.OnGetControlledAwaiter();
            return new ConfiguredControlledTaskAwaitable<TResult>(this, this.AwaiterTask, continueOnCapturedContext);
        }

        /// <summary>
        /// Ends the wait for the completion of the task.
        /// </summary>
        [DebuggerHidden]
        internal override TResult GetResult(ConfiguredTaskAwaitable<TResult>.ConfiguredTaskAwaiter awaiter)
        {
            var callerOp = this.Runtime.Scheduler.GetExecutingOperation<TaskOperation>();
            IO.Debug.WriteLine("<ControlledTask> '{0}' is waiting task '{1}' with result type '{2}' to complete from task '{3}'.",
                callerOp.Name, this.Id, typeof(TResult), Task.CurrentId);
            callerOp.OnWaitTask(this.AwaiterTask);
            return awaiter.GetResult();
        }

        /// <summary>
        /// Sets the action to perform when the task completes.
        /// </summary>
        [DebuggerHidden]
        internal override void OnCompleted(Action continuation, ConfiguredTaskAwaitable<TResult>.ConfiguredTaskAwaiter awaiter) =>
            this.DispatchWork(continuation);

        /// <summary>
        /// Schedules the continuation action that is invoked when the task completes.
        /// </summary>
        [DebuggerHidden]
        internal override void UnsafeOnCompleted(Action continuation, ConfiguredTaskAwaitable<TResult>.ConfiguredTaskAwaiter awaiter) =>
            this.DispatchWork(continuation);

        /// <summary>
        /// Dispatches the work.
        /// </summary>
        [DebuggerHidden]
        private void DispatchWork(Action continuation)
        {
            try
            {
                var callerOp = this.Runtime.Scheduler.GetExecutingOperation<TaskOperation>();
                this.Runtime.Assert(callerOp != null,
                    "Task with id '{0}' that is not controlled by the runtime is executing controlled task '{1}'.",
                    Task.CurrentId.HasValue ? Task.CurrentId.Value.ToString() : "<unknown>", this.Id);

                if (callerOp.IsExecutingInRootAsyncMethod())
                {
                    IO.Debug.WriteLine("<ControlledTask> '{0}' is executing continuation of task '{1}' with result type '{2}' on task '{3}'.",
                        callerOp.Name, this.Id, typeof(TResult), Task.CurrentId);
                    continuation();
                    IO.Debug.WriteLine("<ControlledTask> '{0}' resumed after continuation of task '{1}' with result type '{2}' on task '{3}'.",
                        callerOp.Name, this.Id, typeof(TResult), Task.CurrentId);
                }
                else
                {
                    IO.Debug.WriteLine("<ControlledTask> '{0}' is dispatching continuation of task '{1}' with result type '{2}'.",
                        callerOp.Name, this.Id, typeof(TResult));
                    this.Runtime.DispatchWork(new ActionExecutor(this.Runtime, continuation), this.AwaiterTask);
                    IO.Debug.WriteLine("<ControlledTask> '{0}' dispatched continuation of task '{1}' with result type '{2}'.",
                        callerOp.Name, this.Id, typeof(TResult));
                }
            }
            catch (ExecutionCanceledException)
            {
                IO.Debug.WriteLine($"<Exception> ExecutionCanceledException was thrown from task '{Task.CurrentId}'.");
            }
        }
    }
}
