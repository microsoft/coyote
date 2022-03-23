// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using SystemCompiler = System.Runtime.CompilerServices;

namespace Microsoft.Coyote.Runtime.CompilerServices
{
    /// <summary>
    /// Provides an awaitable object that is the outcome of invoking <see cref="ValueTask.ConfigureAwait"/>.
    /// This type is intended for compiler use only.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public struct ConfiguredValueTaskAwaitable
    {
        /// <summary>
        /// The value task awaiter.
        /// </summary>
        private readonly ConfiguredValueTaskAwaiter Awaiter;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfiguredValueTaskAwaitable"/> struct.
        /// </summary>
        internal ConfiguredValueTaskAwaitable(ref ValueTask awaitedTask, bool continueOnCapturedContext)
        {
            this.Awaiter = new ConfiguredValueTaskAwaiter(ref awaitedTask, continueOnCapturedContext);
        }

        /// <summary>
        /// Returns an awaiter for this awaitable object.
        /// </summary>
        /// <returns>The awaiter.</returns>
        public ConfiguredValueTaskAwaiter GetAwaiter() => this.Awaiter;

        /// <summary>
        /// Provides an awaiter for an awaitable object. This type is intended for compiler use only.
        /// </summary>
        /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
        public struct ConfiguredValueTaskAwaiter : IControllableAwaiter, ICriticalNotifyCompletion, INotifyCompletion
        {
            /// <summary>
            /// The inner task being awaited.
            /// </summary>
            private readonly Task AwaitedTask;

            /// <summary>
            /// The value task awaiter.
            /// </summary>
            private readonly SystemCompiler.ConfiguredValueTaskAwaitable.ConfiguredValueTaskAwaiter Awaiter;

            /// <summary>
            /// The runtime controlling this awaiter.
            /// </summary>
            private readonly CoyoteRuntime Runtime;

            /// <summary>
            /// Gets a value that indicates whether the controlled value task has completed.
            /// </summary>
            public bool IsCompleted => this.AwaitedTask?.IsCompleted ?? this.Awaiter.IsCompleted;

            /// <inheritdoc/>
            bool IControllableAwaiter.IsControlled =>
                !this.Runtime?.IsTaskUncontrolled(this.AwaitedTask) ?? false;

            /// <summary>
            /// Initializes a new instance of the <see cref="ConfiguredValueTaskAwaiter"/> struct.
            /// </summary>
            internal ConfiguredValueTaskAwaiter(ref ValueTask awaitedTask, bool continueOnCapturedContext)
            {
                if (RuntimeProvider.TryGetFromSynchronizationContext(out CoyoteRuntime runtime))
                {
                    // Force the continuation to run on the current context so that it can be controlled.
                    continueOnCapturedContext = true;
                }

                this.AwaitedTask = ValueTaskAwaiter.TryGetTask(ref awaitedTask, out Task innerTask) ?
                    innerTask : null;
                this.Awaiter = awaitedTask.ConfigureAwait(continueOnCapturedContext).GetAwaiter();
                this.Runtime = runtime;
            }

            /// <summary>
            /// Ends the await on the completed value task.
            /// </summary>
            public void GetResult()
            {
                if (this.AwaitedTask != null)
                {
                    this.Runtime?.WaitUntilTaskCompletes(this.AwaitedTask);
                }

                this.Awaiter.GetResult();
            }

            /// <summary>
            /// Schedules the continuation action for the value task associated with this awaiter.
            /// </summary>
            /// <param name="continuation">The action to invoke when the await operation completes.</param>
            public void OnCompleted(Action continuation) => this.Awaiter.OnCompleted(continuation);

            /// <summary>
            /// Schedules the continuation action for the value task associated with this awaiter.
            /// </summary>
            /// <param name="continuation">The action to invoke when the await operation completes.</param>
            public void UnsafeOnCompleted(Action continuation) => this.Awaiter.UnsafeOnCompleted(continuation);
        }
    }

