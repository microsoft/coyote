// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.Coyote.Runtime;

namespace Microsoft.Coyote.SystematicTesting.Interception
{
    /// <summary>
    /// Provides a set of static methods for working with specific kinds of <see cref="Exception"/> instances.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class ExceptionHelpers
    {
        /// <summary>
        /// Checks if the exception object contains a Coyote runtime exception and, if yes,
        /// it throws it so that the exception is not silently consumed.
        /// </summary>
        /// <param name="exception">The exception object.</param>
        public static void ThrowIfCoyoteRuntimeException(object exception)
        {
            if (exception is ExecutionCanceledException ece)
            {
                throw ece;
            }

            if (exception is Exception ex)
            {
                // Look inside in case this is some sort of auto-wrapped AggregateException.
                if (ex.InnerException != null)
                {
                    ThrowIfCoyoteRuntimeException(ex.InnerException);
                }
            }
        }

        /// <summary>
        /// Throws a <see cref="NotSupportedException"/> for the specified unsupported method.
        /// </summary>
        /// <param name="name">The name of the invoked method that is not supported.</param>
        public static void ThrowNotSupportedException(string name)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                throw new NotSupportedException($"Invoking '{name}' is not supported during systematic testing.");
            }
        }

        /// <summary>
        /// Throws an exception if the specified task is not controlled during systematic testing.
        /// </summary>
        /// <param name="task">The task to check if it is controlled or not.</param>
        public static void ThrowIfTaskNotControlled(Task task)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                ControlledRuntime.Current.TaskController.AssertIsAwaitedTaskControlled(task);
            }
        }
    }
}
