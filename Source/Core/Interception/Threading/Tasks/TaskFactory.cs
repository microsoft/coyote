// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Coyote.Runtime;

namespace Microsoft.Coyote.Interception
{
#pragma warning disable CA1822 // Mark members as static
#pragma warning disable CA1068 // CancellationToken parameters must come last
    /// <summary>
    /// Provides support for creating and scheduling controlled <see cref="Task"/> objects.
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
        public TaskContinuationOptions ContinuationOptions => Task.Factory.ContinuationOptions;

        /// <summary>
        /// The default task cancellation token for this task factory.
        /// </summary>
        public CancellationToken CancellationToken => Task.Factory.CancellationToken;

        /// <summary>
        /// The default task creation options for this task factory.
        /// </summary>
        public TaskCreationOptions CreationOptions => Task.Factory.CreationOptions;

        /// <summary>
        /// The default task scheduler for this task factory.
        /// </summary>
        public TaskScheduler Scheduler => Task.Factory.Scheduler;

        /// <summary>
        /// Creates and starts a <see cref="Task"/>.
        /// </summary>
        public Task StartNew(Action action) => this.StartNew(action, CancellationToken.None);

        /// <summary>
        /// Creates and starts a <see cref="Task"/>.
        /// </summary>
        public Task StartNew(Action action, CancellationToken cancellationToken) =>
            Task.Factory.StartNew(action, cancellationToken, TaskCreationOptions.None, CoyoteRuntime.IsExecutionControlled ?
                CoyoteRuntime.Current.ControlledTaskScheduler : TaskScheduler.Default);

        /// <summary>
        /// Creates and starts a <see cref="Task"/>.
        /// </summary>
        public Task StartNew(Action action, TaskCreationOptions creationOptions) =>
            this.StartNew(action, default, creationOptions, TaskScheduler.Default);

        /// <summary>
        /// Creates and starts a <see cref="Task"/>.
        /// </summary>
        public Task StartNew(Action action, CancellationToken cancellationToken, TaskCreationOptions creationOptions, TaskScheduler scheduler)
        {
            ExceptionProvider.ThrowNotSupportedInvocationException(nameof(Task.Factory.StartNew));
            return Task.Factory.StartNew(action, cancellationToken, creationOptions, scheduler);
        }

        /// <summary>
        /// Creates and starts a <see cref="Task"/>.
        /// </summary>
        public Task StartNew(Action<object> action, object state) =>
            this.StartNew(action, state, default, TaskCreationOptions.None, TaskScheduler.Default);

        /// <summary>
        /// Creates and starts a <see cref="Task"/>.
        /// </summary>
        public Task StartNew(Action<object> action, object state, CancellationToken cancellationToken) =>
            this.StartNew(action, state, cancellationToken, TaskCreationOptions.None, TaskScheduler.Default);

        /// <summary>
        /// Creates and starts a <see cref="Task"/>.
        /// </summary>
        public Task StartNew(Action<object> action, object state, TaskCreationOptions creationOptions) =>
            this.StartNew(action, state, default, creationOptions, TaskScheduler.Default);

        /// <summary>
        /// Creates and starts a <see cref="Task"/>.
        /// </summary>
        public Task StartNew(Action<object> action, object state, CancellationToken cancellationToken,
            TaskCreationOptions creationOptions, TaskScheduler scheduler)
        {
            ExceptionProvider.ThrowNotSupportedInvocationException(nameof(Task.Factory.StartNew));
            return Task.Factory.StartNew(action, state, cancellationToken, creationOptions, scheduler);
        }

        /// <summary>
        /// Creates and starts a <see cref="Task{TResult}"/>.
        /// </summary>
        public Task<TResult> StartNew<TResult>(Func<TResult> function) => this.StartNew(function, CancellationToken.None);

        /// <summary>
        /// Creates and starts a <see cref="Task{TResult}"/>.
        /// </summary>
        public Task<TResult> StartNew<TResult>(Func<TResult> function, CancellationToken cancellationToken) =>
            Task.Factory.StartNew(function, cancellationToken, TaskCreationOptions.None, CoyoteRuntime.IsExecutionControlled ?
                CoyoteRuntime.Current.ControlledTaskScheduler : TaskScheduler.Default);

        /// <summary>
        /// Creates and starts a <see cref="Task{TResult}"/>.
        /// </summary>
        public Task<TResult> StartNew<TResult>(Func<TResult> function, TaskCreationOptions creationOptions) =>
            this.StartNew(function, default, creationOptions, TaskScheduler.Default);

        /// <summary>
        /// Creates and starts a <see cref="Task{TResult}"/>.
        /// </summary>
        public Task<TResult> StartNew<TResult>(Func<TResult> function, CancellationToken cancellationToken,
            TaskCreationOptions creationOptions, TaskScheduler scheduler)
        {
            ExceptionProvider.ThrowNotSupportedInvocationException(nameof(Task.Factory.StartNew));
            return Task.Factory.StartNew(function, cancellationToken, creationOptions, scheduler);
        }

        /// <summary>
        /// Creates and starts a <see cref="Task{TResult}"/>.
        /// </summary>
        public Task<TResult> StartNew<TResult>(Func<object, TResult> function, object state) =>
            this.StartNew(function, state, default, TaskCreationOptions.None, TaskScheduler.Default);

        /// <summary>
        /// Creates and starts a <see cref="Task{TResult}"/>.
        /// </summary>
        public Task<TResult> StartNew<TResult>(Func<object, TResult> function, object state, CancellationToken cancellationToken) =>
            this.StartNew(function, state, cancellationToken, TaskCreationOptions.None, TaskScheduler.Default);

        /// <summary>
        /// Creates and starts a <see cref="Task{TResult}"/>.
        /// </summary>
        public Task<TResult> StartNew<TResult>(Func<object, TResult> function, object state, TaskCreationOptions creationOptions) =>
            this.StartNew(function, state, default, creationOptions, TaskScheduler.Default);

