// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Coyote.SystematicTesting
{
    /// <summary>
    /// The context of a controlled operation that is scheduled for execution.
    /// </summary>
    internal class OperationContext<TWork, TResult>
    {
        /// <summary>
        /// The operation that is executing the work.
        /// </summary>
        internal TaskOperation Operation { get; }

        /// <summary>
        /// The work to be executed.
        /// </summary>
        internal TWork Work { get; }

        /// <summary>
        /// Optional predecessor that must complete before the operation starts.
        /// </summary>
        internal Task Predecessor { get; }

        /// <summary>
        /// Provides asynchronous access to the result of the operation.
        /// </summary>
        internal TaskCompletionSource<TResult> ResultSource { get; }

        /// <summary>
        /// Options for executing the operation.
        /// </summary>
        internal OperationExecutionOptions Options;

        /// <summary>
        /// Cancellation token that can be used to cancel the operation.
        /// </summary>
        internal CancellationToken CancellationToken { get; }

        internal OperationContext(TaskOperation op, TWork work, Task predecessor, OperationExecutionOptions options,
            CancellationToken cancellationToken)
        {
            this.Operation = op;
            this.Work = work ?? throw new ArgumentNullException(nameof(work));
            this.Predecessor = predecessor;
            this.ResultSource = new TaskCompletionSource<TResult>();
            this.Options = options;
            this.CancellationToken = cancellationToken;
        }
    }

    /// <summary>
    /// Provides static helpers for managing <see cref="OperationContext{TWork, TResult}"/> objects.
    /// </summary>
    internal static class OperationContext
    {
        /// <summary>
        /// Helper for creating an <see cref="OperationExecutionOptions"/> enum.
        /// </summary>
        internal static OperationExecutionOptions CreateOperationExecutionOptions(bool failOnException = false,
            bool yieldAtStart = false)
        {
            OperationExecutionOptions options = OperationExecutionOptions.None;
            if (failOnException)
            {
                options |= OperationExecutionOptions.FailOnException;
            }

            if (yieldAtStart)
            {
                options |= OperationExecutionOptions.YieldAtStart;
            }

            return options;
        }

        /// <summary>
        /// Helper for checking if an object is an instance of the <see cref="OperationContext{TWork, TResult}"/> type.
        /// </summary>
        internal static bool IsInstance(object instance)
        {
            Type type = instance?.GetType();
            while (type != null)
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(OperationContext<,>))
                {
                    return true;
                }

                type = type.BaseType;
            }

            return false;
        }
    }
}
