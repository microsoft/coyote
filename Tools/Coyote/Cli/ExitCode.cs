// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Coyote.Cli
{
    /// <summary>
    /// The exit code returned by the tool.
    /// </summary>
    internal enum ExitCode
    {
        /// <summary>
        /// Indicates that the tool terminated successfully.
        /// </summary>
        /// <remarks>
        /// If the tool run in test mode, it also indicates that no bugs were found.
        /// </remarks>
        Success = 0,

        /// <summary>
        /// Indicates that the tool terminated with an error.
        /// </summary>
        Error = 1,

        /// <summary>
        /// Indicates that a bug was found during testing.
        /// </summary>
        BugFound = 2,

        /// <summary>
        /// Indicates that the tool terminated with an internal error.
        /// </summary>
        InternalError = 3
    }
}