        /// <summary>
        /// Creates and starts a <see cref="Task{TResult}"/>.
        /// </summary>
        public Task<TResult> StartNew<TResult>(Func<object, TResult> function, object state, CancellationToken cancellationToken,
            TaskCreationOptions creationOptions, TaskScheduler scheduler)
        {
            ExceptionProvider.ThrowNotSupportedInvocationException(nameof(Task.Factory.StartNew));
            return Task.Factory.StartNew(function, state, cancellationToken, creationOptions, scheduler);
        }

        /// <summary>
        /// Creates a continuation task that starts when a set of specified tasks has completed.
        /// </summary>
        public Task ContinueWhenAll(Task[] tasks, Action<Task[]> continuationAction) =>
            this.ContinueWhenAll(tasks, continuationAction, default, TaskContinuationOptions.None, TaskScheduler.Default);

        /// <summary>
        /// Creates a continuation task that starts when a set of specified tasks has completed.
        /// </summary>
        public Task ContinueWhenAll(Task[] tasks, Action<Task[]> continuationAction, CancellationToken cancellationToken) =>
            this.ContinueWhenAll(tasks, continuationAction, cancellationToken, TaskContinuationOptions.None, TaskScheduler.Default);

        /// <summary>
        /// Creates a continuation task that starts when a set of specified tasks has completed.
        /// </summary>
        public Task ContinueWhenAll(Task[] tasks, Action<Task[]> continuationAction, TaskContinuationOptions continuationOptions) =>
            this.ContinueWhenAll(tasks, continuationAction, default, continuationOptions, TaskScheduler.Default);

        /// <summary>
        /// Creates a continuation task that starts when a set of specified tasks has completed.
        /// </summary>
        public Task ContinueWhenAll(Task[] tasks, Action<Task[]> continuationAction, CancellationToken cancellationToken,
            TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            ExceptionProvider.ThrowNotSupportedInvocationException(nameof(Task.Factory.ContinueWhenAll));
            return Task.Factory.ContinueWhenAll(tasks, continuationAction, cancellationToken, continuationOptions, scheduler);
        }

        /// <summary>
        /// Creates a continuation task that starts when a set of specified tasks has completed.
        /// </summary>
        public Task ContinueWhenAll<TAntecedentResult>(Task<TAntecedentResult>[] tasks, Action<Task<TAntecedentResult>[]> continuationAction) =>
            this.ContinueWhenAll(tasks, continuationAction, default, TaskContinuationOptions.None, TaskScheduler.Default);

        /// <summary>
        /// Creates a continuation task that starts when a set of specified tasks has completed.
        /// </summary>
        public Task ContinueWhenAll<TAntecedentResult>(Task<TAntecedentResult>[] tasks, Action<Task<TAntecedentResult>[]> continuationAction,
            CancellationToken cancellationToken) =>
            this.ContinueWhenAll(tasks, continuationAction, cancellationToken, TaskContinuationOptions.None, TaskScheduler.Default);

        /// <summary>
        /// Creates a continuation task that starts when a set of specified tasks has completed.
        /// </summary>
        public Task ContinueWhenAll<TAntecedentResult>(Task<TAntecedentResult>[] tasks, Action<Task<TAntecedentResult>[]> continuationAction,
            TaskContinuationOptions continuationOptions) =>
            this.ContinueWhenAll(tasks, continuationAction, default, continuationOptions, TaskScheduler.Default);

        /// <summary>
        /// Creates a continuation task that starts when a set of specified tasks has completed.
        /// </summary>
        public Task ContinueWhenAll<TAntecedentResult>(Task<TAntecedentResult>[] tasks, Action<Task<TAntecedentResult>[]> continuationAction,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            ExceptionProvider.ThrowNotSupportedInvocationException(nameof(Task.Factory.ContinueWhenAll));
            return Task.Factory.ContinueWhenAll(tasks, continuationAction, cancellationToken, continuationOptions, scheduler);
        }

        /// <summary>
        /// Creates a continuation task that starts when a set of specified tasks has completed.
        /// </summary>
        public Task<TResult> ContinueWhenAll<TResult>(Task[] tasks, Func<Task[], TResult> continuationFunction) =>
            this.ContinueWhenAll(tasks, continuationFunction, default, TaskContinuationOptions.None, TaskScheduler.Default);

        /// <summary>
        /// Creates a continuation task that starts when a set of specified tasks has completed.
        /// </summary>
        public Task<TResult> ContinueWhenAll<TResult>(Task[] tasks, Func<Task[], TResult> continuationFunction,
            CancellationToken cancellationToken) =>
            this.ContinueWhenAll(tasks, continuationFunction, cancellationToken, TaskContinuationOptions.None, TaskScheduler.Default);

        /// <summary>
        /// Creates a continuation task that starts when a set of specified tasks has completed.
        /// </summary>
        public Task<TResult> ContinueWhenAll<TResult>(Task[] tasks, Func<Task[], TResult> continuationFunction,
            TaskContinuationOptions continuationOptions) =>
            this.ContinueWhenAll(tasks, continuationFunction, default, continuationOptions, TaskScheduler.Default);

        /// <summary>
        /// Creates a continuation task that starts when a set of specified tasks has completed.
        /// </summary>
        public Task<TResult> ContinueWhenAll<TResult>(Task[] tasks, Func<Task[], TResult> continuationFunction,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            ExceptionProvider.ThrowNotSupportedInvocationException(nameof(Task.Factory.ContinueWhenAll));
            return Task.Factory.ContinueWhenAll(tasks, continuationFunction, cancellationToken, continuationOptions, scheduler);
        }

