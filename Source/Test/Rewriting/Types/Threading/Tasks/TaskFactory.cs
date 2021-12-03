// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Coyote.Runtime;

using SystemCancellationToken = System.Threading.CancellationToken;
using SystemTask = System.Threading.Tasks.Task;
using SystemTaskContinuationOptions = System.Threading.Tasks.TaskContinuationOptions;
using SystemTaskCreationOptions = System.Threading.Tasks.TaskCreationOptions;
using SystemTasks = System.Threading.Tasks;
using SystemTaskScheduler = System.Threading.Tasks.TaskScheduler;

namespace Microsoft.Coyote.Rewriting.Types.Threading.Tasks
{
#pragma warning disable CA1822 // Mark members as static
#pragma warning disable CA1068 // CancellationToken parameters must come last
    /// <summary>
    /// Provides support for creating and scheduling controlled task objects.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public class TaskFactory
    {
        // Note: we are only intercepting and modeling a very limited set of APIs to enable specific scenarios such
        // as ASP.NET rewriting. Most `TaskFactory` APIs are not supported by our modeling, and we do not currently
        // aim to support user applications with code that explicitly uses the `TaskFactory`.

        /// <summary>
        /// The default task continuation options for this task factory.
        /// </summary>
        public SystemTaskContinuationOptions ContinuationOptions => SystemTask.Factory.ContinuationOptions;

        /// <summary>
        /// The default task cancellation token for this task factory.
        /// </summary>
        public SystemCancellationToken CancellationToken => SystemTask.Factory.CancellationToken;

        /// <summary>
        /// The default task creation options for this task factory.
        /// </summary>
        public SystemTaskCreationOptions CreationOptions => SystemTask.Factory.CreationOptions;

        /// <summary>
        /// The default task scheduler for this task factory.
        /// </summary>
        public SystemTaskScheduler Scheduler => SystemTask.Factory.Scheduler;

        /// <summary>
        /// Creates and starts a task.
        /// </summary>
        public SystemTask StartNew(Action action) => this.StartNew(action, SystemCancellationToken.None);

        /// <summary>
        /// Creates and starts a task.
        /// </summary>
        public SystemTask StartNew(Action action, SystemCancellationToken cancellationToken) =>
            SystemTask.Factory.StartNew(action, cancellationToken, SystemTaskCreationOptions.None,
                CoyoteRuntime.IsExecutionControlled ?
                CoyoteRuntime.Current.ControlledTaskScheduler : SystemTaskScheduler.Default);

        /// <summary>
        /// Creates and starts a task.
        /// </summary>
        public SystemTask StartNew(Action action, SystemTaskCreationOptions creationOptions) =>
            this.StartNew(action, default, creationOptions, SystemTaskScheduler.Default);

        /// <summary>
        /// Creates and starts a task.
        /// </summary>
        public SystemTask StartNew(Action action, SystemCancellationToken cancellationToken,
            SystemTaskCreationOptions creationOptions, SystemTaskScheduler scheduler)
        {
            ExceptionProvider.ThrowUncontrolledInvocationException(nameof(SystemTask.Factory.StartNew));
            return SystemTask.Factory.StartNew(action, cancellationToken, creationOptions, scheduler);
        }

        /// <summary>
        /// Creates and starts a task.
        /// </summary>
        public SystemTask StartNew(Action<object> action, object state) =>
            this.StartNew(action, state, default, SystemTaskCreationOptions.None, SystemTaskScheduler.Default);

        /// <summary>
        /// Creates and starts a task.
        /// </summary>
        public SystemTask StartNew(Action<object> action, object state, SystemCancellationToken cancellationToken) =>
            this.StartNew(action, state, cancellationToken, SystemTaskCreationOptions.None, SystemTaskScheduler.Default);

        /// <summary>
        /// Creates and starts a task.
        /// </summary>
        public SystemTask StartNew(Action<object> action, object state, SystemTaskCreationOptions creationOptions) =>
            this.StartNew(action, state, default, creationOptions, SystemTaskScheduler.Default);

        /// <summary>
        /// Creates and starts a task.
        /// </summary>
        public SystemTask StartNew(Action<object> action, object state, SystemCancellationToken cancellationToken,
            SystemTaskCreationOptions creationOptions, SystemTaskScheduler scheduler)
        {
            ExceptionProvider.ThrowUncontrolledInvocationException(nameof(SystemTask.Factory.StartNew));
            return SystemTask.Factory.StartNew(action, state, cancellationToken, creationOptions, scheduler);
        }

        /// <summary>
        /// Creates and starts a generic task.
        /// </summary>
        public SystemTasks.Task<TResult> StartNew<TResult>(Func<TResult> function) =>
            this.StartNew(function, SystemCancellationToken.None);

        /// <summary>
        /// Creates and starts a generic task.
        /// </summary>
        public SystemTasks.Task<TResult> StartNew<TResult>(Func<TResult> function, SystemCancellationToken cancellationToken) =>
            SystemTask.Factory.StartNew(function, cancellationToken, SystemTaskCreationOptions.None, CoyoteRuntime.IsExecutionControlled ?
                CoyoteRuntime.Current.ControlledTaskScheduler : SystemTaskScheduler.Default);

        /// <summary>
        /// Creates and starts a generic task.
        /// </summary>
        public SystemTasks.Task<TResult> StartNew<TResult>(Func<TResult> function, SystemTaskCreationOptions creationOptions) =>
            this.StartNew(function, default, creationOptions, SystemTaskScheduler.Default);

        /// <summary>
        /// Creates and starts a generic task.
        /// </summary>
        public SystemTasks.Task<TResult> StartNew<TResult>(Func<TResult> function, SystemCancellationToken cancellationToken,
            SystemTaskCreationOptions creationOptions, SystemTaskScheduler scheduler)
        {
            ExceptionProvider.ThrowUncontrolledInvocationException(nameof(SystemTask.Factory.StartNew));
            return SystemTask.Factory.StartNew(function, cancellationToken, creationOptions, scheduler);
        }

        /// <summary>
        /// Creates and starts a generic task.
        /// </summary>
        public SystemTasks.Task<TResult> StartNew<TResult>(Func<object, TResult> function, object state) =>
            this.StartNew(function, state, default, SystemTaskCreationOptions.None, SystemTaskScheduler.Default);

        /// <summary>
        /// Creates and starts a generic task.
        /// </summary>
        public SystemTasks.Task<TResult> StartNew<TResult>(Func<object, TResult> function, object state,
            SystemCancellationToken cancellationToken) =>
            this.StartNew(function, state, cancellationToken, SystemTaskCreationOptions.None, SystemTaskScheduler.Default);

        /// <summary>
        /// Creates and starts a generic task.
        /// </summary>
        public SystemTasks.Task<TResult> StartNew<TResult>(Func<object, TResult> function, object state,
            SystemTaskCreationOptions creationOptions) =>
            this.StartNew(function, state, default, creationOptions, SystemTaskScheduler.Default);

        /// <summary>
        /// Creates and starts a generic task.
        /// </summary>
        public SystemTasks.Task<TResult> StartNew<TResult>(Func<object, TResult> function, object state,
            SystemCancellationToken cancellationToken, SystemTaskCreationOptions creationOptions,
            SystemTaskScheduler scheduler)
        {
            ExceptionProvider.ThrowUncontrolledInvocationException(nameof(SystemTask.Factory.StartNew));
            return SystemTask.Factory.StartNew(function, state, cancellationToken, creationOptions, scheduler);
        }

        /// <summary>
        /// Creates a continuation task that starts when a set of specified tasks has completed.
        /// </summary>
        public SystemTask ContinueWhenAll(SystemTask[] tasks, Action<SystemTask[]> continuationAction) =>
            this.ContinueWhenAll(tasks, continuationAction, default, SystemTaskContinuationOptions.None,
                SystemTaskScheduler.Default);

        /// <summary>
        /// Creates a continuation task that starts when a set of specified tasks has completed.
        /// </summary>
        public SystemTask ContinueWhenAll(SystemTask[] tasks, Action<SystemTask[]> continuationAction,
            SystemCancellationToken cancellationToken) =>
            this.ContinueWhenAll(tasks, continuationAction, cancellationToken, SystemTaskContinuationOptions.None,
                SystemTaskScheduler.Default);

        /// <summary>
        /// Creates a continuation task that starts when a set of specified tasks has completed.
        /// </summary>
        public SystemTask ContinueWhenAll(SystemTask[] tasks, Action<SystemTask[]> continuationAction,
            SystemTaskContinuationOptions continuationOptions) =>
            this.ContinueWhenAll(tasks, continuationAction, default, continuationOptions, SystemTaskScheduler.Default);

        /// <summary>
        /// Creates a continuation task that starts when a set of specified tasks has completed.
        /// </summary>
        public SystemTask ContinueWhenAll(SystemTask[] tasks, Action<SystemTask[]> continuationAction,
            SystemCancellationToken cancellationToken, SystemTaskContinuationOptions continuationOptions,
            SystemTaskScheduler scheduler)
        {
            ExceptionProvider.ThrowUncontrolledInvocationException(nameof(SystemTask.Factory.ContinueWhenAll));
            return SystemTask.Factory.ContinueWhenAll(tasks, continuationAction, cancellationToken, continuationOptions, scheduler);
        }

        /// <summary>
        /// Creates a continuation task that starts when a set of specified tasks has completed.
        /// </summary>
        public SystemTask ContinueWhenAll<TAntecedentResult>(SystemTasks.Task<TAntecedentResult>[] tasks,
            Action<SystemTasks.Task<TAntecedentResult>[]> continuationAction) =>
            this.ContinueWhenAll(tasks, continuationAction, default, SystemTaskContinuationOptions.None,
                SystemTaskScheduler.Default);

        /// <summary>
        /// Creates a continuation task that starts when a set of specified tasks has completed.
        /// </summary>
        public SystemTask ContinueWhenAll<TAntecedentResult>(SystemTasks.Task<TAntecedentResult>[] tasks,
            Action<SystemTasks.Task<TAntecedentResult>[]> continuationAction, SystemCancellationToken cancellationToken) =>
            this.ContinueWhenAll(tasks, continuationAction, cancellationToken, SystemTaskContinuationOptions.None, SystemTaskScheduler.Default);

        /// <summary>
        /// Creates a continuation task that starts when a set of specified tasks has completed.
        /// </summary>
        public SystemTask ContinueWhenAll<TAntecedentResult>(SystemTasks.Task<TAntecedentResult>[] tasks,
            Action<SystemTasks.Task<TAntecedentResult>[]> continuationAction,
            SystemTaskContinuationOptions continuationOptions) =>
            this.ContinueWhenAll(tasks, continuationAction, default, continuationOptions, SystemTaskScheduler.Default);

        /// <summary>
        /// Creates a continuation task that starts when a set of specified tasks has completed.
        /// </summary>
        public SystemTask ContinueWhenAll<TAntecedentResult>(SystemTasks.Task<TAntecedentResult>[] tasks,
            Action<SystemTasks.Task<TAntecedentResult>[]> continuationAction, SystemCancellationToken cancellationToken,
            SystemTaskContinuationOptions continuationOptions, SystemTaskScheduler scheduler)
        {
            ExceptionProvider.ThrowUncontrolledInvocationException(nameof(SystemTask.Factory.ContinueWhenAll));
            return SystemTask.Factory.ContinueWhenAll(tasks, continuationAction, cancellationToken, continuationOptions, scheduler);
        }

        /// <summary>
        /// Creates a continuation task that starts when a set of specified tasks has completed.
        /// </summary>
        public SystemTasks.Task<TResult> ContinueWhenAll<TResult>(SystemTask[] tasks,
            Func<SystemTask[], TResult> continuationFunction) =>
            this.ContinueWhenAll(tasks, continuationFunction, default, SystemTaskContinuationOptions.None, SystemTaskScheduler.Default);

        /// <summary>
        /// Creates a continuation task that starts when a set of specified tasks has completed.
        /// </summary>
        public SystemTasks.Task<TResult> ContinueWhenAll<TResult>(SystemTask[] tasks,
            Func<SystemTask[], TResult> continuationFunction, SystemCancellationToken cancellationToken) =>
            this.ContinueWhenAll(tasks, continuationFunction, cancellationToken, SystemTaskContinuationOptions.None, SystemTaskScheduler.Default);

        /// <summary>
        /// Creates a continuation task that starts when a set of specified tasks has completed.
        /// </summary>
        public SystemTasks.Task<TResult> ContinueWhenAll<TResult>(SystemTask[] tasks,
            Func<SystemTask[], TResult> continuationFunction, SystemTaskContinuationOptions continuationOptions) =>
            this.ContinueWhenAll(tasks, continuationFunction, default, continuationOptions, SystemTaskScheduler.Default);

        /// <summary>
        /// Creates a continuation task that starts when a set of specified tasks has completed.
        /// </summary>
        public SystemTasks.Task<TResult> ContinueWhenAll<TResult>(SystemTask[] tasks,
            Func<SystemTask[], TResult> continuationFunction, SystemCancellationToken cancellationToken,
            SystemTaskContinuationOptions continuationOptions, SystemTaskScheduler scheduler)
        {
            ExceptionProvider.ThrowUncontrolledInvocationException(nameof(SystemTask.Factory.ContinueWhenAll));
            return SystemTask.Factory.ContinueWhenAll(tasks, continuationFunction, cancellationToken, continuationOptions, scheduler);
        }

        /// <summary>
        /// Creates a continuation task that starts when a set of specified tasks has completed.
        /// </summary>
        public SystemTasks.Task<TResult> ContinueWhenAll<TAntecedentResult, TResult>(
            SystemTasks.Task<TAntecedentResult>[] tasks,
            Func<SystemTasks.Task<TAntecedentResult>[], TResult> continuationFunction) =>
            this.ContinueWhenAll(tasks, continuationFunction, default, SystemTaskContinuationOptions.None, SystemTaskScheduler.Default);

        /// <summary>
        /// Creates a continuation task that starts when a set of specified tasks has completed.
        /// </summary>
        public SystemTasks.Task<TResult> ContinueWhenAll<TAntecedentResult, TResult>(
            SystemTasks.Task<TAntecedentResult>[] tasks,
            Func<SystemTasks.Task<TAntecedentResult>[], TResult> continuationFunction,
            SystemCancellationToken cancellationToken) =>
            this.ContinueWhenAll(tasks, continuationFunction, cancellationToken, SystemTaskContinuationOptions.None, SystemTaskScheduler.Default);

        /// <summary>
        /// Creates a continuation task that starts when a set of specified tasks has completed.
        /// </summary>
        public SystemTasks.Task<TResult> ContinueWhenAll<TAntecedentResult, TResult>(
            SystemTasks.Task<TAntecedentResult>[] tasks,
            Func<SystemTasks.Task<TAntecedentResult>[], TResult> continuationFunction,
            SystemTaskContinuationOptions continuationOptions) =>
            this.ContinueWhenAll(tasks, continuationFunction, default, continuationOptions, SystemTaskScheduler.Default);

        /// <summary>
        /// Creates a continuation task that starts when a set of specified tasks has completed.
        /// </summary>
        public SystemTasks.Task<TResult> ContinueWhenAll<TAntecedentResult, TResult>(
            SystemTasks.Task<TAntecedentResult>[] tasks,
            Func<SystemTasks.Task<TAntecedentResult>[], TResult> continuationFunction,
            SystemCancellationToken cancellationToken, SystemTaskContinuationOptions continuationOptions,
            SystemTaskScheduler scheduler)
        {
            ExceptionProvider.ThrowUncontrolledInvocationException(nameof(SystemTask.Factory.ContinueWhenAll));
            return SystemTask.Factory.ContinueWhenAll(tasks, continuationFunction, cancellationToken,
                continuationOptions, scheduler);
        }

        /// <summary>
        /// Creates a continuation task that will be started upon the completion of any task in the provided set.
        /// </summary>
        public SystemTask ContinueWhenAny(SystemTask[] tasks, Action<SystemTask> continuationAction) =>
            this.ContinueWhenAny(tasks, continuationAction, default, SystemTaskContinuationOptions.None,
                SystemTaskScheduler.Default);

        /// <summary>
        /// Creates a continuation task that will be started upon the completion of any task in the provided set.
        /// </summary>
        public SystemTask ContinueWhenAny(SystemTask[] tasks, Action<SystemTask> continuationAction,
            SystemCancellationToken cancellationToken) =>
            this.ContinueWhenAny(tasks, continuationAction, cancellationToken, SystemTaskContinuationOptions.None,
            SystemTaskScheduler.Default);

        /// <summary>
        /// Creates a continuation task that will be started upon the completion of any task in the provided set.
        /// </summary>
        public SystemTask ContinueWhenAny(SystemTask[] tasks, Action<SystemTask> continuationAction,
            SystemTaskContinuationOptions continuationOptions) =>
            this.ContinueWhenAny(tasks, continuationAction, default, continuationOptions, SystemTaskScheduler.Default);

        /// <summary>
        /// Creates a continuation task that will be started upon the completion of any task in the provided set.
        /// </summary>
        public SystemTask ContinueWhenAny(SystemTask[] tasks, Action<SystemTask> continuationAction,
            SystemCancellationToken cancellationToken, SystemTaskContinuationOptions continuationOptions,
            SystemTaskScheduler scheduler)
        {
            ExceptionProvider.ThrowUncontrolledInvocationException(nameof(SystemTask.Factory.ContinueWhenAny));
            return SystemTask.Factory.ContinueWhenAny(tasks, continuationAction, cancellationToken, continuationOptions, scheduler);
        }

        /// <summary>
        /// Creates a continuation task that will be started upon the completion of any task in the provided set.
        /// </summary>
        public SystemTask ContinueWhenAny<TAntecedentResult>(SystemTasks.Task<TAntecedentResult>[] tasks,
            Action<SystemTasks.Task<TAntecedentResult>> continuationAction) =>
            this.ContinueWhenAny(tasks, continuationAction, default, SystemTaskContinuationOptions.None,
                SystemTaskScheduler.Default);

        /// <summary>
        /// Creates a continuation task that will be started upon the completion of any task in the provided set.
        /// </summary>
        public SystemTask ContinueWhenAny<TAntecedentResult>(SystemTasks.Task<TAntecedentResult>[] tasks,
            Action<SystemTasks.Task<TAntecedentResult>> continuationAction, SystemCancellationToken cancellationToken) =>
            this.ContinueWhenAny(tasks, continuationAction, cancellationToken, SystemTaskContinuationOptions.None,
                SystemTaskScheduler.Default);

        /// <summary>
        /// Creates a continuation task that will be started upon the completion of any task in the provided set.
        /// </summary>
        public SystemTask ContinueWhenAny<TAntecedentResult>(SystemTasks.Task<TAntecedentResult>[] tasks,
            Action<SystemTasks.Task<TAntecedentResult>> continuationAction, SystemTaskContinuationOptions continuationOptions) =>
            this.ContinueWhenAny(tasks, continuationAction, default, continuationOptions, SystemTaskScheduler.Default);

        /// <summary>
        /// Creates a continuation task that will be started upon the completion of any task in the provided set.
        /// </summary>
        public SystemTask ContinueWhenAny<TAntecedentResult>(SystemTasks.Task<TAntecedentResult>[] tasks,
            Action<SystemTasks.Task<TAntecedentResult>> continuationAction, SystemCancellationToken cancellationToken,
            SystemTaskContinuationOptions continuationOptions, SystemTaskScheduler scheduler)
        {
            ExceptionProvider.ThrowUncontrolledInvocationException(nameof(SystemTask.Factory.ContinueWhenAny));
            return SystemTask.Factory.ContinueWhenAny(tasks, continuationAction, cancellationToken, continuationOptions, scheduler);
        }

        /// <summary>
        /// Creates a continuation task that will be started upon the completion of any task in the provided set.
        /// </summary>
        public SystemTasks.Task<TResult> ContinueWhenAny<TResult>(SystemTask[] tasks,
            Func<SystemTask, TResult> continuationFunction) =>
            this.ContinueWhenAny(tasks, continuationFunction, default, SystemTaskContinuationOptions.None,
                SystemTaskScheduler.Default);

        /// <summary>
        /// Creates a continuation task that will be started upon the completion of any task in the provided set.
        /// </summary>
        public SystemTasks.Task<TResult> ContinueWhenAny<TResult>(SystemTask[] tasks,
            Func<SystemTask, TResult> continuationFunction, SystemCancellationToken cancellationToken) =>
            this.ContinueWhenAny(tasks, continuationFunction, cancellationToken, SystemTaskContinuationOptions.None,
                SystemTaskScheduler.Default);

        /// <summary>
        /// Creates a continuation task that will be started upon the completion of any task in the provided set.
        /// </summary>
        public SystemTasks.Task<TResult> ContinueWhenAny<TResult>(SystemTask[] tasks,
            Func<SystemTask, TResult> continuationFunction, SystemTaskContinuationOptions continuationOptions) =>
            this.ContinueWhenAny(tasks, continuationFunction, default, continuationOptions, SystemTaskScheduler.Default);

        /// <summary>
        /// Creates a continuation task that will be started upon the completion of any task in the provided set.
        /// </summary>
        public SystemTasks.Task<TResult> ContinueWhenAny<TResult>(SystemTask[] tasks,
            Func<SystemTask, TResult> continuationFunction, SystemCancellationToken cancellationToken,
            SystemTaskContinuationOptions continuationOptions, SystemTaskScheduler scheduler)
        {
            ExceptionProvider.ThrowUncontrolledInvocationException(nameof(SystemTask.Factory.ContinueWhenAny));
            return SystemTask.Factory.ContinueWhenAny(tasks, continuationFunction, cancellationToken,
                continuationOptions, scheduler);
        }

        /// <summary>
        /// Creates a continuation task that will be started upon the completion of any task in the provided set.
        /// </summary>
        public SystemTasks.Task<TResult> ContinueWhenAny<TAntecedentResult, TResult>(
            SystemTasks.Task<TAntecedentResult>[] tasks,
            Func<SystemTasks.Task<TAntecedentResult>, TResult> continuationFunction) =>
            this.ContinueWhenAny(tasks, continuationFunction, default, SystemTaskContinuationOptions.None,
                SystemTaskScheduler.Default);

        /// <summary>
        /// Creates a continuation task that will be started upon the completion of any task in the provided set.
        /// </summary>
        public SystemTasks.Task<TResult> ContinueWhenAny<TAntecedentResult, TResult>(
            SystemTasks.Task<TAntecedentResult>[] tasks,
            Func<SystemTasks.Task<TAntecedentResult>, TResult> continuationFunction,
            SystemCancellationToken cancellationToken) =>
            this.ContinueWhenAny(tasks, continuationFunction, cancellationToken, SystemTaskContinuationOptions.None,
                SystemTaskScheduler.Default);

        /// <summary>
        /// Creates a continuation task that will be started upon the completion of any task in the provided set.
        /// </summary>
        public SystemTasks.Task<TResult> ContinueWhenAny<TAntecedentResult, TResult>(
            SystemTasks.Task<TAntecedentResult>[] tasks,
            Func<SystemTasks.Task<TAntecedentResult>, TResult> continuationFunction,
            SystemTaskContinuationOptions continuationOptions) =>
            this.ContinueWhenAny(tasks, continuationFunction, default, continuationOptions, SystemTaskScheduler.Default);

        /// <summary>
        /// Creates a continuation task that will be started upon the completion of any task in the provided set.
        /// </summary>
        public SystemTasks.Task<TResult> ContinueWhenAny<TAntecedentResult, TResult>(
            SystemTasks.Task<TAntecedentResult>[] tasks,
            Func<SystemTasks.Task<TAntecedentResult>, TResult> continuationFunction,
            SystemCancellationToken cancellationToken,
            SystemTaskContinuationOptions continuationOptions, SystemTaskScheduler scheduler)
        {
            ExceptionProvider.ThrowUncontrolledInvocationException(nameof(SystemTask.Factory.ContinueWhenAny));
            return SystemTask.Factory.ContinueWhenAny(tasks, continuationFunction, cancellationToken,
                continuationOptions, scheduler);
        }

        /// <summary>
        /// Creates a task that represents a pair of begin and end methods that conform
        /// to the Asynchronous Programming Model pattern.
        /// </summary>
        public SystemTask FromAsync(Func<AsyncCallback, object, IAsyncResult> beginMethod,
            Action<IAsyncResult> endMethod, object state) =>
            this.FromAsync(beginMethod, endMethod, state, SystemTaskCreationOptions.None);

        /// <summary>
        /// Creates a task that represents a pair of begin and end methods that conform
        /// to the Asynchronous Programming Model pattern.
        /// </summary>
        public SystemTask FromAsync(Func<AsyncCallback, object, IAsyncResult> beginMethod,
            Action<IAsyncResult> endMethod, object state, SystemTaskCreationOptions creationOptions)
        {
            ExceptionProvider.ThrowUncontrolledInvocationException(nameof(SystemTask.Factory.FromAsync));
            return SystemTask.Factory.FromAsync(beginMethod, endMethod, state, creationOptions);
        }

        /// <summary>
        /// Creates a task that represents a pair of begin and end methods that conform
        /// to the Asynchronous Programming Model pattern.
        /// </summary>
        public SystemTasks.Task<TResult> FromAsync<TResult>(Func<AsyncCallback, object, IAsyncResult> beginMethod,
            Func<IAsyncResult, TResult> endMethod, object state) =>
            this.FromAsync(beginMethod, endMethod, state, SystemTaskCreationOptions.None);

        /// <summary>
        /// Creates a task that represents a pair of begin and end methods that conform
        /// to the Asynchronous Programming Model pattern.
        /// </summary>
        public SystemTasks.Task<TResult> FromAsync<TResult>(Func<AsyncCallback, object, IAsyncResult> beginMethod,
            Func<IAsyncResult, TResult> endMethod, object state, SystemTaskCreationOptions creationOptions)
        {
            ExceptionProvider.ThrowUncontrolledInvocationException(nameof(SystemTask.Factory.FromAsync));
            return SystemTask.Factory.FromAsync(beginMethod, endMethod, state, creationOptions);
        }

        /// <summary>
        /// Creates a task that represents a pair of begin and end methods that conform
        /// to the Asynchronous Programming Model pattern.
        /// </summary>
        public SystemTask FromAsync<TArg1>(Func<TArg1, AsyncCallback, object, IAsyncResult> beginMethod,
            Action<IAsyncResult> endMethod, TArg1 arg1, object state) =>
            this.FromAsync(beginMethod, endMethod, arg1, state, SystemTaskCreationOptions.None);

        /// <summary>
        /// Creates a task that represents a pair of begin and end methods that conform
        /// to the Asynchronous Programming Model pattern.
        /// </summary>
        public SystemTask FromAsync<TArg1>(Func<TArg1, AsyncCallback, object, IAsyncResult> beginMethod,
            Action<IAsyncResult> endMethod, TArg1 arg1, object state, SystemTaskCreationOptions creationOptions)
        {
            ExceptionProvider.ThrowUncontrolledInvocationException(nameof(SystemTask.Factory.FromAsync));
            return SystemTask.Factory.FromAsync(beginMethod, endMethod, arg1, state, creationOptions);
        }

        /// <summary>
        /// Creates a task that represents a pair of begin and end methods that conform
        /// to the Asynchronous Programming Model pattern.
        /// </summary>
        public SystemTask FromAsync<TArg1, TArg2>(
            Func<TArg1, TArg2, AsyncCallback, object, IAsyncResult> beginMethod,
            Action<IAsyncResult> endMethod, TArg1 arg1, TArg2 arg2, object state) =>
            this.FromAsync(beginMethod, endMethod, arg1, arg2, state, SystemTaskCreationOptions.None);

        /// <summary>
        /// Creates a task that represents a pair of begin and end methods that conform
        /// to the Asynchronous Programming Model pattern.
        /// </summary>
        public SystemTask FromAsync<TArg1, TArg2>(
            Func<TArg1, TArg2, AsyncCallback, object, IAsyncResult> beginMethod,
            Action<IAsyncResult> endMethod, TArg1 arg1, TArg2 arg2,
            object state, SystemTaskCreationOptions creationOptions)
        {
            ExceptionProvider.ThrowUncontrolledInvocationException(nameof(SystemTask.Factory.FromAsync));
            return SystemTask.Factory.FromAsync(beginMethod, endMethod, arg1, arg2, state, creationOptions);
        }

        /// <summary>
        /// Creates a task that represents a pair of begin and end methods that conform
        /// to the Asynchronous Programming Model pattern.
        /// </summary>
        public SystemTask FromAsync<TArg1, TArg2, TArg3>(
            Func<TArg1, TArg2, TArg3, AsyncCallback, object, IAsyncResult> beginMethod,
            Action<IAsyncResult> endMethod, TArg1 arg1, TArg2 arg2, TArg3 arg3, object state) =>
            this.FromAsync(beginMethod, endMethod, arg1, arg2, arg3, state, SystemTaskCreationOptions.None);

        /// <summary>
        /// Creates a task that represents a pair of begin and end methods that conform
        /// to the Asynchronous Programming Model pattern.
        /// </summary>
        public SystemTask FromAsync<TArg1, TArg2, TArg3>(
            Func<TArg1, TArg2, TArg3, AsyncCallback, object, IAsyncResult> beginMethod,
            Action<IAsyncResult> endMethod, TArg1 arg1, TArg2 arg2, TArg3 arg3,
            object state, SystemTaskCreationOptions creationOptions)
        {
            ExceptionProvider.ThrowUncontrolledInvocationException(nameof(SystemTask.Factory.FromAsync));
            return SystemTask.Factory.FromAsync(beginMethod, endMethod, arg1, arg2, arg3, state, creationOptions);
        }

        /// <summary>
        /// Creates a task that represents a pair of begin and end methods that conform
        /// to the Asynchronous Programming Model pattern.
        /// </summary>
        public SystemTasks.Task<TResult> FromAsync<TArg1, TResult>(
            Func<TArg1, AsyncCallback, object, IAsyncResult> beginMethod,
            Func<IAsyncResult, TResult> endMethod, TArg1 arg1, object state) =>
            this.FromAsync(beginMethod, endMethod, arg1, state, SystemTaskCreationOptions.None);

        /// <summary>
        /// Creates a task that represents a pair of begin and end methods that conform
        /// to the Asynchronous Programming Model pattern.
        /// </summary>
        public SystemTasks.Task<TResult> FromAsync<TArg1, TResult>(
            Func<TArg1, AsyncCallback, object, IAsyncResult> beginMethod,
            Func<IAsyncResult, TResult> endMethod, TArg1 arg1, object state,
            SystemTaskCreationOptions creationOptions)
        {
            ExceptionProvider.ThrowUncontrolledInvocationException(nameof(SystemTask.Factory.FromAsync));
            return SystemTask.Factory.FromAsync(beginMethod, endMethod, arg1, state, creationOptions);
        }

        /// <summary>
        /// Creates a task that represents a pair of begin and end methods that conform
        /// to the Asynchronous Programming Model pattern.
        /// </summary>
        public SystemTasks.Task<TResult> FromAsync<TArg1, TArg2, TResult>(
            Func<TArg1, TArg2, AsyncCallback, object, IAsyncResult> beginMethod,
            Func<IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, object state) =>
            this.FromAsync(beginMethod, endMethod, arg1, arg2, state, SystemTaskCreationOptions.None);

        /// <summary>
        /// Creates a task that represents a pair of begin and end methods that conform
        /// to the Asynchronous Programming Model pattern.
        /// </summary>
        public SystemTasks.Task<TResult> FromAsync<TArg1, TArg2, TResult>(
            Func<TArg1, TArg2, AsyncCallback, object, IAsyncResult> beginMethod,
            Func<IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, object state,
            SystemTaskCreationOptions creationOptions)
        {
            ExceptionProvider.ThrowUncontrolledInvocationException(nameof(SystemTask.Factory.FromAsync));
            return SystemTask.Factory.FromAsync(beginMethod, endMethod, arg1, arg2, state, creationOptions);
        }

        /// <summary>
        /// Creates a task that represents a pair of begin and end methods that conform
        /// to the Asynchronous Programming Model pattern.
        /// </summary>
        public SystemTasks.Task<TResult> FromAsync<TArg1, TArg2, TArg3, TResult>(
            Func<TArg1, TArg2, TArg3, AsyncCallback, object, IAsyncResult> beginMethod,
            Func<IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, TArg3 arg3, object state) =>
            this.FromAsync(beginMethod, endMethod, arg1, arg2, arg3, state, SystemTaskCreationOptions.None);

        /// <summary>
        /// Creates a task that represents a pair of begin and end methods that conform
        /// to the Asynchronous Programming Model pattern.
        /// </summary>
        public SystemTasks.Task<TResult> FromAsync<TArg1, TArg2, TArg3, TResult>(
            Func<TArg1, TArg2, TArg3, AsyncCallback, object, IAsyncResult> beginMethod,
            Func<IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, TArg3 arg3,
            object state, SystemTaskCreationOptions creationOptions)
        {
            ExceptionProvider.ThrowUncontrolledInvocationException(nameof(SystemTask.Factory.FromAsync));
            return SystemTask.Factory.FromAsync(beginMethod, endMethod, arg1, arg2, arg3, state, creationOptions);
        }

        /// <summary>
        /// Creates a task that executes an end method action when a specified async result completes.
        /// </summary>
        public SystemTask FromAsync(IAsyncResult asyncResult, Action<IAsyncResult> endMethod) =>
            this.FromAsync(asyncResult, endMethod, SystemTaskCreationOptions.None, SystemTaskScheduler.Default);

        /// <summary>
        /// Creates a task that executes an end method action when a specified async result completes.
        /// </summary>
        public SystemTask FromAsync(IAsyncResult asyncResult, Action<IAsyncResult> endMethod,
            SystemTaskCreationOptions creationOptions) =>
            this.FromAsync(asyncResult, endMethod, creationOptions, SystemTaskScheduler.Default);

        /// <summary>
        /// Creates a task that executes an end method action when a specified async result completes.
        /// </summary>
        public SystemTask FromAsync(IAsyncResult asyncResult, Action<IAsyncResult> endMethod,
            SystemTaskCreationOptions creationOptions, SystemTaskScheduler scheduler)
        {
            ExceptionProvider.ThrowUncontrolledInvocationException(nameof(SystemTask.Factory.FromAsync));
            return SystemTask.Factory.FromAsync(asyncResult, endMethod, creationOptions, scheduler);
        }

        /// <summary>
        /// Creates a task that executes an end method action when a specified async result completes.
        /// </summary>
        public SystemTasks.Task<TResult> FromAsync<TResult>(IAsyncResult asyncResult,
            Func<IAsyncResult, TResult> endMethod) =>
            this.FromAsync(asyncResult, endMethod, SystemTaskCreationOptions.None, SystemTaskScheduler.Default);

        /// <summary>
        /// Creates a task that executes an end method action when a specified async result completes.
        /// </summary>
        public SystemTasks.Task<TResult> FromAsync<TResult>(IAsyncResult asyncResult,
            Func<IAsyncResult, TResult> endMethod, SystemTaskCreationOptions creationOptions) =>
            this.FromAsync(asyncResult, endMethod, creationOptions, SystemTaskScheduler.Default);

        /// <summary>
        /// Creates a task that executes an end method action when a specified async result completes.
        /// </summary>
        public SystemTasks.Task<TResult> FromAsync<TResult>(IAsyncResult asyncResult,
            Func<IAsyncResult, TResult> endMethod, SystemTaskCreationOptions creationOptions,
            SystemTaskScheduler scheduler)
        {
            ExceptionProvider.ThrowUncontrolledInvocationException(nameof(SystemTask.Factory.FromAsync));
            return SystemTask.Factory.FromAsync(asyncResult, endMethod, creationOptions, scheduler);
        }
    }

