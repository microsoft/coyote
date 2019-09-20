// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Diagnostics;

namespace Microsoft.Coyote.Runtime
{
    /// <summary>
    /// The exception that is thrown in a Coyote machine upon cancellation
    /// of execution by the Coyote runtime.
    /// </summary>
    [DebuggerStepThrough]
    public sealed class ExecutionCanceledException : RuntimeException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExecutionCanceledException"/> class.
        /// </summary>
        internal ExecutionCanceledException()
        {
        }
    }
}