    /// <summary>
    /// Provides an awaitable object that enables configured awaits on a <see cref="ValueTask{TResult}"/>.
    /// This type is intended for compiler use only.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public struct ConfiguredValueTaskAwaitable<TResult>
    {
        /// <summary>
        /// The value task awaiter.
        /// </summary>
        private readonly ConfiguredValueTaskAwaiter Awaiter;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfiguredValueTaskAwaitable{TResult}"/> struct.
        /// </summary>
        internal ConfiguredValueTaskAwaitable(ref ValueTask<TResult> awaitedTask, bool continueOnCapturedContext)
        {
            this.Awaiter = new ConfiguredValueTaskAwaiter(ref awaitedTask, continueOnCapturedContext);
        }

        /// <summary>
        /// Returns an awaiter for this awaitable object.
        /// </summary>
        /// <returns>The awaiter.</returns>
        public ConfiguredValueTaskAwaiter GetAwaiter() => this.Awaiter;

        /// <summary>
        /// Provides an awaiter for an awaitable object. This type is intended for compiler use only.
        /// </summary>
        /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
        public struct ConfiguredValueTaskAwaiter : IControllableAwaiter, ICriticalNotifyCompletion, INotifyCompletion
        {
            /// <summary>
            /// The inner task being awaited.
            /// </summary>
            private readonly Task<TResult> AwaitedTask;

            /// <summary>
            /// The value task awaiter.
            /// </summary>
            private readonly SystemCompiler.ConfiguredValueTaskAwaitable<TResult>.ConfiguredValueTaskAwaiter Awaiter;

            /// <summary>
            /// The runtime controlling this awaiter.
            /// </summary>
            private readonly CoyoteRuntime Runtime;

            /// <summary>
            /// Gets a value that indicates whether the controlled value task has completed.
            /// </summary>
            public bool IsCompleted => this.AwaitedTask?.IsCompleted ?? this.Awaiter.IsCompleted;

            /// <inheritdoc/>
            bool IControllableAwaiter.IsControlled =>
                !this.Runtime?.IsTaskUncontrolled(this.AwaitedTask) ?? false;

            /// <summary>
            /// Initializes a new instance of the <see cref="ConfiguredValueTaskAwaiter"/> struct.
            /// </summary>
            internal ConfiguredValueTaskAwaiter(ref ValueTask<TResult> awaitedTask, bool continueOnCapturedContext)
            {
                if (RuntimeProvider.TryGetFromSynchronizationContext(out CoyoteRuntime runtime))
                {
                    // Force the continuation to run on the current context so that it can be controlled.
                    continueOnCapturedContext = true;
                }

                this.AwaitedTask = ValueTaskAwaiter.TryGetTask<TResult>(ref awaitedTask, out Task<TResult> innerTask) ?
                    innerTask : null;
                this.Awaiter = awaitedTask.ConfigureAwait(continueOnCapturedContext).GetAwaiter();
                this.Runtime = runtime;
            }

            /// <summary>
            /// Ends the await on the completed value task.
            /// </summary>
            public TResult GetResult()
            {
                if (this.AwaitedTask != null)
                {
                    this.Runtime?.WaitUntilTaskCompletes(this.AwaitedTask);
                }

                return this.Awaiter.GetResult();
            }

            /// <summary>
            /// Schedules the continuation action for the value task associated with this awaiter.
            /// </summary>
            /// <param name="continuation">The action to invoke when the await operation completes.</param>
            public void OnCompleted(Action continuation) => this.Awaiter.OnCompleted(continuation);

            /// <summary>
            /// Schedules the continuation action for the value task associated with this awaiter.
            /// </summary>
            /// <param name="continuation">The action to invoke when the await operation completes.</param>
            public void UnsafeOnCompleted(Action continuation) => this.Awaiter.UnsafeOnCompleted(continuation);
        }
    }
}