        /// <summary>
        /// Creates a continuation task that starts when a set of specified tasks has completed.
        /// </summary>
        public Task<TResult> ContinueWhenAll<TAntecedentResult, TResult>(Task<TAntecedentResult>[] tasks,
            Func<Task<TAntecedentResult>[], TResult> continuationFunction) =>
            this.ContinueWhenAll(tasks, continuationFunction, default, TaskContinuationOptions.None, TaskScheduler.Default);

        /// <summary>
        /// Creates a continuation task that starts when a set of specified tasks has completed.
        /// </summary>
        public Task<TResult> ContinueWhenAll<TAntecedentResult, TResult>(Task<TAntecedentResult>[] tasks,
            Func<Task<TAntecedentResult>[], TResult> continuationFunction, CancellationToken cancellationToken) =>
            this.ContinueWhenAll(tasks, continuationFunction, cancellationToken, TaskContinuationOptions.None, TaskScheduler.Default);

        /// <summary>
        /// Creates a continuation task that starts when a set of specified tasks has completed.
        /// </summary>
        public Task<TResult> ContinueWhenAll<TAntecedentResult, TResult>(Task<TAntecedentResult>[] tasks,
            Func<Task<TAntecedentResult>[], TResult> continuationFunction, TaskContinuationOptions continuationOptions) =>
            this.ContinueWhenAll(tasks, continuationFunction, default, continuationOptions, TaskScheduler.Default);

        /// <summary>
        /// Creates a continuation task that starts when a set of specified tasks has completed.
        /// </summary>
        public Task<TResult> ContinueWhenAll<TAntecedentResult, TResult>(Task<TAntecedentResult>[] tasks,
            Func<Task<TAntecedentResult>[], TResult> continuationFunction, CancellationToken cancellationToken,
            TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            ExceptionProvider.ThrowNotSupportedInvocationException(nameof(Task.Factory.ContinueWhenAll));
            return Task.Factory.ContinueWhenAll(tasks, continuationFunction, cancellationToken, continuationOptions, scheduler);
        }

        /// <summary>
        /// Creates a continuation task that will be started upon the completion of any task in the provided set.
        /// </summary>
        public Task ContinueWhenAny(Task[] tasks, Action<Task> continuationAction) =>
            this.ContinueWhenAny(tasks, continuationAction, default, TaskContinuationOptions.None, TaskScheduler.Default);

        /// <summary>
        /// Creates a continuation task that will be started upon the completion of any task in the provided set.
        /// </summary>
        public Task ContinueWhenAny(Task[] tasks, Action<Task> continuationAction, CancellationToken cancellationToken) =>
            this.ContinueWhenAny(tasks, continuationAction, cancellationToken, TaskContinuationOptions.None, TaskScheduler.Default);

        /// <summary>
        /// Creates a continuation task that will be started upon the completion of any task in the provided set.
        /// </summary>
        public Task ContinueWhenAny(Task[] tasks, Action<Task> continuationAction, TaskContinuationOptions continuationOptions) =>
            this.ContinueWhenAny(tasks, continuationAction, default, continuationOptions, TaskScheduler.Default);

        /// <summary>
        /// Creates a continuation task that will be started upon the completion of any task in the provided set.
        /// </summary>
        public Task ContinueWhenAny(Task[] tasks, Action<Task> continuationAction, CancellationToken cancellationToken,
            TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            ExceptionProvider.ThrowNotSupportedInvocationException(nameof(Task.Factory.ContinueWhenAny));
            return Task.Factory.ContinueWhenAny(tasks, continuationAction, cancellationToken, continuationOptions, scheduler);
        }

        /// <summary>
        /// Creates a continuation task that will be started upon the completion of any task in the provided set.
        /// </summary>
        public Task ContinueWhenAny<TAntecedentResult>(Task<TAntecedentResult>[] tasks, Action<Task<TAntecedentResult>> continuationAction) =>
            this.ContinueWhenAny(tasks, continuationAction, default, TaskContinuationOptions.None, TaskScheduler.Default);

        /// <summary>
        /// Creates a continuation task that will be started upon the completion of any task in the provided set.
        /// </summary>
        public Task ContinueWhenAny<TAntecedentResult>(Task<TAntecedentResult>[] tasks, Action<Task<TAntecedentResult>> continuationAction,
            CancellationToken cancellationToken) =>
            this.ContinueWhenAny(tasks, continuationAction, cancellationToken, TaskContinuationOptions.None, TaskScheduler.Default);

        /// <summary>
        /// Creates a continuation task that will be started upon the completion of any task in the provided set.
        /// </summary>
        public Task ContinueWhenAny<TAntecedentResult>(Task<TAntecedentResult>[] tasks, Action<Task<TAntecedentResult>> continuationAction,
            TaskContinuationOptions continuationOptions) =>
            this.ContinueWhenAny(tasks, continuationAction, default, continuationOptions, TaskScheduler.Default);

        /// <summary>
        /// Creates a continuation task that will be started upon the completion of any task in the provided set.
        /// </summary>
        public Task ContinueWhenAny<TAntecedentResult>(Task<TAntecedentResult>[] tasks, Action<Task<TAntecedentResult>> continuationAction,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            ExceptionProvider.ThrowNotSupportedInvocationException(nameof(Task.Factory.ContinueWhenAny));
            return Task.Factory.ContinueWhenAny(tasks, continuationAction, cancellationToken, continuationOptions, scheduler);
        }

        /// <summary>
        /// Creates a continuation task that will be started upon the completion of any task in the provided set.
        /// </summary>
        public Task<TResult> ContinueWhenAny<TResult>(Task[] tasks, Func<Task, TResult> continuationFunction) =>
            this.ContinueWhenAny(tasks, continuationFunction, default, TaskContinuationOptions.None, TaskScheduler.Default);

