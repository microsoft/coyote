// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Coyote.Runtime
{
    /// <summary>
    /// The context of a controlled asynchronous operation that is scheduled for execution.
    /// </summary>
    internal class AsyncOperationContext<TWork, TExecutor, TResult> : OperationContext<TWork, TResult>
    {
        /// <summary>
        /// Task representing an asynchronous work.
        /// </summary>
        internal TExecutor Executor { get; }

        /// <summary>
        /// Provides asynchronous access to the task executing the operation.
        /// </summary>
        internal TaskCompletionSource<TExecutor> ExecutorSource { get; }

        internal AsyncOperationContext(TaskOperation op, TWork work, TExecutor executor, Task predecessor,
            OperationExecutionOptions options, CancellationToken cancellationToken)
            : base(op, work, predecessor, options, cancellationToken)
        {
            this.Executor = executor;
            this.ExecutorSource = new TaskCompletionSource<TExecutor>(this.ResultSource);
        }
    }
}
