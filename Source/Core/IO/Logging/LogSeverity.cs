// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Coyote.IO
{
    /// <summary>
    /// Flag indicating the type of logging information being provided to the <see cref="ILogger"/>.
    /// </summary>
    public enum LogSeverity
    {
        /// <summary>
        /// General information about what is happening in the program.
        /// </summary>
        Informational,

        /// <summary>
        /// Warnings that something unusual is found and is being handled.
        /// </summary>
        Warning,

        /// <summary>
        /// Error is something unexpected that usually means program cannot proceed normally.
        /// </summary>
        Error,

        /// <summary>
        /// Output that is not an error or warning, but is important.
        /// </summary>
        Important
    }
}
