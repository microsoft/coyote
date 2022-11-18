// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using RuntimeCompiler = Microsoft.Coyote.Runtime.CompilerServices;

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
            if ((exception as Exception)?.GetBaseException() is ThreadInterruptedException ex)
            {
                ExceptionDispatchInfo.Capture(ex).Throw();
            }
        }

        /// <summary>
        /// Throws an exception for the specified uncontrolled method invocation.
        /// </summary>
        /// <param name="methodName">The name of the invoked method that is not controlled.</param>
        public static void ThrowUncontrolledInvocationException(string methodName) =>
            CoyoteRuntime.Current.NotifyUncontrolledInvocation(methodName);

        /// <summary>
        /// Throws an exception for the specified uncontrolled data non-deterministic method invocation.
        /// </summary>
        /// <param name="methodName">The name of the invoked method that is not controlled.</param>
        public static void ThrowUncontrolledDataInvocationException(string methodName) =>
            CoyoteRuntime.Current.NotifyUncontrolledDataNondeterministicInvocation(methodName);

        /// <summary>
        /// Throws an exception if the task returned by the method with the specified name
        /// is not controlled during systematic testing.
        /// </summary>
        /// <param name="task">The task to check if it is controlled or not.</param>
        /// <param name="methodName">The name of the method returning the task.</param>
        public static Task ThrowIfReturnedTaskNotControlled(Task task, string methodName)
        {
            CoyoteRuntime.Current.CheckIfReturnedTaskIsUncontrolled(task, methodName);
            return task;
        }

        /// <summary>
        /// Throws an exception if the task returned by the method with the specified name
        /// is not controlled during systematic testing.
        /// </summary>
        /// <param name="task">The task to check if it is controlled or not.</param>
        /// <param name="methodName">The name of the method returning the task.</param>
        public static Task<TResult> ThrowIfReturnedTaskNotControlled<TResult>(Task<TResult> task, string methodName)
        {
            CoyoteRuntime.Current.CheckIfReturnedTaskIsUncontrolled(task, methodName);
            return task;
        }

        /// <summary>
        /// Throws an exception if the value task returned by the method with the specified name
        /// is not controlled during systematic testing.
        /// </summary>
        /// <param name="task">The value task to check if it is controlled or not.</param>
        /// <param name="methodName">The name of the method returning the task.</param>
        public static ref ValueTask ThrowIfReturnedValueTaskNotControlled(ref ValueTask task, string methodName)
        {
            if (RuntimeCompiler.ValueTaskAwaiter.TryGetTask(ref task, out Task innerTask))
            {
                CoyoteRuntime.Current.CheckIfReturnedTaskIsUncontrolled(innerTask, methodName);
            }

            return ref task;
        }

        /// <summary>
        /// Throws an exception if the value task returned by the method with the specified name
        /// is not controlled during systematic testing.
        /// </summary>
        /// <param name="task">The value task to check if it is controlled or not.</param>
        /// <param name="methodName">The name of the method returning the task.</param>
        public static ref ValueTask<TResult> ThrowIfReturnedValueTaskNotControlled<TResult>(
            ref ValueTask<TResult> task, string methodName)
        {
            if (RuntimeCompiler.ValueTaskAwaiter.TryGetTask<TResult>(ref task, out Task<TResult> innerTask))
            {
                CoyoteRuntime.Current.CheckIfReturnedTaskIsUncontrolled(innerTask, methodName);
            }

            return ref task;
        }
    }
}
