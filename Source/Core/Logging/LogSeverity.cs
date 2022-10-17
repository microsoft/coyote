// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Coyote.Logging
{
    /// <summary>
    /// The severity of the log message being provided to the <see cref="ILogger"/>.
    /// </summary>
    public enum LogSeverity
    {
        /// <summary>
        /// Log that contains information useful for debugging purposes.
        /// </summary>
        Debug = 0,

        /// <summary>
        /// Log that contains general information.
        /// </summary>
        Info,

        /// <summary>
        /// Log that contains information about a warning.
        /// </summary>
        Warning,

        /// <summary>
        /// Log that contains information about an error.
        /// </summary>
        Error,

        /// <summary>
        /// Log that contains important information.
        /// </summary>
        Important
    }
}
