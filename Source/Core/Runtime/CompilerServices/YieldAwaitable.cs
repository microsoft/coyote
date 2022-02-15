// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using SystemCompiler = System.Runtime.CompilerServices;
using SystemTask = System.Threading.Tasks.Task;

namespace Microsoft.Coyote.Runtime.CompilerServices
{
    /// <summary>
    /// Provides an awaitable object that is the outcome of invoking <see cref="SystemTask.Yield"/>.
    /// This type is intended for compiler use only.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public struct YieldAwaitable
    {
        /// <summary>
        /// The yield awaiter.
        /// </summary>
        private readonly YieldAwaiter Awaiter;

        /// <summary>
        /// Initializes a new instance of the <see cref="YieldAwaitable"/> struct.
        /// </summary>
        internal YieldAwaitable(SystemCompiler.YieldAwaitable.YieldAwaiter awaiter)
        {
            this.Awaiter = new YieldAwaiter(ref awaiter);
        }

        /// <summary>
        /// Returns an awaiter for this awaitable object.
        /// </summary>
        /// <returns>The awaiter.</returns>
        public YieldAwaiter GetAwaiter() => this.Awaiter;

        /// <summary>
        /// Provides an awaiter for an awaitable object. This type is intended for compiler use only.
        /// </summary>
        /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
        public struct YieldAwaiter : IControllableAwaiter, ICriticalNotifyCompletion, INotifyCompletion
        {
            /// <summary>
            /// The yield awaiter.
            /// </summary>
            private readonly SystemCompiler.YieldAwaitable.YieldAwaiter Awaiter;

            /// <summary>
            /// The runtime controlling this awaiter.
            /// </summary>
            private readonly CoyoteRuntime Runtime;

            /// <summary>
            /// This value is always false, as yielding is always required.
            /// </summary>
#pragma warning disable CA1822 // Mark members as static
            public bool IsCompleted => false;
#pragma warning restore CA1822 // Mark members as static

            /// <inheritdoc/>
            bool IControllableAwaiter.IsControlled => true;

            /// <summary>
            /// Initializes a new instance of the <see cref="YieldAwaiter"/> struct.
            /// </summary>
            internal YieldAwaiter(ref SystemCompiler.YieldAwaitable.YieldAwaiter awaiter)
            {
                RuntimeProvider.TryGetFromSynchronizationContext(out CoyoteRuntime runtime);
                this.Awaiter = awaiter;
                this.Runtime = runtime;
            }

            /// <summary>
            /// Ends the await on the completed task.
            /// </summary>
            public void GetResult() => this.Awaiter.GetResult();

            /// <summary>
            /// Schedules the continuation action for the task associated with this awaiter.
            /// </summary>
            /// <param name="continuation">The action to invoke when the await operation completes.</param>
            public void OnCompleted(Action continuation) => this.Awaiter.OnCompleted(continuation);

            /// <summary>
            /// Schedules the continuation action for the task associated with this awaiter.
            /// </summary>
            /// <param name="continuation">The action to invoke when the await operation completes.</param>
            public void UnsafeOnCompleted(Action continuation) => this.Awaiter.UnsafeOnCompleted(continuation);
        }
    }
}
