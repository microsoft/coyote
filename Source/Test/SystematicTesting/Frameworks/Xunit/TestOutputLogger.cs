// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text;
using Microsoft.Coyote.Logging;
using Xunit.Abstractions;

namespace Microsoft.Coyote.SystematicTesting.Frameworks.XUnit
{
    /// <summary>
    /// Logger that writes to the xUnit test output.
    /// </summary>
    public sealed class TestOutputLogger : ILogger
    {
        /// <summary>
        /// Underlying test output.
        /// </summary>
        private readonly ITestOutputHelper TestOutput;

        /// <summary>
        /// Saves the log until the end of the test.
        /// </summary>
        private readonly StringBuilder Log;

        /// <summary>
        /// Synchronizes access to the string writer.
        /// </summary>
        private readonly object Lock;

        /// <summary>
        /// True if this logger is disposed, else false.
        /// </summary>
        private bool IsDisposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestOutputLogger"/> class.
        /// </summary>
        /// <param name="output">The test output helper.</param>
        public TestOutputLogger(ITestOutputHelper output)
        {
            this.TestOutput = output;
            this.Log = new StringBuilder();
            this.Lock = new object();
            this.IsDisposed = false;
        }

        /// <inheritdoc/>
        public void Write(string value) => this.Write(LogSeverity.Info, value);

        /// <inheritdoc/>
        public void Write(string format, object arg0) =>
            this.Write(LogSeverity.Info, format, arg0);

        /// <inheritdoc/>
        public void Write(string format, object arg0, object arg1) =>
            this.Write(LogSeverity.Info, format, arg0, arg1);

        /// <inheritdoc/>
        public void Write(string format, object arg0, object arg1, object arg2) =>
            this.Write(LogSeverity.Info, format, arg0, arg1, arg2);

        /// <inheritdoc/>
        public void Write(string format, params object[] args) =>
            this.Write(LogSeverity.Info, format, args);

        /// <inheritdoc/>
        public void Write(LogSeverity severity, string value)
        {
            lock (this.Lock)
            {
                if (!this.IsDisposed)
                {
                    this.Log.Append(value);
                }
            }
        }

        /// <inheritdoc/>
        public void Write(LogSeverity severity, string format, object arg0) =>
            this.Write(severity, string.Format(format, arg0));

        /// <inheritdoc/>
        public void Write(LogSeverity severity, string format, object arg0, object arg1) =>
            this.Write(severity, string.Format(format, arg0, arg1));

        /// <inheritdoc/>
        public void Write(LogSeverity severity, string format, object arg0, object arg1, object arg2) =>
            this.Write(severity, string.Format(format, arg0, arg1, arg2));

        /// <inheritdoc/>
        public void Write(LogSeverity severity, string format, params object[] args) =>
            this.Write(severity, string.Format(format, args));

        /// <inheritdoc/>
        public void WriteLine(string value) => this.WriteLine(LogSeverity.Info, value);

        /// <inheritdoc/>
        public void WriteLine(string format, object arg0) =>
            this.WriteLine(LogSeverity.Info, format, arg0);

        /// <inheritdoc/>
        public void WriteLine(string format, object arg0, object arg1) =>
            this.WriteLine(LogSeverity.Info, format, arg0, arg1);

        /// <inheritdoc/>
        public void WriteLine(string format, object arg0, object arg1, object arg2) =>
            this.WriteLine(LogSeverity.Info, format, arg0, arg1, arg2);

        /// <inheritdoc/>
        public void WriteLine(string format, params object[] args) =>
            this.WriteLine(LogSeverity.Info, format, args);

        /// <inheritdoc/>
        public void WriteLine(LogSeverity severity, string value)
        {
            lock (this.Lock)
            {
                if (!this.IsDisposed)
                {
                    this.Log.AppendLine(value);
                }
            }
        }

        /// <inheritdoc/>
        public void WriteLine(LogSeverity severity, string format, object arg0) =>
            this.WriteLine(severity, string.Format(format, arg0));

        /// <inheritdoc/>
        public void WriteLine(LogSeverity severity, string format, object arg0, object arg1) =>
            this.WriteLine(severity, string.Format(format, arg0, arg1));

        /// <inheritdoc/>
        public void WriteLine(LogSeverity severity, string format, object arg0, object arg1, object arg2) =>
            this.WriteLine(severity, string.Format(format, arg0, arg1, arg2));

        /// <inheritdoc/>
        public void WriteLine(LogSeverity severity, string format, params object[] args) =>
            this.WriteLine(severity, string.Format(format, args));

        /// <summary>
        /// Write all buffered log to the test output logger.
        /// </summary>
        private void FlushLog()
        {
            if (this.Log.Length > 0)
            {
                this.TestOutput.WriteLine(this.Log.ToString());
                this.Log.Clear();
            }
        }

        /// <summary>
        /// Releases any resources held by the logger.
        /// </summary>
        public void Dispose()
        {
            lock (this.Lock)
            {
                if (!this.IsDisposed)
                {
                    this.FlushLog();
                    this.IsDisposed = true;
                }
            }
        }
    }
}
