// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using Microsoft.Coyote.IO;

namespace Microsoft.Coyote.SystematicTesting.Interception
{
    /// <summary>
    /// Provides a set of static methods for working with specific kinds of <see cref="Exception"/> instances.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class ExceptionHandlers
    {
        /// <summary>
        /// Checks if the exception object contains a ExecutionCanceledException, and if so
        /// it throws it so that the exception is not silently handled by customer code.
        /// The Coyote runtime needs to get this exception as a way to cancel the current
        /// test iteration.
        /// </summary>
        /// <param name="e">The exception object.</param>
        public static void ThrowIfCoyoteRuntimeException(object e)
        {
            if (e is ExecutionCanceledException exe)
            {
                throw exe;
            }

            if (e is Exception ex)
            {
                // Look inside in case this is some sort of auto-wrapped AggregateException.
                if (ex.InnerException != null)
                {
                    ThrowIfCoyoteRuntimeException(ex.InnerException);
                }
            }
        }
    }
}