    /// <summary>
    /// Provides support for creating and scheduling controlled generic task objects.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public class TaskFactory<TResult>
    {
        // Note: we are only intercepting and modeling a very limited set of APIs to enable specific scenarios such
        // as ASP.NET rewriting. Most `TaskFactory` APIs are not supported by our modeling, and we do not currently
        // aim to support user applications with code that explicitly uses the `TaskFactory`.

        /// <summary>
        /// The default task continuation options for this task factory.
        /// </summary>
        public SystemTaskContinuationOptions ContinuationOptions => SystemTasks.Task<TResult>.Factory.ContinuationOptions;

        /// <summary>
        /// The default task cancellation token for this task factory.
        /// </summary>
        public SystemCancellationToken CancellationToken => SystemTasks.Task<TResult>.Factory.CancellationToken;

        /// <summary>
        /// The default task creation options for this task factory.
        /// </summary>
        public SystemTaskCreationOptions CreationOptions => SystemTasks.Task<TResult>.Factory.CreationOptions;

        /// <summary>
        /// The default task scheduler for this task factory.
        /// </summary>
        public SystemTaskScheduler Scheduler => SystemTasks.Task<TResult>.Factory.Scheduler;

        /// <summary>
        /// Creates and starts a task.
        /// </summary>
        public SystemTasks.Task<TResult> StartNew(Func<TResult> function) =>
            Task.Factory.StartNew(function);

        /// <summary>
        /// Creates and starts a task.
        /// </summary>
        public SystemTasks.Task<TResult> StartNew(Func<TResult> function, SystemCancellationToken cancellationToken) =>
            Task.Factory.StartNew(function, cancellationToken);

        /// <summary>
        /// Creates and starts a task.
        /// </summary>
        public SystemTasks.Task<TResult> StartNew(Func<TResult> function, SystemTaskCreationOptions creationOptions) =>
            Task.Factory.StartNew(function, creationOptions);

        /// <summary>
        /// Creates and starts a task.
        /// </summary>
        public SystemTasks.Task<TResult> StartNew(Func<TResult> function, SystemCancellationToken cancellationToken,
             SystemTaskCreationOptions creationOptions, SystemTaskScheduler scheduler) =>
            Task.Factory.StartNew(function, cancellationToken, creationOptions, scheduler);

        /// <summary>
        /// Creates and starts a task.
        /// </summary>
        public SystemTasks.Task<TResult> StartNew(Func<object, TResult> function, object state) =>
            Task.Factory.StartNew(function, state);

        /// <summary>
        /// Creates and starts a task.
        /// </summary>
        public SystemTasks.Task<TResult> StartNew(Func<object, TResult> function, object state,
            SystemCancellationToken cancellationToken) =>
            Task.Factory.StartNew(function, state, cancellationToken);

        /// <summary>
        /// Creates and starts a task.
        /// </summary>
        public SystemTasks.Task<TResult> StartNew(Func<object, TResult> function, object state,
            SystemTaskCreationOptions creationOptions) =>
            Task.Factory.StartNew(function, state, creationOptions);

        /// <summary>
        /// Creates and starts a task.
        /// </summary>
        public SystemTasks.Task<TResult> StartNew(Func<object, TResult> function, object state,
            SystemCancellationToken cancellationToken,
            SystemTaskCreationOptions creationOptions, SystemTaskScheduler scheduler) =>
            Task.Factory.StartNew(function, state, cancellationToken, creationOptions, scheduler);

        /// <summary>
        /// Creates a continuation task that starts when a set of specified tasks has completed.
        /// </summary>
        public SystemTasks.Task<TResult> ContinueWhenAll(SystemTask[] tasks,
            Func<SystemTask[], TResult> continuationFunction) =>
            Task.Factory.ContinueWhenAll(tasks, continuationFunction);

        /// <summary>
        /// Creates a continuation task that starts when a set of specified tasks has completed.
        /// </summary>
        public SystemTasks.Task<TResult> ContinueWhenAll(SystemTask[] tasks,
            Func<SystemTask[], TResult> continuationFunction, SystemCancellationToken cancellationToken) =>
            Task.Factory.ContinueWhenAll(tasks, continuationFunction, cancellationToken);

        /// <summary>
        /// Creates a continuation task that starts when a set of specified tasks has completed.
        /// </summary>
        public SystemTasks.Task<TResult> ContinueWhenAll(SystemTask[] tasks,
            Func<SystemTask[], TResult> continuationFunction,
            SystemTaskContinuationOptions continuationOptions) =>
            Task.Factory.ContinueWhenAll(tasks, continuationFunction, continuationOptions);

        /// <summary>
        /// Creates a continuation task that starts when a set of specified tasks has completed.
        /// </summary>
        public SystemTasks.Task<TResult> ContinueWhenAll(SystemTask[] tasks,
            Func<SystemTask[], TResult> continuationFunction, SystemCancellationToken cancellationToken,
            SystemTaskContinuationOptions continuationOptions, SystemTaskScheduler scheduler) =>
            Task.Factory.ContinueWhenAll(tasks, continuationFunction, cancellationToken, continuationOptions, scheduler);

        /// <summary>
        /// Creates a continuation task that starts when a set of specified tasks has completed.
        /// </summary>
        public SystemTasks.Task<TResult> ContinueWhenAll<TAntecedentResult>(
            SystemTasks.Task<TAntecedentResult>[] tasks,
            Func<SystemTasks.Task<TAntecedentResult>[], TResult> continuationFunction) =>
            Task.Factory.ContinueWhenAll(tasks, continuationFunction);

        /// <summary>
        /// Creates a continuation task that starts when a set of specified tasks has completed.
        /// </summary>
        public SystemTasks.Task<TResult> ContinueWhenAll<TAntecedentResult>(
            SystemTasks.Task<TAntecedentResult>[] tasks,
            Func<SystemTasks.Task<TAntecedentResult>[], TResult> continuationFunction,
            SystemCancellationToken cancellationToken) =>
            Task.Factory.ContinueWhenAll(tasks, continuationFunction, cancellationToken);

        /// <summary>
        /// Creates a continuation task that starts when a set of specified tasks has completed.
        /// </summary>
        public SystemTasks.Task<TResult> ContinueWhenAll<TAntecedentResult>(
            SystemTasks.Task<TAntecedentResult>[] tasks,
            Func<SystemTasks.Task<TAntecedentResult>[], TResult> continuationFunction,
            SystemTaskContinuationOptions continuationOptions) =>
            Task.Factory.ContinueWhenAll(tasks, continuationFunction, continuationOptions);

        /// <summary>
        /// Creates a continuation task that starts when a set of specified tasks has completed.
        /// </summary>
        public SystemTasks.Task<TResult> ContinueWhenAll<TAntecedentResult>(
            SystemTasks.Task<TAntecedentResult>[] tasks,
            Func<SystemTasks.Task<TAntecedentResult>[], TResult> continuationFunction,
            SystemCancellationToken cancellationToken,
            SystemTaskContinuationOptions continuationOptions,
            SystemTaskScheduler scheduler) =>
            Task.Factory.ContinueWhenAll(tasks, continuationFunction, cancellationToken, continuationOptions, scheduler);

        /// <summary>
        /// Creates a continuation task that will be started upon the completion of any task in the provided set.
        /// </summary>
        public SystemTasks.Task<TResult> ContinueWhenAny(SystemTask[] tasks,
        Func<SystemTask, TResult> continuationFunction) =>
            Task.Factory.ContinueWhenAny(tasks, continuationFunction);

        /// <summary>
        /// Creates a continuation task that will be started upon the completion of any task in the provided set.
        /// </summary>
        public SystemTasks.Task<TResult> ContinueWhenAny(SystemTask[] tasks,
            Func<SystemTask, TResult> continuationFunction,
            SystemCancellationToken cancellationToken) =>
            Task.Factory.ContinueWhenAny(tasks, continuationFunction, cancellationToken);

        /// <summary>
        /// Creates a continuation task that will be started upon the completion of any task in the provided set.
        /// </summary>
        public SystemTasks.Task<TResult> ContinueWhenAny(SystemTask[] tasks,
            Func<SystemTask, TResult> continuationFunction,
            SystemTaskContinuationOptions continuationOptions) =>
            Task.Factory.ContinueWhenAny(tasks, continuationFunction, continuationOptions);

        /// <summary>
        /// Creates a continuation task that will be started upon the completion of any task in the provided set.
        /// </summary>
        public SystemTasks.Task<TResult> ContinueWhenAny(SystemTask[] tasks,
            Func<SystemTask, TResult> continuationFunction, SystemCancellationToken cancellationToken,
            SystemTaskContinuationOptions continuationOptions, SystemTaskScheduler scheduler) =>
            Task.Factory.ContinueWhenAny(tasks, continuationFunction, cancellationToken,
                continuationOptions, scheduler);

        /// <summary>
        /// Creates a continuation task that will be started upon the completion of any task in the provided set.
        /// </summary>
        public SystemTasks.Task<TResult> ContinueWhenAny<TAntecedentResult>(
            SystemTasks.Task<TAntecedentResult>[] tasks,
            Func<SystemTasks.Task<TAntecedentResult>, TResult> continuationFunction) =>
            Task.Factory.ContinueWhenAny(tasks, continuationFunction);

        /// <summary>
        /// Creates a continuation task that will be started upon the completion of any task in the provided set.
        /// </summary>
        public SystemTasks.Task<TResult> ContinueWhenAny<TAntecedentResult>(
            SystemTasks.Task<TAntecedentResult>[] tasks,
            Func<SystemTasks.Task<TAntecedentResult>, TResult> continuationFunction,
            SystemCancellationToken cancellationToken) =>
            Task.Factory.ContinueWhenAny(tasks, continuationFunction, cancellationToken);

        /// <summary>
        /// Creates a continuation task that will be started upon the completion of any task in the provided set.
        /// </summary>
        public SystemTasks.Task<TResult> ContinueWhenAny<TAntecedentResult>(
            SystemTasks.Task<TAntecedentResult>[] tasks,
            Func<SystemTasks.Task<TAntecedentResult>, TResult> continuationFunction,
            SystemTaskContinuationOptions continuationOptions) =>
            Task.Factory.ContinueWhenAny(tasks, continuationFunction, continuationOptions);

        /// <summary>
        /// Creates a continuation task that will be started upon the completion of any task in the provided set.
        /// </summary>
        public SystemTasks.Task<TResult> ContinueWhenAny<TAntecedentResult>(
            SystemTasks.Task<TAntecedentResult>[] tasks,
            Func<SystemTasks.Task<TAntecedentResult>, TResult> continuationFunction,
            SystemCancellationToken cancellationToken,
            SystemTaskContinuationOptions continuationOptions,
            SystemTaskScheduler scheduler) =>
            Task.Factory.ContinueWhenAny(tasks, continuationFunction, cancellationToken,
                continuationOptions, scheduler);

        /// <summary>
        /// Creates a task that represents a pair of begin and end methods that conform
        /// to the Asynchronous Programming Model pattern.
        /// </summary>
        public SystemTasks.Task<TResult> FromAsync(Func<AsyncCallback, object, IAsyncResult> beginMethod,
            Func<IAsyncResult, TResult> endMethod, object state) =>
            Task.Factory.FromAsync(beginMethod, endMethod, state);

        /// <summary>
        /// Creates a task that represents a pair of begin and end methods that conform
        /// to the Asynchronous Programming Model pattern.
        /// </summary>
        public SystemTasks.Task<TResult> FromAsync(Func<AsyncCallback, object, IAsyncResult> beginMethod,
            Func<IAsyncResult, TResult> endMethod, object state, SystemTaskCreationOptions creationOptions) =>
            Task.Factory.FromAsync(beginMethod, endMethod, state, creationOptions);

        /// <summary>
        /// Creates a task that represents a pair of begin and end methods that conform
        /// to the Asynchronous Programming Model pattern.
        /// </summary>
        public SystemTasks.Task<TResult> FromAsync<TArg1>(
            Func<TArg1, AsyncCallback, object, IAsyncResult> beginMethod,
            Func<IAsyncResult, TResult> endMethod, TArg1 arg1, object state) =>
            Task.Factory.FromAsync(beginMethod, endMethod, arg1, state);

        /// <summary>
        /// Creates a task that represents a pair of begin and end methods that conform
        /// to the Asynchronous Programming Model pattern.
        /// </summary>
        public SystemTasks.Task<TResult> FromAsync<TArg1>(
            Func<TArg1, AsyncCallback, object, IAsyncResult> beginMethod,
            Func<IAsyncResult, TResult> endMethod, TArg1 arg1, object state,
            SystemTaskCreationOptions creationOptions) =>
            Task.Factory.FromAsync(beginMethod, endMethod, arg1, state, creationOptions);

        /// <summary>
        /// Creates a task that represents a pair of begin and end methods that conform
        /// to the Asynchronous Programming Model pattern.
        /// </summary>
        public SystemTasks.Task<TResult> FromAsync<TArg1, TArg2>(
            Func<TArg1, TArg2, AsyncCallback, object, IAsyncResult> beginMethod,
            Func<IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, object state) =>
            Task.Factory.FromAsync(beginMethod, endMethod, arg1, arg2, state);

        /// <summary>
        /// Creates a task that represents a pair of begin and end methods that conform
        /// to the Asynchronous Programming Model pattern.
        /// </summary>
        public SystemTasks.Task<TResult> FromAsync<TArg1, TArg2>(
            Func<TArg1, TArg2, AsyncCallback, object, IAsyncResult> beginMethod,
            Func<IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2,
            object state, SystemTaskCreationOptions creationOptions) =>
            Task.Factory.FromAsync(beginMethod, endMethod, arg1, arg2, state, creationOptions);

        /// <summary>
        /// Creates a task that represents a pair of begin and end methods that conform
        /// to the Asynchronous Programming Model pattern.
        /// </summary>
        public SystemTasks.Task<TResult> FromAsync<TArg1, TArg2, TArg3>(
            Func<TArg1, TArg2, TArg3, AsyncCallback, object, IAsyncResult> beginMethod,
            Func<IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, TArg3 arg3, object state) =>
            Task.Factory.FromAsync(beginMethod, endMethod, arg1, arg2, arg3, state);

        /// <summary>
        /// Creates a task that represents a pair of begin and end methods that conform
        /// to the Asynchronous Programming Model pattern.
        /// </summary>
        public SystemTasks.Task<TResult> FromAsync<TArg1, TArg2, TArg3>(
            Func<TArg1, TArg2, TArg3, AsyncCallback, object, IAsyncResult> beginMethod,
            Func<IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, TArg3 arg3,
            object state, SystemTaskCreationOptions creationOptions) =>
            Task.Factory.FromAsync(beginMethod, endMethod, arg1, arg2, arg3, state, creationOptions);

        /// <summary>
        /// Creates a task that executes an end method action when a specified async result completes.
        /// </summary>
        public SystemTasks.Task<TResult> FromAsync(IAsyncResult asyncResult, Func<IAsyncResult, TResult> endMethod) =>
            Task.Factory.FromAsync(asyncResult, endMethod);

        /// <summary>
        /// Creates a task that executes an end method action when a specified async result completes.
        /// </summary>
        public SystemTasks.Task<TResult> FromAsync(IAsyncResult asyncResult, Func<IAsyncResult, TResult> endMethod,
            SystemTaskCreationOptions creationOptions) =>
            Task.Factory.FromAsync(asyncResult, endMethod, creationOptions);

        /// <summary>
        /// Creates a task that executes an end method action when a specified async result completes.
        /// </summary>
        public SystemTasks.Task<TResult> FromAsync(IAsyncResult asyncResult, Func<IAsyncResult, TResult> endMethod,
            SystemTaskCreationOptions creationOptions, SystemTaskScheduler scheduler) =>
            Task.Factory.FromAsync(asyncResult, endMethod, creationOptions, scheduler);
    }
#pragma warning restore CA1068 // CancellationToken parameters must come last
#pragma warning restore CA1822 // Mark members as static
}
