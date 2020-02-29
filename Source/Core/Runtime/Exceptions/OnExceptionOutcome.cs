// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Coyote.Runtime
{
    /// <summary>
    /// The outcome when a machine throws an exception.
    /// </summary>
    public enum OnExceptionOutcome
    {
        /// <summary>
        /// Throw the exception causing the runtime to fail.
        /// </summary>
        ThrowException = 0,

        /// <summary>
        /// The exception was handled and Machine should continue execution.
        /// </summary>
        HandledException = 1,

        /// <summary>
        /// Halt the machine (do not throw the exception).
        /// </summary>
        HaltMachine = 2
    }
}
