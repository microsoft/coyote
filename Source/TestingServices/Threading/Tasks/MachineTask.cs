// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Coyote.Machines;
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
        /// The type of the task.
        /// </summary>
        private readonly MachineTaskType Type;

        /// <summary>
        /// Initializes a new instance of the <see cref="MachineTask"/> class.
        /// </summary>
        [DebuggerStepThrough]
        internal MachineTask(SystematicTestingRuntime runtime, Task task, MachineTaskType taskType)
            : base(task)
        {
            IO.Debug.WriteLine("<ControlledTask> Creating task '{0}' from task '{1}' (option: {2}).",
                task.Id, Task.CurrentId, taskType);
            this.Runtime = runtime;
            this.Type = taskType;
        }

        /// <summary>
        /// Gets an awaiter for this awaitable.
        /// </summary>
        [DebuggerHidden]
        public override ControlledTaskAwaiter GetAwaiter()
        {
            AsyncMachine caller = this.Runtime.GetExecutingMachine<AsyncMachine>();
            MachineOperation callerOp = this.Runtime.GetAsynchronousOperation(caller.Id.Value);
            callerOp.OnGetControlledAwaiter();
            return new ControlledTaskAwaiter(this, this.AwaiterTask);
        }

        /// <summary>
        /// Ends the wait for the completion of the task.
        /// </summary>
        [DebuggerHidden]
        internal override void GetResult(TaskAwaiter awaiter)
        {
            AsyncMachine caller = this.Runtime.GetExecutingMachine<AsyncMachine>();
            MachineOperation callerOp = this.Runtime.GetAsynchronousOperation(caller.Id.Value);
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
        /// <param name="continueOnCapturedContext">
        /// True to attempt to marshal the continuation back to the original context captured; otherwise, false.
        /// </param>
        [DebuggerHidden]
        public override ConfiguredControlledTaskAwaitable ConfigureAwait(bool continueOnCapturedContext)
        {
            AsyncMachine caller = this.Runtime.GetExecutingMachine<AsyncMachine>();
            MachineOperation callerOp = this.Runtime.GetAsynchronousOperation(caller.Id.Value);
            callerOp.OnGetControlledAwaiter();
            return new ConfiguredControlledTaskAwaitable(this, this.AwaiterTask, continueOnCapturedContext);
        }

        /// <summary>
        /// Ends the wait for the completion of the task.
        /// </summary>
        [DebuggerHidden]
        internal override void GetResult(ConfiguredTaskAwaitable.ConfiguredTaskAwaiter awaiter)
        {
            AsyncMachine caller = this.Runtime.GetExecutingMachine<AsyncMachine>();
            IO.Debug.WriteLine("<ControlledTask> Machine '{0}' is waiting task '{1}' to complete from task '{2}'.",
                caller.Id, this.Id, Task.CurrentId);
            MachineOperation callerOp = this.Runtime.GetAsynchronousOperation(caller.Id.Value);
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
                AsyncMachine caller = this.Runtime.GetExecutingMachine<AsyncMachine>();
                this.Runtime.Assert(caller != null,
                    "Task with id '{0}' that is not controlled by the Coyote runtime is executing controlled task '{1}'.",
                    Task.CurrentId.HasValue ? Task.CurrentId.Value.ToString() : "<unknown>", this.Id);

                if (caller is Machine machine)
                {
                    this.Runtime.Assert((machine.StateManager as SerializedMachineStateManager).IsInsideControlledTaskHandler,
                        "Machine '{0}' is executing controlled task '{1}' inside a handler that does not return a 'ControlledTask'.",
                        caller.Id, this.Id);
                }

                if (this.Type is MachineTaskType.CompletionSourceTask)
                {
                    IO.Debug.WriteLine("<ControlledTask> Machine '{0}' is executing continuation of task '{1}' on task '{2}'.",
                        caller.Id, this.Id, Task.CurrentId);
                    continuation();
                    IO.Debug.WriteLine("<ControlledTask> Machine '{0}' resumed after continuation of task '{1}' on task '{2}'.",
                        caller.Id, this.Id, Task.CurrentId);
                }
                else if (this.Type is MachineTaskType.ExplicitTask)
                {
                    IO.Debug.WriteLine("<ControlledTask> Machine '{0}' is dispatching continuation of task '{1}'.", caller.Id, this.Id);
                    this.Runtime.DispatchWork(new ActionMachine(this.Runtime, continuation), this.AwaiterTask);
                    IO.Debug.WriteLine("<ControlledTask> Machine '{0}' dispatched continuation of task '{1}'.", caller.Id, this.Id);
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
        /// The type of the task.
        /// </summary>
        private readonly MachineTaskType Type;

        /// <summary>
        /// Initializes a new instance of the <see cref="MachineTask{TResult}"/> class.
        /// </summary>
        [DebuggerStepThrough]
        internal MachineTask(SystematicTestingRuntime runtime, Task<TResult> task, MachineTaskType taskType)
            : base(task)
        {
            IO.Debug.WriteLine("<ControlledTask> Creating task '{0}' with result type '{1}' from task '{2}' (option: {3}).",
                task.Id, typeof(TResult), Task.CurrentId, taskType);
            this.Runtime = runtime;
            this.Type = taskType;
        }

        /// <summary>
        /// Gets an awaiter for this awaitable.
        /// </summary>
        [DebuggerHidden]
        public override ControlledTaskAwaiter<TResult> GetAwaiter()
        {
            AsyncMachine caller = this.Runtime.GetExecutingMachine<AsyncMachine>();
            MachineOperation callerOp = this.Runtime.GetAsynchronousOperation(caller.Id.Value);
            callerOp.OnGetControlledAwaiter();
            return new ControlledTaskAwaiter<TResult>(this, this.AwaiterTask);
        }

        /// <summary>
        /// Ends the wait for the completion of the task.
        /// </summary>
        [DebuggerHidden]
        internal override TResult GetResult(TaskAwaiter<TResult> awaiter)
        {
            AsyncMachine caller = this.Runtime.GetExecutingMachine<AsyncMachine>();
            MachineOperation callerOp = this.Runtime.GetAsynchronousOperation(caller.Id.Value);
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
        /// <param name="continueOnCapturedContext">
        /// True to attempt to marshal the continuation back to the original context captured; otherwise, false.
        /// </param>
        [DebuggerHidden]
        public override ConfiguredControlledTaskAwaitable<TResult> ConfigureAwait(bool continueOnCapturedContext)
        {
            AsyncMachine caller = this.Runtime.GetExecutingMachine<AsyncMachine>();
            MachineOperation callerOp = this.Runtime.GetAsynchronousOperation(caller.Id.Value);
            callerOp.OnGetControlledAwaiter();
            return new ConfiguredControlledTaskAwaitable<TResult>(this, this.AwaiterTask, continueOnCapturedContext);
        }

        /// <summary>
        /// Ends the wait for the completion of the task.
        /// </summary>
        [DebuggerHidden]
        internal override TResult GetResult(ConfiguredTaskAwaitable<TResult>.ConfiguredTaskAwaiter awaiter)
        {
            AsyncMachine caller = this.Runtime.GetExecutingMachine<AsyncMachine>();
            IO.Debug.WriteLine("<ControlledTask> Machine '{0}' is waiting task '{1}' with result type '{2}' to complete from task '{3}'.",
                caller.Id, this.Id, typeof(TResult), Task.CurrentId);
            MachineOperation callerOp = this.Runtime.GetAsynchronousOperation(caller.Id.Value);
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
                AsyncMachine caller = this.Runtime.GetExecutingMachine<AsyncMachine>();
                this.Runtime.Assert(caller != null,
                    "Task with id '{0}' that is not controlled by the Coyote runtime is executing controlled task '{1}'.",
                    Task.CurrentId.HasValue ? Task.CurrentId.Value.ToString() : "<unknown>", this.Id);

                if (caller is Machine machine)
                {
                    this.Runtime.Assert((machine.StateManager as SerializedMachineStateManager).IsInsideControlledTaskHandler,
                        "Machine '{0}' is executing controlled task '{1}' inside a handler that does not return a 'ControlledTask'.",
                        caller.Id, this.Id);
                }

                if (this.Type is MachineTaskType.CompletionSourceTask)
                {
                    IO.Debug.WriteLine("<ControlledTask> Machine '{0}' is executing continuation of task '{1}' with result type '{2}' on task '{3}'.",
                        caller.Id, this.Id, typeof(TResult), Task.CurrentId);
                    continuation();
                    IO.Debug.WriteLine("<ControlledTask> Machine '{0}' resumed after continuation of task '{1}' with result type '{2}' on task '{3}'.",
                        caller.Id, this.Id, typeof(TResult), Task.CurrentId);
                }
                else if (this.Type is MachineTaskType.ExplicitTask)
                {
                    IO.Debug.WriteLine("<ControlledTask> Machine '{0}' is dispatching continuation of task '{1}' with result type '{2}'.",
                        caller.Id, this.Id, typeof(TResult));
                    this.Runtime.DispatchWork(new ActionMachine(this.Runtime, continuation), this.AwaiterTask);
                    IO.Debug.WriteLine("<ControlledTask> Machine '{0}' dispatched continuation of task '{1}' with result type '{2}'.",
                        caller.Id, this.Id, typeof(TResult));
                }
            }
            catch (ExecutionCanceledException)
            {
                IO.Debug.WriteLine($"<Exception> ExecutionCanceledException was thrown from task '{Task.CurrentId}'.");
            }
        }
    }
}