        /// <summary>
        /// Creates a continuation task that will be started upon the completion of any task in the provided set.
        /// </summary>
        public Task<TResult> ContinueWhenAny<TResult>(Task[] tasks, Func<Task, TResult> continuationFunction,
            CancellationToken cancellationToken) =>
            this.ContinueWhenAny(tasks, continuationFunction, cancellationToken, TaskContinuationOptions.None, TaskScheduler.Default);

        /// <summary>
        /// Creates a continuation task that will be started upon the completion of any task in the provided set.
        /// </summary>
        public Task<TResult> ContinueWhenAny<TResult>(Task[] tasks, Func<Task, TResult> continuationFunction,
            TaskContinuationOptions continuationOptions) =>
            this.ContinueWhenAny(tasks, continuationFunction, default, continuationOptions, TaskScheduler.Default);

        /// <summary>
        /// Creates a continuation task that will be started upon the completion of any task in the provided set.
        /// </summary>
        public Task<TResult> ContinueWhenAny<TResult>(Task[] tasks, Func<Task, TResult> continuationFunction,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            ExceptionProvider.ThrowNotSupportedInvocationException(nameof(Task.Factory.ContinueWhenAny));
            return Task.Factory.ContinueWhenAny(tasks, continuationFunction, cancellationToken, continuationOptions, scheduler);
        }

        /// <summary>
        /// Creates a continuation task that will be started upon the completion of any task in the provided set.
        /// </summary>
        public Task<TResult> ContinueWhenAny<TAntecedentResult, TResult>(Task<TAntecedentResult>[] tasks,
            Func<Task<TAntecedentResult>, TResult> continuationFunction) =>
            this.ContinueWhenAny(tasks, continuationFunction, default, TaskContinuationOptions.None, TaskScheduler.Default);

        /// <summary>
        /// Creates a continuation task that will be started upon the completion of any task in the provided set.
        /// </summary>
        public Task<TResult> ContinueWhenAny<TAntecedentResult, TResult>(Task<TAntecedentResult>[] tasks,
            Func<Task<TAntecedentResult>, TResult> continuationFunction, CancellationToken cancellationToken) =>
            this.ContinueWhenAny(tasks, continuationFunction, cancellationToken, TaskContinuationOptions.None, TaskScheduler.Default);

        /// <summary>
        /// Creates a continuation task that will be started upon the completion of any task in the provided set.
        /// </summary>
        public Task<TResult> ContinueWhenAny<TAntecedentResult, TResult>(Task<TAntecedentResult>[] tasks,
            Func<Task<TAntecedentResult>, TResult> continuationFunction, TaskContinuationOptions continuationOptions) =>
            this.ContinueWhenAny(tasks, continuationFunction, default, continuationOptions, TaskScheduler.Default);

        /// <summary>
        /// Creates a continuation task that will be started upon the completion of any task in the provided set.
        /// </summary>
        public Task<TResult> ContinueWhenAny<TAntecedentResult, TResult>(Task<TAntecedentResult>[] tasks,
            Func<Task<TAntecedentResult>, TResult> continuationFunction, CancellationToken cancellationToken,
            TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            ExceptionProvider.ThrowNotSupportedInvocationException(nameof(Task.Factory.ContinueWhenAny));
            return Task.Factory.ContinueWhenAny(tasks, continuationFunction, cancellationToken, continuationOptions, scheduler);
        }

        /// <summary>
        /// Creates a task that represents a pair of begin and end methods that conform
        /// to the Asynchronous Programming Model pattern.
        /// </summary>
        public Task FromAsync(Func<AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, object state) =>
            this.FromAsync(beginMethod, endMethod, state, TaskCreationOptions.None);

        /// <summary>
        /// Creates a task that represents a pair of begin and end methods that conform
        /// to the Asynchronous Programming Model pattern.
        /// </summary>
        public Task FromAsync(Func<AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, object state,
            TaskCreationOptions creationOptions)
        {
            ExceptionProvider.ThrowNotSupportedInvocationException(nameof(Task.Factory.FromAsync));
            return Task.Factory.FromAsync(beginMethod, endMethod, state, creationOptions);
        }

        /// <summary>
        /// Creates a task that represents a pair of begin and end methods that conform
        /// to the Asynchronous Programming Model pattern.
        /// </summary>
        public Task<TResult> FromAsync<TResult>(Func<AsyncCallback, object, IAsyncResult> beginMethod,
            Func<IAsyncResult, TResult> endMethod, object state) =>
            this.FromAsync(beginMethod, endMethod, state, TaskCreationOptions.None);

        /// <summary>
        /// Creates a task that represents a pair of begin and end methods that conform
        /// to the Asynchronous Programming Model pattern.
        /// </summary>
        public Task<TResult> FromAsync<TResult>(Func<AsyncCallback, object, IAsyncResult> beginMethod,
            Func<IAsyncResult, TResult> endMethod, object state, TaskCreationOptions creationOptions)
        {
            ExceptionProvider.ThrowNotSupportedInvocationException(nameof(Task.Factory.FromAsync));
            return Task.Factory.FromAsync(beginMethod, endMethod, state, creationOptions);
        }

        /// <summary>
        /// Creates a task that represents a pair of begin and end methods that conform
        /// to the Asynchronous Programming Model pattern.
        /// </summary>
        public Task FromAsync<TArg1>(Func<TArg1, AsyncCallback, object, IAsyncResult> beginMethod,
            Action<IAsyncResult> endMethod, TArg1 arg1, object state) =>
            this.FromAsync(beginMethod, endMethod, arg1, state, TaskCreationOptions.None);

        /// <summary>
        /// Creates a task that represents a pair of begin and end methods that conform
        /// to the Asynchronous Programming Model pattern.
        /// </summary>
        public Task FromAsync<TArg1>(Func<TArg1, AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod,
            TArg1 arg1, object state, TaskCreationOptions creationOptions)
        {
            ExceptionProvider.ThrowNotSupportedInvocationException(nameof(Task.Factory.FromAsync));
            return Task.Factory.FromAsync(beginMethod, endMethod, arg1, state, creationOptions);
        }

