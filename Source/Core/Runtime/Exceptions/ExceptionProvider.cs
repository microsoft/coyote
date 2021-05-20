// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Microsoft.Coyote.Runtime
{
    /// <summary>
    /// Provides a set of static methods for working with specific kinds of <see cref="Exception"/> instances.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class ExceptionProvider
    {
        /// <summary>
        /// Checks if the exception object contains an <see cref="ExecutionCanceledException"/>
        /// and, if yes, it re-throws it so that the exception is not silently consumed.
        /// </summary>
        /// <param name="exception">The exception object.</param>
        public static void ThrowIfExecutionCanceledException(object exception)
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
                    ThrowIfExecutionCanceledException(ex.InnerException);
                }
            }
        }

        /// <summary>
        /// Throws a <see cref="NotSupportedException"/> for the specified unsupported method.
        /// </summary>
        /// <param name="methodName">The name of the invoked method that is not supported.</param>
        public static void ThrowNotSupportedInvocationException(string methodName)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                throw new NotSupportedException($"Invoking '{methodName}' is not intercepted and controlled during " +
                    "testing, so it can interfere with the ability to reproduce bug traces. As a workaround, you can " +
                    "use the '--no-repro' command line option to ignore this error by disabling bug trace repro. " +
                    "Learn more at http://aka.ms/coyote-no-repro.");
            }
        }

        /// <summary>
        /// Throws an exception if the task returned by the method with the specified name
        /// is not controlled during systematic testing.
        /// </summary>
        /// <param name="task">The task to check if it is controlled or not.</param>
        /// <param name="methodName">The name of the method returning the task.</param>
        public static void ThrowIfReturnedTaskNotControlled(Task task, string methodName)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                CoyoteRuntime.Current.AssertIsReturnedTaskControlled(task, methodName);
            }
        }
    }
}
