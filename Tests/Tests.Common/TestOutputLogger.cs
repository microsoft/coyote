// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using System.Runtime.CompilerServices;
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
        /// Saved current line since ITestOutputHelper provides no "Write" method.
        /// </summary>
        private readonly StringBuilder Line = new StringBuilder();

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
                this.Line.Append(value);
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
                this.FlushLine();
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
            base.Dispose(disposing);
            this.FlushLine();
        }

        private void FlushLine()
        {
            if (this.Line.Length > 0)
            {
                this.TestOutput.WriteLine(this.Line.ToString());
                this.Line.Length = 0;
            }
        }
    }
}
