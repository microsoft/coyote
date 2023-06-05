// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Logging;
using Xunit.Abstractions;

namespace Microsoft.Coyote.SystematicTesting.Frameworks.XUnit
{
    /// <summary>
    /// Redirects all xUnit test output to the installed <see cref="ILogger"/>.
    /// </summary>
    internal sealed class TestOutput : ITestOutputHelper, ITestLog
    {
        /// <summary>
        /// Logs messages using the installed <see cref="ILogger"/>.
        /// </summary>
        public LogWriter LogWriter { get; set; }

        /// <inheritdoc/>
        public void WriteLine(string message)
        {
            this.LogWriter.WriteLine(message);
        }

        /// <inheritdoc/>
        public void WriteLine(string format, params object[] args)
        {
            this.LogWriter.WriteLine(format, args);
        }
    }
}
