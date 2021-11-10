// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Coyote.Runtime;
using SystemCompiler = System.Runtime.CompilerServices;

namespace Microsoft.Coyote.Interception
{
    /// <summary>
    /// Represents a builder for asynchronous methods that return a <see cref="Task"/>.
    /// This type is intended for compiler use only.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
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
        public Task Task
        {
            [DebuggerHidden]
            get
            {
                IO.Debug.WriteLine("<AsyncBuilder> Creating builder task '{0}' from task '{1}' (isCompleted {2}).",
                    this.MethodBuilder.Task.Id, Task.CurrentId, this.MethodBuilder.Task.IsCompleted);
                this.Runtime?.OnTaskCompletionSourceGetTask(this.MethodBuilder.Task);
                return this.MethodBuilder.Task;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncTaskMethodBuilder"/> struct.
        /// </summary>
        private AsyncTaskMethodBuilder(CoyoteRuntime runtime)
        {
            this.Runtime = runtime;
            this.MethodBuilder = default;
        }

        /// <summary>
        /// Creates an instance of the <see cref="AsyncTaskMethodBuilder"/> struct.
        /// </summary>
        public static AsyncTaskMethodBuilder Create()
        {
            CoyoteRuntime runtime = null;
            SynchronizationContext context = SynchronizationContext.Current;
            if (context is ControlledSynchronizationContext controlledContext)
            {
                runtime = controlledContext.Runtime;
            }
            else if (context is null)
            {
                // Try get the current runtime, if it exists, and use it to set the current
                // synchronization context to the controlled synchronization context.
                runtime = CoyoteRuntime.Current;
                runtime?.SetControlledSynchronizationContext();
            }

            return new AsyncTaskMethodBuilder(runtime);
        }

        /// <summary>
        /// Begins running the builder with the associated state machine.
        /// </summary>
        [DebuggerStepThrough]
        public void Start<TStateMachine>(ref TStateMachine stateMachine)
            where TStateMachine : IAsyncStateMachine
        {
            IO.Debug.WriteLine("<AsyncBuilder> Start state machine from task '{0}' with context '{1}'.",
                Task.CurrentId, SynchronizationContext.Current);
            this.MethodBuilder.Start(ref stateMachine);
        }

        /// <summary>
        /// Associates the builder with the specified state machine.
        /// </summary>
        public void SetStateMachine(IAsyncStateMachine stateMachine) =>
            this.MethodBuilder.SetStateMachine(stateMachine);

        /// <summary>
        /// Marks the task as successfully completed.
        /// </summary>
        public void SetResult()
        {
            IO.Debug.WriteLine("<AsyncBuilder> Set result of task '{0}' from task '{1}'.",
                this.MethodBuilder.Task.Id, Task.CurrentId);
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine =>
            this.MethodBuilder.AwaitOnCompleted(ref awaiter, ref stateMachine);

        /// <summary>
        /// Schedules the state machine to proceed to the next action when the specified awaiter completes.
        /// </summary>
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine =>
            this.MethodBuilder.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine);
    }

    /// <summary>
    /// Represents a builder for asynchronous methods that return a <see cref="Task{TResult}"/>.
    /// This type is intended for compiler use only.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
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
        public Task<TResult> Task
        {
            [DebuggerHidden]
            get
            {
                IO.Debug.WriteLine("<AsyncBuilder> Creating builder task '{0}' from task '{1}' (isCompleted {2}).",
                    this.MethodBuilder.Task.Id, System.Threading.Tasks.Task.CurrentId, this.MethodBuilder.Task.IsCompleted);
                this.Runtime?.OnTaskCompletionSourceGetTask(this.MethodBuilder.Task);
                return this.MethodBuilder.Task;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncTaskMethodBuilder{TResult}"/> struct.
        /// </summary>
        private AsyncTaskMethodBuilder(CoyoteRuntime runtime)
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
            CoyoteRuntime runtime = null;
            SynchronizationContext context = SynchronizationContext.Current;
            if (context is ControlledSynchronizationContext controlledContext)
            {
                runtime = controlledContext.Runtime;
            }
            else if (context is null)
            {
                // Try get the current runtime, if it exists, and use it to set the current
                // synchronization context to the controlled synchronization context.
                runtime = CoyoteRuntime.Current;
                runtime?.SetControlledSynchronizationContext();
            }

            return new AsyncTaskMethodBuilder<TResult>(runtime);
        }
#pragma warning restore CA1000 // Do not declare static members on generic types

        /// <summary>
        /// Begins running the builder with the associated state machine.
        /// </summary>
        [DebuggerStepThrough]
        public void Start<TStateMachine>(ref TStateMachine stateMachine)
            where TStateMachine : IAsyncStateMachine
        {
            IO.Debug.WriteLine("<AsyncBuilder> Start state machine from task '{0}' with context '{1}'.",
                System.Threading.Tasks.Task.CurrentId, SynchronizationContext.Current);
            this.MethodBuilder.Start(ref stateMachine);
        }

        /// <summary>
        /// Associates the builder with the specified state machine.
        /// </summary>
        public void SetStateMachine(IAsyncStateMachine stateMachine) =>
            this.MethodBuilder.SetStateMachine(stateMachine);

        /// <summary>
        /// Marks the task as successfully completed.
        /// </summary>
        /// <param name="result">The result to use to complete the task.</param>
        public void SetResult(TResult result)
        {
            IO.Debug.WriteLine("<AsyncBuilder> Set result of task '{0}' from task '{1}'.",
                this.MethodBuilder.Task.Id, System.Threading.Tasks.Task.CurrentId);
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
                where TAwaiter : INotifyCompletion
                where TStateMachine : IAsyncStateMachine =>
            this.MethodBuilder.AwaitOnCompleted(ref awaiter, ref stateMachine);

        /// <summary>
        /// Schedules the state machine to proceed to the next action when the specified awaiter completes.
        /// </summary>
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine =>
            this.MethodBuilder.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine);
    }
}
