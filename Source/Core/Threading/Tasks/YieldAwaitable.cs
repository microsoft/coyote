// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Runtime.CompilerServices;
using Microsoft.Coyote.Runtime;

using DefaultYieldAwaiter = System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter;

namespace Microsoft.Coyote.Threading.Tasks
{
    /// <summary>
    /// Implements an awaitable that asynchronously yields back to the current context when awaited.
    /// This type is intended for compiler use only.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    public readonly struct YieldAwaitable
    {
        /// <summary>
        /// Gets an awaiter for this awaitable.
        /// </summary>
#pragma warning disable CA1822 // Mark members as static
        public YieldAwaiter GetAwaiter() => new YieldAwaiter(default);
#pragma warning restore CA1822 // Mark members as static

        /// <summary>
        /// Provides an awaiter that switches into a target environment.
        /// This type is intended for compiler use only.
        /// </summary>
        /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
        public readonly struct YieldAwaiter : ICriticalNotifyCompletion, INotifyCompletion
        {
            /// <summary>
            /// The internal yield awaiter.
            /// </summary>
            private readonly DefaultYieldAwaiter Awaiter;

            /// <summary>
            /// Gets a value that indicates whether a yield is not required.
            /// </summary>
#pragma warning disable CA1822 // Mark members as static
            public bool IsCompleted => false;
#pragma warning restore CA1822 // Mark members as static

            /// <summary>
            /// Initializes a new instance of the <see cref="YieldAwaiter"/> struct.
            /// </summary>
            internal YieldAwaiter(DefaultYieldAwaiter awaiter)
            {
                this.Awaiter = awaiter;
            }

            /// <summary>
            /// Ends the await operation.
            /// </summary>
            public void GetResult() => MachineRuntime.Current.OnGetYieldResult(this.Awaiter);

            /// <summary>
            /// Posts the continuation action back to the current context.
            /// </summary>
            public void OnCompleted(Action continuation) => MachineRuntime.Current.OnYieldCompleted(continuation, this.Awaiter);

            /// <summary>
            /// Posts the continuation action back to the current context.
            /// </summary>
            public void UnsafeOnCompleted(Action continuation) => MachineRuntime.Current.OnUnsafeYieldCompleted(continuation, this.Awaiter);
        }
    }
}
