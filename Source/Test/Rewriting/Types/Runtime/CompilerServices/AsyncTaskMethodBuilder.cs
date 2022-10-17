// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.Runtime.CompilerServices;
using SystemCompiler = System.Runtime.CompilerServices;
using SystemTask = System.Threading.Tasks.Task;
using SystemTasks = System.Threading.Tasks;
using SystemThreading = System.Threading;

namespace Microsoft.Coyote.Rewriting.Types.Runtime.CompilerServices
{
    /// <summary>
    /// Represents a builder for asynchronous methods that return a controlled task.
    /// </summary>
    /// <remarks>This type is intended for compiler use only.</remarks>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [StructLayout(LayoutKind.Auto)]
    public struct AsyncTaskMethodBuilder
    {
        /// <summary>
        /// Responsible for controlling the execution of tasks during systematic testing.
        /// </summary>
        private readonly CoyoteRuntime Runtime;

        /// <summary>
        /// The task builder to which most operations are delegated.
        /// </summary>
#pragma warning disable IDE0044 // Add readonly modifier
        private SystemCompiler.AsyncTaskMethodBuilder MethodBuilder;
#pragma warning restore IDE0044 // Add readonly modifier

        /// <summary>
        /// Gets the task for this builder.
        /// </summary>
        public SystemTask Task => this.MethodBuilder.Task;

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncTaskMethodBuilder"/> struct.
        /// </summary>
        internal AsyncTaskMethodBuilder(CoyoteRuntime runtime)
        {
            this.Runtime = runtime;
            this.MethodBuilder = default;
        }

        /// <summary>
        /// Creates an instance of the <see cref="AsyncTaskMethodBuilder"/> struct.
        /// </summary>
        public static AsyncTaskMethodBuilder Create()
        {
            RuntimeProvider.TryGetFromSynchronizationContext(out CoyoteRuntime runtime);
            return new AsyncTaskMethodBuilder(runtime);
        }

        /// <summary>
        /// Begins running the builder with the associated state machine.
        /// </summary>
        public void Start<TStateMachine>(ref TStateMachine stateMachine)
            where TStateMachine : SystemCompiler.IAsyncStateMachine
        {
            this.Runtime?.LogWriter.LogDebug("[coyote::debug] Started state machine on runtime '{0}' and thread '{1}'.",
                this.Runtime?.Id, SystemThreading.Thread.CurrentThread.ManagedThreadId);
            this.MethodBuilder.Start(ref stateMachine);
        }

        /// <summary>
        /// Associates the builder with the specified state machine.
        /// </summary>
        public void SetStateMachine(SystemCompiler.IAsyncStateMachine stateMachine) =>
            this.MethodBuilder.SetStateMachine(stateMachine);

        /// <summary>
        /// Marks the task as successfully completed.
        /// </summary>
        public void SetResult()
        {
            this.Runtime?.LogWriter.LogDebug("[coyote::debug] Set state machine task '{0}' from thread '{1}'.",
                this.MethodBuilder.Task.Id, SystemThreading.Thread.CurrentThread.ManagedThreadId);
            this.MethodBuilder.SetResult();
        }

        /// <summary>
        /// Marks the task as failed and binds the specified exception to the task.
        /// </summary>
        public void SetException(Exception exception) => this.MethodBuilder.SetException(exception);

        /// <summary>
        /// Schedules the state machine to proceed to the next action when the specified awaiter completes.
        /// </summary>
        [DebuggerHidden]
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : SystemCompiler.INotifyCompletion
            where TStateMachine : SystemCompiler.IAsyncStateMachine
        {
            this.MethodBuilder.AwaitOnCompleted(ref awaiter, ref stateMachine);
            if (this.Runtime != null && awaiter is IControllableAwaiter controllableAwaiter &&
                controllableAwaiter.IsControlled)
            {
                this.AssignStateMachineTask(this.MethodBuilder.Task);
            }
        }

        /// <summary>
        /// Schedules the state machine to proceed to the next action when the specified awaiter completes.
        /// </summary>
        [DebuggerHidden]
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : SystemCompiler.ICriticalNotifyCompletion
            where TStateMachine : SystemCompiler.IAsyncStateMachine
        {
            this.MethodBuilder.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine);
            if (this.Runtime != null && awaiter is IControllableAwaiter controllableAwaiter &&
                controllableAwaiter.IsControlled)
            {
                this.AssignStateMachineTask(this.MethodBuilder.Task);
            }
        }

