// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using System.Text;
using Microsoft.Coyote.IO;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Tests.Common
{
    /// <summary>
    /// Logger that writes to the test output.
    /// </summary>
    public sealed class TestOutputLogger : TextWriter, ILogger
    {
        /// <summary>
        /// Underlying test output.
        /// </summary>
        private readonly ITestOutputHelper TestOutput;

        /// <summary>
        /// Saves current line since ITestOutputHelper provides no "Write" method.
        /// </summary>
        private readonly StringBuilder CurrentLine = new StringBuilder();

        /// <summary>
        /// Serializes access to the string writer.
        /// </summary>
        private readonly object Lock;

        /// <summary>
        /// False means don't write anything.
        /// </summary>
        public bool IsVerbose { get; set; }

        /// <inheritdoc/>
        public TextWriter TextWriter => this;

        /// <inheritdoc/>
        public override Encoding Encoding => Encoding.Unicode;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestOutputLogger"/> class.
        /// </summary>
        /// <param name="output">The test output helper.</param>
        /// <param name="isVerbose">If true, then messages are logged. The default value is false.</param>
        public TestOutputLogger(ITestOutputHelper output, bool isVerbose = false)
        {
            this.TestOutput = output;
            this.Lock = new object();
            this.IsVerbose = isVerbose;
        }

        /// <inheritdoc/>
        public override void Write(string value)
        {
            this.Write(LogSeverity.Informational, value);
        }

        /// <inheritdoc/>
        public override void Write(string format, params object[] args)
        {
            this.Write(LogSeverity.Informational, string.Format(format, args));
        }

        public void Write(LogSeverity severity, string value)
        {
            if (this.IsVerbose)
            {
                lock (this.Lock)
                {
                    this.CurrentLine.Append(value);
                }
            }
        }

        /// <inheritdoc/>
        public void Write(LogSeverity severity, string format, params object[] args)
        {
            this.Write(severity, string.Format(format, args));
        }

        /// <inheritdoc/>
        public override void WriteLine(string value)
        {
            this.WriteLine(LogSeverity.Informational, value);
        }

        /// <inheritdoc/>
        public override void WriteLine(string format, params object[] args)
        {
            this.Write(LogSeverity.Informational, string.Format(format, args));
        }

        /// <inheritdoc/>
        public void WriteLine(LogSeverity severity, string value)
        {
            if (this.IsVerbose)
            {
                this.FlushCurrentLine();
                this.TestOutput.WriteLine(value);
            }
        }

        /// <inheritdoc/>
        public void WriteLine(LogSeverity severity, string format, params object[] args)
        {
            this.WriteLine(severity, string.Format(format, args));
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            this.FlushCurrentLine();
            base.Dispose(disposing);
        }

        private void FlushCurrentLine()
        {
            lock (this.Lock)
            {
                if (this.CurrentLine.Length > 0)
                {
                    this.TestOutput.WriteLine(this.CurrentLine.ToString());
                    this.CurrentLine.Length = 0;
                }
            }
        }
    }
}
