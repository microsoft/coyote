// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Logging;

namespace Microsoft.Coyote.SystematicTesting.Frameworks
{
    /// <summary>
    /// Logs all test output to the installed <see cref="ILogger"/>.
    /// </summary>
    internal interface ITestLog
    {
        /// <summary>
        /// Logs messages using the installed <see cref="ILogger"/>.
        /// </summary>
        LogWriter LogWriter { get; set; }
    }
}
