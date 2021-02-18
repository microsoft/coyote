// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Threading.Tasks;

namespace Microsoft.Coyote
{
    /// <summary>
    /// Exception that is thrown upon cancellation of testing execution by the runtime.
    /// </summary>
    [DebuggerStepThrough]
    public sealed class ExecutionCanceledException : RuntimeException
    {
        internal uint Iteration;
        internal ulong Operationid;
        internal int? TaskId;
        internal string Log;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExecutionCanceledException"/> class.
        /// </summary>
        internal ExecutionCanceledException(uint iteration, ulong opId)
        {
            this.Iteration = iteration;
            this.Operationid = opId;
            this.TaskId = Task.CurrentId;
            this.Log = new StackTrace().ToString();
        }
    }
}
