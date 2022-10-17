// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Coyote.Logging
{
    /// <summary>
    /// Logger that discards all log messages.
    /// </summary>
    internal class NullLogger : ILogger
    {
        /// <inheritdoc/>
        public void Write(string value)
        {
        }

        /// <inheritdoc/>
        public void Write(string format, object arg0)
        {
        }

        /// <inheritdoc/>
        public void Write(string format, object arg0, object arg1)
        {
        }

        /// <inheritdoc/>
        public void Write(string format, object arg0, object arg1, object arg2)
        {
        }

        /// <inheritdoc/>
        public void Write(string format, params object[] args)
        {
        }

        /// <inheritdoc/>
        public void Write(LogSeverity severity, string value)
        {
        }

        /// <inheritdoc/>
        public void Write(LogSeverity severity, string format, object arg0)
        {
        }

        /// <inheritdoc/>
        public void Write(LogSeverity severity, string format, object arg0, object arg1)
        {
        }

        /// <inheritdoc/>
        public void Write(LogSeverity severity, string format, object arg0, object arg1, object arg2)
        {
        }

        /// <inheritdoc/>
        public void Write(LogSeverity severity, string format, params object[] args)
        {
        }

        /// <inheritdoc/>
        public void WriteLine(string value)
        {
        }

        /// <inheritdoc/>
        public void WriteLine(string format, object arg0)
        {
        }

        /// <inheritdoc/>
        public void WriteLine(string format, object arg0, object arg1)
        {
        }

        /// <inheritdoc/>
        public void WriteLine(string format, object arg0, object arg1, object arg2)
        {
        }

        /// <inheritdoc/>
        public void WriteLine(string format, params object[] args)
        {
        }

        /// <inheritdoc/>
        public void WriteLine(LogSeverity severity, string value)
        {
        }

        /// <inheritdoc/>
        public void WriteLine(LogSeverity severity, string format, object arg0)
        {
        }

        /// <inheritdoc/>
        public void WriteLine(LogSeverity severity, string format, object arg0, object arg1)
        {
        }

        /// <inheritdoc/>
        public void WriteLine(LogSeverity severity, string format, object arg0, object arg1, object arg2)
        {
        }

        /// <inheritdoc/>
        public void WriteLine(LogSeverity severity, string format, params object[] args)
        {
        }

        /// <summary>
        /// Releases any resources held by the logger.
        /// </summary>
        public void Dispose()
        {
        }
    }
}