        /// <summary>
        /// Creates a task that represents a pair of begin and end methods that conform
        /// to the Asynchronous Programming Model pattern.
        /// </summary>
        public Task FromAsync<TArg1, TArg2>(Func<TArg1, TArg2, AsyncCallback, object, IAsyncResult> beginMethod,
            Action<IAsyncResult> endMethod, TArg1 arg1, TArg2 arg2, object state) =>
            this.FromAsync(beginMethod, endMethod, arg1, arg2, state, TaskCreationOptions.None);

        /// <summary>
        /// Creates a task that represents a pair of begin and end methods that conform
        /// to the Asynchronous Programming Model pattern.
        /// </summary>
        public Task FromAsync<TArg1, TArg2>(Func<TArg1, TArg2, AsyncCallback, object, IAsyncResult> beginMethod,
            Action<IAsyncResult> endMethod, TArg1 arg1, TArg2 arg2, object state, TaskCreationOptions creationOptions)
        {
            ExceptionProvider.ThrowNotSupportedInvocationException(nameof(Task.Factory.FromAsync));
            return Task.Factory.FromAsync(beginMethod, endMethod, arg1, arg2, state, creationOptions);
        }

        /// <summary>
        /// Creates a task that represents a pair of begin and end methods that conform
        /// to the Asynchronous Programming Model pattern.
        /// </summary>
        public Task FromAsync<TArg1, TArg2, TArg3>(Func<TArg1, TArg2, TArg3, AsyncCallback, object, IAsyncResult> beginMethod,
            Action<IAsyncResult> endMethod, TArg1 arg1, TArg2 arg2, TArg3 arg3, object state) =>
            this.FromAsync(beginMethod, endMethod, arg1, arg2, arg3, state, TaskCreationOptions.None);

        /// <summary>
        /// Creates a task that represents a pair of begin and end methods that conform
        /// to the Asynchronous Programming Model pattern.
        /// </summary>
        public Task FromAsync<TArg1, TArg2, TArg3>(Func<TArg1, TArg2, TArg3, AsyncCallback, object, IAsyncResult> beginMethod,
            Action<IAsyncResult> endMethod, TArg1 arg1, TArg2 arg2, TArg3 arg3, object state, TaskCreationOptions creationOptions)
        {
            ExceptionProvider.ThrowNotSupportedInvocationException(nameof(Task.Factory.FromAsync));
            return Task.Factory.FromAsync(beginMethod, endMethod, arg1, arg2, arg3, state, creationOptions);
        }

        /// <summary>
        /// Creates a task that represents a pair of begin and end methods that conform
        /// to the Asynchronous Programming Model pattern.
        /// </summary>
        public Task<TResult> FromAsync<TArg1, TResult>(Func<TArg1, AsyncCallback, object, IAsyncResult> beginMethod,
            Func<IAsyncResult, TResult> endMethod, TArg1 arg1, object state) =>
            this.FromAsync(beginMethod, endMethod, arg1, state, TaskCreationOptions.None);

        /// <summary>
        /// Creates a task that represents a pair of begin and end methods that conform
        /// to the Asynchronous Programming Model pattern.
        /// </summary>
        public Task<TResult> FromAsync<TArg1, TResult>(Func<TArg1, AsyncCallback, object, IAsyncResult> beginMethod,
            Func<IAsyncResult, TResult> endMethod, TArg1 arg1, object state, TaskCreationOptions creationOptions)
        {
            ExceptionProvider.ThrowNotSupportedInvocationException(nameof(Task.Factory.FromAsync));
            return Task.Factory.FromAsync(beginMethod, endMethod, arg1, state, creationOptions);
        }

        /// <summary>
        /// Creates a task that represents a pair of begin and end methods that conform
        /// to the Asynchronous Programming Model pattern.
        /// </summary>
        public Task<TResult> FromAsync<TArg1, TArg2, TResult>(Func<TArg1, TArg2, AsyncCallback, object, IAsyncResult> beginMethod,
            Func<IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, object state) =>
            this.FromAsync(beginMethod, endMethod, arg1, arg2, state, TaskCreationOptions.None);

        /// <summary>
        /// Creates a task that represents a pair of begin and end methods that conform
        /// to the Asynchronous Programming Model pattern.
        /// </summary>
        public Task<TResult> FromAsync<TArg1, TArg2, TResult>(Func<TArg1, TArg2, AsyncCallback, object, IAsyncResult> beginMethod,
            Func<IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, object state, TaskCreationOptions creationOptions)
        {
            ExceptionProvider.ThrowNotSupportedInvocationException(nameof(Task.Factory.FromAsync));
            return Task.Factory.FromAsync(beginMethod, endMethod, arg1, arg2, state, creationOptions);
        }

        /// <summary>
        /// Creates a task that represents a pair of begin and end methods that conform
        /// to the Asynchronous Programming Model pattern.
        /// </summary>
        public Task<TResult> FromAsync<TArg1, TArg2, TArg3, TResult>(Func<TArg1, TArg2, TArg3, AsyncCallback, object, IAsyncResult> beginMethod,
            Func<IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, TArg3 arg3, object state) =>
            this.FromAsync(beginMethod, endMethod, arg1, arg2, arg3, state, TaskCreationOptions.None);

        /// <summary>
        /// Creates a task that represents a pair of begin and end methods that conform
        /// to the Asynchronous Programming Model pattern.
        /// </summary>
        public Task<TResult> FromAsync<TArg1, TArg2, TArg3, TResult>(Func<TArg1, TArg2, TArg3, AsyncCallback, object, IAsyncResult> beginMethod,
            Func<IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, TArg3 arg3, object state, TaskCreationOptions creationOptions)
        {
            ExceptionProvider.ThrowNotSupportedInvocationException(nameof(Task.Factory.FromAsync));
            return Task.Factory.FromAsync(beginMethod, endMethod, arg1, arg2, arg3, state, creationOptions);
        }

