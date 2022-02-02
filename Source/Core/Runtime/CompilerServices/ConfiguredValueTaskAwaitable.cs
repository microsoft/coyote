// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
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
        internal ConfiguredValueTaskAwaitable(in ValueTask awaitedTask, bool continueOnCapturedContext)
        {
            this.Awaiter = new ConfiguredValueTaskAwaiter(in awaitedTask, continueOnCapturedContext);
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
        public struct ConfiguredValueTaskAwaiter : ICriticalNotifyCompletion, INotifyCompletion
        {
            /// <summary>
            /// The value task being awaited.
            /// </summary>
            private readonly ValueTask AwaitedTask;

            /// <summary>
            /// The value task awaiter.
            /// </summary>
            private readonly SystemCompiler.ConfiguredValueTaskAwaitable.ConfiguredValueTaskAwaiter Awaiter;

            /// <summary>
            /// Gets a value that indicates whether the controlled value task has completed.
            /// </summary>
            public bool IsCompleted => this.AwaitedTask.IsCompleted;

            /// <summary>
            /// Initializes a new instance of the <see cref="ConfiguredValueTaskAwaiter"/> struct.
            /// </summary>
            internal ConfiguredValueTaskAwaiter(in ValueTask awaitedTask, bool continueOnCapturedContext)
            {
                if (SynchronizationContext.Current is ControlledSynchronizationContext)
                {
                    // Force the continuation to run on the current context so that it can be controlled.
                    continueOnCapturedContext = true;
                }

                this.AwaitedTask = awaitedTask;
                this.Awaiter = awaitedTask.ConfigureAwait(continueOnCapturedContext).GetAwaiter();
            }

            /// <summary>
            /// Ends the await on the completed value task.
            /// </summary>
            public void GetResult()
            {
                if (SynchronizationContext.Current is ControlledSynchronizationContext context)
                {
                    context.Runtime?.WaitUntilTaskCompletes(this.AwaitedTask.AsTask());
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
        internal ConfiguredValueTaskAwaitable(in ValueTask<TResult> awaitedTask, bool continueOnCapturedContext)
        {
            this.Awaiter = new ConfiguredValueTaskAwaiter(in awaitedTask, continueOnCapturedContext);
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
        public struct ConfiguredValueTaskAwaiter : ICriticalNotifyCompletion, INotifyCompletion
        {
            /// <summary>
            /// The value task being awaited.
            /// </summary>
            private readonly ValueTask<TResult> AwaitedTask;

            /// <summary>
            /// The value task awaiter.
            /// </summary>
            private readonly SystemCompiler.ConfiguredValueTaskAwaitable<TResult>.ConfiguredValueTaskAwaiter Awaiter;

            /// <summary>
            /// Gets a value that indicates whether the controlled value task has completed.
            /// </summary>
            public bool IsCompleted => this.AwaitedTask.IsCompleted;

            /// <summary>
            /// Initializes a new instance of the <see cref="ConfiguredValueTaskAwaiter"/> struct.
            /// </summary>
            internal ConfiguredValueTaskAwaiter(in ValueTask<TResult> awaitedTask, bool continueOnCapturedContext)
            {
                if (SynchronizationContext.Current is ControlledSynchronizationContext)
                {
                    // Force the continuation to run on the current context so that it can be controlled.
                    continueOnCapturedContext = true;
                }

                this.AwaitedTask = awaitedTask;
                this.Awaiter = awaitedTask.ConfigureAwait(continueOnCapturedContext).GetAwaiter();
            }

            /// <summary>
            /// Ends the await on the completed value task.
            /// </summary>
            public TResult GetResult()
            {
                if (SynchronizationContext.Current is ControlledSynchronizationContext context)
                {
                    context.Runtime?.WaitUntilTaskCompletes(this.AwaitedTask.AsTask());
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
