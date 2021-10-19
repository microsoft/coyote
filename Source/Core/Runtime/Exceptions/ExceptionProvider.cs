// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Threading;
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
        /// Checks if the exception object contains a <see cref="ThreadInterruptedException"/>
        /// and, if yes, it re-throws it so that the exception is not silently consumed.
        /// </summary>
        /// <param name="exception">The exception object.</param>
        public static void ThrowIfThreadInterruptedException(object exception)
        {
            // TODO: only re-throw an exception thrown by the runtime upon detach.
            if (exception is ThreadInterruptedException)
            {
                throw (Exception)exception;
            }

            if (exception is Exception ex)
            {
                // Look inside in case this is some sort of auto-wrapped AggregateException.
                if (ex.InnerException != null)
                {
                    ThrowIfThreadInterruptedException(ex.InnerException);
                }
            }
        }

        /// <summary>
        /// Throws a <see cref="NotSupportedException"/> for the specified uncontrolled method invocation.
        /// </summary>
        /// <param name="methodName">The name of the invoked method that is not controlled.</param>
        public static void ThrowUncontrolledInvocationException(string methodName) =>
            CoyoteRuntime.Current?.NotifyUncontrolledInvocation(methodName);

        /// <summary>
        /// Throws an exception if the task returned by the method with the specified name
        /// is not controlled during systematic testing.
        /// </summary>
        /// <param name="task">The task to check if it is controlled or not.</param>
        /// <param name="methodName">The name of the method returning the task.</param>
        public static void ThrowIfReturnedTaskNotControlled(Task task, string methodName) =>
            CoyoteRuntime.Current?.CheckIfReturnedTaskIsUncontrolled(task, methodName);
    }
}