        /// <summary>
        /// Creates a task that executes an end method action when a specified <see cref="IAsyncResult"/> completes.
        /// </summary>
        public Task FromAsync(IAsyncResult asyncResult, Action<IAsyncResult> endMethod) =>
            this.FromAsync(asyncResult, endMethod, TaskCreationOptions.None, TaskScheduler.Default);

        /// <summary>
        /// Creates a task that executes an end method action when a specified <see cref="IAsyncResult"/> completes.
        /// </summary>
        public Task FromAsync(IAsyncResult asyncResult, Action<IAsyncResult> endMethod, TaskCreationOptions creationOptions) =>
            this.FromAsync(asyncResult, endMethod, creationOptions, TaskScheduler.Default);

        /// <summary>
        /// Creates a task that executes an end method action when a specified <see cref="IAsyncResult"/> completes.
        /// </summary>
        public Task FromAsync(IAsyncResult asyncResult, Action<IAsyncResult> endMethod, TaskCreationOptions creationOptions,
            TaskScheduler scheduler)
        {
            ExceptionProvider.ThrowNotSupportedInvocationException(nameof(Task.Factory.FromAsync));
            return Task.Factory.FromAsync(asyncResult, endMethod, creationOptions, scheduler);
        }

        /// <summary>
        /// Creates a task that executes an end method action when a specified <see cref="IAsyncResult"/> completes.
        /// </summary>
        public Task<TResult> FromAsync<TResult>(IAsyncResult asyncResult, Func<IAsyncResult, TResult> endMethod) =>
            this.FromAsync(asyncResult, endMethod, TaskCreationOptions.None, TaskScheduler.Default);

        /// <summary>
        /// Creates a task that executes an end method action when a specified <see cref="IAsyncResult"/> completes.
        /// </summary>
        public Task<TResult> FromAsync<TResult>(IAsyncResult asyncResult, Func<IAsyncResult, TResult> endMethod,
            TaskCreationOptions creationOptions) =>
            this.FromAsync(asyncResult, endMethod, creationOptions, TaskScheduler.Default);

