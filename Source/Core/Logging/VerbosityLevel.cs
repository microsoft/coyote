// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Coyote.Logging
{
    /// <summary>
    /// The level of verbosity used during logging.
    /// </summary>
    public enum VerbosityLevel
    {
        /// <summary>
        /// Discards any log messages.
        /// </summary>
        None = 0,

        /// <summary>
        /// Logs error messages that are not recoverable.
        /// </summary>
        Error,

        /// <summary>
        /// Logs warnings highlighting an abnormal or unexpected event.
        /// </summary>
        Warning,

        /// <summary>
        /// Logs informational messages.
        /// </summary>
        Info,

        /// <summary>
        /// Logs messages that are useful for debugging.
        /// </summary>
        Debug,

        /// <summary>
        /// Logs the most detailed messages.
        /// </summary>
        Exhaustive
    }
}