        /// <summary>
        /// Assigns the state machine task with the runtime.
        /// </summary>
        private SystemTask AssignStateMachineTask(SystemTask builderTask)
        {
            this.Runtime.LogWriter.LogDebug("[coyote::debug] Assigned state machine task '{0}' from thread '{1}'.",
                builderTask.Id, SystemThreading.Thread.CurrentThread.ManagedThreadId);
            this.Runtime.RegisterKnownControlledTask(builderTask);
            return builderTask;
        }
    }

    /// <summary>
    /// Represents a builder for asynchronous methods that return a <see cref="SystemTasks.Task{TResult}"/>.
    /// </summary>
    /// <remarks>This type is intended for compiler use only.</remarks>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [StructLayout(LayoutKind.Auto)]
    public struct AsyncTaskMethodBuilder<TResult>
    {
        /// <summary>
        /// Responsible for controlling the execution of tasks during systematic testing.
        /// </summary>
        private readonly CoyoteRuntime Runtime;

        /// <summary>
        /// The task builder to which most operations are delegated.
        /// </summary>
#pragma warning disable IDE0044 // Add readonly modifier
        private SystemCompiler.AsyncTaskMethodBuilder<TResult> MethodBuilder;
#pragma warning restore IDE0044 // Add readonly modifier

        /// <summary>
        /// Gets the task for this builder.
        /// </summary>
        public SystemTasks.Task<TResult> Task => this.MethodBuilder.Task;

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncTaskMethodBuilder{TResult}"/> struct.
        /// </summary>
        internal AsyncTaskMethodBuilder(CoyoteRuntime runtime)
        {
            this.Runtime = runtime;
            this.MethodBuilder = default;
        }

        /// <summary>
        /// Creates an instance of the <see cref="AsyncTaskMethodBuilder{TResult}"/> struct.
        /// </summary>
#pragma warning disable CA1000 // Do not declare static members on generic types
        public static AsyncTaskMethodBuilder<TResult> Create()
        {
            RuntimeProvider.TryGetFromSynchronizationContext(out CoyoteRuntime runtime);
            return new AsyncTaskMethodBuilder<TResult>(runtime);
        }
#pragma warning restore CA1000 // Do not declare static members on generic types

        /// <summary>
        /// Begins running the builder with the associated state machine.
        /// </summary>
        public void Start<TStateMachine>(ref TStateMachine stateMachine)
            where TStateMachine : SystemCompiler.IAsyncStateMachine
        {
            this.Runtime?.LogWriter.LogDebug("[coyote::debug] Started state machine on runtime '{0}' and thread '{1}'.",
                this.Runtime?.Id, SystemThreading.Thread.CurrentThread.ManagedThreadId);
            this.MethodBuilder.Start(ref stateMachine);
        }

        /// <summary>
        /// Associates the builder with the specified state machine.
        /// </summary>
        public void SetStateMachine(SystemCompiler.IAsyncStateMachine stateMachine) =>
            this.MethodBuilder.SetStateMachine(stateMachine);

        /// <summary>
        /// Marks the task as successfully completed.
        /// </summary>
        /// <param name="result">The result to use to complete the task.</param>
        public void SetResult(TResult result)
        {
            this.Runtime?.LogWriter.LogDebug("[coyote::debug] Set state machine task '{0}' from thread '{1}'.",
                this.MethodBuilder.Task.Id, SystemThreading.Thread.CurrentThread.ManagedThreadId);
            this.MethodBuilder.SetResult(result);
        }

        /// <summary>
        /// Marks the task as failed and binds the specified exception to the task.
        /// </summary>
        public void SetException(Exception exception) => this.MethodBuilder.SetException(exception);

        /// <summary>
        /// Schedules the state machine to proceed to the next action when the specified awaiter completes.
        /// </summary>
        [DebuggerHidden]
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
                where TAwaiter : SystemCompiler.INotifyCompletion
                where TStateMachine : SystemCompiler.IAsyncStateMachine
        {
            this.MethodBuilder.AwaitOnCompleted(ref awaiter, ref stateMachine);
            if (this.Runtime != null && awaiter is IControllableAwaiter controllableAwaiter &&
                controllableAwaiter.IsControlled)
            {
                this.AssignStateMachineTask(this.MethodBuilder.Task);
            }
        }

        /// <summary>
        /// Schedules the state machine to proceed to the next action when the specified awaiter completes.
        /// </summary>
        [DebuggerHidden]
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : SystemCompiler.ICriticalNotifyCompletion
            where TStateMachine : SystemCompiler.IAsyncStateMachine
        {
            this.MethodBuilder.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine);
            if (this.Runtime != null && awaiter is IControllableAwaiter controllableAwaiter &&
                controllableAwaiter.IsControlled)
            {
                this.AssignStateMachineTask(this.MethodBuilder.Task);
            }
        }

        /// <summary>
        /// Assigns the state machine task with the runtime.
        /// </summary>
        private SystemTasks.Task<TResult> AssignStateMachineTask(SystemTasks.Task<TResult> builderTask)
        {
            this.Runtime.LogWriter.LogDebug("[coyote::debug] Assigned state machine task '{0}' from thread '{1}'.",
                builderTask.Id, SystemThreading.Thread.CurrentThread.ManagedThreadId);
            this.Runtime.RegisterKnownControlledTask(builderTask);
            return builderTask;
        }
    }
}