        /// <summary>
        /// Creates a task that executes an end method action when a specified <see cref="IAsyncResult"/> completes.
        /// </summary>
        public Task<TResult> FromAsync<TResult>(IAsyncResult asyncResult, Func<IAsyncResult, TResult> endMethod,
            TaskCreationOptions creationOptions, TaskScheduler scheduler)
        {
            ExceptionProvider.ThrowNotSupportedInvocationException(nameof(Task.Factory.FromAsync));
            return Task.Factory.FromAsync(asyncResult, endMethod, creationOptions, scheduler);
        }
    }

    /// <summary>
    /// Provides support for creating and scheduling controlled <see cref="Task{TResult}"/> objects.
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
        public TaskContinuationOptions ContinuationOptions => Task<TResult>.Factory.ContinuationOptions;

        /// <summary>
        /// The default task cancellation token for this task factory.
        /// </summary>
        public CancellationToken CancellationToken => Task<TResult>.Factory.CancellationToken;

        /// <summary>
        /// The default task creation options for this task factory.
        /// </summary>
        public TaskCreationOptions CreationOptions => Task<TResult>.Factory.CreationOptions;

        /// <summary>
        /// The default task scheduler for this task factory.
        /// </summary>
        public TaskScheduler Scheduler => Task<TResult>.Factory.Scheduler;

        /// <summary>
        /// Creates and starts a <see cref="Task"/>.
        /// </summary>
        public Task<TResult> StartNew(Func<TResult> function) =>
            ControlledTask.Factory.StartNew(function);

        /// <summary>
        /// Creates and starts a <see cref="Task"/>.
        /// </summary>
        public Task<TResult> StartNew(Func<TResult> function, CancellationToken cancellationToken) =>
            ControlledTask.Factory.StartNew(function, cancellationToken);

        /// <summary>
        /// Creates and starts a <see cref="Task"/>.
        /// </summary>
        public Task<TResult> StartNew(Func<TResult> function, TaskCreationOptions creationOptions) =>
            ControlledTask.Factory.StartNew(function, creationOptions);

        /// <summary>
        /// Creates and starts a <see cref="Task"/>.
        /// </summary>
        public Task<TResult> StartNew(Func<TResult> function, CancellationToken cancellationToken, TaskCreationOptions creationOptions,
            TaskScheduler scheduler) =>
            ControlledTask.Factory.StartNew(function, cancellationToken, creationOptions, scheduler);

        /// <summary>
        /// Creates and starts a <see cref="Task"/>.
        /// </summary>
        public Task<TResult> StartNew(Func<object, TResult> function, object state) =>
            ControlledTask.Factory.StartNew(function, state);

        /// <summary>
        /// Creates and starts a <see cref="Task"/>.
        /// </summary>
        public Task<TResult> StartNew(Func<object, TResult> function, object state, CancellationToken cancellationToken) =>
            ControlledTask.Factory.StartNew(function, state, cancellationToken);

        /// <summary>
        /// Creates and starts a <see cref="Task"/>.
        /// </summary>
        public Task<TResult> StartNew(Func<object, TResult> function, object state, TaskCreationOptions creationOptions) =>
            ControlledTask.Factory.StartNew(function, state, creationOptions);

        /// <summary>
        /// Creates and starts a <see cref="Task"/>.
        /// </summary>
        public Task<TResult> StartNew(Func<object, TResult> function, object state, CancellationToken cancellationToken,
            TaskCreationOptions creationOptions, TaskScheduler scheduler) =>
            ControlledTask.Factory.StartNew(function, state, cancellationToken, creationOptions, scheduler);

        /// <summary>
        /// Creates a continuation task that starts when a set of specified tasks has completed.
        /// </summary>
        public Task<TResult> ContinueWhenAll(Task[] tasks, Func<Task[], TResult> continuationFunction) =>
            ControlledTask.Factory.ContinueWhenAll(tasks, continuationFunction);

        /// <summary>
        /// Creates a continuation task that starts when a set of specified tasks has completed.
        /// </summary>
        public Task<TResult> ContinueWhenAll(Task[] tasks, Func<Task[], TResult> continuationFunction,
            CancellationToken cancellationToken) =>
            ControlledTask.Factory.ContinueWhenAll(tasks, continuationFunction, cancellationToken);

        /// <summary>
        /// Creates a continuation task that starts when a set of specified tasks has completed.
        /// </summary>
        public Task<TResult> ContinueWhenAll(Task[] tasks, Func<Task[], TResult> continuationFunction,
            TaskContinuationOptions continuationOptions) =>
            ControlledTask.Factory.ContinueWhenAll(tasks, continuationFunction, continuationOptions);

        /// <summary>
        /// Creates a continuation task that starts when a set of specified tasks has completed.
        /// </summary>
        public Task<TResult> ContinueWhenAll(Task[] tasks, Func<Task[], TResult> continuationFunction,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler) =>
            ControlledTask.Factory.ContinueWhenAll(tasks, continuationFunction, cancellationToken, continuationOptions, scheduler);

        /// <summary>
        /// Creates a continuation task that starts when a set of specified tasks has completed.
        /// </summary>
        public Task<TResult> ContinueWhenAll<TAntecedentResult>(Task<TAntecedentResult>[] tasks,
            Func<Task<TAntecedentResult>[], TResult> continuationFunction) =>
            ControlledTask.Factory.ContinueWhenAll(tasks, continuationFunction);

        /// <summary>
        /// Creates a continuation task that starts when a set of specified tasks has completed.
        /// </summary>
        public Task<TResult> ContinueWhenAll<TAntecedentResult>(Task<TAntecedentResult>[] tasks,
            Func<Task<TAntecedentResult>[], TResult> continuationFunction, CancellationToken cancellationToken) =>
            ControlledTask.Factory.ContinueWhenAll(tasks, continuationFunction, cancellationToken);

        /// <summary>
        /// Creates a continuation task that starts when a set of specified tasks has completed.
        /// </summary>
        public Task<TResult> ContinueWhenAll<TAntecedentResult>(Task<TAntecedentResult>[] tasks,
            Func<Task<TAntecedentResult>[], TResult> continuationFunction, TaskContinuationOptions continuationOptions) =>
            ControlledTask.Factory.ContinueWhenAll(tasks, continuationFunction, continuationOptions);

        /// <summary>
        /// Creates a continuation task that starts when a set of specified tasks has completed.
        /// </summary>
        public Task<TResult> ContinueWhenAll<TAntecedentResult>(Task<TAntecedentResult>[] tasks,
            Func<Task<TAntecedentResult>[], TResult> continuationFunction, CancellationToken cancellationToken,
            TaskContinuationOptions continuationOptions, TaskScheduler scheduler) =>
            ControlledTask.Factory.ContinueWhenAll(tasks, continuationFunction, cancellationToken, continuationOptions, scheduler);

        /// <summary>
        /// Creates a continuation task that will be started upon the completion of any task in the provided set.
        /// </summary>
        public Task<TResult> ContinueWhenAny(Task[] tasks, Func<Task, TResult> continuationFunction) =>
            ControlledTask.Factory.ContinueWhenAny(tasks, continuationFunction);

        /// <summary>
        /// Creates a continuation task that will be started upon the completion of any task in the provided set.
        /// </summary>
        public Task<TResult> ContinueWhenAny(Task[] tasks, Func<Task, TResult> continuationFunction,
            CancellationToken cancellationToken) =>
            ControlledTask.Factory.ContinueWhenAny(tasks, continuationFunction, cancellationToken);

        /// <summary>
        /// Creates a continuation task that will be started upon the completion of any task in the provided set.
        /// </summary>
        public Task<TResult> ContinueWhenAny(Task[] tasks, Func<Task, TResult> continuationFunction,
            TaskContinuationOptions continuationOptions) =>
            ControlledTask.Factory.ContinueWhenAny(tasks, continuationFunction, continuationOptions);

        /// <summary>
        /// Creates a continuation task that will be started upon the completion of any task in the provided set.
        /// </summary>
        public Task<TResult> ContinueWhenAny(Task[] tasks, Func<Task, TResult> continuationFunction,
            CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler) =>
            ControlledTask.Factory.ContinueWhenAny(tasks, continuationFunction, cancellationToken, continuationOptions, scheduler);

        /// <summary>
        /// Creates a continuation task that will be started upon the completion of any task in the provided set.
        /// </summary>
        public Task<TResult> ContinueWhenAny<TAntecedentResult>(Task<TAntecedentResult>[] tasks,
            Func<Task<TAntecedentResult>, TResult> continuationFunction) =>
            ControlledTask.Factory.ContinueWhenAny(tasks, continuationFunction);

        /// <summary>
        /// Creates a continuation task that will be started upon the completion of any task in the provided set.
        /// </summary>
        public Task<TResult> ContinueWhenAny<TAntecedentResult>(Task<TAntecedentResult>[] tasks,
            Func<Task<TAntecedentResult>, TResult> continuationFunction, CancellationToken cancellationToken) =>
            ControlledTask.Factory.ContinueWhenAny(tasks, continuationFunction, cancellationToken);

        /// <summary>
        /// Creates a continuation task that will be started upon the completion of any task in the provided set.
        /// </summary>
        public Task<TResult> ContinueWhenAny<TAntecedentResult>(Task<TAntecedentResult>[] tasks,
            Func<Task<TAntecedentResult>, TResult> continuationFunction, TaskContinuationOptions continuationOptions) =>
            ControlledTask.Factory.ContinueWhenAny(tasks, continuationFunction, continuationOptions);

        /// <summary>
        /// Creates a continuation task that will be started upon the completion of any task in the provided set.
        /// </summary>
        public Task<TResult> ContinueWhenAny<TAntecedentResult>(Task<TAntecedentResult>[] tasks,
            Func<Task<TAntecedentResult>, TResult> continuationFunction, CancellationToken cancellationToken,
            TaskContinuationOptions continuationOptions, TaskScheduler scheduler) =>
            ControlledTask.Factory.ContinueWhenAny(tasks, continuationFunction, cancellationToken, continuationOptions, scheduler);

        /// <summary>
        /// Creates a task that represents a pair of begin and end methods that conform
        /// to the Asynchronous Programming Model pattern.
        /// </summary>
        public Task<TResult> FromAsync(Func<AsyncCallback, object, IAsyncResult> beginMethod,
            Func<IAsyncResult, TResult> endMethod, object state) =>
            ControlledTask.Factory.FromAsync(beginMethod, endMethod, state);

        /// <summary>
        /// Creates a task that represents a pair of begin and end methods that conform
        /// to the Asynchronous Programming Model pattern.
        /// </summary>
        public Task<TResult> FromAsync(Func<AsyncCallback, object, IAsyncResult> beginMethod,
            Func<IAsyncResult, TResult> endMethod, object state, TaskCreationOptions creationOptions) =>
            ControlledTask.Factory.FromAsync(beginMethod, endMethod, state, creationOptions);

        /// <summary>
        /// Creates a task that represents a pair of begin and end methods that conform
        /// to the Asynchronous Programming Model pattern.
        /// </summary>
        public Task<TResult> FromAsync<TArg1>(Func<TArg1, AsyncCallback, object, IAsyncResult> beginMethod,
            Func<IAsyncResult, TResult> endMethod, TArg1 arg1, object state) =>
            ControlledTask.Factory.FromAsync(beginMethod, endMethod, arg1, state);

        /// <summary>
        /// Creates a task that represents a pair of begin and end methods that conform
        /// to the Asynchronous Programming Model pattern.
        /// </summary>
        public Task<TResult> FromAsync<TArg1>(Func<TArg1, AsyncCallback, object, IAsyncResult> beginMethod,
            Func<IAsyncResult, TResult> endMethod, TArg1 arg1, object state, TaskCreationOptions creationOptions) =>
            ControlledTask.Factory.FromAsync(beginMethod, endMethod, arg1, state, creationOptions);

        /// <summary>
        /// Creates a task that represents a pair of begin and end methods that conform
        /// to the Asynchronous Programming Model pattern.
        /// </summary>
        public Task<TResult> FromAsync<TArg1, TArg2>(Func<TArg1, TArg2, AsyncCallback, object, IAsyncResult> beginMethod,
            Func<IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, object state) =>
            ControlledTask.Factory.FromAsync(beginMethod, endMethod, arg1, arg2, state);

        /// <summary>
        /// Creates a task that represents a pair of begin and end methods that conform
        /// to the Asynchronous Programming Model pattern.
        /// </summary>
        public Task<TResult> FromAsync<TArg1, TArg2>(Func<TArg1, TArg2, AsyncCallback, object, IAsyncResult> beginMethod,
            Func<IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, object state, TaskCreationOptions creationOptions) =>
            ControlledTask.Factory.FromAsync(beginMethod, endMethod, arg1, arg2, state, creationOptions);

        /// <summary>
        /// Creates a task that represents a pair of begin and end methods that conform
        /// to the Asynchronous Programming Model pattern.
        /// </summary>
        public Task<TResult> FromAsync<TArg1, TArg2, TArg3>(Func<TArg1, TArg2, TArg3, AsyncCallback, object, IAsyncResult> beginMethod,
            Func<IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, TArg3 arg3, object state) =>
            ControlledTask.Factory.FromAsync(beginMethod, endMethod, arg1, arg2, arg3, state);

        /// <summary>
        /// Creates a task that represents a pair of begin and end methods that conform
        /// to the Asynchronous Programming Model pattern.
        /// </summary>
        public Task<TResult> FromAsync<TArg1, TArg2, TArg3>(Func<TArg1, TArg2, TArg3, AsyncCallback, object, IAsyncResult> beginMethod,
            Func<IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, TArg3 arg3, object state, TaskCreationOptions creationOptions) =>
            ControlledTask.Factory.FromAsync(beginMethod, endMethod, arg1, arg2, arg3, state, creationOptions);

        /// <summary>
        /// Creates a task that executes an end method action when a specified <see cref="IAsyncResult"/> completes.
        /// </summary>
        public Task<TResult> FromAsync(IAsyncResult asyncResult, Func<IAsyncResult, TResult> endMethod) =>
            ControlledTask.Factory.FromAsync(asyncResult, endMethod);

        /// <summary>
        /// Creates a task that executes an end method action when a specified <see cref="IAsyncResult"/> completes.
        /// </summary>
        public Task<TResult> FromAsync(IAsyncResult asyncResult, Func<IAsyncResult, TResult> endMethod,
            TaskCreationOptions creationOptions) =>
            ControlledTask.Factory.FromAsync(asyncResult, endMethod, creationOptions);

        /// <summary>
        /// Creates a task that executes an end method action when a specified <see cref="IAsyncResult"/> completes.
        /// </summary>
        public Task<TResult> FromAsync(IAsyncResult asyncResult, Func<IAsyncResult, TResult> endMethod,
            TaskCreationOptions creationOptions, TaskScheduler scheduler) =>
            ControlledTask.Factory.FromAsync(asyncResult, endMethod, creationOptions, scheduler);
    }
#pragma warning restore CA1068 // CancellationToken parameters must come last
#pragma warning restore CA1822 // Mark members as static
}
