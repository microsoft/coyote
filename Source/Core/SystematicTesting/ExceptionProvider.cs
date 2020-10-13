﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.Coyote.Runtime;

namespace Microsoft.Coyote.SystematicTesting
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
        /// <param name="name">The name of the invoked method that is not supported.</param>
        public static void ThrowNotSupportedInvocationException(string name)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                throw new NotSupportedException($"Invoking '{name}' is not supported during systematic testing.");
            }
        }
    }
}
