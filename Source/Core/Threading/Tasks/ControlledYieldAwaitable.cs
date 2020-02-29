// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using Microsoft.Coyote.Runtime;

namespace Microsoft.Coyote.Threading.Tasks
{
    /// <summary>
    /// Implements an awaitable that asynchronously yields back to the current context when awaited.
    /// This type is intended for compiler use only.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    public readonly struct ControlledYieldAwaitable
    {
        /// <summary>
        /// Gets an awaiter for this awaitable.
        /// </summary>
#pragma warning disable CA1822 // Mark members as static
        public ControlledYieldAwaiter GetAwaiter() => CoyoteRuntime.Provider.Current.CreateControlledYieldAwaiter();
#pragma warning restore CA1822 // Mark members as static

        /// <summary>
        /// Provides an awaiter that switches into a target environment.
        /// This type is intended for compiler use only.
        /// </summary>
        /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
        public readonly struct ControlledYieldAwaiter : ICriticalNotifyCompletion, INotifyCompletion
        {
            /// <summary>
            /// The runtime executing this awaiter.
            /// </summary>
            private readonly CoyoteRuntime Runtime;

            /// <summary>
            /// The internal yield awaiter.
            /// </summary>
            private readonly YieldAwaitable.YieldAwaiter Awaiter;

            /// <summary>
            /// Gets a value that indicates whether a yield is not required.
            /// </summary>
#pragma warning disable CA1822 // Mark members as static
            public bool IsCompleted => false;
#pragma warning restore CA1822 // Mark members as static

            /// <summary>
            /// Initializes a new instance of the <see cref="ControlledYieldAwaiter"/> struct.
            /// </summary>
            internal ControlledYieldAwaiter(CoyoteRuntime runtime, YieldAwaitable.YieldAwaiter awaiter)
            {
                this.Runtime = runtime;
                this.Awaiter = awaiter;
            }

            /// <summary>
            /// Ends the await operation.
            /// </summary>
            public void GetResult() => this.Runtime.OnGetYieldResult(this.Awaiter);

            /// <summary>
            /// Posts the continuation action back to the current context.
            /// </summary>
            public void OnCompleted(Action continuation) => this.Runtime.OnYieldCompleted(continuation, this.Awaiter);

            /// <summary>
            /// Posts the continuation action back to the current context.
            /// </summary>
            public void UnsafeOnCompleted(Action continuation) => this.Runtime.OnUnsafeYieldCompleted(continuation, this.Awaiter);
        }
    }
}
