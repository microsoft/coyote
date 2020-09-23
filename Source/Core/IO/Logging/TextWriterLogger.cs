// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using System.Text;

namespace Microsoft.Coyote.IO
{
    /// <summary>
    /// Bridges custom user provided TextWriter logger so it can be passed into
    /// Coyote via the <see cref="ILogger"/> interface.
    /// </summary>
    internal class TextWriterLogger : TextWriter, ILogger
    {
        private readonly TextWriter UserLogger;

        /// <inheritdoc/>
        public TextWriter TextWriter => this.UserLogger;

        /// <summary>
        /// Initializes a new instance of the <see cref="TextWriterLogger"/> class.
        /// </summary>
        /// <param name="userLogger">The TextWriter to delegate to.</param>
        public TextWriterLogger(TextWriter userLogger)
        {
            this.UserLogger = userLogger;
        }

        /// <inheritdoc/>
        public override Encoding Encoding => this.UserLogger.Encoding;

        /// <inheritdoc/>
        public override void Write(string message)
        {
            this.UserLogger.Write(message);
        }

        /// <inheritdoc/>
        public override void Write(string format, object[] args)
        {
            this.UserLogger.Write(format, args);
        }

        /// <inheritdoc/>
        public void Write(LogSeverity severity, string value)
        {
            this.Write(value);
        }

        /// <inheritdoc/>
        public void Write(LogSeverity severity, string format, params object[] args)
        {
            this.Write(format, args);
        }

        /// <inheritdoc/>
        public override void WriteLine(string message)
        {
            this.UserLogger.WriteLine(message);
        }

        /// <inheritdoc/>
        public override void WriteLine(string format, object[] args)
        {
            this.UserLogger.WriteLine(format, args);
        }

        /// <inheritdoc/>
        public void WriteLine(LogSeverity severity, string value)
        {
            this.UserLogger.WriteLine(value);
        }

        /// <inheritdoc/>
        public void WriteLine(LogSeverity severity, string format, params object[] args)
        {
            this.WriteLine(format, args);
        }
    }
}
